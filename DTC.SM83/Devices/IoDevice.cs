// Code authored by Dean Edis (DeanTheCoder).
// Anyone is free to copy, modify, use, compile, or distribute this software,
// either in source code form or as a compiled binary, for any non-commercial
// purpose.
//
// If you modify the code, please retain this copyright header,
// and consider contributing back to the repository or letting us know
// about your modifications. Your contributions are valued!
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.

using JetBrains.Annotations;

namespace DTC.SM83.Devices;

/// <summary>
/// Represents device for IO.
/// </summary>
public class IoDevice : IMemDevice, ILcd
{
    private readonly byte[] m_data = new byte[0x80];
    private readonly Bus m_bus;
    private readonly BootRom m_bootRom;
    private byte m_joypSelect = 0x30;
    private readonly Joypad m_joypad;

    public IoDevice(Bus bus, [NotNull] BootRom bootRom, Joypad joypad)
    {
        m_bus = bus ?? throw new ArgumentNullException(nameof(bus));
        m_bootRom = bootRom ?? throw new ArgumentNullException(nameof(bootRom));
        m_joypad = joypad;
    }

    public ushort FromAddr => 0xFF00;
    public ushort ToAddr => 0xFF7F;

    public byte LCDC => m_data[0x40];
    public byte STAT
    {
        get => m_data[0x41];
        set => m_data[0x41] = value;
    }
    public byte SCY => m_data[0x42];
    public byte SCX => m_data[0x43];
    public byte LY
    {
        get => m_data[0x44];
        set => m_data[0x44] = value;
    }
    public byte LYC => m_data[0x45];
    public byte BGP => m_data[0x47];
    public byte OBP0 => m_data[0x48];
    public byte OBP1 => m_data[0x49];
    public byte WY => m_data[0x4A];
    public byte WX => m_data[0x4B];

    public bool IsDMATransferActive { get; private set; }

    public byte Read8(ushort addr)
    {
        switch (addr)
        {
            // Joypad input.
            case 0xFF00:
                return ReadJoyp();
            
            // CGB-specific registers
            case 0xFF4C or 0xFF4D:        // KEY0 and KEY1 - CGB speed switching registers
                return 0xFF;
            
            default:
            {
                var idx = addr - FromAddr;
                return m_data[idx];
            }
        }

    }

    public void Write8(ushort addr, byte value)
    {
        var idx = addr - FromAddr;
        m_data[idx] = value;
        
        // Joypad
        if (idx == 0x00)
        {
            WriteJoyp(value);
            return;
        }

        // Writing to LY resets the counter, but is otherwise ignored.
        if (idx == 0x44)
        {
            m_bus.PPU?.ResetLyCounter();
            return;
        }

        // OAM DMA Transfer request?
        if (idx == 0x46)
        {
            if (value > 0xDF)
                return; // Ignorable.

            IsDMATransferActive = true;
            var src = (ushort)(value << 8);
            var dest = 0xFE00;
            for (var i = 0; i <= 0x9F; i++)
            {
                m_bus.UncheckedWrite((ushort)dest++, m_bus.UncheckedRead(src++));
                m_bus.AdvanceT(4); // One cycle for each byte.
            }
            IsDMATransferActive = false;
            return;
        }
        
        // Boot Disable (Unload the boot ROM).
        if (idx == 0x50)
            m_bootRom.Unload();
    }

    private void WriteJoyp(byte value)
    {
        // Only bits 4–5 are writable. Bits 0–3 are button inputs (read-only).
        // Bits 6–7 read as 1.
        m_joypSelect = (byte)((m_joypSelect & 0xCF) | (value & 0x30));
    }

    private byte ReadJoyp()
    {
        var result = (byte)(0xC0 | m_joypSelect | 0x0F);
        if (m_joypad == null)
            return result;

        var state = m_joypad.GetPressedButtons();

        // P14 low = d-pad selected.
        if ((m_joypSelect & 0x10) == 0)
        {
            if ((state & Joypad.JoypadButtons.Right) != 0) result &= 0b1110;
            if ((state & Joypad.JoypadButtons.Left) != 0) result &= 0b1101;
            if ((state & Joypad.JoypadButtons.Up) != 0) result &= 0b1011;
            if ((state & Joypad.JoypadButtons.Down) != 0) result &= 0b0111;
        }

        // P15 low = buttons selected.
        if ((m_joypSelect & 0x20) == 0)
        {
            if ((state & Joypad.JoypadButtons.A) != 0) result &= 0b1110;
            if ((state & Joypad.JoypadButtons.B) != 0) result &= 0b1101;
            if ((state & Joypad.JoypadButtons.Select) != 0) result &= 0b1011;
            if ((state & Joypad.JoypadButtons.Start) != 0) result &= 0b0111;
        }

        return result;
    }
}
