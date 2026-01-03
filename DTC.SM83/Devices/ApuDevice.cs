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
    private const double SampleSeconds = 1.0 / SampleHz;
    private const double TicksPerSample = Cpu.Hz / SampleHz;
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

    public bool SuppressTriggers { get; set; }
    public InstructionLogger InstructionLogger { get; set; }

    public ApuDevice(SoundDevice audioSink)
    {
        m_audioSink = audioSink;

        // DMG wave RAM powers up with non-zero, semi-random contents (varies by unit).
        // Some commercial games rely on it being non-zero if they forget to initialize it.
        // See https://gbdev.gg8.se/wiki/articles/Gameboy_sound_hardware#Power_Control
        InitializeWaveRamPowerOnPattern();

        m_channel3 = new WaveChannel(m_waveRam);
        m_channel1.OnFrequencyWritten = UpdateChannel1FrequencyRegisters;
        ResetRegisters();
    }

    private void InitializeWaveRamPowerOnPattern()
    {
        // Deterministic "DMG-ish" pattern. Real DMG units power up with a stable-but-unique per-unit pattern,
        // and some docs cite the pattern used by R-Type DX to approximate DMG power-on wave RAM.
        // Using a non-zero default improves compatibility with titles that assume wave RAM is not all zeros.
        var pattern = new byte[]
        {
            0xAC, 0xDD, 0xDA, 0x48, 0x36, 0x02, 0xCF, 0x16,
            0x2C, 0x04, 0xE5, 0x2C, 0xAC, 0xDD, 0xDA, 0x48
        };

        Array.Copy(pattern, m_waveRam, m_waveRam.Length);
    }

    public void AdvanceT(ulong tStates)
    {
        ClockFrameSequencer(tStates);
        m_channel3.AdvanceT(tStates);

        // Accumulate CPU time towards the next audio sample.
        m_ticksUntilSample -= tStates;
        while (m_ticksUntilSample <= 0.0)
        {
            // Generate one audio sample using a fixed tick delta per sample.
            GenerateAudioSample(SampleSeconds);
            m_ticksUntilSample += TicksPerSample;
        }
    }

    private void ClockFrameSequencer(ulong tStates)
    {
        if (!m_isPowered)
            return;

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

    private void GenerateAudioSample(double sampleSeconds)
    {
        var ch1 = m_channel1.Sample(sampleSeconds);
        var ch2 = m_channel2.Sample(sampleSeconds);
        var ch3 = m_channel3.Sample();
        var ch4 = m_channel4.Sample(sampleSeconds);

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

        // Normalize by max possible channels (4).
        left /= 4.0;
        right /= 4.0;

        // Apply NR50 master volume (0-7, each adds +1 to the multiplier)
        var leftVolRaw = (m_nr50 >> 4) & 0x07;
        var rightVolRaw = m_nr50 & 0x07;
        if (leftVolRaw == 0)
            left = 0.0;
        else
            left *= (leftVolRaw + 1) / 8.0;
        if (rightVolRaw == 0)
            right = 0.0;
        else
            right *= (rightVolRaw + 1) / 8.0;

        m_audioSink?.AddSample(left, right);
    }

    public byte Read8(ushort addr)
    {
        if (addr is >= 0xFF30 and <= 0xFF3F)
            return m_channel3.ReadWaveRam(addr);

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
        // Wave RAM is unaffected by APU power state and should also not be blocked by SuppressTriggers.
        if (addr is >= 0xFF30 and <= 0xFF3F)
        {
            m_channel3.WriteWaveRam(addr, value);
            return;
        }

        if (SuppressTriggers)
            return;

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
            InstructionLogger?.Write(() => powerOn ? "APU power on" : "APU power off");

            if (!powerOn)
            {
                // Turning sound off:
                //  - Clear all sound registers (except NR52 bit 7, which is now 0).
                //  - Disable all channels.
                //  - Keep wave RAM as-is.
                ResetRegisters(resetLengthCounters: false); // Preserve length counters (DMG quirk).
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
                m_channel3.ResetState(resetLengthCounter: false);
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
                {
                    InstructionLogger?.Write(() => $"APU CH1 trigger freq={CombineFrequency(m_nr13, m_nr14):X3}");
                    m_channel1.Trigger(nextStepClocksLength);
                }
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
                {
                    InstructionLogger?.Write(() => $"APU CH2 trigger freq={CombineFrequency(m_nr23, m_nr24):X3}");
                    m_channel2.Trigger(nextStepClocksLength);
                }
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
                {
                    m_channel3.ApplyDmgTriggerCorruptionIfReading();
                    InstructionLogger?.Write(() => $"APU CH3 trigger freq={CombineFrequency(m_nr33, m_nr34):X3}");
                    m_channel3.Trigger(nextStepClocksLength);
                }
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
                {
                    InstructionLogger?.Write(() => $"APU CH4 trigger poly={m_nr43:X2}");
                    m_channel4.Trigger(nextStepClocksLength);
                }
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

    private void ResetRegisters(bool resetLengthCounters = true)
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
        m_channel1.SetLengthEnable(false);
        m_channel2.SetLengthEnable(false);
        m_channel3.SetLengthEnable(false);
        m_channel4.SetLengthEnable(false);

        m_channel1.Disable();
        m_channel2.Disable();
        m_channel3.SetDacEnabled(false);
        m_channel3.ResetState(resetLengthCounters);
        m_channel4.Disable();
        m_frameSequencerTicks = 0;
        m_frameSequencerStep = 0;
        UpdateStatusFlags();
    }

    private static ushort CombineFrequency(byte low, byte high) =>
        (ushort)(low | ((high & 0x07) << 8));

    private void UpdateChannel1FrequencyRegisters(ushort frequency)
    {
        m_nr13 = (byte)(frequency & 0xFF);
        m_nr14 = (byte)((m_nr14 & 0xF8) | ((frequency >> 8) & 0x07));
    }
    
    private bool IsChannelEnabled(int channel) =>
        m_channelEnabled[channel];
    
    private class SquareChannel
    {
        private static readonly double[] DutyCycle = [0.125, 0.25, 0.5, 0.75];
        protected ushort m_frequency;
        private double m_frequencyHz;
        private double m_phase;
        private byte m_volume;
        private byte m_initialVolume;
        private byte m_envelopePeriod;
        private byte m_envelopeTimer;
        private bool m_envelopeIncrease;
        private bool m_dacEnabled;
        private bool m_lengthEnabled;
        private byte m_lengthCounter;
        private byte m_dutyIndex;

        public bool Enabled { get; private set; }

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

        private void UpdateFrequencyHz() =>
            m_frequencyHz = m_frequency >= 2048 ? 0.0 : 131072.0 / (2048 - m_frequency);

        public virtual void Trigger(bool nextStepClocksLength)
        {
            if (m_lengthCounter == 0)
            {
                m_lengthCounter = 64;
                if (m_lengthEnabled && !nextStepClocksLength)
                    m_lengthCounter--;
            }
            m_volume = m_initialVolume;
            m_phase = 0.0;
            m_envelopeTimer = (byte)(m_envelopePeriod == 0 ? 8 : m_envelopePeriod);
            Enabled = m_dacEnabled && m_frequencyHz > 0.0;
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

        public double Sample(double elapsedSeconds)
        {
            if (!Enabled || !m_dacEnabled || m_volume == 0 || m_frequencyHz <= 0.0)
                return 0.0;

            // Frequencies above Nyquist alias badly at the host sample rate; approximate them by
            // their DC average so ultrasonic PCM writes don't produce an audible whine.
            const double nyquist = SampleHz * 0.5;
            var duty = DutyCycle[m_dutyIndex];
            if (m_frequencyHz >= nyquist)
                return duty * (m_volume / 15.0);

            m_phase += m_frequencyHz * elapsedSeconds;
            m_phase -= Math.Floor(m_phase);

            var high = m_phase < duty;
            var amp = m_volume / 15.0;
            return high ? amp : -amp;
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
        private bool m_sweepWasEnabledOnTrigger;
        private bool m_sweepPerformedNegate;
        
        public Action<ushort> OnFrequencyWritten { get; set; }

        public void SetSweep(byte nr10)
        {
            // Store old settings.
            var oldNegate = m_sweepNegate;
            var wasEnabled = m_sweepEnabled;

            // Decode NR10.
            m_sweepPeriod = (byte)((nr10 >> 4) & 0x07);
            m_sweepNegate = (nr10 & 0x08) != 0;
            m_sweepShift = (byte)(nr10 & 0x07);

            // Hardware "sweep enabled" definition: period != 0 or shift != 0.
            // (Independent of the negate bit.)
            var sweepDefined = m_sweepPeriod != 0 || m_sweepShift != 0;
            
            // Sweep can only run if it was active when the channel was last triggered.
            m_sweepEnabled = m_sweepWasEnabledOnTrigger && sweepDefined;

            // DMG quirk:
            // If a negative sweep calculation has occurred, and NR10 is later written
            // with the negate bit cleared while sweep is enabled (with the *new* NR10),
            // the channel is immediately disabled. The quirk still applies even if the new
            // NR10 would disable sweep (period=0 and shift=0).
            if (oldNegate && !m_sweepNegate && m_sweepPerformedNegate)
            {
                Disable();
                m_sweepEnabled = false;
            }

            // If sweep becomes enabled via NR10 (after being disabled), reset the timer using the new period.
            if (!wasEnabled && m_sweepEnabled)
            {
                // Start a fresh sweep timer so the first calculation occurs promptly after enabling sweep via NR10.
                m_sweepTimer = 0;
            }
        }
        
        public override void Trigger(bool nextStepClocksLength)
        {
            base.Trigger(nextStepClocksLength);

            m_sweepShadowFreq = m_frequency;
            m_sweepTimer = (byte)(m_sweepPeriod == 0 ? 8 : m_sweepPeriod);
            m_sweepWasEnabledOnTrigger = m_sweepPeriod != 0 || m_sweepShift != 0;
            m_sweepEnabled = m_sweepWasEnabledOnTrigger;

            // New trigger starts a fresh sweep sequence; no negate calculation has occurred yet.
            m_sweepPerformedNegate = false;

            if (!m_sweepEnabled)
                return;

            if (m_sweepShift != 0)
            {
                // Initial pre-calculation for overflow check; does not count as a "performed"
                // negate calculation for the NR10 negate->clear quirk.
                var target = CalculateSweepTarget();

                // Only addition mode can overflow and disable the channel; a decreasing sweep cannot underflow.
                if (!m_sweepNegate && target > 2047)
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

            if (m_sweepTimer > 0)
                m_sweepTimer--;
            if (m_sweepTimer > 0)
                return false;

            // Reload the sweep timer (period 0 treated as 8 for timing).
            m_sweepTimer = (byte)(m_sweepPeriod == 0 ? 8 : m_sweepPeriod);

            if (!m_sweepEnabled)
                return false;

            // If sweep period is 0, do not perform periodic sweep (see Pan Docs/gbdev).
            if (m_sweepPeriod == 0)
                return false;

            var target = CalculateSweepTarget();

            // Only addition mode can overflow and disable the channel; a decreasing sweep cannot underflow.
            if (!m_sweepNegate && target > 2047)
            {
                Disable();
                m_sweepEnabled = false;
                return true;
            }

            if (m_sweepShift != 0)
            {
                m_sweepShadowFreq = (ushort)target;
                SetFrequency((ushort)target);
                OnFrequencyWritten?.Invoke((ushort)target);

                var nextTarget = CalculateSweepTarget();
                if (!m_sweepNegate && nextTarget > 2047)
                {
                    Disable();
                    m_sweepEnabled = false;
                    return true;
                }
            }

            return false;
        }

        private int CalculateSweepTarget()
        {
            var delta = m_sweepShadowFreq >> m_sweepShift;

            if (m_sweepNegate)
            {
                // Any negate sweep calculation (trigger pre-check or periodic sweep step)
                // counts as having "performed" a negate calculation for the NR10
                // negate->clear quirk.
                m_sweepPerformedNegate = true;

                // Subtract mode uses 11-bit two's complement arithmetic; wrap within 0x000-0x7FF.
                var raw = m_sweepShadowFreq - delta;
                var result = raw & 0x7FF;

                return result;
            }

            // Addition mode: let caller perform overflow check on the raw value.
            var target = m_sweepShadowFreq + delta;

            return target;
        }
    }

    private sealed class WaveChannel
    {
        private const int FirstSampleDelayTicks = 6;
        private readonly byte[] m_waveRam;
        private ushort m_pendingFrequency;
        private int m_timerPeriod;
        private int m_pendingTimerPeriod;
        private int m_sampleIndex;
        private byte m_sampleBuffer;
        private byte m_volumeCode;
        private bool m_dacEnabled = true;
        private ushort m_lengthCounter;
        private bool m_lengthEnabled;
        private bool m_frequencyChangePending;
        private ulong m_ticks;
        private ulong m_nextSampleTick;
        private ulong m_lastWaveReadTick;
        public bool Enabled { get; private set; }

        public WaveChannel(byte[] waveRam) =>
            m_waveRam = waveRam ?? throw new ArgumentNullException(nameof(waveRam));

        /// <summary>
        /// Advance the wave timer using CPU T-states so reads align with CPU accesses.
        /// </summary>
        public void AdvanceT(ulong tStates)
        {
            if (tStates == 0)
                return;

            var targetTick = m_ticks + tStates;

            if (Enabled && m_timerPeriod > 0 && m_nextSampleTick != 0)
            {
                while (m_nextSampleTick <= targetTick)
                {
                    ClockSample(m_nextSampleTick);
                    if (!Enabled || m_timerPeriod <= 0)
                        break;

                    m_nextSampleTick += (ulong)m_timerPeriod;
                }
            }

            m_ticks = targetTick;
        }

        /// <summary>
        /// Wave RAM is only readable while CH3 is actively fetching a byte; otherwise returns 0xFF.
        /// </summary>
        public byte ReadWaveRam(ushort addr)
        {
            var index = (addr - 0xFF30) & 0x0F;

            if (!Enabled)
                return m_waveRam[index];

            return IsWaveRamAccessible()
                ? m_waveRam[m_sampleIndex >> 1]
                : (byte)0xFF;
        }

        /// <summary>
        /// Writes are ignored while CH3 is active unless they occur on the fetch cycle.
        /// </summary>
        public void WriteWaveRam(ushort addr, byte value)
        {
            var index = (addr - 0xFF30) & 0x0F;

            if (Enabled && !IsWaveRamAccessible())
                return;

            var targetIndex = Enabled ? m_sampleIndex >> 1 : index;
            m_waveRam[targetIndex] = value;
        }

        private bool IsWaveRamAccessible() =>
            Enabled && m_lastWaveReadTick == m_ticks;

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
            m_pendingFrequency = (ushort)(frequency & 0x7FF);
            m_pendingTimerPeriod = CalculateTimerPeriod(m_pendingFrequency);

            if (!Enabled)
                ApplyPendingFrequency();
            else
                m_frequencyChangePending = true;
        }

        /// <summary>
        /// DMG quirk: retriggering CH3 while it is fetching a sample byte corrupts the first bytes of wave RAM.
        /// </summary>
        public void ApplyDmgTriggerCorruptionIfReading()
        {
            if (!Enabled || m_nextSampleTick == 0 || m_timerPeriod <= 0)
                return;

            // Treat a sample fetch occurring within the current 4T CPU machine cycle as overlapping the trigger.
            var ticksUntilRead = m_nextSampleTick > m_ticks ? m_nextSampleTick - m_ticks : 0;
            if (ticksUntilRead > 3)
                return;

            // The byte being fetched is the next one the channel will step to.
            var byteIndex = ((m_sampleIndex + 1) & 0x1F) >> 1;
            if (byteIndex < 4)
            {
                m_waveRam[0] = m_waveRam[byteIndex];
                return;
            }

            var blockStart = byteIndex & ~0x03;
            for (var i = 0; i < 4; i++)
                m_waveRam[i] = m_waveRam[blockStart + i];
        }

        public void Trigger(bool nextStepClocksLength)
        {
            ApplyPendingFrequency();

            var timerActive = m_timerPeriod > 0;
            Enabled = m_dacEnabled && timerActive;

            if (m_lengthCounter == 0)
            {
                m_lengthCounter = 256;
                if (m_lengthEnabled && !nextStepClocksLength)
                    m_lengthCounter--;
            }

            // Start from sample #1 (lower nibble of byte 0). Buffer is intentionally preserved.
            m_sampleIndex = 0;
            m_nextSampleTick = Enabled && timerActive
                ? m_ticks + (ulong)(m_timerPeriod + FirstSampleDelayTicks)
                : 0;
            m_frequencyChangePending = false;
        }

        /// <summary>
        /// Clears playback state (sample buffer/timers) without touching wave RAM.
        /// </summary>
        public void ResetState(bool resetLengthCounter = true)
        {
            m_pendingFrequency = 0;
            m_timerPeriod = CalculateTimerPeriod(0);
            m_pendingTimerPeriod = m_timerPeriod;
            m_sampleIndex = 0;
            m_sampleBuffer = 0;
            if (resetLengthCounter)
                m_lengthCounter = 0;
            m_lengthEnabled = false;
            m_frequencyChangePending = false;
            m_nextSampleTick = 0;
            m_lastWaveReadTick = 0;
            Enabled = false;
        }

        private void Disable()
        {
            Enabled = false;
            m_nextSampleTick = 0;
            if (m_frequencyChangePending)
                ApplyPendingFrequency();
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

        public double Sample()
        {
            if (!Enabled || !m_dacEnabled || m_volumeCode == 0 || m_timerPeriod <= 0)
                return 0.0;

            // Frequencies above Nyquist alias badly at the host sample rate; approximate them by
            // the waveform's DC average to avoid an audible whine.
            const double nyquist = SampleHz * 0.5;
            if (GetFrequencyHz() >= nyquist)
                return GetWaveDcAverage();

            var waveByte = m_sampleBuffer;
            var sample4 = (m_sampleIndex & 1) == 0 ? waveByte >> 4 : waveByte & 0x0F;

            var shifted = m_volumeCode switch
            {
                1 => sample4,
                2 => (byte)(sample4 >> 1),
                3 => (byte)(sample4 >> 2),
                _ => 0
            };

            var normalized = shifted / 15.0;
            return normalized * 2.0 - 1.0;
        }

        private double GetFrequencyHz()
        {
            if (m_timerPeriod <= 0)
                return 0.0;

            var stepRateHz = Cpu.Hz / m_timerPeriod;
            return stepRateHz / 32.0;
        }

        private double GetWaveDcAverage()
        {
            var sum = 0;
            for (var i = 0; i < m_waveRam.Length; i++)
            {
                var waveByte = m_waveRam[i];
                sum += ApplyVolumeShift(waveByte >> 4);
                sum += ApplyVolumeShift(waveByte & 0x0F);
            }

            return (sum / 32.0) / 15.0;
        }

        private int ApplyVolumeShift(int sample4) =>
            m_volumeCode switch
            {
                1 => sample4,
                2 => sample4 >> 1,
                3 => sample4 >> 2,
                _ => 0
            };

        private void ClockSample(ulong tick)
        {
            m_lastWaveReadTick = tick;
            m_sampleIndex = (m_sampleIndex + 1) & 0x1F;

            var waveByte = m_waveRam[m_sampleIndex >> 1];
            m_sampleBuffer = waveByte;

            if (m_frequencyChangePending)
                ApplyPendingFrequency();

            if (m_timerPeriod <= 0)
                Disable();
        }

        private void ApplyPendingFrequency()
        {
            m_timerPeriod = m_pendingTimerPeriod;
            m_frequencyChangePending = false;
        }

        private static int CalculateTimerPeriod(ushort frequency)
        {
            var period = 2048 - (frequency & 0x7FF);
            return period <= 0 ? 0 : period * 2;
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
            if (m_lengthCounter == 0)
            {
                m_lengthCounter = 64;
                if (m_lengthEnabled && !nextStepClocksLength)
                    m_lengthCounter--;
            }
            m_volume = m_initialVolume;
            m_envelopeTimer = (byte)(m_envelopePeriod == 0 ? 8 : m_envelopePeriod);
            m_lfsr = 0x7FFF;
            m_lfsrTimerSeconds = 0.0;
            Enabled = m_dacEnabled && m_lfsrFrequencyHz > 0.0;
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

        public double Sample(double elapsedSeconds)
        {
            if (!Enabled || !m_dacEnabled || m_volume == 0 || m_lfsrFrequencyHz <= 0.0)
                return 0.0;

            m_lfsrTimerSeconds += elapsedSeconds;

            var period = 1.0 / m_lfsrFrequencyHz;
            while (m_lfsrTimerSeconds >= period)
            {
                m_lfsrTimerSeconds -= period;
                StepLfsr();
            }

            var outputBit = (~m_lfsr) & 1;
            var amp = m_volume / 15.0;
            return outputBit != 0 ? amp : -amp;
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
