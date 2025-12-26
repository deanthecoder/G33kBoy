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
using System.Diagnostics;

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
    private readonly byte[] m_greenMap =
    [
        0x81, 0x7D, 0x15,
        0x63, 0x74, 0x3F,
        0x45, 0x5D, 0x4E,
        0x28, 0x32, 0x23
    ];
    private readonly double[] m_colorAccumulator = new double[FrameWidth * FrameHeight * 3];
    private readonly byte[] m_spriteIndices = new byte[10];
    private readonly bool[] m_spritePixelCoverage = new bool[FrameWidth];
    private readonly ILcd m_lcd;
    private readonly VramDevice m_vram;
    private readonly InterruptDevice m_interruptDevice;
    private readonly OamDevice m_oam;
    private readonly LcdcRegister m_lcdc;
    private readonly StatRegister m_stat;
    private const int TicksPerScanline = 456;
    private const int OamCycles = 80;
    private const int Mode3Cycles = 172;
    private const int HBlankCycles = TicksPerScanline - OamCycles - Mode3Cycles; // 204
    // Blargg oam_bug/1-lcd_sync expects LY to increment ~110 M-cycles after LCD is enabled from off.
    // That matches the first scanline starting a few M-cycles "late" (i.e. the scanline counter is already part-way through).
    private const int LcdEnableDelayCycles = 0;
    private const int LcdEnableScanlineOffsetCycles = 6; // T-cycles (1.5 M-cycles)
    private ulong m_tCycles;
    private ulong m_stateCycles;
    private bool m_hblankEndsScanline = true;
    private ulong m_lcdStartDelay;
    private bool m_statInterruptsEnabled = true;
    private bool m_line153Wrapped;
    private bool m_lcdOff;
    private int m_spriteCount;
    private int m_windowLine;
    private bool m_windowLineUsedThisScanline;
    private bool m_motionBlurEnabled;
    private bool m_motionBlurPrimed;
    private bool m_lcdEmulationEnabled = true;
    private const double MotionBlurOldWeight = 0.6;
    private const double MotionBlurOldWeightDark = 0.3;

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

    public event Action HBlankStarted;

    public bool BackgroundVisible { get; set; } = true;

    public bool SpritesVisible { get; set; } = true;

    public InstructionLogger InstructionLogger { get; set; }

    public GameBoyMode Mode { get; private set; } = GameBoyMode.Dmg;

    public bool LcdEmulationEnabled
    {
        get => m_lcdEmulationEnabled;
        set
        {
            m_lcdEmulationEnabled = value;
            SetMotionBlurEnabled(value);
        }
    }

    private void SetMotionBlurEnabled(bool enabled)
    {
        if (m_motionBlurEnabled == enabled)
            return;
        m_motionBlurEnabled = enabled;
        if (!m_motionBlurEnabled)
            m_motionBlurPrimed = false;
    }

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
        set => m_stat.SetMode((byte)value, m_statInterruptsEnabled && m_lcdc.LcdEnable);
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
        // LCD disabled: hold LY/mode reset until re-enabled.
        if (!m_lcdc.LcdEnable)
        {
            if (!m_lcdOff)
            {
                m_lcdOff = true;
                m_lcdStartDelay = 0;
                m_tCycles = 0;
                m_line153Wrapped = false;

                // Hardware: LY becomes 0, mode 0, timing reset.
                UpdateLineIndex(false);           // LY = 0, windowLine reset.
                EnterHBlank(endsScanline: true, cycles: HBlankCycles); // Mode 0.

                ClearFrameBufferToBaseColor();
            }
            return; // Stop PPU while LCD is off.
        }

        // Turn on the display?
        if (m_lcdOff && m_lcdc.LcdEnable)
        {
            m_lcdOff = false;
            m_lcdStartDelay = LcdEnableDelayCycles;
            m_tCycles = LcdEnableScanlineOffsetCycles;
            m_line153Wrapped = false;
            m_stateCycles = 0;
            m_hblankEndsScanline = true;

            // Start a fresh frame from LY=0.
            // LY is already 0 from the disable, so don't bump it again.
            m_statInterruptsEnabled = false;
            UpdateLineIndex(false);
            EnterOamScan();
            m_statInterruptsEnabled = true;
        }

        var remaining = tCycles;
        while (remaining > 0)
        {
            if (m_lcdStartDelay > 0)
            {
                var step = Math.Min(remaining, m_lcdStartDelay);
                m_lcdStartDelay -= step;
                remaining -= step;

                if (m_lcdStartDelay > 0)
                    continue;

                m_tCycles = 0;
                EnterOamScan();
                continue;
            }

            switch (CurrentState)
            {
                // Capture up to 10 sprites.
                case FrameState.OAMScan:
                {
                    var untilDrawing = m_stateCycles - m_tCycles;
                    var step = Math.Min(remaining, untilDrawing);
                    m_tCycles += step;
                    remaining -= step;

                    if (m_tCycles < m_stateCycles)
                        continue;

                    m_tCycles -= m_stateCycles;
                    CaptureVisibleSprites();
                    EnterDrawing();
                    break;
                }
            
                // Build up a scanline of pixels.
                case FrameState.Drawing:
                {
                    var untilHBlank = m_stateCycles - m_tCycles;
                    var step = Math.Min(remaining, untilHBlank);
                    m_tCycles += step;
                    remaining -= step;

                    if (m_tCycles < m_stateCycles)
                        continue;

                    m_tCycles -= m_stateCycles;
                    RenderScanline();
                    EnterHBlank(endsScanline: true, cycles: HBlankCycles);
                    break;
                }
            
                // Wait until end of scanline.
                case FrameState.HBlank:
                {
                    var untilHBlankEnd = m_stateCycles - m_tCycles;
                    var step = Math.Min(remaining, untilHBlankEnd);
                    m_tCycles += step;
                    remaining -= step;

                    if (m_tCycles < m_stateCycles)
                        continue;

                    m_tCycles -= m_stateCycles;

                    // The "startup" scanline begins in mode 0; at the end of that initial period we enter mode 3
                    // without advancing LY. Normal mode 0 is the end-of-line HBlank.
                    if (!m_hblankEndsScanline)
                    {
                        EnterDrawing();
                        break;
                    }

                    LogScanlineEnd(m_lcd.LY);
                    UpdateLineIndex(true);
                    
                    if (m_lcd.LY == FrameHeight)
                    {
                        InstructionLogger?.Write(() => "PPU entering VBlank");
                        CurrentState = FrameState.FrameWait;
                        FrameRendered?.Invoke(this, m_frameBuffer);
                        m_motionBlurPrimed = m_motionBlurEnabled;
                        RaiseInterrupt(InterruptDevice.InterruptType.VBlank);
                        LogScanlineStart();
                    }
                    else
                    {
                        EnterOamScan();
                    }
                    break;
                }
            
                // Wait until end of frame.
                case FrameState.FrameWait:
                {
                    // LY resets to 0 a few cycles into the final vblank line (LY=153).
                    if (m_lcd.LY == 153 && m_tCycles < 4UL)
                    {
                        var untilReset = Math.Min(remaining, 4UL - m_tCycles);
                        m_tCycles += untilReset;
                        remaining -= untilReset;

                        if (m_tCycles == 4UL)
                        {
                            m_line153Wrapped = true;
                            UpdateLineIndex(false);
                        }

                        if (remaining == 0)
                            break;
                    }

                    var untilLineEnd = TicksPerScanline - m_tCycles;
                    var step = Math.Min(remaining, untilLineEnd);
                    m_tCycles += step;
                    remaining -= step;

                    if (m_tCycles < TicksPerScanline)
                        continue;

                    m_tCycles -= TicksPerScanline;
                    var currentLine = m_line153Wrapped ? 153 : m_lcd.LY;
                    LogScanlineEnd(currentLine);

                    if (m_line153Wrapped)
                    {
                        m_line153Wrapped = false;
                        InstructionLogger?.Write(() => "PPU VBlank end");
                        EnterOamScan(); // Start of line 0.
                    }
                    else
                    {
                        UpdateLineIndex(true);
                        if (m_lcd.LY == 0)
                        {
                            InstructionLogger?.Write(() => "PPU VBlank end");
                            EnterOamScan();
                        }
                        else
                        {
                            LogScanlineStart();
                        }
                    }
                    break;
                }
            
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private void CaptureVisibleSprites()
    {
        m_spriteCount = 0;
        if (!m_lcdc.SpriteEnable || !SpritesVisible)
            return; // No sprite drawing required.

        // Capture up to 10 sprites for this LY in OAM order.
        var isCgb = Mode == GameBoyMode.Cgb;
        var objectPriorityByX = isCgb && m_lcd.OPRI.IsBitSet(0);
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

        // DMG: Sort sprite indices based on their X position (smallest first).
        // CGB: Priority mode is controlled by OPRI bit 0.
        //      0 = OAM order, 1 = X coordinate order (DMG-like).
        if (!isCgb || objectPriorityByX)
        {
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
        }

        // Build a lookup table of which screen X positions have sprite coverage.
        Array.Clear(m_spritePixelCoverage);
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
        var isCgb = Mode == GameBoyMode.Cgb;
        var cgbMasterPriority = isCgb && m_lcd.LCDC.IsBitSet(0);

        // DMG: LCDC.0 disables BG/WIN. CGB: LCDC.0 is master priority, BG/WIN still render.
        var bgEnabled = (isCgb || m_lcdc.BgWindowEnablePriority) && BackgroundVisible;
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
            byte bgPaletteIndex = 0x00;
            bool bgPriority = false;
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
                var tileMapEntryAddr = (ushort)(bgTileMapAddr + tileOffset);
                var tileIndex = isCgb ? m_vram.ReadBanked(tileMapEntryAddr, 0) : m_vram.Read8(tileMapEntryAddr);
                byte tileBank = 0;
                bool xFlip = false;
                bool yFlip = false;
                if (isCgb)
                {
                    var attributes = m_vram.ReadBanked(tileMapEntryAddr, 1);
                    bgPaletteIndex = (byte)(attributes & 0x07);
                    tileBank = (byte)((attributes & 0x08) != 0 ? 1 : 0);
                    xFlip = (attributes & 0x20) != 0;
                    yFlip = (attributes & 0x40) != 0;
                    bgPriority = (attributes & 0x80) != 0;
                }

                // Get the start of the tile data. (One tile = (8 x 2) * 8 = 16 bytes)
                int tileDataAddr;
                if (bgWindowTileDataArea)
                    tileDataAddr = 0x8000 + tileIndex * 16;
                else
                    tileDataAddr = 0x9000 + (sbyte)tileIndex * 16;

                // Sanity check BG tile base address is within the expected pattern table range.
                if (bgWindowTileDataArea)
                {
                    if (tileDataAddr < 0x8000 || tileDataAddr >= 0x9000)
                    {
                        throw new InvalidOperationException(
                            $"BG tile (unsigned) out of pattern range: 0x{tileDataAddr:X4} (index={tileIndex})");
                    }
                }
                else
                {
                    if (tileDataAddr < 0x8800 || tileDataAddr >= 0x9800)
                    {
                        throw new InvalidOperationException(
                            $"BG tile (signed) out of pattern range: 0x{tileDataAddr:X4} (index={tileIndex})");
                    }
                }
                // Offset into the correct tile Y.
                var tileRowOffset = srcY % 8;
                if (yFlip)
                    tileRowOffset = 7 - tileRowOffset;
                tileDataAddr += tileRowOffset * 2;
                ValidateVramRange(tileDataAddr, 2, "BG tile row");

                // Read the 8 pixel tile row.
                var lowBits = isCgb
                    ? m_vram.ReadBanked((ushort)tileDataAddr, tileBank)
                    : m_vram.Read8((ushort)tileDataAddr);
                var highBits = isCgb
                    ? m_vram.ReadBanked((ushort)(tileDataAddr + 1), tileBank)
                    : m_vram.Read8((ushort)(tileDataAddr + 1));

                // Shift bits to the correct position for our screen X.
                var pixelX = srcX % 8;
                if (xFlip)
                    pixelX = 7 - pixelX;
                var bitShift = 7 - pixelX;
                lowBits = (byte)((lowBits >> bitShift) & 0x01);
                highBits = (byte)((highBits >> bitShift) & 0x01);

                // Get the 2-bit color index.
                bgColorIndex = (byte)(highBits << 1 | lowBits);
            }

            // Now we draw the sprites.
            byte spriteColorIndex = 0x00;
            byte spritePalette = 0x00;
            byte spritePaletteIndex = 0x00;
            if (m_spritePixelCoverage[x] && SpritesVisible)
            {
                for (var i = 0; i < m_spriteCount && spriteColorIndex == 0x00; i++)
                {
                    var sprite = sprites[m_spriteIndices[i]];
                    var spriteX = sprite.X - 8;
                    if (spriteX > x || spriteX + 7 < x)
                        continue; // Sprite doesn't cover this X position.

                    var isBackgroundOpaque = bgColorIndex != 0x00;

                    // CGB master priority (LCDC.0): when 0, OBJ always wins over BG/WIN (except color 0 transparency).
                    // When 1 (or in DMG), apply normal priority rules.
                    if (!isCgb || cgbMasterPriority)
                    {
                        if (isCgb && bgPriority && isBackgroundOpaque)
                            continue; // Background tile forces priority.
                        if (sprite.Priority && isBackgroundOpaque)
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

                    // Sanity check sprite tile base address is within the expected pattern table range.
                    if (tileDataAddr < 0x8000 || tileDataAddr >= 0x9000)
                    {
                        throw new InvalidOperationException(
                            $"Sprite tile out of pattern range: 0x{tileDataAddr:X4} (tile={sprite.Tile}, height={spriteHeight})");
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
                    ValidateVramRange(tileDataAddr, 2, "sprite tile row");
                    // Read the 8 pixel tile row.
                    var spriteBank = isCgb && sprite.UseCgbBank ? (byte)1 : (byte)0;
                    var lowBits = isCgb
                        ? m_vram.ReadBanked((ushort)tileDataAddr, spriteBank)
                        : m_vram.Read8((ushort)tileDataAddr);
                    var highBits = isCgb
                        ? m_vram.ReadBanked((ushort)(tileDataAddr + 1), spriteBank)
                        : m_vram.Read8((ushort)(tileDataAddr + 1));

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

                    spritePaletteIndex = sprite.CgbPaletteIndex;
                    spritePalette = sprite.UseObp1 ? m_lcd.OBP1 : m_lcd.OBP0;
                    spriteColorIndex = colorIndex;
                }
            }

            // Update the frame buffer.
            var frameIndex = lcdLy * FrameWidth + x;
            var frameOffset = frameIndex * 4;
            var accumulatorOffset = frameIndex * 3;
            byte targetR;
            byte targetG;
            byte targetB;

            if (isCgb)
            {
                var color = spriteColorIndex != 0x00
                    ? m_lcd.ReadCgbObjPaletteColor(spritePaletteIndex, spriteColorIndex)
                    : m_lcd.ReadCgbBgPaletteColor(bgPaletteIndex, bgColorIndex);
                targetR = Expand5To8(color & 0x1F);
                targetG = Expand5To8((color >> 5) & 0x1F);
                targetB = Expand5To8((color >> 10) & 0x1F);
            }
            else
            {
                // Background palette entry (includes color 0).
                var bgPaletteValue = (byte)((lcdBGP >> (2 * bgColorIndex)) & 0x03);

                byte colorValue;
                if (spriteColorIndex != 0x00)
                {
                    // Sprite is drawn, so use the sprite palette.
                    colorValue = (byte)((spritePalette >> (2 * spriteColorIndex)) & 0x03);
                }
                else
                {
                    // No sprite, so use the background palette (color 0 included).
                    colorValue = bgPaletteValue;
                }

                if (m_lcdEmulationEnabled)
                {
                    var greenIndex = colorValue * 3;
                    targetR = m_greenMap[greenIndex];
                    targetG = m_greenMap[greenIndex + 1];
                    targetB = m_greenMap[greenIndex + 2];
                }
                else
                {
                    var grey = m_greyMap[colorValue];
                    targetR = grey;
                    targetG = grey;
                    targetB = grey;
                }
            }

            var oldR = m_colorAccumulator[accumulatorOffset];
            var oldG = m_colorAccumulator[accumulatorOffset + 1];
            var oldB = m_colorAccumulator[accumulatorOffset + 2];

            var oldWeight = !m_motionBlurEnabled || !m_motionBlurPrimed ? 0.0 : MotionBlurOldWeight;
            if (m_motionBlurEnabled && m_motionBlurPrimed && targetR < oldR)
                oldWeight = MotionBlurOldWeightDark; // Different bias for darker colors.
            var newWeight = 1.0 - oldWeight;

            m_colorAccumulator[accumulatorOffset] = oldR * oldWeight + targetR * newWeight;
            m_colorAccumulator[accumulatorOffset + 1] = oldG * oldWeight + targetG * newWeight;
            m_colorAccumulator[accumulatorOffset + 2] = oldB * oldWeight + targetB * newWeight;

            m_frameBuffer[frameOffset] = (byte)Math.Clamp((int)Math.Round(m_colorAccumulator[accumulatorOffset]), 0, 255);
            m_frameBuffer[frameOffset + 1] = (byte)Math.Clamp((int)Math.Round(m_colorAccumulator[accumulatorOffset + 1]), 0, 255);
            m_frameBuffer[frameOffset + 2] = (byte)Math.Clamp((int)Math.Round(m_colorAccumulator[accumulatorOffset + 2]), 0, 255);
            m_frameBuffer[frameOffset + 3] = 0xFF; // A
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

    private void EnterOamScan()
    {
        CurrentState = FrameState.OAMScan;
        m_stateCycles = OamCycles;
        m_hblankEndsScanline = true;
        LogScanlineStart();
    }

    private void EnterDrawing()
    {
        CurrentState = FrameState.Drawing;
        m_stateCycles = Mode3Cycles;
        m_hblankEndsScanline = true;
    }

    private void EnterHBlank(bool endsScanline, int cycles)
    {
        CurrentState = FrameState.HBlank;
        m_stateCycles = (ulong)cycles;
        m_hblankEndsScanline = endsScanline;
        if (endsScanline)
            HBlankStarted?.Invoke();
    }

    private void LogScanlineStart() =>
        InstructionLogger?.Write(() => $"PPU scanline start: LY={m_lcd.LY}");

    private void LogScanlineEnd(int line) =>
        InstructionLogger?.Write(() => $"PPU scanline end: LY={line}");

    public void ResetLyCounter() =>
        UpdateLineIndex(false);

    public void SetMode(GameBoyMode mode) =>
        Mode = mode;

    private void ClearFrameBufferToBaseColor()
    {
        byte baseR;
        byte baseG;
        byte baseB;

        if (Mode == GameBoyMode.Cgb)
        {
            baseR = 0xFF;
            baseG = 0xFF;
            baseB = 0xFF;
        }
        else if (m_lcdEmulationEnabled)
        {
            baseR = m_greenMap[0];
            baseG = m_greenMap[1];
            baseB = m_greenMap[2];
        }
        else
        {
            var grey = m_greyMap[0];
            baseR = grey;
            baseG = grey;
            baseB = grey;
        }

        var pixelCount = FrameWidth * FrameHeight;
        for (var i = 0; i < pixelCount; i++)
        {
            var fbOffset = i * 4;
            m_frameBuffer[fbOffset] = baseR;
            m_frameBuffer[fbOffset + 1] = baseG;
            m_frameBuffer[fbOffset + 2] = baseB;
            m_frameBuffer[fbOffset + 3] = 0xFF;

            var accOffset = i * 3;
            m_colorAccumulator[accOffset] = baseR;
            m_colorAccumulator[accOffset + 1] = baseG;
            m_colorAccumulator[accOffset + 2] = baseB;
        }
    }

    private static byte Expand5To8(int value) =>
        (byte)((value << 3) | (value >> 2));

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
                ValidateVramRange(rowAddr, 2, "DumpTileMap row");
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

    [Conditional("DEBUG")]
    private static void ValidateVramRange(int startAddr, int length, string context)
    {
        const int vramStart = 0x8000;
        const int vramEndExclusive = 0xA000;
        if (startAddr < vramStart || startAddr + length > vramEndExclusive)
        {
            throw new InvalidOperationException(
                $"PPU VRAM out of range in {context}: 0x{startAddr:X4}..0x{startAddr + length - 1:X4}");
        }
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
