// Code authored by Dean Edis (DeanTheCoder).
// Anyone is free to copy, modify, use, compile, or distribute this software,
// either in source code form or as a compiled binary, for any
// purpose.
//
// If you modify the code, please retain this copyright header,
// and consider contributing back to the repository or letting us know
// about your modifications. Your contributions are valued!
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.

using DTC.SM83.HostDevices;

namespace DTC.SM83.Devices;

/// <summary>
/// Simplified Game Boy APU. Implements square channels (1 &amp; 2), wave (3) and noise (4)
/// and feeds mixed stereo output into the audio sink.
/// </summary>
public sealed class ApuDevice : IMemDevice
{
    public ushort FromAddr => 0xFF10;
    public ushort ToAddr => 0xFF3F;
    
    private const double SampleHz = 44100.0;
    private static readonly double TicksPerSample = Cpu.Hz / SampleHz;
    private const ulong FrameSequencerStepTStates = (ulong)(Cpu.Hz / 512.0); // 512 Hz.
    private double m_ticksUntilSample = TicksPerSample;
    private ulong m_frameSequencerTicks;
    private int m_frameSequencerStep;
    private readonly byte[] m_waveRam = new byte[16];
    private readonly SoundDevice m_audioSink;
    private readonly SquareChannel1 m_channel1 = new();
    private readonly SquareChannel m_channel2 = new();
    private readonly WaveChannel m_channel3;
    private readonly NoiseChannel m_channel4 = new();
    private readonly bool[] m_channelEnabled = [true, true, true, true];
    private bool m_isPowered = true;
    private byte m_nr10;
    private byte m_nr11;
    private byte m_nr12;
    private byte m_nr13;
    private byte m_nr14;
    private byte m_nr21;
    private byte m_nr22;
    private byte m_nr23;
    private byte m_nr24;
    private byte m_nr30;
    private byte m_nr31;
    private byte m_nr32;
    private byte m_nr33;
    private byte m_nr34;
    private byte m_nr41;
    private byte m_nr42;
    private byte m_nr43;
    private byte m_nr44;
    private byte m_nr50;
    private byte m_nr51;
    private byte m_nr52 = 0x80;

    public ApuDevice(SoundDevice audioSink)
    {
        m_audioSink = audioSink;
        m_channel3 = new WaveChannel(m_waveRam);
        ResetRegisters();
    }

    public void AdvanceT(ulong tStates)
    {
        ClockFrameSequencer(tStates);

        // Accumulate CPU time towards the next audio sample.
        m_ticksUntilSample -= tStates;
        while (m_ticksUntilSample <= 0.0)
        {
            // Generate one audio sample using a fixed tick delta per sample.
            GenerateAudioSample((ulong)TicksPerSample);
            m_ticksUntilSample += TicksPerSample;
        }
    }

    private void ClockFrameSequencer(ulong tStates)
    {
        m_frameSequencerTicks += tStates;
        var channelChanged = false;
        while (m_frameSequencerTicks >= FrameSequencerStepTStates)
        {
            m_frameSequencerTicks -= FrameSequencerStepTStates;
            channelChanged |= StepFrameSequencer();
            m_frameSequencerStep = (m_frameSequencerStep + 1) & 0x07;
        }

        if (channelChanged)
            UpdateStatusFlags();
    }

    private bool StepFrameSequencer()
    {
        var channelChanged = false;

        switch (m_frameSequencerStep)
        {
            case 0:
            case 4:
                channelChanged |= ClockLength();
                break;

            case 2:
            case 6:
                channelChanged |= ClockLength();
                channelChanged |= ClockSweep();
                break;

            case 7:
                channelChanged |= ClockEnvelope();
                break;
        }

        return channelChanged;
    }

    private bool ClockLength()
    {
        var changed = false;
        changed |= m_channel1.StepLength();
        changed |= m_channel2.StepLength();
        changed |= m_channel3.StepLength();
        changed |= m_channel4.StepLength();
        return changed;
    }

    private bool ClockSweep() =>
        m_channel1.StepSweep();

    private bool ClockEnvelope()
    {
        var changed = false;
        changed |= m_channel1.StepEnvelope();
        changed |= m_channel2.StepEnvelope();
        changed |= m_channel4.StepEnvelope();
        return changed;
    }

    /// <summary>
    /// Applies the DMG quirk where enabling the length timer mid-frame clocks it once immediately
    /// if the current frame sequencer step will not clock length on its own.
    /// </summary>
    /// <param name="wasEnabled">Whether length was enabled before the write.</param>
    /// <param name="isEnabled">Whether length is enabled after the write.</param>
    /// <param name="nextStepClocksLength">True if the current sequencer step already clocks length.</param>
    /// <param name="stepLength">Delegate that performs a single length clock.</param>
    private static void ApplyLengthEnableEdgeClock(bool wasEnabled, bool isEnabled, bool nextStepClocksLength, Func<bool> stepLength)
    {
        if (wasEnabled || !isEnabled)
            return;

        // When enabling length during a non-length sequencer step, clock the counter once immediately.
        if (!nextStepClocksLength)
            stepLength();
    }

    private static bool IsLengthClockStep(int frameStep) =>
        (frameStep & 1) == 0;

    private void GenerateAudioSample(ulong sampleTicks)
    {
        var ch1 = m_channel1.Sample(sampleTicks);
        var ch2 = m_channel2.Sample(sampleTicks);
        var ch3 = m_channel3.Sample(sampleTicks);
        var ch4 = m_channel4.Sample(sampleTicks);

        // Route channels according to NR51.
        var ch1L = (m_nr51 & (1 << 4)) != 0;
        var ch2L = (m_nr51 & (1 << 5)) != 0;
        var ch3L = (m_nr51 & (1 << 6)) != 0;
        var ch4L = (m_nr51 & (1 << 7)) != 0;

        var ch1R = (m_nr51 & (1 << 0)) != 0;
        var ch2R = (m_nr51 & (1 << 1)) != 0;
        var ch3R = (m_nr51 & (1 << 2)) != 0;
        var ch4R = (m_nr51 & (1 << 3)) != 0;

        // Only include channels that are enabled and routed.
        var left =
            (ch1L && IsChannelEnabled(0) ? ch1 : 0.0) +
            (ch2L && IsChannelEnabled(1) ? ch2 : 0.0) +
            (ch3L && IsChannelEnabled(2) ? ch3 : 0.0) +
            (ch4L && IsChannelEnabled(3) ? ch4 : 0.0);

        var right =
            (ch1R && IsChannelEnabled(0) ? ch1 : 0.0) +
            (ch2R && IsChannelEnabled(1) ? ch2 : 0.0) +
            (ch3R && IsChannelEnabled(2) ? ch3 : 0.0) +
            (ch4R && IsChannelEnabled(3) ? ch4 : 0.0);

        // NR50: left and right master volumes (0–7).
        var so1 = m_nr50 & 0x07;        // right
        var so2 = (m_nr50 >> 4) & 0x07; // left

        var leftVol = so2 / 7.0;
        var rightVol = so1 / 7.0;

        left *= leftVol;
        right *= rightVol;

        // Normalize for "up to 4 loud channels".
        left /= 4.0;
        right /= 4.0;

        // Clamp and send to the audio sink.
        left = Math.Clamp(left, -1.0, 1.0);
        right = Math.Clamp(right, -1.0, 1.0);

        m_audioSink?.AddSample(left, right);
    }

    public byte Read8(ushort addr)
    {
        if (addr is >= 0xFF30 and <= 0xFF3F)
            return m_waveRam[addr - 0xFF30];

        var raw = addr switch
        {
            0xFF10 => m_nr10,
            0xFF11 => m_nr11,
            0xFF12 => m_nr12,
            0xFF13 => m_nr13,
            0xFF14 => m_nr14,
            0xFF16 => m_nr21,
            0xFF17 => m_nr22,
            0xFF18 => m_nr23,
            0xFF19 => m_nr24,
            0xFF1A => m_nr30,
            0xFF1B => m_nr31,
            0xFF1C => m_nr32,
            0xFF1D => m_nr33,
            0xFF1E => m_nr34,
            0xFF20 => m_nr41,
            0xFF21 => m_nr42,
            0xFF22 => m_nr43,
            0xFF23 => m_nr44,
            0xFF24 => m_nr50,
            0xFF25 => m_nr51,
            0xFF26 => m_nr52,
            _ => 0xFF
        };
        return ApplyReadMask(addr, (byte)raw);
    }

    private static byte ApplyReadMask(ushort addr, byte value)
    {
        return addr switch
        {
            // NR1x
            0xFF10 => (byte)(value | 0x80),
            0xFF11 => (byte)(value | 0x3F),
            0xFF12 => (byte)(value | 0x00),
            0xFF13 => 0xFF,
            0xFF14 => (byte)(value | 0xBF),

            // NR2x
            0xFF16 => (byte)(value | 0x3F),
            0xFF17 => (byte)(value | 0x00),
            0xFF18 => (byte)(value | 0xFF),
            0xFF19 => (byte)(value | 0xBF),

            // NR3x
            0xFF1A => (byte)(value | 0x7F),
            0xFF1B => 0xFF,
            0xFF1C => (byte)(value | 0x9F),
            0xFF1D => 0xFF,
            0xFF1E => (byte)(value | 0xBF),

            // NR4x
            0xFF20 => 0xFF,
            0xFF21 => (byte)(value | 0x00),
            0xFF22 => (byte)(value | 0x00),
            0xFF23 => (byte)(value | 0xBF),

            // NR5x
            0xFF24 => (byte)(value | 0x00),
            0xFF25 => (byte)(value | 0x00),
            0xFF26 => (byte)(value | 0x70),

            _ => value
        };
    }
    
    public void Write8(ushort addr, byte value)
    {
        // Wave RAM is unaffected by APU power state.
        if (addr is >= 0xFF30 and <= 0xFF3F)
        {
            m_waveRam[addr - 0xFF30] = value;
            return;
        }

        // NR52 – sound on/off.
        if (addr == 0xFF26)
        {
            var powerOn = (value & 0x80) != 0;

            // Always store write to NR52 (masked), regardless of state.
            m_nr52 = (byte) (value & 0x80);

            if (powerOn == m_isPowered)
            {
                // State did not change, just refresh status bits from channels.
                UpdateStatusFlags();
                return;
            }

            m_isPowered = powerOn;

            if (!powerOn)
            {
                // Turning sound off:
                //  - Clear all sound registers (except NR52 bit 7, which is now 0).
                //  - Disable all channels.
                //  - Keep wave RAM as-is.
                ResetRegisters(); // Make sure this does NOT clear m_waveRam.
                m_nr52 = 0; // Bit 7 now off, lower bits already cleared.
            }
            else
            {
                // Turning sound on:
                //  - Registers stay at their cleared state.
                //  - Channels are disabled until triggered.
                //  - NR52 bit 7 set, lower bits will be updated by UpdateStatusFlags.
                m_nr52 = 0x80;
                m_frameSequencerTicks = 0;
                m_frameSequencerStep = 0;
                UpdateStatusFlags();
            }

            return;
        }

        // APU powered off: only certain writes still have effect.
        if (!m_isPowered)
        {
            switch (addr)
            {
                case 0xFF11: // NR11 (duty/length) – only length writable when off.
                    m_nr11 = (byte) ((m_nr11 & 0xC0) | (value & 0x3F));
                    m_channel1.SetLength(value);
                    break;

                case 0xFF16: // NR21 (duty/length) – only length writable when off.
                    m_nr21 = (byte) ((m_nr21 & 0xC0) | (value & 0x3F));
                    m_channel2.SetLength(value);
                    break;

                case 0xFF1B: // NR31 (length).
                    m_nr31 = value;
                    m_channel3.SetLength(value);
                    break;

                case 0xFF20: // NR41 (length).
                    m_nr41 = value;
                    m_channel4.SetLength(value);
                    break;
            }

            // All other sound writes ignored while powered off.
            return;
        }

        // Normal powered-on behaviour from here down.
        switch (addr)
        {
            case 0xFF10: // NR10 (sweep).
                m_nr10 = value;
                m_channel1.SetSweep(value);
                break;

            case 0xFF11: // NR11 (duty/length).
                m_nr11 = value;
                m_channel1.SetDuty((byte) ((value >> 6) & 0x03));
                m_channel1.SetLength(value);
                break;

            case 0xFF12: // NR12 (envelope).
                m_nr12 = value;
                m_channel1.SetEnvelope(value);
                break;

            case 0xFF13: // NR13 (freq low).
                m_nr13 = value;
                m_channel1.SetFrequency(CombineFrequency(m_nr13, m_nr14));
                break;

            case 0xFF14: // NR14 (trigger/freq high).
            {
                var nextStepClocksLength = IsLengthClockStep(m_frameSequencerStep);
                var wasLengthEnabled = (m_nr14 & 0x40) != 0;
                m_nr14 = value;
                m_channel1.SetFrequency(CombineFrequency(m_nr13, m_nr14));
                m_channel1.SetLengthEnable((value & 0x40) != 0);
                ApplyLengthEnableEdgeClock(wasLengthEnabled, (value & 0x40) != 0, nextStepClocksLength, () => m_channel1.StepLength(true));
                if ((value & 0x80) != 0)
                    m_channel1.Trigger(nextStepClocksLength);
                break;
            }

            case 0xFF16: // NR21 (duty/length).
                m_nr21 = value;
                m_channel2.SetDuty((byte) ((value >> 6) & 0x03));
                m_channel2.SetLength(value);
                break;

            case 0xFF17: // NR22 (envelope).
                m_nr22 = value;
                m_channel2.SetEnvelope(value);
                break;

            case 0xFF18: // NR23 (freq low).
                m_nr23 = value;
                m_channel2.SetFrequency(CombineFrequency(m_nr23, m_nr24));
                break;

            case 0xFF19: // NR24 (trigger/freq high).
            {
                var nextStepClocksLength = IsLengthClockStep(m_frameSequencerStep);
                var wasLengthEnabled = (m_nr24 & 0x40) != 0;
                m_nr24 = value;
                m_channel2.SetFrequency(CombineFrequency(m_nr23, m_nr24));
                m_channel2.SetLengthEnable((value & 0x40) != 0);
                ApplyLengthEnableEdgeClock(wasLengthEnabled, (value & 0x40) != 0, nextStepClocksLength, () => m_channel2.StepLength(true));
                if ((value & 0x80) != 0)
                    m_channel2.Trigger(nextStepClocksLength);
                break;
            }

            case 0xFF1A: // NR30 (DAC power).
                m_nr30 = value;
                m_channel3.SetDacEnabled((value & 0x80) != 0);
                break;

            case 0xFF1B: // NR31 (length).
                m_nr31 = value;
                m_channel3.SetLength(value);
                break;

            case 0xFF1C: // NR32 (volume).
                m_nr32 = value;
                m_channel3.SetVolume((byte) ((value >> 5) & 0x03));
                break;

            case 0xFF1D: // NR33 (freq low).
                m_nr33 = value;
                m_channel3.SetFrequency(CombineFrequency(m_nr33, m_nr34));
                break;

            case 0xFF1E: // NR34 (trigger/freq high).
            {
                var nextStepClocksLength = IsLengthClockStep(m_frameSequencerStep);
                var wasLengthEnabled = (m_nr34 & 0x40) != 0;
                m_nr34 = value;
                m_channel3.SetFrequency(CombineFrequency(m_nr33, m_nr34));
                m_channel3.SetLengthEnable((value & 0x40) != 0);
                ApplyLengthEnableEdgeClock(wasLengthEnabled, (value & 0x40) != 0, nextStepClocksLength, () => m_channel3.StepLength(true));
                if ((value & 0x80) != 0)
                    m_channel3.Trigger(nextStepClocksLength);
                break;
            }

            case 0xFF20: // NR41 (length).
                m_nr41 = value;
                m_channel4.SetLength(value);
                break;

            case 0xFF21: // NR42 (envelope).
                m_nr42 = value;
                m_channel4.SetEnvelope(value);
                break;

            case 0xFF22: // NR43 (polynomial counter).
                m_nr43 = value;
                m_channel4.SetPolynomial(value);
                break;

            case 0xFF23: // NR44 (trigger/length enable).
            {
                var nextStepClocksLength = IsLengthClockStep(m_frameSequencerStep);
                var wasLengthEnabled = (m_nr44 & 0x40) != 0;
                m_nr44 = value;
                m_channel4.SetLengthEnable((value & 0x40) != 0);
                ApplyLengthEnableEdgeClock(wasLengthEnabled, (value & 0x40) != 0, nextStepClocksLength, () => m_channel4.StepLength(true));
                if ((value & 0x80) != 0)
                    m_channel4.Trigger(nextStepClocksLength);
                break;
            }

            case 0xFF24: // NR50 (master volume).
                m_nr50 = value;
                break;

            case 0xFF25: // NR51 (panning).
                m_nr51 = value;
                break;
        }

        UpdateStatusFlags();
    }

    public void SetChannelEnabled(int channel, bool isEnabled)
    {
        if (channel is < 1 or > 4)
            return;

        m_channelEnabled[channel - 1] = isEnabled;
    }

    private void UpdateStatusFlags()
    {
        var active =
            (m_channel1.Enabled ? 0x01 : 0x00) |
            (m_channel2.Enabled ? 0x02 : 0x00) |
            (m_channel3.Enabled ? 0x04 : 0x00) |
            (m_channel4.Enabled ? 0x08 : 0x00);
        m_nr52 = (byte)(0x70 | (m_isPowered ? 0x80 : 0x00) | active);
    }

    private void ResetRegisters()
    {
        m_nr10 = m_nr11 = m_nr12 = m_nr13 = m_nr14 = 0;
        m_nr21 = m_nr22 = m_nr23 = m_nr24 = 0;
        m_nr30 = m_nr31 = m_nr32 = m_nr33 = m_nr34 = 0;
        m_nr41 = m_nr42 = m_nr43 = m_nr44 = 0;
        m_nr50 = m_nr51 = 0;

        // Sync channel DAC/envelope state with cleared registers.
        m_channel1.SetEnvelope(m_nr12);
        m_channel2.SetEnvelope(m_nr22);
        m_channel4.SetEnvelope(m_nr42);

        m_channel1.Disable();
        m_channel2.Disable();
        m_channel3.SetDacEnabled(false);
        m_channel3.Disable();
        m_channel4.Disable();
        m_frameSequencerTicks = 0;
        m_frameSequencerStep = 0;
        UpdateStatusFlags();
    }

    private static ushort CombineFrequency(byte low, byte high) =>
        (ushort)(low | ((high & 0x07) << 8));
    
    private bool IsChannelEnabled(int channel) =>
        m_channelEnabled[channel];
    
    private class SquareChannel
    {
        private static readonly double[] DutyCycle = { 0.125, 0.25, 0.5, 0.75 };
        protected ushort m_frequency;
        protected double m_frequencyHz;
        protected double m_phase;
        protected byte m_volume;
        protected byte m_initialVolume;
        protected byte m_envelopePeriod;
        protected byte m_envelopeTimer;
        protected bool m_envelopeIncrease;
        protected bool m_dacEnabled;
        protected bool m_lengthEnabled;
        protected byte m_lengthCounter;
        private byte m_dutyIndex;

        public bool Enabled { get; protected set; }

        public void SetDuty(byte dutyIndex) =>
            m_dutyIndex = (byte)Math.Min(DutyCycle.Length - 1, dutyIndex);

        public void SetLength(byte lengthValue)
        {
            // Length is 64 - t1 bits.
            var len = (byte)(64 - (lengthValue & 0x3F));
            m_lengthCounter = len == 0 ? (byte)64 : len;
        }

        public void SetLengthEnable(bool enabled) =>
            m_lengthEnabled = enabled;

        public void SetEnvelope(byte nrx2)
        {
            m_initialVolume = (byte)((nrx2 >> 4) & 0x0F);
            m_envelopeIncrease = (nrx2 & 0x08) != 0;
            m_envelopePeriod = (byte)(nrx2 & 0x07);
            m_dacEnabled = (nrx2 & 0xF8) != 0;
            if (!m_dacEnabled)
                Disable();
        }

        public void SetFrequency(ushort frequency)
        {
            m_frequency = (ushort)(frequency & 0x7FF);
            UpdateFrequencyHz();
        }

        protected void UpdateFrequencyHz() =>
            m_frequencyHz = m_frequency >= 2048 ? 0.0 : 131072.0 / (2048 - m_frequency);

        public virtual void Trigger(bool nextStepClocksLength)
        {
            if (!m_dacEnabled)
            {
                Disable();
                return;
            }

            Enabled = m_frequencyHz > 0.0;
            if (m_lengthCounter == 0)
            {
                m_lengthCounter = 64;
                if (m_lengthEnabled && !nextStepClocksLength)
                    m_lengthCounter--;
            }
            m_volume = m_initialVolume;
            m_phase = 0.0;
            m_envelopeTimer = (byte)(m_envelopePeriod == 0 ? 8 : m_envelopePeriod);
        }

        public void Disable()
        {
            Enabled = false;
            m_phase = 0.0;
            m_volume = 0;
        }

        public bool StepLength(bool forceClock = false)
        {
            if ((!m_lengthEnabled && !forceClock) || m_lengthCounter == 0)
                return false;

            if (m_lengthCounter > 0)
                m_lengthCounter--;

            if (m_lengthCounter == 0 && Enabled)
            {
                Disable();
                return true;
            }

            return false;
        }

        public bool StepEnvelope()
        {
            if (!Enabled || !m_dacEnabled)
                return false;

            // Period of 0 means no automatic envelope updates.
            if (m_envelopePeriod == 0)
                return false;

            if (m_envelopeTimer > 0)
                m_envelopeTimer--;

            if (m_envelopeTimer > 0)
                return false;

            m_envelopeTimer = m_envelopePeriod;

            if (m_envelopeIncrease && m_volume < 15)
                m_volume++;
            else if (!m_envelopeIncrease && m_volume > 0)
                m_volume--;

            return false;
        }

        public double Sample(ulong tStates)
        {
            if (!Enabled || !m_dacEnabled || m_volume == 0 || m_frequencyHz <= 0.0)
                return 0.0;

            var elapsedSeconds = tStates / Cpu.Hz;
            m_phase += m_frequencyHz * elapsedSeconds;
            m_phase -= Math.Floor(m_phase);

            var duty = DutyCycle[m_dutyIndex];
            var high = m_phase < duty;
            return high ? m_volume / 15.0 : 0.0;
        }
    }

    private sealed class SquareChannel1 : SquareChannel
    {
        private byte m_sweepPeriod;
        private bool m_sweepNegate;
        private byte m_sweepShift;
        private byte m_sweepTimer;
        private ushort m_sweepShadowFreq;
        private bool m_sweepEnabled;

        public void SetSweep(byte nr10)
        {
            m_sweepPeriod = (byte)((nr10 >> 4) & 0x07);
            m_sweepNegate = (nr10 & 0x08) != 0;
            m_sweepShift = (byte)(nr10 & 0x07);
        }

        public override void Trigger(bool nextStepClocksLength)
        {
            base.Trigger(nextStepClocksLength);

            m_sweepShadowFreq = m_frequency;
            m_sweepTimer = (byte)(m_sweepPeriod == 0 ? 8 : m_sweepPeriod);
            m_sweepEnabled = m_sweepPeriod != 0 || m_sweepShift != 0;

            if (m_sweepShift != 0)
            {
                var target = CalculateSweepTarget();
                if (target > 2047 || target < 0)
                {
                    Disable();
                    m_sweepEnabled = false;
                }
            }
        }

        public bool StepSweep()
        {
            if (!Enabled)
                return false;

            if (!m_sweepEnabled)
                return false;

            if (m_sweepTimer > 0)
                m_sweepTimer--;

            if (m_sweepTimer > 0)
                return false;

            m_sweepTimer = (byte)(m_sweepPeriod == 0 ? 8 : m_sweepPeriod);

            var target = CalculateSweepTarget();
            if (target > 2047 || target < 0)
            {
                Disable();
                return true;
            }

            if (m_sweepShift != 0)
            {
                m_sweepShadowFreq = (ushort)target;
                SetFrequency((ushort)target);

                // Second overflow check.
                var nextTarget = CalculateSweepTarget();
                if (nextTarget > 2047 || nextTarget < 0)
                {
                    Disable();
                    return true;
                }
            }

            return false;
        }

        private int CalculateSweepTarget()
        {
            var delta = m_sweepShadowFreq >> m_sweepShift;
            return m_sweepNegate
                ? m_sweepShadowFreq - delta
                : m_sweepShadowFreq + delta;
        }
    }

    private sealed class WaveChannel
    {
        private readonly byte[] m_waveRam;
        private ushort m_frequency;
        private double m_frequencyHz;
        private double m_phase;
        private byte m_volumeCode;
        private bool m_dacEnabled = true;
        private ushort m_lengthCounter;
        private bool m_lengthEnabled;

        public bool Enabled { get; private set; }

        public WaveChannel(byte[] waveRam) =>
            m_waveRam = waveRam ?? throw new ArgumentNullException(nameof(waveRam));

        public void SetDacEnabled(bool enabled)
        {
            m_dacEnabled = enabled;
            if (!m_dacEnabled)
                Disable();
        }

        public void SetLength(byte lengthValue)
        {
            // Length is 256 - t1 bits.
            var len = (ushort)(256 - lengthValue);
            m_lengthCounter = len == 0 ? (ushort)256 : len;
        }

        public void SetLengthEnable(bool enabled) =>
            m_lengthEnabled = enabled;

        public void SetVolume(byte volumeCode) =>
            m_volumeCode = Math.Min((byte)3, volumeCode);

        public void SetFrequency(ushort frequency)
        {
            m_frequency = (ushort)(frequency & 0x7FF);
            m_frequencyHz = m_frequency >= 2048 ? 0.0 : 65536.0 / (2048 - m_frequency);
        }

        public void Trigger(bool nextStepClocksLength)
        {
            if (!m_dacEnabled)
            {
                Disable();
                return;
            }

            Enabled = m_frequencyHz > 0.0;
            if (!Enabled)
                return;

            if (m_lengthCounter == 0)
            {
                m_lengthCounter = 256;
                if (m_lengthEnabled && !nextStepClocksLength)
                    m_lengthCounter--;
            }
            m_phase = 0.0;
        }

        public void Disable()
        {
            Enabled = false;
            m_phase = 0.0;
        }

        public bool StepLength(bool forceClock = false)
        {
            if ((!m_lengthEnabled && !forceClock) || m_lengthCounter == 0)
                return false;

            if (m_lengthCounter > 0)
                m_lengthCounter--;

            if (m_lengthCounter == 0 && Enabled)
            {
                Disable();
                return true;
            }

            return false;
        }

        public double Sample(ulong tStates)
        {
            if (!Enabled || !m_dacEnabled || m_volumeCode == 0 || m_frequencyHz <= 0.0)
                return 0.0;

            var elapsedSeconds = tStates / Cpu.Hz;
            m_phase += m_frequencyHz * elapsedSeconds;
            m_phase -= Math.Floor(m_phase);

            var sampleIndex = (int)(m_phase * 32.0);
            sampleIndex = Math.Clamp(sampleIndex, 0, 31);
            var waveByte = m_waveRam[sampleIndex >> 1];
            var sample4 = (sampleIndex & 1) == 0 ? waveByte >> 4 : waveByte & 0x0F;
            if (sample4 == 0)
                return 0.0;

            var volumeFactor = m_volumeCode switch
            {
                1 => 1.0,
                2 => 0.5,
                3 => 0.25,
                _ => 0.0
            };
            return (sample4 / 15.0) * volumeFactor;
        }
    }

    private sealed class NoiseChannel
    {
        private ushort m_lfsr = 0x7FFF;
        private double m_lfsrFrequencyHz;
        private double m_lfsrTimerSeconds;

        private byte m_volume;
        private byte m_initialVolume;
        private byte m_envelopePeriod;
        private byte m_envelopeTimer;
        private bool m_envelopeIncrease;
        private bool m_dacEnabled = true;

        private byte m_lengthCounter;
        private bool m_lengthEnabled;

        public bool Enabled { get; private set; }

        public void SetLength(byte lengthValue)
        {
            var len = (byte)(64 - (lengthValue & 0x3F));
            m_lengthCounter = len == 0 ? (byte)64 : len;
        }

        public void SetLengthEnable(bool enabled) =>
            m_lengthEnabled = enabled;

        public void SetEnvelope(byte nrx2)
        {
            m_initialVolume = (byte)((nrx2 >> 4) & 0x0F);
            m_envelopeIncrease = (nrx2 & 0x08) != 0;
            m_envelopePeriod = (byte)(nrx2 & 0x07);
            m_dacEnabled = (nrx2 & 0xF8) != 0;
            if (!m_dacEnabled)
                Disable();
        }

        public void SetPolynomial(byte nr43)
        {
            var divisorCode = nr43 & 0x07;
            var divisor = divisorCode switch
            {
                0 => 8,
                1 => 16,
                2 => 32,
                3 => 48,
                4 => 64,
                5 => 80,
                6 => 96,
                _ => 112
            };

            var shiftClock = (nr43 >> 4) & 0x0F;
            var baseClock = 524288.0; // 2^19 Hz.
            m_lfsrFrequencyHz = baseClock / divisor / Math.Pow(2.0, shiftClock + 1);

            var widthMode7Bit = (nr43 & 0x08) != 0;
            m_useShortMode = widthMode7Bit;
        }

        private bool m_useShortMode;

        public void Trigger(bool nextStepClocksLength)
        {
            if (!m_dacEnabled)
            {
                Disable();
                return;
            }

            Enabled = m_lfsrFrequencyHz > 0.0;
            m_volume = m_initialVolume;
            if (m_lengthCounter == 0)
            {
                m_lengthCounter = 64;
                if (m_lengthEnabled && !nextStepClocksLength)
                    m_lengthCounter--;
            }
            m_envelopeTimer = (byte)(m_envelopePeriod == 0 ? 8 : m_envelopePeriod);
            m_lfsr = 0x7FFF;
            m_lfsrTimerSeconds = 0.0;
        }

        public void Disable()
        {
            Enabled = false;
            m_volume = 0;
        }

        public bool StepLength(bool forceClock = false)
        {
            if ((!m_lengthEnabled && !forceClock) || m_lengthCounter == 0)
                return false;

            if (m_lengthCounter > 0)
                m_lengthCounter--;

            if (m_lengthCounter == 0 && Enabled)
            {
                Disable();
                return true;
            }

            return false;
        }

        public bool StepEnvelope()
        {
            if (!Enabled || !m_dacEnabled)
                return false;

            // Period of 0 means no automatic envelope updates.
            if (m_envelopePeriod == 0)
                return false;

            if (m_envelopeTimer > 0)
                m_envelopeTimer--;

            if (m_envelopeTimer > 0)
                return false;

            m_envelopeTimer = m_envelopePeriod;

            if (m_envelopeIncrease && m_volume < 15)
                m_volume++;
            else if (!m_envelopeIncrease && m_volume > 0)
                m_volume--;

            return false;
        }

        public double Sample(ulong tStates)
        {
            if (!Enabled || !m_dacEnabled || m_volume == 0 || m_lfsrFrequencyHz <= 0.0)
                return 0.0;

            var elapsedSeconds = tStates / Cpu.Hz;
            m_lfsrTimerSeconds += elapsedSeconds;

            var period = 1.0 / m_lfsrFrequencyHz;
            while (m_lfsrTimerSeconds >= period)
            {
                m_lfsrTimerSeconds -= period;
                StepLfsr();
            }

            var outputBit = (~m_lfsr) & 1;
            return outputBit != 0 ? m_volume / 15.0 : 0.0;
        }

        private void StepLfsr()
        {
            var xorBit = (m_lfsr & 1) ^ ((m_lfsr >> 1) & 1);
            m_lfsr >>= 1;
            m_lfsr |= (ushort)(xorBit << 14);

            if (m_useShortMode)
            {
                m_lfsr &= 0xFFBF;
                m_lfsr |= (ushort)(xorBit << 6);
            }
        }
    }
}
