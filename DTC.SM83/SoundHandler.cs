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

using DTC.Core;
using DTC.Core.ViewModels;
using DTC.SM83.HostDevices;

namespace DTC.SM83;

/// <summary>
/// Emulated sound support, recording virtual speaker movements.
/// Uses a SoundDevice to send sound to the host device.
/// </summary>
public class SoundHandler : ViewModelBase, IDisposable
{
    public int LevelResolution { get; }

    private byte m_soundLevel;
    private const int SampleHz = 22050;
    private const double TicksPerSample = Cpu.Hz / SampleHz;
    private double m_ticksUntilSample = TicksPerSample;
    private readonly int[] m_soundLevels;
    private readonly SoundDevice m_soundDevice;
    private bool m_isDisposed;
    private readonly Thread m_thread;

    public SoundHandler(int levelResolution = 256)
    {
        LevelResolution = Math.Clamp(levelResolution, 2, 256);
        m_soundLevels = new int[LevelResolution];

        try
        {
            m_soundDevice = new SoundDevice(SampleHz);
            m_thread = new Thread(() => m_soundDevice.SoundLoop(() => m_isDisposed))
            {
                Name = "Sound Device",
                Priority = ThreadPriority.AboveNormal
            };
        }
        catch (Exception e)
        {
            Logger.Instance.Error($"Failed to initialize host sound device: {e.Message}");
        }
    }

    public void SetEnabled(bool value) =>
        m_soundDevice?.SetEnabled(value);

    public void Start()
    {
        if (m_thread?.IsAlive != true)
            m_thread?.Start();
    }

    /// <summary>
    /// Called whenever the CPU's speaker state changes.
    /// </summary>
    public void SetSpeakerState(byte soundLevel) =>
        m_soundLevel = soundLevel;

    public void Dispose()
    {
        m_soundDevice?.Mute();

        // Wait for the sound thread to exit.
        m_isDisposed = true;
        m_thread?.Join();
    }

    /// <summary>
    /// Called every CPU tick to build a collection of speaker samples.
    /// Passed to the sound device's buffer when enough are collected.
    /// </summary>
    public void SampleSpeakerState(ulong tStateCount)
    {
        // Update the count for the current speaker level (0-15).
        if (m_soundLevel < m_soundLevels.Length)
        {
            m_soundLevels[m_soundLevel]++;
        }

        m_ticksUntilSample -= tStateCount;
        if (m_ticksUntilSample > 0)
            return; // Not enough time elapsed - Keep collecting speaker states.

        // We've collected enough samples for averaging to occur.
        m_ticksUntilSample += TicksPerSample;
        var sampleValue = 0.0;
        var sampleCount = 0.0;
        for (var i = 0; i < m_soundLevels.Length; i++)
        {
            sampleValue += i * m_soundLevels[i];
            sampleCount += m_soundLevels[i];
            m_soundLevels[i] = 0;
        }

        // Append to the sample buffer.
        // Map averaged level (0..LevelResolution-1) into a 0.0-1.0 sample value.
        var value = sampleCount > 0.0 ? sampleValue / (sampleCount * (LevelResolution - 1)) : 0.0;
        m_soundDevice?.AddSample(value);
    }
}
