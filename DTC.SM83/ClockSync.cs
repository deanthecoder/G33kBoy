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

using System.Diagnostics;

namespace DTC.SM83;

public class ClockSync
{
    private readonly Stopwatch m_realTime;
    private readonly Func<double> m_emulatedTicksPerSecond;
    private readonly Func<long> m_ticksSinceCpuStart;
    private readonly Action m_resetCpuTicks;
    private readonly Lock m_lock = new();
    private SpinWait m_spinWait;
    private double m_lastEmulatedTicksPerSecond;
    private volatile bool m_resyncRequested;

    /// <summary>
    /// Number of T states when this stopwatch was started.
    /// </summary>
    private long m_tStateCountAtStart;

    private long m_tStateCountAtLastSync;
    private long m_ticksSinceLastSync;

    public ClockSync(Func<double> emulatedCpuHz, Func<long> ticksSinceCpuStart, Action resetCpuTicks)
    {
        m_realTime = Stopwatch.StartNew();
        m_emulatedTicksPerSecond = emulatedCpuHz ?? throw new ArgumentNullException(nameof(emulatedCpuHz));
        m_ticksSinceCpuStart = ticksSinceCpuStart;
        m_resetCpuTicks = resetCpuTicks;
        m_lastEmulatedTicksPerSecond = m_emulatedTicksPerSecond();
    }

    public void SyncWithRealTime()
    {
        // Accumulate actual T-states executed since last sync (robust against call frequency changes)
        var currentTicks = m_ticksSinceCpuStart();
        if (TryApplyPendingResync(currentTicks))
            return;
        UpdateTicksPerSecondIfNeeded(currentTicks);

        var delta = currentTicks - m_tStateCountAtLastSync;
        m_tStateCountAtLastSync = currentTicks;
        m_ticksSinceLastSync += delta;

        // Only sync every 2048 ticks for efficiency.
        if (m_ticksSinceLastSync < 2048)
            return;

        m_ticksSinceLastSync = 0;

        // Compute target time while holding the lock, then spin without holding it.
        double targetRealElapsedMs;
        lock (m_lock)
        {
            var emulatedUptimeSecs = (currentTicks - m_tStateCountAtStart) / m_lastEmulatedTicksPerSecond;
            targetRealElapsedMs = emulatedUptimeSecs * 1000.0;
        }

        // Spin for the last bit for tight timing.
        while (m_realTime.ElapsedMilliseconds < targetRealElapsedMs)
        {
            if (m_resyncRequested && TryApplyPendingResync(m_ticksSinceCpuStart()))
                return;
            m_spinWait.SpinOnce();
        }
    }

    public void Reset()
    {
        lock (m_lock)
        {
            m_realTime.Restart();
            m_tStateCountAtStart = 0;
            m_tStateCountAtLastSync = 0;
            m_ticksSinceLastSync = 0;
            m_lastEmulatedTicksPerSecond = m_emulatedTicksPerSecond();
            m_resetCpuTicks();
        }
    }

    public void Resync()
    {
        m_resyncRequested = true;
    }

    private void UpdateTicksPerSecondIfNeeded(long currentTicks)
    {
        var currentTicksPerSecond = m_emulatedTicksPerSecond();
        if (Math.Abs(currentTicksPerSecond - m_lastEmulatedTicksPerSecond) < 0.001f)
            return;

        lock (m_lock)
        {
            if (Math.Abs(currentTicksPerSecond - m_lastEmulatedTicksPerSecond) < 0.001f)
                return;
            m_lastEmulatedTicksPerSecond = currentTicksPerSecond;
            ResetTimingLocked(currentTicks);
        }
    }

    private void ResetTimingLocked(long currentTicks)
    {
        m_tStateCountAtStart = currentTicks;
        m_tStateCountAtLastSync = currentTicks;
        m_ticksSinceLastSync = 0;
        m_realTime.Restart();
    }

    private bool TryApplyPendingResync(long currentTicks)
    {
        if (!m_resyncRequested)
            return false;

        lock (m_lock)
        {
            if (!m_resyncRequested)
                return false;
            m_lastEmulatedTicksPerSecond = m_emulatedTicksPerSecond();
            ResetTimingLocked(currentTicks);
            m_resyncRequested = false;
            return true;
        }
    }
}
