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
/// Represents the video RAM device (0x8000 - 0x9FFF) with optional CGB banking.
/// </summary>
public class VramDevice : IMemDevice
{
    private const int BankSize = 0x2000;
    private readonly byte[] m_bank0 = new byte[BankSize];
    private readonly byte[] m_bank1 = new byte[BankSize];
    private byte m_currentBank;

    public ushort FromAddr => 0x8000;
    public ushort ToAddr => 0x9FFF;

    public byte CurrentBank => m_currentBank;

    public void SetMode(GameBoyMode mode)
    {
        m_currentBank = 0;
    }

    public void SetCurrentBank(byte bank) =>
        m_currentBank = (byte)(bank & 0x01);

    public byte Read8(ushort addr) =>
        ReadBanked(addr, m_currentBank);

    public void Write8(ushort addr, byte value) =>
        WriteBanked(addr, m_currentBank, value);

    public byte ReadBanked(ushort addr, byte bank)
    {
        var idx = addr - FromAddr;
        return (bank & 0x01) == 0 ? m_bank0[idx] : m_bank1[idx];
    }

    public void WriteBanked(ushort addr, byte bank, byte value)
    {
        var idx = addr - FromAddr;
        if ((bank & 0x01) == 0)
            m_bank0[idx] = value;
        else
            m_bank1[idx] = value;
    }
}
