// Code authored by Dean Edis (DeanTheCoder).
// Anyone is free to copy, modify, use, compile, or distribute this software,
// either in source code form or as a compiled binary, for any purpose.
//
// If you modify the code, please retain this copyright header,
// and consider contributing back to the repository or letting us know
// about your modifications. Your contributions are valued!
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.

namespace DTC.SM83.MemoryBankControllers;

/// <summary>
/// MBC3: ROM/RAM banking with RTC.
/// </summary>
internal sealed class Mbc3Controller : MemoryBankControllerBase
{
    private const int RtcSecondsRegister = 0x08;
    private const int RtcMinutesRegister = 0x09;
    private const int RtcHoursRegister = 0x0A;
    private const int RtcDayLowRegister = 0x0B;
    private const int RtcControlRegister = 0x0C;
    private const int SecondsPerDay = 24 * 60 * 60;
    private const int RtcSnapshotLength = 19;

    private int m_romBank = 1;
    private int m_bankRegister;
    private byte m_lastLatchWrite;
    private readonly RtcState m_rtc = new();

    public Mbc3Controller(Cartridge cartridge) : base(cartridge)
    {
    }

    public override byte ReadRom(ushort addr)
    {
        var bank = addr < 0x4000 ? 0 : m_romBank;
        return ReadRomFromBank(bank, addr);
    }

    public override void WriteRom(ushort addr, byte value)
    {
        switch (addr)
        {
            case <= 0x1FFF:
                m_ramEnabled = (value & 0x0F) == 0x0A;
                break;

            case <= 0x3FFF:
                m_romBank = value & 0x7F;
                if (m_romBank == 0)
                    m_romBank = 1;
                break;

            case <= 0x5FFF:
                m_bankRegister = value & 0x0F;
                break;

            case <= 0x7FFF:
                HandleLatchCommand(value);
                break;
        }
    }

    public override byte ReadRam(ushort addr)
    {
        if (!m_ramEnabled)
            return 0xFF;

        if (IsRtcRegisterSelected())
            return ReadRtcRegister();

        return base.ReadRam(addr);
    }

    public override void WriteRam(ushort addr, byte value)
    {
        if (!m_ramEnabled)
            return;

        if (IsRtcRegisterSelected())
        {
            WriteRtcRegister(value);
            return;
        }

        base.WriteRam(addr, value);
    }

    public override byte[] GetRamSnapshot()
    {
        SyncClock();

        var ram = base.GetRamSnapshot();
        var rtc = BuildRtcSnapshot();

        if (rtc.Length == 0)
            return ram;

        var combined = new byte[ram.Length + rtc.Length];
        ram.CopyTo(combined, 0);
        rtc.CopyTo(combined, ram.Length);
        return combined;
    }

    public override void LoadRamSnapshot(ReadOnlySpan<byte> data)
    {
        var ramLength = m_ramBanks.Length * 8 * 1024;

        if (ramLength > 0 && !data.IsEmpty)
        {
            var ramSpan = data[..Math.Min(data.Length, ramLength)];
            base.LoadRamSnapshot(ramSpan);
        }

        var rtcOffset = ramLength;
        var rtcBytesAvailable = data.Length - rtcOffset;
        if (rtcBytesAvailable >= RtcSnapshotLength)
            LoadRtcSnapshot(data.Slice(rtcOffset, RtcSnapshotLength));
        else
            m_rtc.LastUpdatedUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    protected override int GetRamBankIndex() =>
        m_bankRegister < m_ramBanks.Length ? m_bankRegister : 0;

    private void HandleLatchCommand(byte value)
    {
        if (m_lastLatchWrite == 0 && value == 1)
            LatchRtc();

        m_lastLatchWrite = value;
    }

    private bool IsRtcRegisterSelected() =>
        m_bankRegister is >= RtcSecondsRegister and <= RtcControlRegister;

    private byte ReadRtcRegister()
    {
        SyncClock();

        var latched = m_rtc.Latched;
        var days = latched ? m_rtc.LatchedDays : m_rtc.Days;
        var halted = latched ? m_rtc.LatchedHalted : m_rtc.Halted;
        var carry = latched ? m_rtc.LatchedDayCarry : m_rtc.DayCarry;

        return m_bankRegister switch
        {
            RtcSecondsRegister => latched ? m_rtc.LatchedSeconds : m_rtc.Seconds,
            RtcMinutesRegister => latched ? m_rtc.LatchedMinutes : m_rtc.Minutes,
            RtcHoursRegister => latched ? m_rtc.LatchedHours : m_rtc.Hours,
            RtcDayLowRegister => (byte)(days & 0xFF),
            RtcControlRegister =>
                (byte)(((days >> 8) & 0x01) |
                       (halted ? 0x40 : 0) |
                       (carry ? 0x80 : 0)),
            _ => 0xFF
        };
    }

    private void WriteRtcRegister(byte value)
    {
        SyncClock();

        switch (m_bankRegister)
        {
            case RtcSecondsRegister:
                m_rtc.Seconds = (byte)(value % 60);
                break;

            case RtcMinutesRegister:
                m_rtc.Minutes = (byte)(value % 60);
                break;

            case RtcHoursRegister:
                m_rtc.Hours = (byte)(value % 24);
                break;

            case RtcDayLowRegister:
                m_rtc.Days = (m_rtc.Days & 0x100) | value;
                break;

            case RtcControlRegister:
                var wasHalted = m_rtc.Halted;

                m_rtc.Days = (m_rtc.Days & 0xFF) | ((value & 0x01) << 8);
                m_rtc.Halted = (value & 0x40) != 0;

                if ((value & 0x80) == 0)
                    m_rtc.DayCarry = false;

                if (wasHalted && !m_rtc.Halted)
                    m_rtc.LastUpdatedUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                break;
        }
    }

    private void SyncClock()
    {
        var nowSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        if (m_rtc.Halted)
        {
            m_rtc.LastUpdatedUnixSeconds = nowSeconds;
            return;
        }

        var elapsedSeconds = nowSeconds - m_rtc.LastUpdatedUnixSeconds;
        if (elapsedSeconds <= 0)
            return;

        m_rtc.LastUpdatedUnixSeconds = nowSeconds;
        AdvanceSeconds(elapsedSeconds);
    }

    private void AdvanceSeconds(long elapsedSeconds)
    {
        var totalSeconds = (m_rtc.Hours * 3600L) + (m_rtc.Minutes * 60L) + m_rtc.Seconds + elapsedSeconds;

        var daysToAdd = totalSeconds / SecondsPerDay;
        var secondsIntoDay = totalSeconds % SecondsPerDay;

        m_rtc.Hours = (byte)(secondsIntoDay / 3600);
        secondsIntoDay %= 3600;
        m_rtc.Minutes = (byte)(secondsIntoDay / 60);
        m_rtc.Seconds = (byte)(secondsIntoDay % 60);

        if (daysToAdd <= 0)
            return;

        var newDays = m_rtc.Days + (int)daysToAdd;
        if (newDays > 511)
        {
            m_rtc.DayCarry = true;
            newDays %= 512;
        }

        m_rtc.Days = newDays;
    }

    private void LatchRtc()
    {
        SyncClock();

        m_rtc.LatchedSeconds = m_rtc.Seconds;
        m_rtc.LatchedMinutes = m_rtc.Minutes;
        m_rtc.LatchedHours = m_rtc.Hours;
        m_rtc.LatchedDays = m_rtc.Days;
        m_rtc.LatchedHalted = m_rtc.Halted;
        m_rtc.LatchedDayCarry = m_rtc.DayCarry;
        m_rtc.Latched = true;
    }

    private byte[] BuildRtcSnapshot()
    {
        var snapshot = new byte[RtcSnapshotLength];

        snapshot[0] = m_rtc.Seconds;
        snapshot[1] = m_rtc.Minutes;
        snapshot[2] = m_rtc.Hours;
        snapshot[3] = (byte)(m_rtc.Days & 0xFF);
        snapshot[4] = (byte)(((m_rtc.Days >> 8) & 0x01) |
                             (m_rtc.Halted ? 0x40 : 0) |
                             (m_rtc.DayCarry ? 0x80 : 0));

        snapshot[5] = m_rtc.LatchedSeconds;
        snapshot[6] = m_rtc.LatchedMinutes;
        snapshot[7] = m_rtc.LatchedHours;
        snapshot[8] = (byte)(m_rtc.LatchedDays & 0xFF);
        snapshot[9] = (byte)(((m_rtc.LatchedDays >> 8) & 0x01) |
                             (m_rtc.LatchedHalted ? 0x40 : 0) |
                             (m_rtc.LatchedDayCarry ? 0x80 : 0));

        snapshot[10] = (byte)(m_rtc.Latched ? 1 : 0);

        var timeStamp = m_rtc.LastUpdatedUnixSeconds;
        for (var i = 0; i < 8; i++)
            snapshot[11 + i] = (byte)(timeStamp >> (8 * i));

        return snapshot;
    }

    private void LoadRtcSnapshot(ReadOnlySpan<byte> data)
    {
        m_rtc.Seconds = (byte)(data[0] % 60);
        m_rtc.Minutes = (byte)(data[1] % 60);
        m_rtc.Hours = (byte)(data[2] % 24);

        var dayLow = data[3];
        var dayHigh = data[4];
        m_rtc.Days = dayLow | ((dayHigh & 0x01) << 8);
        m_rtc.Halted = (dayHigh & 0x40) != 0;
        m_rtc.DayCarry = (dayHigh & 0x80) != 0;

        m_rtc.LatchedSeconds = data[5];
        m_rtc.LatchedMinutes = data[6];
        m_rtc.LatchedHours = data[7];

        var latchedDayLow = data[8];
        var latchedDayHigh = data[9];
        m_rtc.LatchedDays = latchedDayLow | ((latchedDayHigh & 0x01) << 8);
        m_rtc.LatchedHalted = (latchedDayHigh & 0x40) != 0;
        m_rtc.LatchedDayCarry = (latchedDayHigh & 0x80) != 0;

        m_rtc.Latched = data[10] != 0;

        long timeStamp = 0;
        for (var i = 0; i < 8; i++)
            timeStamp |= (long)data[11 + i] << (8 * i);

        m_rtc.LastUpdatedUnixSeconds = timeStamp <= 0
            ? DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            : timeStamp;

        SyncClock();
    }

    private sealed class RtcState
    {
        public byte Seconds;
        public byte Minutes;
        public byte Hours;
        public int Days;
        public bool Halted;
        public bool DayCarry;

        public bool Latched;
        public byte LatchedSeconds;
        public byte LatchedMinutes;
        public byte LatchedHours;
        public int LatchedDays;
        public bool LatchedHalted;
        public bool LatchedDayCarry;

        public long LastUpdatedUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}
