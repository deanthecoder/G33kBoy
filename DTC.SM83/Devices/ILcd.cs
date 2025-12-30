// Code authored by Dean Edis (DeanTheCoder).
// Anyone is free to copy, modify, use, compile, or distribute this software,
// either in source code form or as a compiled binary, for any purpose.
// 
// If you modify the code, please retain this copyright header,
// and consider contributing back to the repository or letting us know
// about your modifications. Your contributions are valued!
// 
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.
namespace DTC.SM83.Devices;

/// <summary>
/// This interface exposes state for managing the LCD's behavior, including
/// control registers, scroll positions, and palette data.
/// </summary>
public interface ILcd
{
    /// <summary>
    /// True when running a DMG-only cartridge in CGB compatibility mode.
    /// </summary>
    bool IsDmgCompatMode { get; }

    /// <summary>
    /// LCD Control (LCDC) - 0xFF40
    /// </summary>
    /// <remarks>
    /// 7 - LCD Enable (0=Off, 1=On)
    /// 6 - Window Tile Map Area (0=9800-9BFF, 1=9C00-9FFF)
    /// 5 - Window Enable (0=Off, 1=On)
    /// 4 - BG/Window Tile Data Area (0=8800-97FF, 1=8000-8FFF)
    /// 3 - BG Tile Map Area (0=9800-9BFF, 1=9C00-9FFF)
    /// 2 - Sprite Size (0=8x8, 1=8x16)
    /// 1 - Sprite Enable (0=Off, 1=On)
    /// 0 - BG/Window Enable/Priority (0=Off, 1=On)
    /// </remarks>
    byte LCDC { get; }

    /// <summary>
    /// LCDC Status (STAT) - 0xFF41
    /// </summary>
    /// <remarks>
    /// 7   - Unused
    /// 6   - LYC=LY Coincidence Interrupt (0=Off, 1=On)
    /// 5   - Mode 2 OAM Interrupt (0=Off, 1=On)
    /// 4   - Mode 1 V-Blank Interrupt (0=Off, 1=On)
    /// 3   - Mode 0 H-Blank Interrupt (0=Off, 1=On)
    /// 2   - LYC==LY Coincidence Interrupt Flag (0=Off, 1=On)
    /// 1-0 - Mode 0/1/2 Selection (0=Mode 0, 1=Mode 1, 2=Mode 2)
    /// </remarks>
    byte STAT { get; set; }

    /// <summary>
    /// SCY (Scroll Y) - 0xFF42
    /// </summary>
    byte SCY { get; }

    /// <summary>
    /// SCX (Scroll X) - 0xFF43
    /// </summary>
    byte SCX { get; }

    /// <summary>
    /// LY (LCD Y Coordinate) - 0xFF44
    /// </summary>
    byte LY { get; set; }

    /// <summary>
    /// LYC (LY Compare) - 0xFF45
    /// </summary>
    byte LYC { get; }

    /// <summary>
    /// BGP (BG Palette Data) - 0xFF47
    /// </summary>
    /// <remarks>
    /// Each bit pair represents a color index.
    /// 6-7 - ID 3
    /// 4-5 - ID 2
    /// 2-3 - ID 1
    /// 0-1 - ID 0
    /// where
    /// 0 => White
    /// 1 => Light gray
    /// 2 => Dark gray
    /// 3 => Black
    /// </remarks>
    byte BGP { get; }

    /// <summary>
    /// OBP0 (Object Palette 0 Data) - 0xFF48
    /// </summary>
    byte OBP0 { get; }

    /// <summary>
    /// OBP1 (Object Palette 1 Data) - 0xFF49
    /// </summary>
    byte OBP1 { get; }

    /// <summary>
    /// WY (Window Y Position) - 0xFF4A
    /// </summary>
    byte WY { get; }

    /// <summary>
    /// WX (Window X Position) - 0xFF4B
    /// </summary>
    byte WX { get; }

    /// <summary>
    /// OPRI (Object Priority Mode) - 0xFF6C (CGB only)
    /// </summary>
    byte OPRI { get; }

    /// <summary>
    /// Read a 15-bit CGB background palette color.
    /// </summary>
    ushort ReadCgbBgPaletteColor(int paletteIndex, int colorIndex);

    /// <summary>
    /// Read a 15-bit CGB object palette color.
    /// </summary>
    ushort ReadCgbObjPaletteColor(int paletteIndex, int colorIndex);
}
