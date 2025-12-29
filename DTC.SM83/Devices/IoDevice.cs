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

using System.Diagnostics;
using DTC.Core;
using DTC.SM83.Snapshot;
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
    private readonly byte[] m_bgPaletteData = new byte[0x40];
    private readonly byte[] m_objPaletteData = new byte[0x40];
    private bool m_prepareSpeedSwitch;
    private byte m_vramBank;
    private byte m_wramBank = 1;
    private byte m_bgPaletteIndex;
    private byte m_objPaletteIndex;
    private bool m_bgPaletteAutoIncrement;
    private bool m_objPaletteAutoIncrement;
    private byte m_opri;

    public IoDevice(Bus bus, [NotNull] BootRom bootRom)
    {
        m_bus = bus ?? throw new ArgumentNullException(nameof(bus));
        m_bootRom = bootRom ?? throw new ArgumentNullException(nameof(bootRom));

        // STAT bit 7 is always 1; bits 0-2 are read-only (mode + coincidence flag).
        m_data[0x41] = 0x80;
    }

    public ushort FromAddr => 0xFF00;
    public ushort ToAddr => 0xFF7F;

    public GameBoyMode Mode { get; private set; } = GameBoyMode.Dmg;

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
    public byte OPRI => Mode == GameBoyMode.Cgb ? (byte)(0xFE | (m_opri & 0x01)) : (byte)0xFF;

    public void SetMode(GameBoyMode mode)
    {
        Mode = mode;
        m_prepareSpeedSwitch = false;
        m_vramBank = 0;
        m_wramBank = 1;
        m_bgPaletteIndex = 0;
        m_objPaletteIndex = 0;
        m_bgPaletteAutoIncrement = false;
        m_objPaletteAutoIncrement = false;
        m_opri = 0;
        m_bus.Vram?.SetCurrentBank(m_vramBank);
        m_bus.WorkRam?.SetCurrentBank(m_wramBank);
    }

    public bool TryHandleSpeedSwitch()
    {
        if (Mode != GameBoyMode.Cgb || !m_prepareSpeedSwitch)
            return false;

        m_prepareSpeedSwitch = false;
        m_bus.SetDoubleSpeed(!m_bus.IsDoubleSpeed);
        m_bus.ResetDivider();
        LogSpeedSwitch(m_bus.IsDoubleSpeed);
        return true;
    }

    public ushort ReadCgbBgPaletteColor(int paletteIndex, int colorIndex) =>
        ReadPaletteColor(m_bgPaletteData, paletteIndex, colorIndex);

    public ushort ReadCgbObjPaletteColor(int paletteIndex, int colorIndex) =>
        ReadPaletteColor(m_objPaletteData, paletteIndex, colorIndex);

    public byte Read8(ushort addr)
    {
        switch (addr)
        {
            // CGB-specific registers
            case 0xFF4C: // KEY0 - CGB mode select
                return Mode == GameBoyMode.Cgb ? (byte)0xFB : (byte)0xFF;

            case 0xFF4D: // KEY1 - speed switch
                return Mode == GameBoyMode.Cgb
                    ? (byte)(0x7E | (m_bus.IsDoubleSpeed ? 0x80 : 0x00) | (m_prepareSpeedSwitch ? 0x01 : 0x00))
                    : (byte)0xFF;

            case 0xFF4F: // VBK - VRAM bank
                return Mode == GameBoyMode.Cgb ? (byte)(0xFE | m_vramBank) : (byte)0xFF;

            case 0xFF51: // HDMA1
                return Mode == GameBoyMode.Cgb ? m_bus.Hdma?.ReadHdma1() ?? 0xFF : (byte)0xFF;
            case 0xFF52: // HDMA2
                return Mode == GameBoyMode.Cgb ? m_bus.Hdma?.ReadHdma2() ?? 0xFF : (byte)0xFF;
            case 0xFF53: // HDMA3
                return Mode == GameBoyMode.Cgb ? m_bus.Hdma?.ReadHdma3() ?? 0xFF : (byte)0xFF;
            case 0xFF54: // HDMA4
                return Mode == GameBoyMode.Cgb ? m_bus.Hdma?.ReadHdma4() ?? 0xFF : (byte)0xFF;
            case 0xFF55: // HDMA5
                return Mode == GameBoyMode.Cgb ? m_bus.Hdma?.ReadHdma5() ?? 0xFF : (byte)0xFF;

            case 0xFF68: // BGPI
                return Mode == GameBoyMode.Cgb
                    ? (byte)((m_bgPaletteAutoIncrement ? 0x80 : 0x00) | (m_bgPaletteIndex & 0x3F))
                    : (byte)0xFF;
            case 0xFF69: // BGPD
                return Mode == GameBoyMode.Cgb
                    ? ReadPaletteData(m_bgPaletteData, m_bgPaletteIndex)
                    : (byte)0xFF;

            case 0xFF6A: // OBPI
                return Mode == GameBoyMode.Cgb
                    ? (byte)((m_objPaletteAutoIncrement ? 0x80 : 0x00) | (m_objPaletteIndex & 0x3F))
                    : (byte)0xFF;
            case 0xFF6B: // OBPD
                return Mode == GameBoyMode.Cgb
                    ? ReadPaletteData(m_objPaletteData, m_objPaletteIndex)
                    : (byte)0xFF;

            case 0xFF6C: // OPRI
                return Mode == GameBoyMode.Cgb ? (byte)(0xFE | (m_opri & 0x01)) : (byte)0xFF;

            case 0xFF70: // SVBK
                return Mode == GameBoyMode.Cgb ? (byte)(0xF8 | m_wramBank) : (byte)0xFF;
            
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

        switch (addr)
        {
            case 0xFF4D: // KEY1
                if (Mode == GameBoyMode.Cgb)
                    m_prepareSpeedSwitch = (value & 0x01) != 0;
                return;

            case 0xFF4F: // VBK
                if (Mode == GameBoyMode.Cgb)
                {
                    m_vramBank = (byte)(value & 0x01);
                    m_bus.Vram?.SetCurrentBank(m_vramBank);
                }
                return;

            case 0xFF51: // HDMA1
                if (Mode == GameBoyMode.Cgb)
                    m_bus.Hdma?.WriteHdma1(value);
                return;
            case 0xFF52: // HDMA2
                if (Mode == GameBoyMode.Cgb)
                    m_bus.Hdma?.WriteHdma2(value);
                return;
            case 0xFF53: // HDMA3
                if (Mode == GameBoyMode.Cgb)
                    m_bus.Hdma?.WriteHdma3(value);
                return;
            case 0xFF54: // HDMA4
                if (Mode == GameBoyMode.Cgb)
                    m_bus.Hdma?.WriteHdma4(value);
                return;
            case 0xFF55: // HDMA5
                if (Mode == GameBoyMode.Cgb)
                    m_bus.Hdma?.WriteHdma5(value);
                return;

            case 0xFF68: // BGPI
                if (Mode == GameBoyMode.Cgb)
                {
                    m_bgPaletteAutoIncrement = (value & 0x80) != 0;
                    m_bgPaletteIndex = (byte)(value & 0x3F);
                }
                return;
            case 0xFF69: // BGPD
                if (Mode == GameBoyMode.Cgb)
                    WritePaletteData(m_bgPaletteData, ref m_bgPaletteIndex, m_bgPaletteAutoIncrement, value);
                return;

            case 0xFF6A: // OBPI
                if (Mode == GameBoyMode.Cgb)
                {
                    m_objPaletteAutoIncrement = (value & 0x80) != 0;
                    m_objPaletteIndex = (byte)(value & 0x3F);
                }
                return;
            case 0xFF6B: // OBPD
                if (Mode == GameBoyMode.Cgb)
                    WritePaletteData(m_objPaletteData, ref m_objPaletteIndex, m_objPaletteAutoIncrement, value);
                return;

            case 0xFF6C: // OPRI
                if (Mode == GameBoyMode.Cgb)
                    m_opri = (byte)(value & 0x01);
                return;

            case 0xFF70: // SVBK
                if (Mode == GameBoyMode.Cgb)
                {
                    var bank = (byte)(value & 0x07);
                    if (bank == 0)
                        bank = 1;
                    m_wramBank = bank;
                    m_bus.WorkRam?.SetCurrentBank(m_wramBank);
                }
                return;
        }

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
            m_bus.Dma?.Start(value);
            return;
        }
        
        // Boot Disable (Unload the boot ROM).
        if (idx == 0x50)
        {
            m_bus.Detach(m_bootRom, m_bus.CartridgeRom);
            m_bootRom.Unload();
        }
    }

    private static byte ReadPaletteData(byte[] paletteData, byte index) =>
        paletteData[index & 0x3F];

    private static void WritePaletteData(byte[] paletteData, ref byte index, bool autoIncrement, byte value)
    {
        paletteData[index & 0x3F] = value;
        if (autoIncrement)
            index = (byte)((index + 1) & 0x3F);
    }

    private static ushort ReadPaletteColor(byte[] paletteData, int paletteIndex, int colorIndex)
    {
        var safePalette = paletteIndex & 0x07;
        var safeColor = colorIndex & 0x03;
        var offset = safePalette * 8 + safeColor * 2;
        return (ushort)(paletteData[offset] | (paletteData[offset + 1] << 8));
    }

    [Conditional("DEBUG")]
    private static void LogSpeedSwitch(bool isDoubleSpeed) =>
        Logger.Instance.Info($"CGB speed switch: {(isDoubleSpeed ? "double" : "normal")} speed.");

    internal int GetStateSize() =>
        sizeof(byte) + // Mode
        sizeof(byte) * 9 + // m_prepareSpeedSwitch, m_vramBank, m_wramBank, m_bgPaletteIndex, m_objPaletteIndex, m_bgPaletteAutoIncrement, m_objPaletteAutoIncrement, m_opri, padding
        m_data.Length +
        m_bgPaletteData.Length +
        m_objPaletteData.Length;

    internal void SaveState(ref StateWriter writer)
    {
        writer.WriteByte((byte)Mode);
        writer.WriteBool(m_prepareSpeedSwitch);
        writer.WriteByte(m_vramBank);
        writer.WriteByte(m_wramBank);
        writer.WriteByte(m_bgPaletteIndex);
        writer.WriteByte(m_objPaletteIndex);
        writer.WriteBool(m_bgPaletteAutoIncrement);
        writer.WriteBool(m_objPaletteAutoIncrement);
        writer.WriteByte(m_opri);
        writer.WriteByte(0); // reserved
        writer.WriteBytes(m_data);
        writer.WriteBytes(m_bgPaletteData);
        writer.WriteBytes(m_objPaletteData);
    }

    internal void LoadState(ref StateReader reader)
    {
        Mode = (GameBoyMode)reader.ReadByte();
        m_prepareSpeedSwitch = reader.ReadBool();
        m_vramBank = reader.ReadByte();
        m_wramBank = reader.ReadByte();
        m_bgPaletteIndex = reader.ReadByte();
        m_objPaletteIndex = reader.ReadByte();
        m_bgPaletteAutoIncrement = reader.ReadBool();
        m_objPaletteAutoIncrement = reader.ReadBool();
        m_opri = reader.ReadByte();
        reader.ReadByte(); // reserved
        reader.ReadBytes(m_data);
        reader.ReadBytes(m_bgPaletteData);
        reader.ReadBytes(m_objPaletteData);

        m_bus.Vram?.SetCurrentBank(m_vramBank);
        m_bus.WorkRam?.SetCurrentBank(m_wramBank);
    }
}
