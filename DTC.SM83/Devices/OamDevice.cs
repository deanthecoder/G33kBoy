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

using System.Runtime.InteropServices;

namespace DTC.SM83.Devices;

/// <summary>
/// Represents memory device for Object Attribute Memory(/Sprites)
/// </summary>
/// <remarks>
/// Holds data for 40 sprites (x4 bytes each).
/// </remarks>
public class OamDevice : RamDeviceBase
{
    public OamDevice() : base(0xFE00, 0xFE9F, isUsable: true)
    {
    }
    
    /// <summary>
    /// Returns the sprite OAM entries.
    /// </summary>
    public ReadOnlySpan<OamEntry> GetSprites() =>
        MemoryMarshal.Cast<byte, OamEntry>(m_data);

    /// <summary>
    /// Single OAM entry (4 bytes).
    /// </summary>
    /// <remarks>
    /// Layout matches DMG OAM: Y, X, Tile, Attr. Stored X is screenX + 8; stored Y is screenY + 16.
    /// Attr bits (DMG): bit7=Priority (1=behind BG), bit6=Y flip, bit5=X flip, bit4=Palette (0=OBP0, 1=OBP1).
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct OamEntry
    {
        /// <summary>
        /// Stored Y (on-screen Y = Y - 16).
        /// </summary>
        public byte Y;

        /// <summary>
        /// Stored X (on-screen X = X - 8).
        /// </summary>
        public byte X;

        /// <summary>
        /// Tile index.
        /// </summary>
        public byte Tile;

        /// <summary>
        /// Attribute flags (priority/flip/palette).
        /// </summary>
        private byte Attr;

        /// <summary>
        /// True if sprite is drawn behind background colors 1..3.
        /// </summary>
        public bool Priority => (Attr & 0x80) != 0;

        /// <summary>
        /// True if vertically flipped.
        /// </summary>
        public bool YFlip => (Attr & 0x40) != 0;

        /// <summary>
        /// True if horizontally flipped.
        /// </summary>
        public bool XFlip => (Attr & 0x20) != 0;

        /// <summary>
        /// True to use OBP1; false uses OBP0. (Object Palette)
        /// </summary>
        public bool UseObp1 => (Attr & 0x10) != 0;
    }
}