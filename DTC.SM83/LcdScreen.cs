// Code authored by Dean Edis (DeanTheCoder).
// Anyone is free to copy, modify, use, compile, or distribute this software,
// either in source code form or as a compiled binary, for any purpose.
//
// If you modify the code, please retain this copyright header,
// and consider contributing back to the repository or letting us know
// about your modifications. Your contributions are valued!
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.

using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using DTC.Core.Extensions;

namespace DTC.SM83;

public sealed class LcdScreen : IDisposable
{
    private readonly int m_sourceWidth;
    private readonly int m_sourceHeight;
    private readonly int m_scale = 3;
    private readonly int m_destWidth;
    private readonly double[] m_grain;
    private uint m_previousFrameBufferHash;

    public WriteableBitmap Display { get; }

    public LcdScreen(int sourceWidth, int sourceHeight)
    {
        m_sourceWidth = sourceWidth;
        m_sourceHeight = sourceHeight;
        m_destWidth = sourceWidth * m_scale;
        var destHeight = sourceHeight * m_scale;

        var pixelSize = new PixelSize(m_destWidth, destHeight);
        Display = new WriteableBitmap(pixelSize, new Vector(96, 96), PixelFormat.Rgba8888);

        // Pre-build the screen grain.
        m_grain = new double[m_destWidth * destHeight];
        var random = new Random(0);
        for (var i = 0; i < m_grain.Length; i++)
            m_grain[i] = 1.0 + 0.03 * (random.NextDouble() * 2.0 - 1.0);
    }

    public unsafe void Update(byte[] frameBuffer)
    {
        if (frameBuffer == null)
            throw new ArgumentNullException(nameof(frameBuffer));

        var frameBufferHash = ComputeFrameBufferHash(frameBuffer);
        if (frameBufferHash == m_previousFrameBufferHash)
            return; // Nothing to do - No change in frame data.

        using var locked = Display.Lock();
        var destStride = locked.RowBytes;
        var destPtr = (byte*)locked.Address;

        for (var y = 0; y < m_sourceHeight; y++)
        {
            var sourceRowOffset = y * m_sourceWidth * 4;
            var destBaseY = y * m_scale;
            
            // Top/bottom tinge.
            var redBoostTop = (1.0 - y / 5.0).Clamp(0.0, 1.0) * 1.1;
            var redBoostBottom = (1.0 - (m_sourceHeight - y) / 5.0).Clamp(0.0, 1.0) * 1.1;

            for (var x = 0; x < m_sourceWidth; x++)
            {
                // Get source RGB.
                var srcOffset = sourceRowOffset + x * 4;
                var r = frameBuffer[srcOffset];
                var g = frameBuffer[srcOffset + 1];
                var b = frameBuffer[srcOffset + 2];
                var a = frameBuffer[srcOffset + 3];
                
                // Convert RGB to [0.0, 1.0].
                var destBaseX = x * m_scale;
                var grainBaseIndex = destBaseY * m_destWidth + destBaseX;
                var redBase = r / 255.0;
                var greenBase = g / 255.0;
                var blueBase = b / 255.0;

                // Left/right tinge.
                var redBoostLeft = (1.0 - x / 3.0).Clamp(0.0, 1.0);
                var redBoostRight = (1.0 - (m_sourceWidth - x) / 3.0).Clamp(0.0, 1.0);
                var redBoost = Math.Max(redBoostTop, Math.Max(redBoostBottom, Math.Max(redBoostLeft, redBoostRight)));
                redBase *= 1.0 + redBoost * 0.25;
                greenBase *= 1.0 + redBoost * 0.07;
                
                for (var py = 0; py < m_scale; py++)
                {
                    var destRowStart = destPtr + (destBaseY + py) * destStride;
                    var grainRowStart = grainBaseIndex + py * m_destWidth;
                    for (var px = 0; px < m_scale; px++)
                    {
                        var grain = m_grain[grainRowStart + px];

                        // Left/right shadow.
                        var shadowL = (x + px / (double)m_scale).InverseLerp(3.0, 6.0).Clamp(0.0, 1.0);
                        var shadowR = (m_sourceWidth - (x + px / (double)m_scale)).InverseLerp(3.0, 6.0).Clamp(0.0, 1.0);
                        var shadow = Math.Min(shadowL, shadowR); 
                        grain *= 0.6 + 0.4 * shadow;

                        var red = redBase * grain;
                        var green = greenBase * grain;
                        var blue = blueBase * grain;

                        // Pixel outlines.
                        var s = 1.0 - px % m_scale + 1.0 - py % m_scale;
                        s *= 0.005;
                        red -= s;
                        green -= s;
                        blue -= s;
                
                        // Set target pixel.
                        var destOffset = destRowStart + (destBaseX + px) * 4;
                        destOffset[0] = (byte)(red * 255.0).Clamp(0.0, 255.0);
                        destOffset[1] = (byte)(green * 255.0).Clamp(0.0, 255.0);
                        destOffset[2] = (byte)(blue * 255.0).Clamp(0.0, 255.0);
                        destOffset[3] = a;
                    }
                }
            }
        }

        m_previousFrameBufferHash = frameBufferHash;
    }

    public void Dispose() =>
        Display?.Dispose();

    private static uint ComputeFrameBufferHash(ReadOnlySpan<byte> buffer)
    {
        const uint fnvOffset = 2166136261;
        const uint fnvPrime = 16777619;
        var hash = fnvOffset;
        foreach (var b in buffer)
        {
            hash ^= b;
            hash *= fnvPrime;
        }

        return hash;
    }
}
