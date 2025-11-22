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
/// Creates an appropriate MBC for a cartridge.
/// </summary>
internal static class MemoryBankControllerFactory
{
    public static IMemoryBankController Create(Cartridge cartridge) =>
        cartridge.CartridgeType switch
        {
            CartridgeType.RomOnly or
            CartridgeType.RomRam or
            CartridgeType.RomRamBattery => new RomOnlyController(cartridge),

            CartridgeType.Mbc1 or
            CartridgeType.Mbc1Ram or
            CartridgeType.Mbc1RamBattery => new Mbc1Controller(cartridge),

            CartridgeType.Mbc3 or
            CartridgeType.Mbc3Ram or
            CartridgeType.Mbc3RamBattery or
            CartridgeType.Mbc3TimerBattery or
            CartridgeType.Mbc3TimerRamBattery => new Mbc3Controller(cartridge),

            CartridgeType.Mbc5 or
            CartridgeType.Mbc5Ram or
            CartridgeType.Mbc5RamBattery or
            CartridgeType.Mbc5Rumble or
            CartridgeType.Mbc5RumbleRam or
            CartridgeType.Mbc5RumbleRamBattery => new Mbc5Controller(cartridge),

            _ => new RomOnlyController(cartridge) // Unknown types fall back to simple mapping.
        };
}
