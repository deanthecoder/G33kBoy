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
/// Minimal controller for 32 KiB ROMs (optionally with static RAM).
/// </summary>
internal sealed class RomOnlyController : MemoryBankControllerBase
{
    public RomOnlyController(Cartridge cartridge) : base(cartridge)
    {
        m_ramEnabled = true; // No enable register for ROM-only cartridges.
    }

    public override byte ReadRom(ushort addr) =>
        ReadRomFromBank(addr < 0x4000 ? 0 : 1, addr);

    public override void WriteRom(ushort addr, byte value)
    {
        // No bank switching for ROM-only cartridges.
    }
}
