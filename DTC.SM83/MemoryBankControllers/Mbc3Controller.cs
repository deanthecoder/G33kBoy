// Code authored by Dean Edis (DeanTheCoder).
// Anyone is free to copy, modify, use, compile, or distribute this software,
// either in source code form or as a compiled binary, for any purpose.
//
// If you modify the code, please retain this copyright header,
// and consider contributing back to the repository or letting us know
// about your modifications. Your contributions are valued!
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.

using DTC.Emulation.Snapshot;

namespace DTC.SM83.MemoryBankControllers;

/// <summary>
/// MBC3: ROM/RAM banking with RTC.
/// </summary>
internal sealed class Mbc3Controller : MemoryBankControllerBase
{
    private int m_romBank = 1;
    private int m_bankRegister;

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
        }
    }

    public override byte ReadRam(ushort addr) =>
        m_ramEnabled ? base.ReadRam(addr) : (byte)0xFF;

    public override void WriteRam(ushort addr, byte value)
    {
        if (m_ramEnabled)
            base.WriteRam(addr, value);
    }
    
    protected override int GetRamBankIndex() =>
        m_bankRegister < m_ramBanks.Length ? m_bankRegister : 0;

    internal override int GetStateSize() =>
        base.GetStateSize() +
        sizeof(byte) * 2;

    internal override void SaveState(ref StateWriter writer)
    {
        base.SaveState(ref writer);
        writer.WriteByte((byte)m_romBank);
        writer.WriteByte((byte)m_bankRegister);
    }

    internal override void LoadState(ref StateReader reader)
    {
        base.LoadState(ref reader);
        m_romBank = reader.ReadByte();
        m_bankRegister = reader.ReadByte();
    }
}
