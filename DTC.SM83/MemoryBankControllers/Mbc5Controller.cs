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
/// MBC5: ROM up to 8MB and RAM up to 128KB.
/// </summary>
internal sealed class Mbc5Controller : MemoryBankControllerBase
{
    private int m_romBank;   // 9-bit bank number
    private int m_ramBank;

    public Mbc5Controller(Cartridge cartridge) : base(cartridge)
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

            case <= 0x2FFF:
                m_romBank = (m_romBank & 0x100) | value;
                break;

            case <= 0x3FFF:
                m_romBank = (m_romBank & 0xFF) | ((value & 0x01) << 8);
                break;

            case <= 0x5FFF:
                m_ramBank = value & 0x0F;
                break;
        }
    }

    protected override int GetRamBankIndex() =>
        m_ramBank < m_ramBanks.Length ? m_ramBank : 0;
}
