// Code authored by Dean Edis (DeanTheCoder).
// Anyone is free to copy, modify, use, compile, or distribute this software,
// either in source code form or as a compiled binary, for any purpose.
//
// If you modify the code, please retain this copyright header,
// and consider contributing back to the repository or letting us know
// about your modifications. Your contributions are valued!
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.

using System.Numerics;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using DTC.Core.Extensions;
using Vector = Avalonia.Vector;

namespace DTC.SM83;

public sealed class LcdScreen : IDisposable
{
    private const int Scale = 4;
    private readonly int m_sourceWidth;
    private readonly int m_sourceHeight;
    private readonly int m_destWidth;
    private readonly int m_destHeight;
    private readonly float[] m_redBoostPerPixel;
    private readonly float[] m_grainWithShadow;
    private uint m_previousFrameBufferHash;
    private bool m_forceRefresh;
    private readonly Vector3 m_inv255 = new Vector3(1.0f / 255.0f);
    private readonly Vector3 m_redBoostMult = new Vector3(0.25f, 0.07f, 0.0f);
    private readonly Vector3 m_outlineColor = new Vector3(0x81 / 255.0f, 0x7D / 255.0f, 0x15 / 255.0f);

    public WriteableBitmap Display { get; }
    private bool m_lcdEmulationEnabled = true;
    private GameBoyMode m_mode = GameBoyMode.Dmg;

    public bool LcdEmulationEnabled
    {
        get => m_lcdEmulationEnabled;
        set
        {
            if (m_lcdEmulationEnabled == value)
                return;
            m_lcdEmulationEnabled = value;
            m_forceRefresh = true;
        }
    }
    
    public GameBoyMode Mode
    {
        get => m_mode;
        set
        {
            if (m_mode == value)
                return;
            m_mode = value;
            m_forceRefresh = true;
        }
    }

    public LcdScreen(int sourceWidth, int sourceHeight)
    {
        m_sourceWidth = sourceWidth;
        m_sourceHeight = sourceHeight;
        m_destWidth = sourceWidth * Scale;
        m_destHeight = sourceHeight * Scale;

        var pixelSize = new PixelSize(m_destWidth, m_destHeight);
        Display = new WriteableBitmap(pixelSize, new Vector(96, 96), PixelFormat.Rgba8888);

        // Pre-build the screen grain and static spatial effects.
        m_redBoostPerPixel = new float[m_sourceWidth * m_sourceHeight];
        m_grainWithShadow = new float[m_destWidth * m_destHeight];

        PrecomputeRedBoost();
        PrecomputeGrainWithShadow();
    }

    /// <summary>
    /// Precomputes a per-source-pixel red‑boost map used for simulating LCD tinge on the edges.
    /// </summary>
    private void PrecomputeRedBoost()
    {
        for (var y = 0; y < m_sourceHeight; y++)
        {
            var redBoostTop = (1.0 - y / 5.0).Clamp(0.0, 1.0) * 1.1;
            var redBoostBottom = (1.0 - (m_sourceHeight - y) / 5.0).Clamp(0.0, 1.0) * 1.1;

            for (var x = 0; x < m_sourceWidth; x++)
            {
                var redBoostLeft = (1.0 - x / 3.0).Clamp(0.0, 1.0);
                var redBoostRight = (1.0 - (m_sourceWidth - x) / 3.0).Clamp(0.0, 1.0);
                var redBoost = Math.Max(redBoostTop, Math.Max(redBoostBottom, Math.Max(redBoostLeft, redBoostRight)));

                m_redBoostPerPixel[y * m_sourceWidth + x] = (float)redBoost;
            }
        }
    }

    /// <summary>
    /// Precomputes grain combined with a left/right edge shadow for each destination pixel.
    /// </summary>
    private void PrecomputeGrainWithShadow()
    {
        var random = new Random(0);
        for (var destY = 0; destY < m_destHeight; destY++)
        {
            var rowOffset = destY * m_destWidth;
            for (var destX = 0; destX < m_destWidth; destX++)
            {
                var xPos = destX / (double)Scale;
                var shadowL = xPos.InverseLerp(3.0, 6.0).Clamp(0.0, 1.0);
                var shadowR = (m_sourceWidth - xPos).InverseLerp(3.0, 6.0).Clamp(0.0, 1.0);
                var shadow = Math.Min(shadowL, shadowR);
                var grain = 1.0 + 0.03 * (random.NextDouble() * 2.0 - 1.0);

                m_grainWithShadow[rowOffset + destX] = (float)(grain * (0.6 + 0.4 * shadow) * 255.0).Clamp(0.0f, 255.0f);
            }
        }
    }

    public unsafe bool Update(byte[] frameBuffer)
    {
        if (frameBuffer == null)
            throw new ArgumentNullException(nameof(frameBuffer));

        var frameBufferHash = ComputeFrameBufferHash(frameBuffer);
        if (!m_forceRefresh && frameBufferHash == m_previousFrameBufferHash)
            return false; // Nothing to do - No change in frame data.

        using var locked = Display.Lock();
        var destStride = locked.RowBytes;
        var destPtr = (byte*)locked.Address;

        if (LcdEmulationEnabled)
            RenderWithLcdEffects(frameBuffer, destPtr, destStride);
        else
            RenderSimple(frameBuffer, destPtr, destStride);

        m_previousFrameBufferHash = frameBufferHash;
        m_forceRefresh = false;
        return true; // Frame data changed.
    }

    /// <summary>
    /// Renders the framebuffer with no LCD effects applied — just a straight nearest‑neighbour scale.
    /// </summary>
    private unsafe void RenderSimple(byte[] frameBuffer, byte* destPtr, int destStride)
    {
        fixed (byte* srcPtr = frameBuffer)
        {
            for (var y = 0; y < m_sourceHeight; y++)
            {
                var sourceRowOffset = y * m_sourceWidth * 4;
                var destBaseY = y * Scale;

                for (var py = 0; py < Scale; py++)
                {
                    var destRowStart = destPtr + (destBaseY + py) * destStride;

                    for (var x = 0; x < m_sourceWidth; x++)
                    {
                        var src = srcPtr + sourceRowOffset + x * 4;
                        var r = src[0];
                        var g = src[1];
                        var b = src[2];

                        var destBaseX = x * Scale;
                        for (var px = 0; px < Scale; px++)
                        {
                            var destOffset = destRowStart + (destBaseX + px) * 4;
                            destOffset[0] = r;
                            destOffset[1] = g;
                            destOffset[2] = b;
                            destOffset[3] = 255;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Renders the framebuffer using grain, edge tinge, per‑pixel shading, and outlines to simulate a Game Boy LCD panel.
    /// </summary>
    private unsafe void RenderWithLcdEffects(byte[] frameBuffer, byte* destPtr, int destStride)
    {
        var useRedBoost = Mode == GameBoyMode.Dmg;
        fixed (byte* srcPtr = frameBuffer)
        {
            for (var y = 0; y < m_sourceHeight; y++)
            {
                var sourceRowOffset = y * m_sourceWidth * 4;
                var destBaseY = y * Scale;
                var redBoostRowOffset = y * m_sourceWidth;
                var grainBaseRowIndex = destBaseY * m_destWidth;

                for (var x = 0; x < m_sourceWidth; x++)
                {
                    // Load source RGB and normalize to 0..1.
                    var src = srcPtr + sourceRowOffset + x * 4;
                    var r = src[0] * m_inv255.X;
                    var g = src[1] * m_inv255.X;
                    var b = src[2] * m_inv255.X;

                    // Precomputed red boost (top/bottom/left/right tinge).
                    var redBoost = useRedBoost ? m_redBoostPerPixel[redBoostRowOffset + x] : 0.0f;
                    r *= 1.0f + m_redBoostMult.X * redBoost;
                    g *= 1.0f + m_redBoostMult.Y * redBoost;

                    // Precompute outline colour once per source pixel.
                    var bright = 0.6f + r * (1.5f - 0.6f);
                    var outlineR = (r + m_outlineColor.X * bright) * 0.5f;
                    var outlineG = (g + m_outlineColor.Y * bright) * 0.5f;
                    var outlineB = (b + m_outlineColor.Z * bright) * 0.5f;

                    var destBaseX = x * Scale;
                    var destBaseXBytes = destBaseX * 4;
                    var grainBaseIndex = grainBaseRowIndex + destBaseX;

                    for (var py = 0; py < Scale; py++)
                    {
                        var destRowStart = destPtr + (destBaseY + py) * destStride + destBaseXBytes;
                        var grainRowStart = grainBaseIndex + py * m_destWidth;

                        // py == 0 => whole top row uses outline color.
                        if (py == 0)
                        {
                            for (var px = 0; px < Scale; px++)
                            {
                                var grain = m_grainWithShadow[grainRowStart + px];
                                var destOffset = destRowStart + px * 4;
                                destOffset[0] = (byte)(outlineR * grain);
                                destOffset[1] = (byte)(outlineG * grain);
                                destOffset[2] = (byte)(outlineB * grain);
                                destOffset[3] = 255;
                            }
                            continue;
                        }

                        // py > 0 => left column uses outline, remaining pixels use base colour.
                        {
                            // px == 0 (outline).
                            var grain0 = m_grainWithShadow[grainRowStart];
                            destRowStart[0] = (byte)(outlineR * grain0);
                            destRowStart[1] = (byte)(outlineG * grain0);
                            destRowStart[2] = (byte)(outlineB * grain0);
                            destRowStart[3] = 255;

                            // px == 1..3 (base colour).
                            for (var px = 1; px < Scale; px++)
                            {
                                var grain = m_grainWithShadow[grainRowStart + px];
                                var destOffset = destRowStart + px * 4;
                                destOffset[0] = (byte)(r * grain);
                                destOffset[1] = (byte)(g * grain);
                                destOffset[2] = (byte)(b * grain);
                                destOffset[3] = 255;
                            }
                        }
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// FNV-1a (Fowler–Noll–Vo) hash function.
    /// </summary>
    private static uint ComputeFrameBufferHash(ReadOnlySpan<byte> buffer)
    {
        const uint fnvOffset = 2166136261;
        const uint fnvPrime = 16777619;
        var hash = fnvOffset;
        for (var i = 0; i < buffer.Length; i++)
        {
            hash ^= buffer[i];
            hash *= fnvPrime;
        }

        return hash;
    }
    
    public void Dispose() =>
        Display?.Dispose();
}
