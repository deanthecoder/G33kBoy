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

namespace DTC.SM83.Devices;

/// <summary>
/// Simplified Game Boy APU. Implements square channels (1 &amp; 2), wave (3) and noise (4)
/// and feeds mixed output into the shared sound handler.
/// </summary>
public sealed class ApuDevice
{
    private readonly byte[] m_waveRam = new byte[16];
    private readonly SoundHandler m_soundHandler;
    private readonly SquareChannel m_channel1 = new();
    private readonly SquareChannel m_channel2 = new();
    private readonly WaveChannel m_channel3;
    private readonly NoiseChannel m_channel4 = new();
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

    public ApuDevice(SoundHandler soundHandler)
    {
        m_soundHandler = soundHandler;
        m_channel3 = new WaveChannel(m_waveRam);
    }

    public void AdvanceT(ulong tStates)
    {
        if (!m_isPowered)
        {
            m_soundHandler?.SetSpeakerState(0);
            m_soundHandler?.SampleSpeakerState(tStates);
            return;
        }

        var ch1Changed = m_channel1.Advance(tStates);
        var ch2Changed = m_channel2.Advance(tStates);
        var ch3Changed = m_channel3.Advance(tStates);
        var ch4Changed = m_channel4.Advance(tStates);
        if (ch1Changed || ch2Changed || ch3Changed || ch4Changed)
            UpdateStatusFlags();

        var ch1 = m_channel1.Sample(tStates);
        var ch2 = m_channel2.Sample(tStates);
        var ch3 = m_channel3.Sample(tStates);
        var ch4 = m_channel4.Sample(tStates);

        var routedCh1 = IsChannelRouted(0) ? ch1 : 0.0;
        var routedCh2 = IsChannelRouted(1) ? ch2 : 0.0;
        var routedCh3 = IsChannelRouted(2) ? ch3 : 0.0;
        var routedCh4 = IsChannelRouted(3) ? ch4 : 0.0;

        // Normalize by the maximum number of mixed channels to avoid per-sample pumping when
        // channels (especially noise) cross zero between successive samples.
        var mix = routedCh1 + routedCh2 + routedCh3 + routedCh4;
        mix /= 4.0;
        mix *= MasterVolume;

        var maxLevel = (m_soundHandler?.LevelResolution ?? 16) - 1;
        var level = (byte)Math.Clamp(mix * maxLevel, 0, maxLevel);
        m_soundHandler?.SetSpeakerState(level);
        m_soundHandler?.SampleSpeakerState(tStates);
    }

    public byte Read8(ushort addr)
    {
        if (addr is >= 0xFF30 and <= 0xFF3F)
            return m_waveRam[addr - 0xFF30];

        if (!m_isPowered && addr != 0xFF26)
            return 0x00;

        return addr switch
        {
            0xFF10 => m_nr10,
            0xFF11 => m_nr11,
            0xFF12 => m_nr12,
            0xFF13 => m_nr13,
            0xFF14 => (byte)(m_nr14 | 0xBF),
            0xFF16 => m_nr21,
            0xFF17 => m_nr22,
            0xFF18 => m_nr23,
            0xFF19 => (byte)(m_nr24 | 0xBF),
            0xFF1A => m_nr30,
            0xFF1B => m_nr31,
            0xFF1C => m_nr32,
            0xFF1D => m_nr33,
            0xFF1E => (byte)(m_nr34 | 0xBF),
            0xFF20 => m_nr41,
            0xFF21 => m_nr42,
            0xFF22 => m_nr43,
            0xFF23 => (byte)(m_nr44 | 0xBF),
            0xFF24 => m_nr50,
            0xFF25 => m_nr51,
            0xFF26 => (byte)(m_nr52 | 0x70),
            _ => 0xFF
        };
    }

    public void Write8(ushort addr, byte value)
    {
        if (addr is >= 0xFF30 and <= 0xFF3F)
        {
            m_waveRam[addr - 0xFF30] = value;
            return;
        }

        if (addr == 0xFF26)
        {
            var powerOn = (value & 0x80) != 0;
            if (powerOn == m_isPowered)
            {
                UpdateStatusFlags();
                return;
            }

            m_isPowered = powerOn;
            if (!powerOn)
                ResetRegisters();
            else
                UpdateStatusFlags();
            return;
        }

        if (!m_isPowered)
            return;

        switch (addr)
        {
            case 0xFF10: // NR10 (sweep) - not implemented yet
                m_nr10 = value;
                break;
            case 0xFF11: // NR11 (duty/length)
                m_nr11 = value;
                m_channel1.SetDuty((byte)((value >> 6) & 0x03));
                m_channel1.SetLength(value);
                break;
            case 0xFF12: // NR12 (envelope)
                m_nr12 = value;
                m_channel1.SetEnvelope(value);
                break;
            case 0xFF13: // NR13 (freq low)
                m_nr13 = value;
                m_channel1.SetFrequency(CombineFrequency(m_nr13, m_nr14));
                break;
            case 0xFF14: // NR14 (trigger/freq high)
                m_nr14 = value;
                m_channel1.SetFrequency(CombineFrequency(m_nr13, m_nr14));
                m_channel1.SetLengthEnable((value & 0x40) != 0);
                if ((value & 0x80) != 0)
                    m_channel1.Trigger();
                break;

            case 0xFF16: // NR21 (duty/length)
                m_nr21 = value;
                m_channel2.SetDuty((byte)((value >> 6) & 0x03));
                m_channel2.SetLength(value);
                break;
            case 0xFF17: // NR22 (envelope)
                m_nr22 = value;
                m_channel2.SetEnvelope(value);
                break;
            case 0xFF18: // NR23 (freq low)
                m_nr23 = value;
                m_channel2.SetFrequency(CombineFrequency(m_nr23, m_nr24));
                break;
            case 0xFF19: // NR24 (trigger/freq high)
                m_nr24 = value;
                m_channel2.SetFrequency(CombineFrequency(m_nr23, m_nr24));
                m_channel2.SetLengthEnable((value & 0x40) != 0);
                if ((value & 0x80) != 0)
                    m_channel2.Trigger();
                break;

            case 0xFF1A: // NR30 (DAC power)
                m_nr30 = value;
                m_channel3.SetDacEnabled((value & 0x80) != 0);
                break;
            case 0xFF1B: // NR31 (length)
                m_nr31 = value;
                m_channel3.SetLength(value);
                break;
            case 0xFF1C: // NR32 (volume)
                m_nr32 = value;
                m_channel3.SetVolume((byte)((value >> 5) & 0x03));
                break;
            case 0xFF1D: // NR33 (freq low)
                m_nr33 = value;
                m_channel3.SetFrequency(CombineFrequency(m_nr33, m_nr34));
                break;
            case 0xFF1E: // NR34 (trigger/freq high)
                m_nr34 = value;
                m_channel3.SetFrequency(CombineFrequency(m_nr33, m_nr34));
                m_channel3.SetLengthEnable((value & 0x40) != 0);
                if ((value & 0x80) != 0)
                    m_channel3.Trigger();
                break;

            case 0xFF20: // NR41 (length)
                m_nr41 = value;
                m_channel4.SetLength(value);
                break;
            case 0xFF21: // NR42 (envelope)
                m_nr42 = value;
                m_channel4.SetEnvelope(value);
                break;
            case 0xFF22: // NR43 (polynomial counter)
                m_nr43 = value;
                m_channel4.SetPolynomial(value);
                break;
            case 0xFF23: // NR44 (trigger/length enable)
                m_nr44 = value;
                m_channel4.SetLengthEnable((value & 0x40) != 0);
                if ((value & 0x80) != 0)
                    m_channel4.Trigger();
                break;

            case 0xFF24: // NR50 (master volume)
                m_nr50 = value;
                break;
            case 0xFF25: // NR51 (panning)
                m_nr51 = value;
                break;
        }

        UpdateStatusFlags();
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
        Array.Clear(m_waveRam);
        m_channel1.Disable();
        m_channel2.Disable();
        m_channel3.SetDacEnabled(false);
        m_channel3.Disable();
        m_channel4.Disable();
        UpdateStatusFlags();
    }

    private static ushort CombineFrequency(byte low, byte high) =>
        (ushort)(low | ((high & 0x07) << 8));

    private bool IsChannelRouted(int channel)
    {
        var rightMask = 1 << channel;
        var leftMask = 1 << (channel + 4);
        return (m_nr51 & (rightMask | leftMask)) != 0;
    }

    private double MasterVolume
    {
        get
        {
            var so1 = m_nr50 & 0x07;
            var so2 = (m_nr50 >> 4) & 0x07;
            var max = Math.Max(so1, so2);
            return max / 7.0;
        }
    }

    private sealed class SquareChannel
    {
        private const ulong LengthStepTStates = (ulong)(4194304.0 / 256.0);  // 256 Hz
        private const ulong EnvelopeStepTStates = (ulong)(4194304.0 / 64.0); // 64 Hz
        private static readonly double[] DutyCycle = { 0.125, 0.25, 0.5, 0.75 };
        private ushort m_frequency;
        private double m_frequencyHz;
        private double m_phase;
        private byte m_volume;
        private byte m_initialVolume;
        public bool Enabled { get; private set; }
        private byte m_dutyIndex;
        private byte m_lengthCounter;
        private bool m_lengthEnabled;
        private byte m_envelopePeriod;
        private bool m_envelopeIncrease;
        private ulong m_lengthTicks;
        private ulong m_envelopeTicks;

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
            m_volume = m_initialVolume;
            m_envelopeIncrease = (nrx2 & 0x08) != 0;
            m_envelopePeriod = (byte)(nrx2 & 0x07);
        }

        public void SetFrequency(ushort frequency)
        {
            m_frequency = (ushort)(frequency & 0x7FF);
            m_frequencyHz = m_frequency >= 2048 ? 0.0 : 131072.0 / (2048 - m_frequency);
        }

        public void Trigger()
        {
            Enabled = m_initialVolume > 0 && m_frequencyHz > 0;
            m_volume = m_initialVolume;
            m_phase = 0.0;
            if (m_lengthCounter == 0)
                m_lengthCounter = 64;
            m_lengthTicks = 0;
            m_envelopeTicks = 0;
            // Channel state changed; caller updates NR52.
        }

        public void Disable()
        {
            Enabled = false;
            m_phase = 0.0;
            m_volume = 0;
            // Channel state changed; caller updates NR52.
        }

        public bool Advance(ulong tStates)
        {
            var wasEnabled = Enabled;
            if (Enabled && m_lengthEnabled && m_lengthCounter > 0)
            {
                m_lengthTicks += tStates;
                while (m_lengthTicks >= LengthStepTStates && m_lengthCounter > 0)
                {
                    m_lengthTicks -= LengthStepTStates;
                    m_lengthCounter--;
                    if (m_lengthCounter == 0)
                    {
                        Disable();
                        break;
                    }
                }
            }

            if (Enabled)
            {
                m_envelopeTicks += tStates;
                var periodSteps = (ulong)(m_envelopePeriod == 0 ? 8 : m_envelopePeriod);
                var stepPeriod = periodSteps * EnvelopeStepTStates;
                while (stepPeriod > 0 && m_envelopeTicks >= stepPeriod)
                {
                    m_envelopeTicks -= stepPeriod;
                    if (m_envelopeIncrease && m_volume < 15)
                    {
                        m_volume++;
                    }
                    else if (!m_envelopeIncrease && m_volume > 0)
                    {
                        m_volume--;
                    }
                }
            }

            return wasEnabled != Enabled;
        }

        public double Sample(ulong tStates)
        {
            if (!Enabled || m_volume == 0 || m_frequencyHz <= 0.0)
                return 0.0;

            var elapsedSeconds = tStates / Cpu.Hz;
            m_phase += m_frequencyHz * elapsedSeconds;
            m_phase -= Math.Floor(m_phase);

            var duty = DutyCycle[m_dutyIndex];
            var high = m_phase < duty;
            return high ? m_volume / 15.0 : 0.0;
        }
    }

    private sealed class WaveChannel
    {
        private const ulong LengthStepTStates = (ulong)(4194304.0 / 256.0);  // 256 Hz.
        private readonly byte[] m_waveRam;
        private ushort m_frequency;
        private double m_frequencyHz;
        private double m_phase;
        private byte m_volumeCode;
        private bool m_dacEnabled = true;
        private ushort m_lengthCounter;
        private bool m_lengthEnabled;
        private ulong m_lengthTicks;

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
            m_volumeCode = (byte)Math.Min((byte)3, volumeCode);

        public void SetFrequency(ushort frequency)
        {
            m_frequency = (ushort)(frequency & 0x7FF);
            m_frequencyHz = m_frequency >= 2048 ? 0.0 : 65536.0 / (2048 - m_frequency);
        }

        public void Trigger()
        {
            Enabled = m_dacEnabled && m_frequencyHz > 0.0;
            if (!Enabled)
                return;

            if (m_lengthCounter == 0)
                m_lengthCounter = 256;
            m_lengthTicks = 0;
            m_phase = 0.0;
        }

        public void Disable()
        {
            Enabled = false;
            m_phase = 0.0;
        }

        public bool Advance(ulong tStates)
        {
            var wasEnabled = Enabled;
            if (Enabled && m_lengthEnabled && m_lengthCounter > 0)
            {
                m_lengthTicks += tStates;
                while (m_lengthTicks >= LengthStepTStates && m_lengthCounter > 0)
                {
                    m_lengthTicks -= LengthStepTStates;
                    m_lengthCounter--;
                    if (m_lengthCounter == 0)
                    {
                        Disable();
                        break;
                    }
                }
            }

            return wasEnabled != Enabled;
        }

        public double Sample(ulong tStates)
        {
            if (!Enabled || m_volumeCode == 0 || m_frequencyHz <= 0.0)
                return 0.0;

            var elapsedSeconds = tStates / Cpu.Hz;
            m_phase += m_frequencyHz * elapsedSeconds;
            m_phase -= Math.Floor(m_phase);

            var sampleIndex = (int)(m_phase * 32.0);
            sampleIndex = Math.Clamp(sampleIndex, 0, 31);
            var waveByte = m_waveRam[sampleIndex >> 1];
            var sample4 = (sampleIndex & 1) == 0 ? waveByte >> 4 : waveByte & 0x0F;

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
        private const ulong LengthStepTStates = (ulong)(4194304.0 / 256.0);  // 256 Hz.
        private const ulong EnvelopeStepTStates = (ulong)(4194304.0 / 64.0); // 64 Hz.

        private ushort m_lfsr = 0x7FFF;
        private double m_lfsrFrequencyHz;
        private double m_lfsrTimerSeconds;

        private byte m_volume;
        private byte m_initialVolume;
        private byte m_envelopePeriod;
        private bool m_envelopeIncrease;

        private byte m_lengthCounter;
        private bool m_lengthEnabled;
        private ulong m_lengthTicks;
        private ulong m_envelopeTicks;

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
            m_volume = m_initialVolume;
            m_envelopeIncrease = (nrx2 & 0x08) != 0;
            m_envelopePeriod = (byte)(nrx2 & 0x07);
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

        public void Trigger()
        {
            Enabled = m_initialVolume > 0 && m_lfsrFrequencyHz > 0.0;
            m_volume = m_initialVolume;
            if (m_lengthCounter == 0)
                m_lengthCounter = 64;
            m_lengthTicks = 0;
            m_envelopeTicks = 0;
            m_lfsr = 0x7FFF;
            m_lfsrTimerSeconds = 0.0;
        }

        public void Disable()
        {
            Enabled = false;
            m_volume = 0;
        }

        public bool Advance(ulong tStates)
        {
            var wasEnabled = Enabled;

            if (Enabled && m_lengthEnabled && m_lengthCounter > 0)
            {
                m_lengthTicks += tStates;
                while (m_lengthTicks >= LengthStepTStates && m_lengthCounter > 0)
                {
                    m_lengthTicks -= LengthStepTStates;
                    m_lengthCounter--;
                    if (m_lengthCounter == 0)
                    {
                        Disable();
                        break;
                    }
                }
            }

            if (Enabled)
            {
                m_envelopeTicks += tStates;
                var periodSteps = (ulong)(m_envelopePeriod == 0 ? 8 : m_envelopePeriod);
                var stepPeriod = periodSteps * EnvelopeStepTStates;
                while (stepPeriod > 0 && m_envelopeTicks >= stepPeriod)
                {
                    m_envelopeTicks -= stepPeriod;
                    if (m_envelopeIncrease && m_volume < 15)
                    {
                        m_volume++;
                    }
                    else if (!m_envelopeIncrease && m_volume > 0)
                    {
                        m_volume--;
                    }
                }
            }

            return wasEnabled != Enabled;
        }

        public double Sample(ulong tStates)
        {
            if (!Enabled || m_volume == 0 || m_lfsrFrequencyHz <= 0.0)
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
