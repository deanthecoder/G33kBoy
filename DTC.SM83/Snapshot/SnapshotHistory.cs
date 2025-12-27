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

using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using DTC.Core.ViewModels;

namespace DTC.SM83.Snapshot;

/// <summary>
/// Periodically snapshots machine state to support time travel.
/// </summary>
public sealed class SnapshotHistory : ViewModelBase
{
    private const int MaxSamples = 30;
    private const ulong TicksPerSample = (ulong)Cpu.Hz;
    private readonly GameBoy m_gameBoy;
    private readonly MachineState[] m_states = new MachineState[MaxSamples];
    private readonly byte[][] m_frameBuffers = new byte[MaxSamples][];
    private readonly WriteableBitmap m_preview;
    private int m_count;
    private int m_startIndex;
    private int m_indexToRestore;
    private ulong m_ticksToNextSample = TicksPerSample;
    private ulong m_lastCpuTicks;
    private string m_romPath;
    private int m_stateSize;

    public event EventHandler Activated;

    public SnapshotHistory(GameBoy gameBoy)
    {
        m_gameBoy = gameBoy ?? throw new ArgumentNullException(nameof(gameBoy));
        var size = new PixelSize(PPU.FrameWidth, PPU.FrameHeight);
        m_preview = new WriteableBitmap(size, new Vector(96, 96), PixelFormat.Rgba8888);
    }

    public object CpuStepLock => m_gameBoy.CpuStepLock;

    public IDisposable CreatePauser() =>
        m_gameBoy.CreatePauser();

    public WriteableBitmap ScreenPreview => m_preview;

    public int LastSampleIndex => m_count - 1;

    public bool CanRestore => LastSampleIndex >= 0 && IndexToRestore < LastSampleIndex;

    public bool HasSnapshots => m_count > 0;

    public int IndexToRestore
    {
        get => m_indexToRestore;
        set
        {
            if (!SetField(ref m_indexToRestore, value))
                return;
            UpdatePreview();
            OnPropertyChanged(nameof(CanRestore));
        }
    }

    public void ResetForRom(string romPath)
    {
        m_romPath = romPath;
        EnsureBuffersAllocated();
        Clear();
    }

    private void Clear()
    {
        m_count = 0;
        m_startIndex = 0;
        m_lastCpuTicks = 0;
        m_ticksToNextSample = TicksPerSample;
        IndexToRestore = 0;
        ClearPreview();
        OnPropertyChanged(nameof(LastSampleIndex));
        OnPropertyChanged(nameof(HasSnapshots));
        OnPropertyChanged(nameof(CanRestore));
    }

    internal void OnFrameRendered(ulong currentCpuTicks)
    {
        if (!m_gameBoy.IsRunning || !m_gameBoy.HasLoadedCartridge)
            return;

        if (m_lastCpuTicks == 0)
        {
            m_lastCpuTicks = currentCpuTicks;
            return;
        }

        var delta = currentCpuTicks - m_lastCpuTicks;
        if (delta == 0)
            return;
        m_lastCpuTicks = currentCpuTicks;

        if (delta < m_ticksToNextSample)
        {
            m_ticksToNextSample -= delta;
            return;
        }

        while (delta >= m_ticksToNextSample)
        {
            delta -= m_ticksToNextSample;
            m_ticksToNextSample = TicksPerSample;
            CaptureSnapshot();
        }

        if (delta > 0)
            m_ticksToNextSample -= delta;
    }

    public void Rollback()
    {
        if (!CanRestore)
            return;

        var state = GetSnapshot(IndexToRestore);
        if (state == null)
            return;

        m_gameBoy.LoadState(state);
        TrimTo(IndexToRestore + 1);
        IndexToRestore = LastSampleIndex;
        m_ticksToNextSample = TicksPerSample;
        m_lastCpuTicks = m_gameBoy.CpuClockTicks;

        Activated?.Invoke(this, EventArgs.Empty);
    }

    private void CaptureSnapshot()
    {
        EnsureBuffersAllocated();

        var index = GetWriteIndex();
        var state = m_states[index];
        var frameBuffer = m_frameBuffers[index];
        if (state == null || frameBuffer == null)
            return;

        m_gameBoy.CaptureState(state, frameBuffer);
        state.RomPath = m_romPath;

        if (m_count == MaxSamples)
            m_startIndex = (m_startIndex + 1) % MaxSamples;
        else
            m_count++;

        OnPropertyChanged(nameof(LastSampleIndex));
        OnPropertyChanged(nameof(HasSnapshots));

        IndexToRestore = LastSampleIndex;
        UpdatePreview();
        OnPropertyChanged(nameof(CanRestore));
    }

    private void TrimTo(int count)
    {
        m_count = Math.Clamp(count, 0, m_count);
        OnPropertyChanged(nameof(LastSampleIndex));
        OnPropertyChanged(nameof(HasSnapshots));
        OnPropertyChanged(nameof(CanRestore));
    }

    private MachineState GetSnapshot(int index)
    {
        if ((uint)index >= (uint)m_count)
            return null;
        var physicalIndex = (m_startIndex + index) % MaxSamples;
        return m_states[physicalIndex];
    }

    private byte[] GetPreviewBuffer(int index)
    {
        if ((uint)index >= (uint)m_count)
            return null;
        var physicalIndex = (m_startIndex + index) % MaxSamples;
        return m_frameBuffers[physicalIndex];
    }

    private int GetWriteIndex()
    {
        if (m_count < MaxSamples)
            return (m_startIndex + m_count) % MaxSamples;
        return m_startIndex;
    }

    private void EnsureBuffersAllocated()
    {
        var stateSize = m_gameBoy.GetStateSize();
        if (stateSize <= 0)
            return;
        if (m_stateSize == stateSize && m_states[0] != null)
            return;

        m_stateSize = stateSize;

        for (var i = 0; i < MaxSamples; i++)
        {
            if (m_states[i] == null || m_states[i].Size != stateSize)
                m_states[i] = new MachineState(stateSize);

            if (m_frameBuffers[i] == null || m_frameBuffers[i].Length != PPU.FrameWidth * PPU.FrameHeight * 4)
                m_frameBuffers[i] = new byte[PPU.FrameWidth * PPU.FrameHeight * 4];
        }
    }

    private void UpdatePreview()
    {
        var buffer = GetPreviewBuffer(IndexToRestore);
        if (buffer == null)
        {
            ClearPreview();
            return;
        }

        using var locked = m_preview.Lock();
        var destStride = locked.RowBytes;
        var srcStride = PPU.FrameWidth * 4;
        unsafe
        {
            fixed (byte* srcPtr = buffer)
            {
                var destPtr = (byte*)locked.Address;
                for (var y = 0; y < PPU.FrameHeight; y++)
                {
                    Buffer.MemoryCopy(srcPtr + y * srcStride, destPtr + y * destStride, destStride, srcStride);
                }
            }
        }

        OnPropertyChanged(nameof(ScreenPreview));
    }

    private void ClearPreview()
    {
        using var locked = m_preview.Lock();
        var totalBytes = locked.RowBytes * PPU.FrameHeight;
        unsafe
        {
            var span = new Span<byte>((void*)locked.Address, totalBytes);
            span.Clear();
        }

        OnPropertyChanged(nameof(ScreenPreview));
    }
}
