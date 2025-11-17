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
    public const int FrameWidth = 160;
    public const int FrameHeight = 144;

    /// <summary>
    /// Buffer of RGBA values (0x00 - 0xFF) for each pixel in the frame.
    /// </summary>
    private readonly byte[] m_frameBuffer = new byte[FrameWidth * FrameHeight * 4];

    private readonly byte[] m_greyMap = [0xE0, 0xA8, 0x58, 0x10];
    private readonly byte[] m_spriteIndices = new byte[10];
    private readonly bool[] m_spritePixelCoverage = new bool[FrameWidth];
    private readonly ILcd m_lcd;
    private readonly VramDevice m_vram;
    private readonly InterruptDevice m_interruptDevice;
    private readonly OamDevice m_oam;
    private readonly LcdcRegister m_lcdc;
    private readonly StatRegister m_stat;
    private ulong m_tCycles;
    private bool m_lcdOff;
    private int m_spriteCount;
    private int m_windowLine;
    private bool m_windowLineUsedThisScanline;

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

    /// <summary>
    /// Raised whenever a full frame (160x144) has been rendered to the frame buffer.
    /// </summary>
    public event EventHandler<byte[]> FrameRendered;

    public bool BackgroundVisible { get; set; } = true;
    public bool SpritesVisible { get; set; } = true;

    /// <summary>
    /// True when the CPU is allowed to read/write OAM (LCD disabled, HBlank or VBlank).
    /// </summary>
    public bool CanAccessOam =>
        !m_lcdc.LcdEnable ||
        CurrentState == FrameState.HBlank ||
        CurrentState == FrameState.FrameWait;

    private FrameState CurrentState
    {
        get => (FrameState)m_stat.GetMode();
        set => m_stat.SetMode((byte)value, m_lcdc.LcdEnable);
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
        if (!m_lcdOff && !m_lcdc.LcdEnable)
        {
            m_lcdOff = true;

            // Hardware: LY becomes 0, mode 0, timing reset.
            UpdateLineIndex(false);           // LY = 0, windowLine reset.
            CurrentState = FrameState.HBlank; // Mode 0.
            m_tCycles = 0;

            Array.Clear(m_frameBuffer);
            return; // Stop PPU while LCD is off.
        }

        // Turn on the display?
        if (m_lcdOff && m_lcdc.LcdEnable)
        {
            m_lcdOff = false;

            // Start a fresh frame from LY=0, mode 2, dot=0.
            // LY is already 0 from the disable, so don't bump it again.
            m_tCycles = 0;
            CurrentState = FrameState.OAMScan; // Mode 2 at start of scanline 0.
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
                    CaptureVisibleSprites();
                    CurrentState = FrameState.Drawing;
                }
                break;
            
            // Build up a scanline of pixels.
            case FrameState.Drawing:
                if (m_tCycles >= drawStartCycle) // 172-289 T per scanline.
                {
                    m_tCycles -= drawStartCycle;
                    RenderScanline();
                    CurrentState = FrameState.HBlank;
                }
                break;
            
            // Wait until end of scanline.
            case FrameState.HBlank:
                var hblankLen = tCyclesPerScanline - drawStartCycle - 80;
                if (m_tCycles >= hblankLen)
                {
                    m_tCycles -= hblankLen;
                    UpdateLineIndex(true);
                    
                    if (m_lcd.LY == FrameHeight)
                    {
                        CurrentState = FrameState.FrameWait;
                        FrameRendered?.Invoke(this, m_frameBuffer);
                        RaiseInterrupt(InterruptDevice.InterruptType.VBlank);
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
                        UpdateLineIndex(false);
                        CurrentState = FrameState.OAMScan;
                    }
                    else
                    {
                        // Not at the bottom of the frame - Continue the current one.
                        UpdateLineIndex(true);
                    }
                }
                break;
            
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void CaptureVisibleSprites()
    {
        m_spriteCount = 0;
        Array.Clear(m_spritePixelCoverage);
        if (!m_lcdc.SpriteEnable || !SpritesVisible)
            return; // No sprite drawing required.
        
        // Capture up to 10 sprites for this LY in OAM order.
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

        // Sort sprite indices based on their X position (smallest first).
        for (var i = 1; i < m_spriteCount; i++)
        {
            var key = m_spriteIndices[i];
            var keyX = sprites[key].X;
            var j = i - 1;

            while (j >= 0 && sprites[m_spriteIndices[j]].X > keyX)
            {
                m_spriteIndices[j + 1] = m_spriteIndices[j];
                j--;
            }

            m_spriteIndices[j + 1] = key;
        }

        // Build a lookup table of which screen X positions have sprite coverage.
        for (var i = 0; i < m_spriteCount; i++)
        {
            var spriteX = sprites[m_spriteIndices[i]].X - 8;
            for (var x = Math.Max(0, spriteX); x <= Math.Min(159, spriteX + 7); x++)
                m_spritePixelCoverage[x] = true;
        }
    }

    private void RenderScanline()
    {
        m_windowLineUsedThisScanline = false;
        
        var lcdLy = m_lcd.LY;
        if (lcdLy >= FrameHeight)
            return; 
        
        var lcdSCY = m_lcd.SCY;
        var lcdSCX = m_lcd.SCX;
        var lcdBGP = m_lcd.BGP;
        var lcdWX = m_lcd.WX;

        var bgEnabled = m_lcdc.BgWindowEnablePriority && BackgroundVisible;
        var windowEnabled =
            bgEnabled &&
            m_lcdc.WindowEnable &&
            lcdLy >= m_lcd.WY;
        
        var bgWindowTileDataArea = m_lcdc.BgWindowTileDataArea;
        var windowTileMapArea = m_lcdc.WindowTileMapArea;
        var bgTileMapArea = m_lcdc.BgTileMapArea;
        
        var sprites = m_oam.GetSprites();
        var spriteHeight = m_lcdc.SpriteSize;
        
        for (var x = 0; x < FrameWidth; x++)
        {
            // First we draw the background.
            byte bgColorIndex = 0x00;
            if (bgEnabled)
            {
                var isWindow = windowEnabled && x >= lcdWX - 7;

                int srcY;
                int srcX;
                bool tileMapSelector;
                if (isWindow)
                {
                    srcY = m_windowLine;
                    srcX = x - (lcdWX - 7);
                    m_windowLineUsedThisScanline = true;
                    tileMapSelector = windowTileMapArea;
                }
                else
                {
                    srcY = (lcdSCY + lcdLy) & 0xFF;
                    srcX = (lcdSCX + x) & 0xFF;
                    tileMapSelector = bgTileMapArea;
                }
                            
                // srcX, srcY are the tile coordinates.
                // We need to convert these to the tile offset into the 32x32 tile map.
                var tileColumn = srcX / 8;
                var tileRow = srcY / 8;
                var tileOffset = tileColumn + tileRow * 32;

                // Get the tile index (Window or background).
                var bgTileMapAddr = tileMapSelector ? 0x9C00 : 0x9800;
                var tileIndex = m_vram.Read8((ushort)(bgTileMapAddr + tileOffset));

                // Get the start of the tile data. (One tile = (8 x 2) * 8 = 16 bytes)
                int tileDataAddr;
                if (bgWindowTileDataArea)
                    tileDataAddr = 0x8000 + tileIndex * 16;
                else
                    tileDataAddr = 0x9000 + (sbyte)tileIndex * 16;

                // Offset into the correct tile Y.
                tileDataAddr += srcY % 8 * 2;

                // Read the 8 pixel tile row.
                var lowBits = m_vram.Read8((ushort)tileDataAddr);
                var highBits = m_vram.Read8((ushort)(tileDataAddr + 1));

                // Shift bits to the correct position for our screen X.
                lowBits = (byte)((lowBits >> (7 - srcX % 8)) & 0x01);
                highBits = (byte)((highBits >> (7 - srcX % 8)) & 0x01);

                // Get the 2-bit color index.
                bgColorIndex = (byte)(highBits << 1 | lowBits);
            }

            // Now we draw the sprites.
            byte spriteColorIndex = 0x00;
            byte spritePalette = 0x00;
            if (SpritesVisible && m_spritePixelCoverage[x])
            {
                for (var i = 0; i < m_spriteCount && spriteColorIndex == 0x00; i++)
                {
                    var sprite = sprites[m_spriteIndices[i]];
                    var spriteX = sprite.X - 8;
                    if (spriteX > x || spriteX + 7 < x)
                        continue; // Sprite doesn't cover this X position.

                    if (sprite.Priority)
                    {
                        // Sprite is drawn behind non-transparent background, so only plot if background is transparent.
                        var isBackgroundOpaque = bgColorIndex != 0x00;
                        if (isBackgroundOpaque)
                            continue; // Favor the background pixel.
                    }

                    // Find the sprite tile address.
                    var tileDataAddr = 0x8000;
                    if (spriteHeight == 8)
                    {
                        // Normal tile index.
                        tileDataAddr += sprite.Tile * 16;
                    }
                    else
                    {
                        // Find the first address of the two 8x8 tiles.
                        tileDataAddr += (sprite.Tile & 0xFE) * 16;
                    }

                    // Offset into the correct tile Y.
                    var spriteTop = sprite.Y - 16;
                    var y = lcdLy - spriteTop;
                    if (sprite.YFlip)
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
                    if (sprite.XFlip)
                    {
                        lowBits = lowBits.Mirror();
                        highBits = highBits.Mirror();
                    }

                    // Shift bits to the correct position for our screen X.
                    var pixelOffsetX = x - spriteX;
                    var bitShift = 7 - pixelOffsetX;
                    lowBits = (byte)((lowBits >> bitShift) & 0x01);
                    highBits = (byte)((highBits >> bitShift) & 0x01);

                    // Get the 2-bit color index.
                    var colorIndex = (byte)(highBits << 1 | lowBits);
                    if (colorIndex == 0x00)
                        continue; // Skip transparent pixels.

                    spritePalette = sprite.UseObp1 ? m_lcd.OBP1 : m_lcd.OBP0;
                    spriteColorIndex = colorIndex;
                }
            }

            // Update the frame buffer.
            byte colorValue;
            if (spriteColorIndex != 0x00)
            {
                // Sprite is drawn, so use the sprite palette.
                colorValue = (byte)((spritePalette >> (2 * spriteColorIndex)) & 0x03);
            }
            else
            {
                // Background (including color 0) uses the background palette.
                colorValue = (byte)((lcdBGP >> (2 * bgColorIndex)) & 0x03);
            }

            var frameOffset = (lcdLy * FrameWidth + x) * 4;
            m_frameBuffer[frameOffset] = m_greyMap[colorValue];     // R
            m_frameBuffer[frameOffset + 1] = m_greyMap[colorValue]; // G
            m_frameBuffer[frameOffset + 2] = m_greyMap[colorValue]; // B
            m_frameBuffer[frameOffset + 3] = 0xFF;                  // A
        }
    }

    private void UpdateLineIndex(bool inc)
    {
        if (inc)
        {
            if (m_windowLineUsedThisScanline)
            {
                m_windowLine++;
                m_windowLineUsedThisScanline = false;
            }
            m_lcd.LY++;
        }
        else
        {
            // Reset the values.
            m_lcd.LY = 0;
            m_windowLine = 0;
            m_windowLineUsedThisScanline = false;
        }
        
        UpdateLycAndMaybeStatIrq();
    }

    public void ResetLyCounter() =>
        UpdateLineIndex(false);

    /// <summary>
    /// Compare LY vs LYC; set STAT.coincidence + IF STAT if enabled.
    /// </summary>
    private void UpdateLycAndMaybeStatIrq()
    {
        var coincidence = m_lcd.LY == m_lcd.LYC;
        m_stat.SetCoincidenceFlag(coincidence);

        // Coincidence interrupt is edge-triggered: only when it becomes true.
        if (coincidence && m_stat.CoincidenceInterruptEnabled)
            RaiseInterrupt(InterruptDevice.InterruptType.Stat);
    }

    private void RaiseInterrupt(InterruptDevice.InterruptType type)
    {
        if (m_lcdc.LcdEnable)
            m_interruptDevice.Raise(type);
    }

    /// <summary>
    /// Dump the frame buffer to disk (.tga)
    /// </summary>
    public void Dump(FileInfo tgaFile) =>
        TgaWriter.Write(tgaFile, m_frameBuffer, FrameWidth, FrameHeight, 4);

    /// <summary>
    /// Export all tiles from 0x8000-0x97FF (384 tiles) as a 16x24 tile map.
    /// </summary>
    public void DumpTileMap(FileInfo tgaFile)
    {
        if (tgaFile == null)
            throw new ArgumentNullException(nameof(tgaFile));

        const int tilesPerRow = 16;
        const int tileRows = 24;
        const int tileWidth = 8;
        const int tileHeight = 8;
        const int tileBytes = 16;
        const int tileDataStart = 0x8000;
        const int totalTiles = tilesPerRow * tileRows;
        var width = tilesPerRow * tileWidth;
        var height = tileRows * tileHeight;
        var buffer = new byte[width * height];

        for (var tile = 0; tile < totalTiles; tile++)
        {
            var tileX = (tile % tilesPerRow) * tileWidth;
            var tileY = (tile / tilesPerRow) * tileHeight;
            var tileAddr = tileDataStart + tile * tileBytes;

            for (var row = 0; row < tileHeight; row++)
            {
                var rowAddr = tileAddr + row * 2;
                var lowBits = m_vram.Read8((ushort)rowAddr);
                var highBits = m_vram.Read8((ushort)(rowAddr + 1));

                for (var col = 0; col < tileWidth; col++)
                {
                    var shift = 7 - col;
                    var colorIndex = (byte)(((highBits >> shift) & 0x01) << 1 | ((lowBits >> shift) & 0x01));
                    var destX = tileX + col;
                    var destY = tileY + row;
                    buffer[destY * width + destX] = m_greyMap[colorIndex];
                }
            }
        }

        TgaWriter.Write(tgaFile, buffer, width, height, 1);
    }

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
        public bool BgWindowTileDataArea => m_lcd.LCDC.IsBitSet(4);

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
        public bool CoincidenceInterruptEnabled => m_lcd.STAT.IsBitSet(6);

        /// <summary>
        /// Bit 5 - Mode 2 OAM Interrupt (0=Off, 1=On)
        /// </summary>
        private bool OamInterruptEnabled => m_lcd.STAT.IsBitSet(5);

        /// <summary>
        /// Bit 4 - Mode 1 V-Blank Interrupt (0=Off, 1=On)
        /// </summary>
        private bool VBlankInterruptEnabled => m_lcd.STAT.IsBitSet(4);

        /// <summary>
        /// Bit 3 - Mode 0 H-Blank Interrupt (0=Off, 1=On)
        /// </summary>
        private bool HBlankInterruptEnabled => m_lcd.STAT.IsBitSet(3);
        
        /// <summary>
        /// Bit 2 - LYC==LY Coincidence Flag (0=Different, 1=Equal)
        /// </summary>
        public void SetCoincidenceFlag(bool value) => m_lcd.STAT = value ? m_lcd.STAT.SetBit(2) : m_lcd.STAT.ResetBit(2);

        /// <summary>
        /// Bits 1-0 - Mode Flag (0-3)
        /// </summary>
        public byte GetMode() => (byte) (m_lcd.STAT & 0x03);

        /// <summary>
        /// Bits 1-0 - Mode Flag (0-3)
        /// </summary>
        public void SetMode(byte value, bool isLcdEnabled)
        {
            var newValue = (byte) (m_lcd.STAT & 0xFC | (value & 0x03));
            if (m_lcd.STAT == newValue)
                return;
            m_lcd.STAT = newValue;

            if ((value == (int) FrameState.OAMScan && OamInterruptEnabled) ||
                (value == (int) FrameState.HBlank && HBlankInterruptEnabled) ||
                (value == (int) FrameState.FrameWait && VBlankInterruptEnabled))
            {
                if (isLcdEnabled)
                    m_interruptDevice.Raise(InterruptDevice.InterruptType.Stat);
            }
        }
    }
}
