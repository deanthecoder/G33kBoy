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
/// MBC1: Supports up to 2MB ROM and 32KB RAM with banking.
/// </summary>
internal sealed class Mbc1Controller : MemoryBankControllerBase
{
    private int m_romBankLow5 = 1;
    private int m_romBankHigh2;
    private bool m_ramBankingMode;

    public Mbc1Controller(Cartridge cartridge) : base(cartridge)
    {
    }

    public override byte ReadRom(ushort addr)
    {
        var bank = addr < 0x4000 ? GetBank0() : GetBankX();
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
                m_romBankLow5 = value & 0x1F;
                if (m_romBankLow5 == 0)
                    m_romBankLow5 = 1;
                break;

            case <= 0x5FFF:
                m_romBankHigh2 = value & 0x03;
                break;

            case <= 0x7FFF:
                m_ramBankingMode = (value & 0x01) != 0;
                break;
        }
    }

    protected override int GetRamBankIndex()
    {
        if (!m_ramBankingMode || m_ramBanks.Length == 0)
            return 0;

        return m_romBankHigh2 % m_ramBanks.Length;
    }

    private int GetBank0()
    {
        if (!m_ramBankingMode)
        {
            // In ROM banking mode, 0000-3FFF always maps to bank 0.
            return 0;
        }

        // In RAM banking mode, 0000-3FFF uses the high two bits as the upper ROM bank bits.
        var bank = m_romBankHigh2 << 5;

        if (m_cartridge.RomBankCount > 0)
            bank %= m_cartridge.RomBankCount; // Allow mirroring to bank 0 on small ROMs.

        return bank;
    }

    private int GetBankX()
    {
        var bank = m_ramBankingMode
            ? m_romBankLow5
            : (m_romBankHigh2 << 5) | m_romBankLow5;

        if (m_cartridge.RomBankCount > 0)
            bank %= m_cartridge.RomBankCount;

        return bank;
    }
}
