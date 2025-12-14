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

using System.Buffers;
using System.Diagnostics;
using DTC.Core;
using OpenTK.Audio.OpenAL;

namespace DTC.SM83.HostDevices;

/// <summary>
/// A sound device to interface with the host machine's sound card.
/// </summary>
public class SoundDevice
{
    private const int BufferCount = 3;

    private readonly int m_source;
    private readonly int[] m_buffers;
    private readonly int m_sampleRate;
    private readonly double m_bufferDurationMs;
    private readonly int m_transferFrames;
    private readonly int m_targetBufferedFrames;
    private readonly CircularBuffer<byte> m_cpuBuffer;
    private readonly Lock m_bufferLock = new();
    private readonly ManualResetEventSlim m_dataAvailable = new(false);
    private readonly byte[] m_transferBuffer;
    private Task m_loopTask;
    private bool m_isSoundEnabled = true;
    private byte m_lastLeftSample = 128;
    private byte m_lastRightSample = 128;
    private bool m_isCancelled;
    private int m_minBufferedFrames = int.MaxValue;
    private int m_maxBufferedFrames;
    private int m_dropResampleCount;
    private int m_stretchResampleCount;
    private long m_overflowFrames;
    
    public SoundDevice(int sampleHz)
    {
        m_sampleRate = sampleHz;

        // Initialize OpenAL.
        var device = ALC.OpenDevice(null);
        var context = ALC.CreateContext(device, (int[])null);
        ALC.MakeContextCurrent(context);

        // Generate buffers and a source.
        m_buffers = AL.GenBuffers(BufferCount);
        m_source = AL.GenSource();

        // Enough data for 0.1 seconds of play, split between all buffers (stereo: 2 bytes per sample).
        var bufferSize = (int)(m_sampleRate * 0.1 * 2 / BufferCount);
        m_transferBuffer = new byte[bufferSize];
        m_transferFrames = m_transferBuffer.Length / 2;
        m_targetBufferedFrames = m_transferFrames * BufferCount;
        var cpuBufferCapacityFrames = m_targetBufferedFrames * 3; // Leave headroom for brief spikes.
        m_cpuBuffer = new CircularBuffer<byte>(cpuBufferCapacityFrames * 2);
        m_bufferDurationMs = 1000.0 * m_transferFrames / m_sampleRate;
    }

    public void Start()
    {
        if (m_loopTask != null)
            return; // Already started.
        
        m_loopTask = Task.Run(SoundLoop);
    }

    private void SoundLoop()
    {
        Logger.Instance.Info("Sound thread started.");
        
        WaitForInitialData();

        // Pre-fill all buffers with initial data.
        foreach (var bufferId in m_buffers)
            UpdateBufferData(bufferId);

        // Start playback (Muted, to avoid the 'pop').
        Mute();
        ExecuteAl("SourcePlay", () => AL.SourcePlay(m_source));

        var gain = 0.0f;
        var healthTimer = Stopwatch.StartNew();
        while (!m_isCancelled)
        {
            if (gain < 0.2)
            {
                gain = Math.Min(0.2f, gain + 0.005f);
                AL.Source(m_source, ALSourcef.Gain, gain);
            }

            var buffersProcessed = 0;
            ExecuteAl("GetSource(BuffersProcessed)", () => AL.GetSource(m_source, ALGetSourcei.BuffersProcessed, out buffersProcessed));
            while (buffersProcessed-- > 0)
            {
                var bufferId = 0;
                ExecuteAl("SourceUnqueueBuffer", () => bufferId = AL.SourceUnqueueBuffer(m_source));
                UpdateBufferData(bufferId);
            }

            var buffersQueued = 0;
            ExecuteAl("GetSource(BuffersQueued)", () => AL.GetSource(m_source, ALGetSourcei.BuffersQueued, out buffersQueued));

            var state = 0;
            ExecuteAl("GetSource(SourceState)", () => AL.GetSource(m_source, ALGetSourcei.SourceState, out state));
            if ((ALSourceState)state != ALSourceState.Playing && buffersQueued > 0)
            {
                gain = 0.0f;
                AL.Source(m_source, ALSourcef.Gain, gain);
                ExecuteAl("SourcePlay", () => AL.SourcePlay(m_source));
                ClearCpuBuffer();
            }

            if (healthTimer.Elapsed >= TimeSpan.FromSeconds(5))
            {
                LogBufferHealth();
                healthTimer.Restart();
            }

            m_dataAvailable.Wait(CalculateSleepMs(buffersQueued));
            m_dataAvailable.Reset();
        }

        Mute();
        AL.SourceStop(m_source);
        LogBufferHealth();
        
        Logger.Instance.Info("Sound thread stopped.");
    }

    private void WaitForInitialData()
    {
        while (!m_isCancelled)
        {
            int bufferedFrames;
            lock (m_bufferLock)
                bufferedFrames = m_cpuBuffer.Count / 2;

            if (bufferedFrames >= m_targetBufferedFrames)
                return;

            m_dataAvailable.Wait((int)Math.Max(1, m_bufferDurationMs));
            m_dataAvailable.Reset();
        }
    }

    private int CalculateSleepMs(int buffersQueued)
    {
        if (buffersQueued <= 1)
            return (int)Math.Max(1, m_bufferDurationMs * 0.25);

        var queuedMs = buffersQueued * m_bufferDurationMs;
        var waitMs = Math.Min(queuedMs * 0.25, m_bufferDurationMs);
        return (int)Math.Max(1, waitMs);
    }

    private void LogBufferHealth()
    {
        // min/max track how close we stayed to the target buffered frames (low min risks underrun, high max means backlog/latency),
        // stretch increments when we had to upsample to mask underrun, drop increments when we downsample to shed backlog,
        // overwritten counts frames discarded because the producer outran the buffer (should remain zero in healthy runs).
        var minFrames = m_minBufferedFrames == int.MaxValue ? 0 : m_minBufferedFrames;
        var framesToMs = 1000.0 / m_sampleRate;
        var targetMs = m_targetBufferedFrames * framesToMs;

        var minPct = m_targetBufferedFrames > 0 ? (double)minFrames / m_targetBufferedFrames * 100.0 : 0.0;
        var maxPct = m_targetBufferedFrames > 0 ? (double)m_maxBufferedFrames / m_targetBufferedFrames * 100.0 : 0.0;

        Logger.Instance.Info(
            $"Sound buffer health: target {m_targetBufferedFrames} frames ({targetMs:F0} ms), " +
            $"min {minPct:F0}%, max {maxPct:F0}%, " +
            $"stretch {m_stretchResampleCount}, drop {m_dropResampleCount}, " +
            $"overwritten {m_overflowFrames} frames.");

        // Reset window stats so the next call reports just the latest period.
        m_minBufferedFrames = int.MaxValue;
        m_maxBufferedFrames = 0;
        m_stretchResampleCount = 0;
        m_dropResampleCount = 0;
    }
    
    private void Mute() =>
        AL.Source(m_source, ALSourcef.Gain, 0.0f);

    private static void ExecuteAl(string operation, Action action)
    {
        action();
        var err = AL.GetError();
        if (err != ALError.NoError)
            Logger.Instance.Error($"Sound device error during {operation}: {err}");
    }

    private void UpdateBufferData(int bufferId)
    {
        FillTransferBuffer();

        ExecuteAl("BufferData", () => AL.BufferData(bufferId, ALFormat.Stereo8, m_transferBuffer, m_sampleRate));

        // Queue the device buffer for playback.
        ExecuteAl("SourceQueueBuffer", () => AL.SourceQueueBuffer(m_source, bufferId));
    }

    private void FillTransferBuffer()
    {
        byte[] rentedArray = null;
        try
        {
            Span<byte> sourceSpan;
            int sourceFrames;

            lock (m_bufferLock)
            {
                var bufferedFrames = m_cpuBuffer.Count / 2;
                m_minBufferedFrames = Math.Min(m_minBufferedFrames, bufferedFrames);
                m_maxBufferedFrames = Math.Max(m_maxBufferedFrames, bufferedFrames);

                if (bufferedFrames == 0)
                {
                    FillWithLastSample(m_transferFrames);
                    return;
                }

                var extraFrames = Math.Max(0, bufferedFrames - m_targetBufferedFrames);
                var catchUpFrames = Math.Min(extraFrames, m_transferFrames);
                sourceFrames = Math.Min(bufferedFrames, m_transferFrames + catchUpFrames);

                if (sourceFrames > m_transferFrames)
                    m_dropResampleCount++;
                else if (sourceFrames < m_transferFrames)
                    m_stretchResampleCount++;

                var sourceBytes = sourceFrames * 2;
                rentedArray = ArrayPool<byte>.Shared.Rent(sourceBytes);
                sourceSpan = rentedArray.AsSpan(0, sourceBytes);

                var bytesRead = m_cpuBuffer.Read(sourceSpan);
                sourceSpan = sourceSpan[..bytesRead];
                sourceFrames = bytesRead / 2;
            }

            if (sourceFrames == 0)
            {
                FillWithLastSample(m_transferFrames);
                return;
            }

            ResampleIntoTransfer(sourceSpan, sourceFrames, m_transferFrames);
        }
        finally
        {
            if (rentedArray != null)
                ArrayPool<byte>.Shared.Return(rentedArray);
        }
    }

    private void ResampleIntoTransfer(Span<byte> sourceSpan, int sourceFrames, int destFrames)
    {
        if (sourceFrames <= 0)
        {
            FillWithLastSample(destFrames);
            return;
        }

        var step = sourceFrames <= 1 || destFrames <= 1
            ? 0.0
            : (double)(sourceFrames - 1) / (destFrames - 1);

        for (var i = 0; i < destFrames; i++)
        {
            var pos = step * i;
            var srcIndex = (int)pos;
            var t = pos - srcIndex;

            var baseIndex = srcIndex * 2;
            var nextIndex = Math.Min(srcIndex + 1, sourceFrames - 1) * 2;

            var left0 = sourceSpan[baseIndex];
            var right0 = sourceSpan[baseIndex + 1];
            var left1 = sourceSpan[nextIndex];
            var right1 = sourceSpan[nextIndex + 1];

            m_transferBuffer[i * 2] = LerpByte(left0, left1, t);
            m_transferBuffer[i * 2 + 1] = LerpByte(right0, right1, t);
        }

        var lastIndex = (destFrames - 1) * 2;
        m_lastLeftSample = m_transferBuffer[lastIndex];
        m_lastRightSample = m_transferBuffer[lastIndex + 1];
    }

    private void FillWithLastSample(int destFrames)
    {
        var left = m_lastLeftSample;
        var right = m_lastRightSample;
        for (var i = 0; i < destFrames; i++)
        {
            var dstIndex = i * 2;
            m_transferBuffer[dstIndex] = left;
            m_transferBuffer[dstIndex + 1] = right;
        }
    }

    private static byte LerpByte(byte a, byte b, double t) =>
        (byte)Math.Clamp(a + (b - a) * t, byte.MinValue, byte.MaxValue);

    public void AddSample(double leftSample, double rightSample)
    {
        if (!m_isSoundEnabled)
        {
            // Silence: center both channels.
            leftSample = 0.0;
            rightSample = 0.0;
        }

        var leftByte = ToUnsigned8(leftSample);
        var rightByte = ToUnsigned8(rightSample);

        m_lastLeftSample = leftByte;
        m_lastRightSample = rightByte;

        lock (m_bufferLock)
        {
            var overflowBytes = Math.Max(0, m_cpuBuffer.Count + 2 - m_cpuBuffer.Capacity);
            if (overflowBytes > 0)
                m_overflowFrames += overflowBytes / 2;

            m_cpuBuffer.Write(leftByte);
            m_cpuBuffer.Write(rightByte);
        }
        
        m_dataAvailable.Set();
        return;

        byte ToUnsigned8(double sample) =>
            (byte)Math.Clamp(128.0 + sample * 127.0, 0.0, 255.0);
    }
    
    public void SetEnabled(bool isSoundEnabled)
    {
        if (m_isSoundEnabled == isSoundEnabled)
            return;
        m_isSoundEnabled = isSoundEnabled;
        ClearCpuBuffer();
    }

    private void ClearCpuBuffer()
    {
        lock (m_bufferLock)
            m_cpuBuffer.Clear();
        m_lastLeftSample = 128;
        m_lastRightSample = 128;
    }
    
    public void Dispose()
    {
        m_isCancelled = true;
        m_dataAvailable.Set();
        m_loopTask?.Wait();
        m_loopTask = null;
        
        AL.DeleteBuffers(m_buffers);
        AL.DeleteSource(m_source);
        ALC.DestroyContext(ALC.GetCurrentContext());
        m_dataAvailable.Dispose();
    }
}
