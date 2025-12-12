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
    private readonly double m_emulatedTicksPerSecond;
    private readonly Func<long> m_ticksSinceCpuStart;
    private readonly Func<long> m_resetCpuTicks;
    private readonly Lock m_lock = new();
    private Speed m_speed = Speed.Actual;

    /// <summary>
    /// Number of T states when this stopwatch was started.
    /// </summary>
    private long m_tStateCountAtStart;

    private long m_tStateCountAtLastSync;
    private long m_ticksSinceLastSync;

    public enum Speed { Actual, Fast, Maximum, Pause }

    public ClockSync(double emulatedCpuHz, Func<long> ticksSinceCpuStart, Func<long> resetCpuTicks)
    {
        m_realTime = Stopwatch.StartNew();
        m_emulatedTicksPerSecond = emulatedCpuHz;
        m_ticksSinceCpuStart = ticksSinceCpuStart;
        m_resetCpuTicks = resetCpuTicks;
    }

    /// <summary>
    /// Operations external to emulation (such as loading a ROM) should pause
    /// emulated machine whilst they're 'busy'.
    /// </summary>
    public IDisposable CreatePauser() => new Pauser(m_realTime, m_lock);

    /// <summary>
    /// Call to set whether the emulator is running at 100% emulated speed,
    /// or 'full throttle'.
    /// </summary>
    public void SetSpeed(Speed speed)
    {
        lock (m_lock)
        {
            if (m_speed == speed)
                return;
            m_speed = speed;

            // Reset the timing variables when re-enabling 100% emulated speed.
            var currentTicks = m_ticksSinceCpuStart();
            m_tStateCountAtStart = currentTicks;
            m_tStateCountAtLastSync = currentTicks;
            m_ticksSinceLastSync = 0;
            m_realTime.Restart();
        }
    }

    public void SyncWithRealTime()
    {
        if (m_speed == Speed.Maximum)
            return; // Don't delay.

        // For pause mode, always sleep every tick
        if (m_speed == Speed.Pause)
        {
            Thread.Sleep(50);
            return;
        }

        // Accumulate actual T-states executed since last sync (robust against call frequency changes)
        var currentTicks = m_ticksSinceCpuStart();
        var delta = currentTicks - m_tStateCountAtLastSync;
        m_tStateCountAtLastSync = currentTicks;
        m_ticksSinceLastSync += delta;

        // For non-pause modes, only sync every 2048 ticks for efficiency
        if (m_ticksSinceLastSync < 2048)
            return;

        m_ticksSinceLastSync = 0;

        // Compute target time while holding the lock
        long targetRealElapsedTicks;
        lock (m_lock)
        {
            var emulatedUptimeSecs = (currentTicks - m_tStateCountAtStart) / m_emulatedTicksPerSecond;

            var speedMultiplier = m_speed switch
            {
                Speed.Fast => 1.6,
                _ => 1.0
            };

            targetRealElapsedTicks = (long)(Stopwatch.Frequency * emulatedUptimeSecs / speedMultiplier);
        }

        // Wait outside the lock (hybrid sleep + spin for efficiency)
        var remaining = targetRealElapsedTicks - m_realTime.ElapsedTicks;
        if (remaining > 0)
        {
            // If we need to wait more than ~2ms, sleep to avoid pegging the CPU
            var remainingMs = remaining * 1000.0 / Stopwatch.Frequency;
            if (remainingMs > 2.0)
                Thread.Sleep((int)(remainingMs - 1.0)); // Sleep most of it, leave 1ms for spin

            // Spin for the last bit for tight timing
            var spinWait = new SpinWait();
            while (m_realTime.ElapsedTicks < targetRealElapsedTicks)
                spinWait.SpinOnce();
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
            m_resetCpuTicks();
        }
    }

    private class Pauser : IDisposable
    {
        private readonly Stopwatch m_stopwatch;
        private readonly Lock m_lock;

        public Pauser(Stopwatch stopwatch, Lock lockObj)
        {
            m_stopwatch = stopwatch;
            m_lock = lockObj;
            lock (m_lock)
                stopwatch.Stop();
        }

        public void Dispose()
        {
            lock (m_lock)
                m_stopwatch.Start();
        }
    }
}
