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
    /// Applies the DMG OAM corruption pattern for the given row and access type.
    /// </summary>
    internal void ApplyCorruption(int row, OamCorruptionType type)
    {
        if ((uint)row >= 20u)
            return;

        switch (type)
        {
            case OamCorruptionType.Read:
                ApplyReadCorruption(row);
                break;
            case OamCorruptionType.Write:
                ApplyWriteCorruption(row);
                break;
            case OamCorruptionType.ReadDuringIncDec:
                ApplyReadDuringIncDecCorruption(row);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown OAM corruption type.");
        }
    }

    /// <summary>
    /// DMG "write corruption": modifies the current row using the preceding row.
    /// </summary>
    private void ApplyWriteCorruption(int row)
    {
        if (row == 0)
            return;

        var prev = row - 1;
        var a = ReadWord(row, 0);
        var b = ReadWord(prev, 0);
        var c = ReadWord(prev, 2);
        var result = (ushort)(((a ^ c) & (b ^ c)) ^ c);
        WriteWord(row, 0, result);

        CopyTrailingWords(prev, row);
    }

    /// <summary>
    /// DMG "read corruption": like write corruption, but with the read-specific expression.
    /// </summary>
    private void ApplyReadCorruption(int row)
    {
        if (row == 0)
            return;

        var prev = row - 1;
        var a = ReadWord(row, 0);
        var b = ReadWord(prev, 0);
        var c = ReadWord(prev, 2);
        var result = (ushort)(b | (a & c));
        WriteWord(row, 0, result);

        CopyTrailingWords(prev, row);
    }

    /// <summary>
    /// DMG read+inc/dec corruption, then the normal read corruption.
    /// </summary>
    private void ApplyReadDuringIncDecCorruption(int row)
    {
        if (row > 3 && row < 19)
        {
            var prev = row - 1;
            var prevPrev = row - 2;
            var a = ReadWord(prevPrev, 0);
            var b = ReadWord(prev, 0);
            var c = ReadWord(row, 0);
            var d = ReadWord(prev, 2);
            var result = (ushort)((b & (a | c | d)) | (a & c & d));
            WriteWord(prev, 0, result);

            CopyRow(prev, row);
            CopyRow(prev, prevPrev);
        }

        ApplyReadCorruption(row);
    }

    /// <summary>
    /// Read a 16-bit word within a row (wordIndex 0..3).
    /// </summary>
    private ushort ReadWord(int row, int wordIndex)
    {
        var baseIndex = row * 8 + wordIndex * 2;
        return (ushort)(m_data[baseIndex] | (m_data[baseIndex + 1] << 8));
    }

    /// <summary>
    /// Write a 16-bit word within a row (wordIndex 0..3).
    /// </summary>
    private void WriteWord(int row, int wordIndex, ushort value)
    {
        var baseIndex = row * 8 + wordIndex * 2;
        m_data[baseIndex] = (byte)(value & 0xFF);
        m_data[baseIndex + 1] = (byte)(value >> 8);
    }

    /// <summary>
    /// Copy the last three words (bytes 2..7) from srcRow into dstRow.
    /// </summary>
    private void CopyTrailingWords(int srcRow, int dstRow)
    {
        var srcIndex = srcRow * 8 + 2;
        var dstIndex = dstRow * 8 + 2;
        Buffer.BlockCopy(m_data, srcIndex, m_data, dstIndex, 6);
    }

    /// <summary>
    /// Copy an entire row (8 bytes) from srcRow to dstRow.
    /// </summary>
    private void CopyRow(int srcRow, int dstRow)
    {
        var srcIndex = srcRow * 8;
        var dstIndex = dstRow * 8;
        Buffer.BlockCopy(m_data, srcIndex, m_data, dstIndex, 8);
    }

    /// <summary>
    /// Single OAM entry (4 bytes).
    /// </summary>
    /// <remarks>
    /// Layout matches DMG OAM: Y, X, Tile, Attr. Stored X is screenX + 8; stored Y is screenY + 16.
    /// Attr bits (DMG): bit7=Priority (1=behind BG), bit6=Y flip, bit5=X flip, bit4=Palette (0=OBP0, 1=OBP1).
    /// Attr bits (CGB): bit7=Priority, bit6=Y flip, bit5=X flip, bit4=DMG palette, bit3=VRAM bank, bits0-2=CGB palette.
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

        /// <summary>
        /// True when the sprite uses VRAM bank 1 (CGB mode).
        /// </summary>
        public bool UseCgbBank => (Attr & 0x08) != 0;

        /// <summary>
        /// CGB palette index (0-7).
        /// </summary>
        public byte CgbPaletteIndex => (byte)(Attr & 0x07);
    }
}
