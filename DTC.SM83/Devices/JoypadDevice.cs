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

using DTC.Emulation.Snapshot;

namespace DTC.SM83.Devices;

/// <summary>
/// Handles the joypad input register (0xFF00).
/// </summary>
public sealed class JoypadDevice : IMemDevice
{
    private readonly Joypad m_joypad;
    private byte m_joySelect = 0x30;

    public ushort FromAddr => 0xFF00;
    public ushort ToAddr => 0xFF00;

    public JoypadDevice(Joypad joypad) =>
        m_joypad = joypad;

    public Joypad.JoypadButtons GetPressedButtons() =>
        m_joypad?.GetPressedButtons() ?? Joypad.JoypadButtons.None;
    
    public byte Read8(ushort addr)
    {
        var result = (byte)(0xC0 | m_joySelect | 0x0F);
        if (m_joypad == null)
            return result;

        var state = m_joypad.GetPressedButtons();

        // P14 low = d-pad selected.
        if ((m_joySelect & 0x10) == 0)
        {
            if ((state & Joypad.JoypadButtons.Right) != 0) result &= 0b1110;
            if ((state & Joypad.JoypadButtons.Left) != 0) result &= 0b1101;
            if ((state & Joypad.JoypadButtons.Up) != 0) result &= 0b1011;
            if ((state & Joypad.JoypadButtons.Down) != 0) result &= 0b0111;
        }

        // P15 low = buttons selected.
        if ((m_joySelect & 0x20) == 0)
        {
            if ((state & Joypad.JoypadButtons.A) != 0) result &= 0b1110;
            if ((state & Joypad.JoypadButtons.B) != 0) result &= 0b1101;
            if ((state & Joypad.JoypadButtons.Select) != 0) result &= 0b1011;
            if ((state & Joypad.JoypadButtons.Start) != 0) result &= 0b0111;
        }

        return result;
    }

    public void Write8(ushort addr, byte value)
    {
        // Only bits 4–5 are writable. Bits 0–3 are button inputs (read-only).
        // Bits 6–7 read as 1.
        m_joySelect = (byte)((m_joySelect & 0xCF) | (value & 0x30));
    }

    internal int GetStateSize() => sizeof(byte);

    internal void SaveState(ref StateWriter writer) =>
        writer.WriteByte(m_joySelect);

    internal void LoadState(ref StateReader reader) =>
        m_joySelect = reader.ReadByte();
}
