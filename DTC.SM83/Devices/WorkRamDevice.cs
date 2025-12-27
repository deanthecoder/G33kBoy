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
/// Represents the working RAM device (0xC000 - 0xDFFF) with optional CGB banking.
/// </summary>
public class WorkRamDevice : IMemDevice
{
    private const int BankSize = 0x1000;
    private readonly byte[] m_fixedBank = new byte[BankSize];
    private readonly byte[] m_banked = new byte[BankSize * 7];
    private byte m_currentBank = 1;

    public ushort FromAddr => 0xC000;
    public ushort ToAddr => 0xDFFF;

    public void SetCurrentBank(byte bank)
    {
        var normalized = (byte)(bank & 0x07);
        if (normalized == 0)
            normalized = 1;
        m_currentBank = normalized;
    }

    public byte Read8(ushort addr)
    {
        if (addr < 0xD000)
            return m_fixedBank[addr - 0xC000];

        var offset = addr - 0xD000;
        var bankOffset = (m_currentBank - 1) * BankSize;
        return m_banked[bankOffset + offset];
    }

    public void Write8(ushort addr, byte value)
    {
        if (addr < 0xD000)
        {
            m_fixedBank[addr - 0xC000] = value;
            return;
        }

        var offset = addr - 0xD000;
        var bankOffset = (m_currentBank - 1) * BankSize;
        m_banked[bankOffset + offset] = value;
    }

    public bool Contains(ushort addr) =>
        addr >= FromAddr && addr <= ToAddr;
}
