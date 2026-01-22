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
using DTC.Core;
using OpenTK.Audio.OpenAL;

namespace DTC.SM83.HostDevices;

/// <summary>
/// A sound device to interface with the host machine's sound card.
/// </summary>
public class SoundDevice
{
    private const int BufferCount = 3;
    private const double VolumeRampMs = 100.0;
    private const double LowPassCutoffHz = 7000.0;
    
    // The real Game Boy output is AC-coupled (a series capacitor) which blocks DC offsets.
    // Without this, DC bias in the generated signal can cause pops/bumps when the bias changes.
    private const double HighPassCutoffHz = 20.0;
    
    // Default output gain. 1.0 can be uncomfortably loud on some systems; 0.5 gives headroom.
    private const double DefaultGain = 0.5;
    private const int CaptureBufferFrames = 1024;

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
    private readonly double m_gainStep;
    private readonly double m_lowPassAlpha;
    private readonly double m_highPassAlpha;
    private Task m_loopTask;
    private bool m_isSoundEnabled = true;
    private double m_targetGain = DefaultGain;
    private double m_outputGain;
    private double m_lastDeviceGain = -1.0;
    private bool m_isLowPassFilterEnabled = true;
    private double m_lowPassLeft;
    private double m_lowPassRight;
    private bool m_lowPassInitialized;
    private double m_highPassPrevInLeft;
    private double m_highPassPrevInRight;
    private double m_highPassPrevOutLeft;
    private double m_highPassPrevOutRight;
    private bool m_highPassInitialized;
    private byte m_lastLeftSample = 128;
    private byte m_lastRightSample = 128;
    private bool m_isCancelled;
    private readonly short[] m_captureBuffer = new short[CaptureBufferFrames * 2];
    private int m_captureBufferIndex;
    private IAudioSampleSink m_captureSink;
    
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
        m_gainStep = 1.0 / (m_sampleRate * (VolumeRampMs / 1000.0));
        m_lowPassAlpha = ComputeLowPassAlpha(LowPassCutoffHz, m_sampleRate);
        m_highPassAlpha = ComputeHighPassAlpha(HighPassCutoffHz, m_sampleRate);
    }

    public IAudioSampleSink CaptureSink
    {
        get => m_captureSink;
        set
        {
            m_captureSink = value;
            m_captureBufferIndex = 0;
        }
    }

    public void Start()
    {
        if (m_loopTask != null)
            return; // Already started.

        m_outputGain = 0.0;
        m_targetGain = m_isSoundEnabled ? DefaultGain : 0.0;
        m_lastDeviceGain = -1.0;
        m_loopTask = Task.Run(SoundLoop);
    }

    private void SoundLoop()
    {
        Logger.Instance.Info("Sound thread started.");
        
        WaitForInitialData();

        // Pre-fill all buffers with initial data.
        foreach (var bufferId in m_buffers)
            UpdateBufferData(bufferId);

        // Start playback (gain ramps via samples and device gain).
        ExecuteAl("SourcePlay", () => AL.SourcePlay(m_source));
        while (!m_isCancelled)
        {
            UpdateDeviceGain();

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
                m_outputGain = 0.0;
                m_targetGain = m_isSoundEnabled ? DefaultGain : 0.0;
                m_lastDeviceGain = -1.0;
                ExecuteAl("SourcePlay", () => AL.SourcePlay(m_source));
                ClearCpuBuffer();
            }

            m_dataAvailable.Wait(CalculateSleepMs(buffersQueued));
            m_dataAvailable.Reset();
        }

        AL.SourceStop(m_source);
        
        Logger.Instance.Info("Sound thread stopped.");
    }

    private void UpdateDeviceGain()
    {
        var gain = Math.Clamp(m_outputGain, 0.0, 1.0);
        if (Math.Abs(gain - m_lastDeviceGain) < 0.0001)
            return;

        m_lastDeviceGain = gain;
        ExecuteAl("Source(Gain)", () => AL.Source(m_source, ALSourcef.Gain, (float)gain));
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
                if (bufferedFrames == 0)
                {
                    FillWithLastSample(m_transferFrames);
                    return;
                }

                var extraFrames = Math.Max(0, bufferedFrames - m_targetBufferedFrames);
                var catchUpFrames = Math.Min(extraFrames, m_transferFrames);
                sourceFrames = Math.Min(bufferedFrames, m_transferFrames + catchUpFrames);

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
        var targetGain = m_targetGain;
        if (m_outputGain < targetGain)
            m_outputGain = Math.Min(targetGain, m_outputGain + m_gainStep);
        else if (m_outputGain > targetGain)
            m_outputGain = Math.Max(targetGain, m_outputGain - m_gainStep);
        // Device gain is applied in the sound thread; keep samples unscaled here to avoid double attenuation.

        if (m_isLowPassFilterEnabled)
        {
            if (!m_lowPassInitialized)
            {
                m_lowPassLeft = leftSample;
                m_lowPassRight = rightSample;
                m_lowPassInitialized = true;
            }
            else
            {
                m_lowPassLeft += m_lowPassAlpha * (leftSample - m_lowPassLeft);
                m_lowPassRight += m_lowPassAlpha * (rightSample - m_lowPassRight);
            }

            leftSample = m_lowPassLeft;
            rightSample = m_lowPassRight;
        }

        // High-pass (AC-coupling) to remove DC bias. This mimics the Game Boy's output coupling capacitor
        // and reduces audible pops/bumps when the DC level changes (e.g., when channels are enabled/disabled).
        if (!m_highPassInitialized)
        {
            m_highPassPrevInLeft = leftSample;
            m_highPassPrevInRight = rightSample;
            m_highPassPrevOutLeft = 0.0;
            m_highPassPrevOutRight = 0.0;
            m_highPassInitialized = true;
        }
        else
        {
            var outLeft = m_highPassAlpha * (m_highPassPrevOutLeft + leftSample - m_highPassPrevInLeft);
            var outRight = m_highPassAlpha * (m_highPassPrevOutRight + rightSample - m_highPassPrevInRight);

            m_highPassPrevInLeft = leftSample;
            m_highPassPrevInRight = rightSample;
            m_highPassPrevOutLeft = outLeft;
            m_highPassPrevOutRight = outRight;

            leftSample = outLeft;
            rightSample = outRight;
        }

        var captureSink = m_captureSink;
        if (captureSink != null)
        {
            var captureGain = Math.Clamp(m_outputGain, 0.0, 1.0);
            var leftPcm = ToPcm16(leftSample * captureGain);
            var rightPcm = ToPcm16(rightSample * captureGain);

            m_captureBuffer[m_captureBufferIndex++] = leftPcm;
            m_captureBuffer[m_captureBufferIndex++] = rightPcm;
            if (m_captureBufferIndex >= m_captureBuffer.Length)
            {
                captureSink.OnSamples(m_captureBuffer.AsSpan(0, m_captureBufferIndex), m_sampleRate);
                m_captureBufferIndex = 0;
            }
        }

        var leftByte = ToUnsigned8(leftSample);
        var rightByte = ToUnsigned8(rightSample);

        m_lastLeftSample = leftByte;
        m_lastRightSample = rightByte;

        lock (m_bufferLock)
        {
            m_cpuBuffer.Write(leftByte);
            m_cpuBuffer.Write(rightByte);
        }
        
        m_dataAvailable.Set();
        return;

        byte ToUnsigned8(double sample) =>
            (byte)Math.Clamp(128.0 + sample * 127.0, 0.0, 255.0);

        static short ToPcm16(double sample) =>
            (short)Math.Clamp(sample * 32767.0, short.MinValue, short.MaxValue);
    }
    
    public void SetEnabled(bool isSoundEnabled)
    {
        if (m_isSoundEnabled == isSoundEnabled)
            return;
        m_isSoundEnabled = isSoundEnabled;
        m_targetGain = isSoundEnabled ? DefaultGain : 0.0;
        if (isSoundEnabled && m_outputGain <= 0.001)
            ClearCpuBuffer();
    }

    public void SetLowPassFilterEnabled(bool isEnabled)
    {
        if (m_isLowPassFilterEnabled == isEnabled)
            return;

        m_isLowPassFilterEnabled = isEnabled;
        if (isEnabled)
            m_lowPassInitialized = false;
    }

    private void ClearCpuBuffer()
    {
        lock (m_bufferLock)
            m_cpuBuffer.Clear();
        m_lastLeftSample = 128;
        m_lastRightSample = 128;
        m_lowPassInitialized = false;
        m_lowPassLeft = 0.0;
        m_lowPassRight = 0.0;
        m_highPassInitialized = false;
        m_highPassPrevInLeft = 0.0;
        m_highPassPrevInRight = 0.0;
        m_highPassPrevOutLeft = 0.0;
        m_highPassPrevOutRight = 0.0;
    }

    public void FlushCapture()
    {
        var captureSink = m_captureSink;
        if (captureSink == null || m_captureBufferIndex <= 0)
            return;

        captureSink.OnSamples(m_captureBuffer.AsSpan(0, m_captureBufferIndex), m_sampleRate);
        m_captureBufferIndex = 0;
    }

    private static double ComputeLowPassAlpha(double cutoffHz, int sampleRate)
    {
        var rc = 1.0 / (2.0 * Math.PI * cutoffHz);
        var dt = 1.0 / sampleRate;
        return dt / (rc + dt);
    }

    private static double ComputeHighPassAlpha(double cutoffHz, int sampleRate)
    {
        var rc = 1.0 / (2.0 * Math.PI * cutoffHz);
        var dt = 1.0 / sampleRate;
        return rc / (rc + dt);
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
