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

    public IoDevice(Bus bus, [NotNull] BootRom bootRom)
    {
        m_bus = bus ?? throw new ArgumentNullException(nameof(bus));
        m_bootRom = bootRom ?? throw new ArgumentNullException(nameof(bootRom));

        // STAT bit 7 is always 1; bits 0-2 are read-only (mode + coincidence flag).
        m_data[0x41] = 0x80;
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

        // STAT: Only bits 3-6 are writable; bit 7 always reads as 1; bits 0-2 are read-only.
        if (idx == 0x41)
        {
            var preserved = (byte)(m_data[idx] & 0x07); // mode + coincidence flag
            m_data[idx] = (byte)(0x80 | preserved | (value & 0x78));
            return;
        }
        
        m_data[idx] = value;

        // Writing to LY resets the counter, but is otherwise ignored.
        if (idx == 0x44)
        {
            m_bus.PPU?.ResetLyCounter();
            return;
        }

        // OAM DMA Transfer request?
        if (idx == 0x46)
        {
            var src = (ushort)(value << 8);

            IsDMATransferActive = true;
            const int oamSize = 0xA0; // 160 bytes.
            var dest = 0xFE00;
            for (var i = 0; i < oamSize; i++)
            {
                var b = m_bus.UncheckedRead(src++);
                m_bus.UncheckedWrite((ushort)dest++, b);
                m_bus.AdvanceT(4); // Four T-cycles per byte.
            }
            IsDMATransferActive = false;
            return;
        }
        
        // Boot Disable (Unload the boot ROM).
        if (idx == 0x50)
        {
            m_bus.Detach(m_bootRom, m_bus.CartridgeRom);
            m_bootRom.Unload();
        }
    }
}
