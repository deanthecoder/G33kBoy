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
/// Shared helpers for memory bank controllers.
/// </summary>
internal abstract class MemoryBankControllerBase : IMemoryBankController
{
    private readonly byte[] m_rom;
    protected readonly Cartridge m_cartridge;
    protected readonly byte[][] m_ramBanks;
    protected bool m_ramEnabled;

    protected MemoryBankControllerBase(Cartridge cartridge)
    {
        m_cartridge = cartridge ?? throw new ArgumentNullException(nameof(cartridge));
        m_rom = cartridge.RomData;
        m_ramBanks = AllocateRamBanks(cartridge.RamBankCount);
    }

    public bool HasRam => m_ramBanks.Length > 0;

    public abstract byte ReadRom(ushort addr);
    public abstract void WriteRom(ushort addr, byte value);

    public virtual byte ReadRam(ushort addr)
    {
        if (!m_ramEnabled || m_ramBanks.Length == 0)
            return 0xFF;

        var bankIndex = GetRamBankIndex();
        if ((uint)bankIndex >= (uint)m_ramBanks.Length)
            return 0xFF;

        var offset = addr - 0xA000;
        var ram = m_ramBanks[bankIndex];
        return offset < ram.Length ? ram[offset] : (byte)0xFF;
    }

    public virtual void WriteRam(ushort addr, byte value)
    {
        if (!m_ramEnabled || m_ramBanks.Length == 0)
            return;

        var bankIndex = GetRamBankIndex();
        if ((uint)bankIndex >= (uint)m_ramBanks.Length)
            return;

        var offset = addr - 0xA000;
        var ram = m_ramBanks[bankIndex];
        if (offset < ram.Length)
            ram[offset] = value;
    }

    protected byte ReadRomFromBank(int bank, ushort addr)
    {
        if (bank < 0)
            return 0xFF;

        var offset = (bank * 0x4000) + (addr & 0x3FFF);
        return offset < m_rom.Length ? m_rom[offset] : (byte)0xFF;
    }

    private static byte[][] AllocateRamBanks(int count)
    {
        if (count <= 0)
            return [];

        var banks = new byte[count][];
        for (var i = 0; i < count; i++)
            banks[i] = new byte[8 * 1024];
        return banks;
    }

    protected virtual int GetRamBankIndex() => 0;
}
