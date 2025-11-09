// Code authored by Dean Edis (DeanTheCoder).
// Anyone is free to copy, modify, use, compile, or distribute this software,
// either in source code form or as a compiled binary, for any purpose.
// 
// If you modify the code, please retain this copyright header,
// and consider contributing back to the repository or letting us know
// about your modifications. Your contributions are valued!
// 
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.
using DTC.Core.Image;
using DTC.SM83.Devices;
using JetBrains.Annotations;

namespace DTC.SM83;

/// <summary>
/// The Pixel Processing Unit.
/// </summary>
public class PPU
{
    /// <summary>
    /// Buffer of grey values (0x00 - 0xFF) for each pixel in the frame.
    /// </summary>
    private readonly byte[] m_frameBuffer = new byte[160 * 144];

    private readonly byte[] m_greyMap = [0xE0, 0xA8, 0x58, 0x10];
    private readonly ILcd m_lcd;
    private readonly VramDevice m_vram;
    private readonly InterruptDevice m_interruptDevice;
    private readonly LcdcRegister m_lcdc;
    private readonly StatRegister m_stat;
    private ulong m_tCycles;
    private bool m_lcdOff;

    private enum FrameState
    {
        /// <summary>
        /// Mode 0 (87-204 T) - Wait until end of scanline.
        /// </summary>
        HBlank,

        /// <summary>
        /// Mode 1 (...until 70224 T) - Wait until end of frame.
        /// </summary>
        FrameWait,

        /// <summary>
        /// Mode 2 (80 T) - Searches OAM for sprites to draw.
        /// </summary>
        OAMScan,
    
        /// <summary>
        /// Mode 3 (172-289 T) - Send pixels to LCD.
        /// </summary>
        Drawing
    }

    private FrameState CurrentState
    {
        get => (FrameState)m_stat.Mode;
        set => m_stat.Mode = (byte)value;
    }

    public PPU(ILcd lcd, VramDevice vram, InterruptDevice interruptDevice)
    {
        m_lcd = lcd ?? throw new ArgumentNullException(nameof(lcd));
        m_vram = vram ?? throw new ArgumentNullException(nameof(vram));
        m_interruptDevice = interruptDevice ?? throw new ArgumentNullException(nameof(interruptDevice));

        m_lcdc = new LcdcRegister(lcd);
        m_stat = new StatRegister(lcd, interruptDevice);;
    }
    
    /// <summary>
    /// Advances the timer by the specified T-cycles.
    /// </summary>
    /// <remarks>
    /// Call with every elapsed T-cycle chunk to keep timing correct.
    /// </remarks>
    public void AdvanceT(ulong tCycles)
    {
        // Turn off the display?
        if (!m_lcdc.LcdEnable)
        {
            SetLY(0);
            CurrentState = FrameState.HBlank;
            m_tCycles = 0;
            m_lcdOff = true;
            return;
        }
        
        // Turn on the display?
        if (m_lcdOff && m_lcdc.LcdEnable)
        {
            m_lcdOff = false;
            SetLY(0);
            CurrentState = FrameState.HBlank;
        }
        
        m_tCycles += tCycles;

        const int tCyclesPerScanline = 456;
        ulong DrawStartCycle = 172;
        switch (CurrentState)
        {
            // Capture up to 10 sprites.
            case FrameState.OAMScan:
                if (m_tCycles >= 80)
                {
                    m_tCycles -= 80;
                    // todo - Capture up to 10 sprites for this LY in OAM order (no allocation).
                    // todo - Support 8x16 sprites: treat tile index as even (ignore bit 0), pick top/bottom tile based on (LY - (Y-16)) >= 8, and apply Y/X flip correctly.
                    CurrentState = FrameState.Drawing;
                }
                break;
            
            // Build up a scanline of pixels.
            case FrameState.Drawing:
                if (m_tCycles >= DrawStartCycle) // 172-289 T per scanline.
                {
                    m_tCycles -= DrawStartCycle;

                    var bgEnabled = m_lcdc.BgWindowEnablePriority;
                    if (bgEnabled)
                    {
                        var srcY = (m_lcd.SCY + m_lcd.LY) & 0xFF;

                        for (var screenX = 0; screenX < 160; screenX++)
                        {
                            var srcX = (m_lcd.SCX + screenX) & 0xFF;
                            
                            // srcX, srcY are the tile coordinates.
                            // We need to convert these to the tile offset into the 32x32 tile map.
                            var tileColumn = srcX / 8;
                            var tileRow = srcY / 8;
                            var tileOffset = tileColumn + tileRow * 32;
                            
                            // Get the tile index.
                            var bgTileMapAddr = m_lcdc.BgTileMapArea ? 0x9C00 : 0x9800; // todo - Pull out opf loop (and others)
                            var tileIndex = m_vram.Read8((ushort)(bgTileMapAddr + tileOffset));
                            
                            // Get the start of the tile data. (One tile = (8 x 2) * 8 = 16 bytes)
                            int tileDataAddr;
                            if (m_lcdc.BgWindowTileDataArea)
                                tileDataAddr = 0x8000 + tileIndex * 16;
                            else
                                tileDataAddr = 0x9000 + (sbyte)tileIndex * 16;
                            
                            // Offset into the correct tile Y.
                            tileDataAddr += (srcY % 8) * 2;
                            
                            // Read the 8 pixel tile row.
                            var lowBits = m_vram.Read8((ushort)tileDataAddr);
                            var highBits = m_vram.Read8((ushort)(tileDataAddr + 1));
                            
                            // todo - mirror on X if required.
                            
                            // Shift bits to the correct position for our screen X.
                            lowBits = (byte)((lowBits >> (7 - srcX % 8)) & 0x01);
                            highBits = (byte)((highBits >> (7 - srcX % 8)) & 0x01);
                            
                            // Get the 2 bit palette index.
                            var paletteIndex = (byte)(highBits << 1 | lowBits);
                            
                            // Map the palette index to a greyscale value.
                            var paletteValue = (m_lcd.BGP >> (2 * paletteIndex)) & 0x03;
                            
                            // Update the frame buffer.
                            m_frameBuffer[m_lcd.LY * 160 + screenX] = m_greyMap[paletteValue];
                        }
                    }
                    else
                    {
                        // Background off - Fill white.
                        Array.Fill(m_frameBuffer, (byte)0xFF, m_lcd.LY * 160, 160);;
                    }

                    CurrentState = FrameState.HBlank;
                    // todo - Overlay sprites captured in Mode 2 (respect OBP0/OBP1, priority vs BG, and 8x16 sprite pairing rules).
                }
                break;
            
            // Wait until end of scanline.
            case FrameState.HBlank:
                var hblankLen = tCyclesPerScanline - DrawStartCycle - 80;
                if (m_tCycles >= hblankLen)
                {
                    m_tCycles -= hblankLen;
                    SetLY((byte)(m_lcd.LY + 1));
                    
                    if (m_lcd.LY == 144)
                    {
                        CurrentState = FrameState.FrameWait;
                        m_interruptDevice.Raise(InterruptDevice.InterruptType.VBlank);
                    }
                    else
                    {
                        CurrentState = FrameState.OAMScan;
                    }
                }
                break;
            
            // Wait until end of frame.
            case FrameState.FrameWait:
                if (m_tCycles >= tCyclesPerScanline)
                {
                    // We're at the end of a hidden scanline.
                    m_tCycles -= tCyclesPerScanline;
                    if (m_lcd.LY + 1 == 154)
                    {
                        // Reached the bottom of the frame - Start a new one.
                        SetLY(0);
                        CurrentState = FrameState.OAMScan;
                    }
                    else
                    {
                        // Not at the bottom of the frame - Continue the current one.
                        SetLY((byte)(m_lcd.LY + 1));
                    }
                }
                break;
            
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    private void SetLY(byte newLy)
    {
        if (newLy == m_lcd.LY)
            return;
        m_lcd.LY = newLy;
        UpdateLycAndMaybeStatIrq();
    }

    /// <summary>
    /// Compare LY vs LYC; set STAT.coincidence + IF STAT if enabled.
    /// </summary>
    private void UpdateLycAndMaybeStatIrq()
    {
        var coincidence = m_lcd.LY == m_lcd.LYC;
        m_stat.CoincidenceFlag = coincidence;

        if (coincidence && m_stat.CoincidenceInterruptEnabled)
            m_interruptDevice.Raise(InterruptDevice.InterruptType.Stat);
    }
    
    /// <summary>
    /// Dump the frame buffer to disk (.ppm)
    /// </summary>
    public void Dump(FileInfo ppmFile) =>
        PpmWriter.Write(ppmFile, m_frameBuffer, 160, 144, 1);

    /// <summary>
    /// Represents the LCDC (LCD Control) register at 0xFF40.
    /// Provides convenient access to individual control bits.
    /// </summary>
    private readonly struct LcdcRegister
    {
        private readonly ILcd m_lcd;
        
        public LcdcRegister(ILcd lcd)
        {
            m_lcd = lcd ?? throw new ArgumentNullException(nameof(lcd));
        }

        /// <summary>
        /// Bit 7 - LCD Enable (0=Off, 1=On)
        /// </summary>
        public bool LcdEnable => (m_lcd.LCDC & 0x80) != 0;

        /// <summary>
        /// Bit 6 - Window Tile Map Area (0=9800-9BFF, 1=9C00-9FFF)
        /// </summary>
        public bool WindowTileMapArea => (m_lcd.LCDC & 0x40) != 0;

        /// <summary>
        /// Bit 5 - Window Enable (0=Off, 1=On)
        /// </summary>
        public bool WindowEnable => (m_lcd.LCDC & 0x20) != 0;

        /// <summary>
        /// Bit 4 - BG/Window Tile Data Area (0=8800-97FF, 1=8000-8FFF)
        /// </summary>
        public bool BgWindowTileDataArea => (m_lcd.LCDC & 0x10) != 0; // todo - Make the BgWindowTileDataArea property return the address? And BgTileMapArea.

        /// <summary>
        /// Bit 3 - BG Tile Map Area (0=9800-9BFF, 1=9C00-9FFF)
        /// </summary>
        public bool BgTileMapArea => (m_lcd.LCDC & 0x08) != 0;

        // todo - Wire this into OAM scan/render: when true, sprites are 8x16 and use paired tiles with even tile indices.
        /// <summary>
        /// Bit 2 - Sprite Size (0=8x8, 1=8x16)
        /// </summary>
        public bool SpriteSize => (m_lcd.LCDC & 0x04) != 0;

        /// <summary>
        /// Bit 1 - Sprite Enable (0=Off, 1=On)
        /// </summary>
        public bool SpriteEnable => (m_lcd.LCDC & 0x02) != 0;

        /// <summary>
        /// Bit 0 - BG/Window Enable/Priority (0=Off, 1=On)
        /// </summary>
        public bool BgWindowEnablePriority => (m_lcd.LCDC & 0x01) != 0;
    }

    /// <summary>
    /// Represents the STAT (LCD Status) register at 0xFF41.
    /// Provides convenient access to individual status bits.
    /// </summary>
    private readonly struct StatRegister
    {
        private readonly ILcd m_lcd;
        private readonly InterruptDevice m_interruptDevice;

        public StatRegister(ILcd lcd, [NotNull] InterruptDevice interruptDevice)
        {
            m_lcd = lcd ?? throw new ArgumentNullException(nameof(lcd));
            m_interruptDevice = interruptDevice ?? throw new ArgumentNullException(nameof(interruptDevice));
        }

        /// <summary>
        /// Bit 6 - LYC=LY Coincidence Interrupt (0=Off, 1=On)
        /// </summary>
        public bool CoincidenceInterruptEnabled => (m_lcd.STAT & 0x40) != 0;

        /// <summary>
        /// Bit 5 - Mode 2 OAM Interrupt (0=Off, 1=On)
        /// </summary>
        public bool OamInterruptEnabled => (m_lcd.STAT & 0x20) != 0;

        /// <summary>
        /// Bit 4 - Mode 1 V-Blank Interrupt (0=Off, 1=On)
        /// </summary>
        public bool VBlankInterruptEnabled => (m_lcd.STAT & 0x10) != 0;

        /// <summary>
        /// Bit 3 - Mode 0 H-Blank Interrupt (0=Off, 1=On)
        /// </summary>
        public bool HBlankInterruptEnabled => (m_lcd.STAT & 0x08) != 0;

        /// <summary>
        /// Bit 2 - LYC==LY Coincidence Flag (0=Different, 1=Equal)
        /// </summary>
        public bool CoincidenceFlag
        {
            get => (m_lcd.STAT & 0x04) != 0;
            set => m_lcd.STAT = (byte)(m_lcd.STAT & 0xF3 | (value ? 0x04 : 0x00));
        }

        /// <summary>
        /// Bits 1-0 - Mode Flag (0-3)
        /// </summary>
        public byte Mode
        {
            get => (byte)(m_lcd.STAT & 0x03);
            set
            {
                var newValue = (byte)(m_lcd.STAT & 0xFC | (value & 0x03));
                if (m_lcd.STAT == newValue)
                    return;
                m_lcd.STAT = newValue;

                if ((value == (int)FrameState.OAMScan && OamInterruptEnabled) ||
                    (value == (int)FrameState.HBlank && HBlankInterruptEnabled) ||
                    (value == (int)FrameState.FrameWait && VBlankInterruptEnabled))
                {
                    m_interruptDevice.Raise(InterruptDevice.InterruptType.Stat);
                }
            }
        }
    }
}