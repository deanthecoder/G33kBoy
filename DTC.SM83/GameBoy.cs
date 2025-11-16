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
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using DTC.Core;
using DTC.Core.Extensions;
using DTC.SM83.Extensions;

namespace DTC.SM83;

public sealed class GameBoy : IDisposable
{
    private readonly Bus m_bus;
    private readonly Cpu m_cpu;
    private readonly ClockSync m_clockSync;
    private readonly Joypad m_joypad;

    private Thread m_cpuThread;
    private bool m_shutdownRequested;
    private readonly Stopwatch m_frameStopwatch = Stopwatch.StartNew();
    private double m_relativeSpeed;

    public event EventHandler DisplayUpdated;

    public WriteableBitmap Display { get; }

    public double RelativeSpeed => m_relativeSpeed;
    public Joypad Joypad => m_joypad;

    public GameBoy()
    {
        m_joypad = new Joypad();
        m_bus = new Bus(0x10000, Bus.BusType.GameBoy, m_joypad);
        m_cpu = new Cpu(m_bus) { DebugMode = true };
        Display = new WriteableBitmap(new PixelSize(PPU.FrameWidth, PPU.FrameHeight), new Vector(96, 96), PixelFormat.Rgba8888);

        m_bus.PPU.FrameRendered += OnFrameRendered;

        m_clockSync = new ClockSync(4194304, () => (long)m_bus.ClockTicks, () => 0);
    }

    public void PowerOnAsync()
    {
        var romFile = new FileInfo(@"external\blargg-test-roms\cpu_instrs\cpu_instrs.gb");
        if (romFile.Exists)
            m_cpu.LoadRom(romFile.ReadAllBytes());
        
        m_cpuThread = new Thread(RunLoop) { Name = "GameBoy CPU" };
        m_cpuThread.Start();
    }

    private void RunLoop()
    {
        m_shutdownRequested = false;
        
        Logger.Instance.Info("CPU loop started.");

        try
        {
            while (!m_shutdownRequested)
            {
                // Sync the clock speed.
                m_clockSync.SyncWithRealTime();
            
                m_cpu.Step();
            }
        }
        catch (Exception e)
        {
            // Shut down gracefully.
            Logger.Instance.Error($"Stopping CPU loop due to exception: {e.Message}");
        }
        
        Logger.Instance.Info("CPU loop stopped.");
    }

    private void OnFrameRendered(object sender, byte[] frameBuffer)
    {
        using (var lockedBuffer = Display.Lock())
            Marshal.Copy(frameBuffer, 0, lockedBuffer.Address, frameBuffer.Length);

        // Calculate relative speed based on frame frequency (60Hz = 100%)
        var elapsedMs = m_frameStopwatch.Elapsed.TotalMilliseconds;
        if (elapsedMs > 0)
        {
            var currentFrequency = 1000.0 / elapsedMs;
            var speedPercentage = currentFrequency / 60.0;

            // Apply filtering to smooth out the value
            m_relativeSpeed = Math.Round(speedPercentage / 0.2) * 0.2;
        }
        m_frameStopwatch.Restart();

        DisplayUpdated?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        m_shutdownRequested = true;
        m_cpuThread.Join();
        
        m_bus.Dispose();
        m_joypad.Dispose();
    }

    public void SetSpeed(ClockSync.Speed speed) =>
        m_clockSync.SetSpeed(speed);

    public void SaveScreenshot(FileInfo tgaFile)
    {
        if (tgaFile == null)
            throw new ArgumentNullException(nameof(tgaFile));
        m_bus.PPU.Dump(tgaFile);
    }

    public void ExportTileMap(FileInfo tgaFile)
    {
        if (tgaFile == null)
            throw new ArgumentNullException(nameof(tgaFile));
        m_bus.PPU.DumpTileMap(tgaFile);
    }
}
