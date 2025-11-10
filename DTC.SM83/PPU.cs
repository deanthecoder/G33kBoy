// Code authored by Dean Edis (DeanTheCoder).
// Anyone is free to copy, modify, use, compile, or distribute this software,
// either in source code form or as a compiled binary, for any purpose.
// 
// If you modify the code, please retain this copyright header,
// and consider contributing back to the repository or letting us know
// about your modifications. Your contributions are valued!
// 
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.
using DTC.Core.Extensions;
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
    /// Holds pixel color data for a single scanline.
    /// </summary>
    /// <remarks>
    /// The lower two bits hold the color value (I.e. The actual color value found by taking a color index and looking-up
    /// the color from the active palette.)
    /// This will feed into the frame buffer when the scanline is complete, converted to (0x00 - 0xFF) greyscale value.
    /// If the high bit (7) is set it means the pixel is opaque. 
    /// </remarks>
    private readonly byte[] m_scanlineBuffer = new byte[160];

    /// <summary>
    /// Buffer of grey values (0x00 - 0xFF) for each pixel in the frame.
    /// </summary>
    private readonly byte[] m_frameBuffer = new byte[160 * 144];

    private readonly byte[] m_greyMap = [0xE0, 0xA8, 0x58, 0x10];
    private readonly byte[] m_spriteIndices = new byte[10];
    private readonly ILcd m_lcd;
    private readonly VramDevice m_vram;
    private readonly InterruptDevice m_interruptDevice;
    private readonly OamDevice m_oam;
    private readonly LcdcRegister m_lcdc;
    private readonly StatRegister m_stat;
    private ulong m_tCycles;
    private bool m_lcdOff;
    private int m_spriteCount;

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

    public PPU(ILcd lcd, VramDevice vram, InterruptDevice interruptDevice, [NotNull] OamDevice oam)
    {
        m_lcd = lcd ?? throw new ArgumentNullException(nameof(lcd));
        m_vram = vram ?? throw new ArgumentNullException(nameof(vram));
        m_interruptDevice = interruptDevice ?? throw new ArgumentNullException(nameof(interruptDevice));
        m_oam = oam ?? throw new ArgumentNullException(nameof(oam));

        m_lcdc = new LcdcRegister(lcd);
        m_stat = new StatRegister(lcd, interruptDevice);
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
        const ulong drawStartCycle = 172;
        switch (CurrentState)
        {
            // Capture up to 10 sprites.
            case FrameState.OAMScan:
                if (m_tCycles >= 80)
                {
                    m_tCycles -= 80;
                    
                    // Capture up to 10 sprites for this LY in OAM order.
                    m_spriteCount = 0;
                    var sprites = m_oam.GetSprites();
                    for (byte i = 0; i < 40 && m_spriteCount < 10; i++)
                    {
                        var sprite = sprites[i];
                        var spriteY = sprite.Y - 16;
                        if (spriteY > m_lcd.LY || spriteY + m_lcdc.SpriteSize - 1 < m_lcd.LY)
                            continue; // Sprite not visible on this scanline.
                        
                        // Sprite is visible on this scanline.
                        m_spriteIndices[m_spriteCount++] = i;
                    }
                    
                    CurrentState = FrameState.Drawing;
                }
                break;
            
            // Build up a scanline of pixels.
            case FrameState.Drawing:
                if (m_tCycles >= drawStartCycle) // 172-289 T per scanline.
                {
                    m_tCycles -= drawStartCycle;

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
                            
                            // Shift bits to the correct position for our screen X.
                            lowBits = (byte)((lowBits >> (7 - srcX % 8)) & 0x01);
                            highBits = (byte)((highBits >> (7 - srcX % 8)) & 0x01);
                            
                            // Get the 2-bit palette index.
                            var paletteIndex = (byte)(highBits << 1 | lowBits);
                            var isTransparent = paletteIndex == 0x00;
                            
                            // Look up the 2-bit color value.
                            var palette = m_lcd.BGP;
                            var colorValue = (byte)((palette >> (2 * paletteIndex)) & 0x03);
                            
                            // Encode the transparency.
                            if (!isTransparent)
                                colorValue |= 0x80;  // Opaque.
                            
                            m_scanlineBuffer[screenX] = colorValue;
                        }
                    }
                    else
                    {
                        // Background off - Fill white.
                        Array.Clear(m_scanlineBuffer);
                    }

                    if (m_lcdc.SpriteEnable)
                    {
                        // Now we draw the sprites.
                        for (var x = 0; x < 160; x++)
                        {
                            // Find the first sprite that starts on this X position.
                            OamDevice.OamEntry? sprite = null;
                            for (var i = 0; i < m_spriteCount; i++)
                            {
                                var oamEntry = m_oam.GetSprites()[m_spriteIndices[i]];
                                var spriteX = oamEntry.X - 8;
                                if (spriteX > x || spriteX + 8 <= x)
                                    continue; // Sprite doesn't cover this X position.

                                // Get in - We found one!
                                sprite = oamEntry;
                                break;
                            }

                            if (sprite == null)
                                continue; // No sprites at this position.

                            // Draw the sprite.
                            var spriteLeft = sprite.Value.X - 8;
                            var spriteTop = sprite.Value.Y - 16;
                            var pixelOffsetX = x - spriteLeft;

                            int tileDataAddr;
                            if (m_lcdc.SpriteSize == 8)
                            {
                                // Normal tile index.
                                tileDataAddr = 0x8000 + sprite.Value.Tile * 16;
                            }
                            else
                            {
                                // Find the first address of the two 8x8 tiles.
                                tileDataAddr = 0x8000 + (sprite.Value.Tile & 0xFE) * 16;
                            }

                            // Offset into the correct tile Y.
                            var spriteHeight = m_lcdc.SpriteSize;
                            var y = m_lcd.LY - spriteTop;
                            if (sprite.Value.YFlip)
                                y = spriteHeight - 1 - y;
                            if (spriteHeight == 16 && y >= 8)
                            {
                                tileDataAddr += 16;
                                y -= 8;
                            }
                            tileDataAddr += y * 2;

                            // Read the 8 pixel tile row.
                            var lowBits = m_vram.Read8((ushort)tileDataAddr);
                            var highBits = m_vram.Read8((ushort)(tileDataAddr + 1));

                            // Mirror on X if required.
                            if (sprite.Value.XFlip)
                            {
                                lowBits = lowBits.Mirror();
                                highBits = highBits.Mirror();
                            }

                            // Shift bits to the correct position for our screen X.
                            var bitShift = 7 - pixelOffsetX;
                            lowBits = (byte)((lowBits >> bitShift) & 0x01);
                            highBits = (byte)((highBits >> bitShift) & 0x01);

                            // Get the 2-bit palette index.
                            var paletteIndex = (byte)(highBits << 1 | lowBits);
                            var isTransparent = paletteIndex == 0x00;
                            
                            if (isTransparent)
                                continue; // Skip transparent pixels.

                            if (sprite.Value.Priority)
                            {
                                // Sprite is drawn behind non-transparent background., so only plot if background is transparent.
                                var isBackgroundOpaque = m_scanlineBuffer[x].IsBitSet(7);
                                if (isBackgroundOpaque)
                                    continue; // Favour the background pixel.
                            }
                            
                            // Look up the 2-bit color value.
                            var palette = sprite.Value.UseObp1 ? m_lcd.OBP1 : m_lcd.OBP0;
                            var colorValue = (byte)((palette >> (2 * paletteIndex)) & 0x03);

                            // Encode the transparency.
                            colorValue |= 0x80;  // Opaque.

                            m_scanlineBuffer[x] = colorValue;
                        }
                    }

                    // Update the frame buffer.
                    for (var i = 0; i < m_scanlineBuffer.Length; i++)
                        m_frameBuffer[m_lcd.LY * 160 + i] = m_greyMap[m_scanlineBuffer[i] & 0x0F];

                    CurrentState = FrameState.HBlank;
                    // todo - Overlay sprites captured in Mode 2 (respect OBP0/OBP1, priority vs BG, and 8x16 sprite pairing rules).
                }
                break;
            
            // Wait until end of scanline.
            case FrameState.HBlank:
                var hblankLen = tCyclesPerScanline - drawStartCycle - 80;
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
        public bool LcdEnable => m_lcd.LCDC.IsBitSet(7);

        /// <summary>
        /// Bit 6 - Window Tile Map Area (0=9800-9BFF, 1=9C00-9FFF)
        /// </summary>
        public bool WindowTileMapArea => m_lcd.LCDC.IsBitSet(6);

        /// <summary>
        /// Bit 5 - Window Enable (0=Off, 1=On)
        /// </summary>
        public bool WindowEnable => m_lcd.LCDC.IsBitSet(5);

        /// <summary>
        /// Bit 4 - BG/Window Tile Data Area (0=8800-97FF, 1=8000-8FFF)
        /// </summary>
        public bool BgWindowTileDataArea => m_lcd.LCDC.IsBitSet(4); // todo - Make the BgWindowTileDataArea property return the address? And BgTileMapArea.

        /// <summary>
        /// Bit 3 - BG Tile Map Area (0=9800-9BFF, 1=9C00-9FFF)
        /// </summary>
        public bool BgTileMapArea => m_lcd.LCDC.IsBitSet(3);

        /// <summary>
        /// Bit 2 - Sprite Size (0=8x8, 1=8x16)
        /// </summary>
        public int SpriteSize => m_lcd.LCDC.IsBitSet(2) ? 16 : 8;

        /// <summary>
        /// Bit 1 - Sprite Enable (0=Off, 1=On)
        /// </summary>
        public bool SpriteEnable => m_lcd.LCDC.IsBitSet(1);

        /// <summary>
        /// Bit 0 - BG/Window Enable/Priority (0=Off, 1=On)
        /// </summary>
        public bool BgWindowEnablePriority => m_lcd.LCDC.IsBitSet(0);
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
