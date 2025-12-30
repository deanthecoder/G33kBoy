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
using System.IO.Compression;
using Avalonia.Media.Imaging;
using DTC.Core;
using DTC.Core.Extensions;
using DTC.Core.UI;
using DTC.SM83.Extensions;
using DTC.SM83.HostDevices;
using DTC.SM83.Snapshot;
using Material.Icons;

namespace DTC.SM83;

public sealed class GameBoy : IDisposable
{
    private readonly ClockSync m_clockSync;
    private readonly LcdScreen m_screen;
    private readonly Stopwatch m_frameStopwatch = Stopwatch.StartNew();
    private readonly SoundDevice m_audioSink;
    private readonly bool[] m_soundChannelsEnabled = [true, true, true, true];
    private readonly Lock m_cpuStepLock = new();
    private long m_lastFrameClockTicks;
    private long m_lastFrameCpuTicks;
    private Bus m_bus;
    private Cpu m_cpu;
    private Thread m_cpuThread;
    private bool m_shutdownRequested;
    private Cartridge m_loadedCartridge;
    private bool m_lcdEmulationEnabled = true;
    private bool m_isUserSoundEnabled = true;
    private bool m_isRunningAtNormalSpeed = true;
    private bool m_isCpuHistoryTracked;
    private double m_relativeSpeedRaw;
    private GameBoyMode m_requestedMode = GameBoyMode.Cgb;
    private string m_loadedRomPath;
    private byte[] m_frameBufferScratch;

    public event EventHandler<string> RomLoaded;
    public event EventHandler DisplayUpdated;

    public WriteableBitmap Display => m_screen.Display;
    public GameBoyMode Mode { get; private set; } = GameBoyMode.Dmg;

    public double RelativeSpeed => Math.Round(m_relativeSpeedRaw / 0.2) * 0.2;

    public Joypad Joypad { get; }
    public SnapshotHistory SnapshotHistory { get; }
    public bool IsRunning => m_cpu != null && m_bus != null;
    public bool HasLoadedCartridge => m_loadedCartridge != null;
    public ulong CpuClockTicks => m_bus?.CpuClockTicks ?? 0;

    /// <summary>
    /// Debugging aid.
    /// </summary>
    private static bool WriteDisassemblyOnLoad => false;

    public GameBoy()
    {
        Joypad = new Joypad();
        m_audioSink = new SoundDevice(44100);
        CreateHardware();
        m_screen = new LcdScreen(PPU.FrameWidth, PPU.FrameHeight)
        {
            Mode = Mode
        };

        m_clockSync = new ClockSync(GetEffectiveCpuHz, () => (long)(m_bus?.CpuClockTicks ?? 0), ResetBusClock);

        // m_cpu.AddDebugger(new MemoryWriteDebugger(0x1234, () => Console.WriteLine("Memory write detected!")));
        // m_cpu.AddDebugger(new MemoryWriteDebugger(0x1234, targetValue: 0x34, () => Console.WriteLine("Memory write detected!")));
        // m_cpu.AddDebugger(new MemoryReadDebugger(0x1234, value => Console.WriteLine($"Memory read detected! (0x{value:X2})")));
        // m_cpu.AddDebugger(new MemoryReadDebugger(0x1234, targetValue: 0x34, value => Console.WriteLine($"Memory read detected! (0x{value:X2})")));
        // m_cpu.AddDebugger(new PcBreakpointDebugger(0x1234, () => Console.WriteLine("PC breakpoint hit!")));
        SnapshotHistory = new SnapshotHistory(this);
    }

    public void PowerOnAsync(FileInfo romFile)
    {
        if (romFile == null)
            throw new ArgumentNullException(nameof(romFile));

        if (!romFile.Exists)
        {
            Logger.Instance.Warn($"ROM file '{romFile.FullName}' not found. Unable to power on.");
            return;
        }

        var romData = ReadRomData(romFile, out var cartridgeKey);
        if (romData == null || romData.Length == 0)
        {
            DialogService.Instance.ShowMessage($"Unable to load ROM '{romFile.Name}'", "No valid ROM data found.", MaterialIconKind.GamepadClassicOutline);
            return;
        }

        var cartridge = new Cartridge(romData);
        var supportCheck = cartridge.IsSupported();
        if (!supportCheck.IsSupported)
        {
            DialogService.Instance.ShowMessage($"Unable to load ROM '{romFile.Name}'", supportCheck.Message, MaterialIconKind.GamepadClassicOutline);
            return;
        }

        ShutdownCpuThread();
        RecreateHardware();
        m_clockSync.Reset();

        m_loadedCartridge = cartridge;
        m_loadedRomPath = romFile.FullName;
        ApplyHardwareMode(m_loadedCartridge);
        RomLoaded?.Invoke(this, m_loadedCartridge.Title);
        m_cpu.LoadRom(m_loadedCartridge);
        SnapshotHistory.ResetForRom(m_loadedRomPath);

        WriteDisassemblyIfEnabled(romFile, romData, cartridgeKey);

        m_audioSink?.Start();
        
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
            
                lock (m_cpuStepLock)
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

    private static byte[] ReadRomData(FileInfo romFile, out string cartridgeKey)
    {
        cartridgeKey = romFile.Name;
        if (!romFile.Extension.Equals(".zip", StringComparison.OrdinalIgnoreCase))
            return romFile.ReadAllBytes();

        using var archive = ZipFile.OpenRead(romFile.FullName);
        foreach (var entry in archive.Entries)
        {
            if (!entry.Name.EndsWith(".gb", StringComparison.OrdinalIgnoreCase) && !entry.Name.EndsWith(".gbc", StringComparison.OrdinalIgnoreCase))
                continue;

            var buffer = new byte[(int)entry.Length];
            using var stream = entry.Open();
            stream.ReadExactly(buffer.AsSpan());

            cartridgeKey = entry.Name;
            return buffer;
        }

        return null;
    }

    private void OnFrameRendered(object sender, byte[] frameBuffer)
    {
        // Calculate relative speed based on emulated CPU clock ticks.
        var currentClockTicks = (long)(m_bus?.ClockTicks ?? 0);
        var clockTicksDelta = currentClockTicks - m_lastFrameClockTicks;
        m_lastFrameClockTicks = currentClockTicks;
        LogFrameTiming(clockTicksDelta);

        var currentCpuTicks = (long)(m_bus?.CpuClockTicks ?? 0);
        var cpuTicksDelta = currentCpuTicks - m_lastFrameCpuTicks;
        m_lastFrameCpuTicks = currentCpuTicks;

        if (cpuTicksDelta > 0)
        {
            var elapsedSecs = m_frameStopwatch.Elapsed.TotalSeconds;
            m_frameStopwatch.Restart();
            
            if (elapsedSecs > 0)
            {
                // Calculate actual emulated Hz based on ticks executed vs real time elapsed
                var emulatedHz = cpuTicksDelta / elapsedSecs;
                var speedPercentage = emulatedHz / GetEffectiveCpuHz();

                // Apply exponential moving average filter to smooth out the value
                m_relativeSpeedRaw = m_relativeSpeedRaw * 0.98 + speedPercentage * 0.02;
            }
        }

        var didUpdate = m_screen.Update(frameBuffer);
        if (didUpdate)
            DisplayUpdated?.Invoke(this, EventArgs.Empty);

        SnapshotHistory?.OnFrameRendered(m_bus?.CpuClockTicks ?? 0);
    }

    public void Dispose()
    {
        m_audioSink?.Dispose();
        ShutdownCpuThread();
        DisposeHardware();
        Joypad.Dispose();
        m_screen.Dispose();
    }

    public void SetSpeed(ClockSync.Speed speed)
    {
        var isNormalSpeed = speed == ClockSync.Speed.Actual;
        if (m_isRunningAtNormalSpeed != isNormalSpeed)
        {
            m_isRunningAtNormalSpeed = isNormalSpeed;
            UpdateSoundEnabled();
        }

        m_clockSync.SetSpeed(speed);
    }

    public void SetRequestedMode(GameBoyMode mode)
    {
        if (m_requestedMode == mode)
            return;
        m_requestedMode = mode;
        if (m_loadedCartridge != null)
            ApplyHardwareMode(m_loadedCartridge);
    }

    public void SetBackgroundVisibility(bool isVisible)
    {
        var ppu = m_bus?.PPU;
        if (ppu != null)
            ppu.BackgroundVisible = isVisible;
    }

    public void SetSpriteVisibility(bool isVisible)
    {
        var ppu = m_bus?.PPU;
        if (ppu != null)
            ppu.SpritesVisible = isVisible;
    }

    public void SetLcdEmulation(bool isEnabled)
    {
        m_lcdEmulationEnabled = isEnabled;
        m_screen.LcdEmulationEnabled = isEnabled;
        var ppu = m_bus?.PPU;
        if (ppu != null)
            ppu.LcdEmulationEnabled = isEnabled;
    }

    public void SetSoundChannelEnabled(int channel, bool isEnabled)
    {
        if (channel is < 1 or > 4)
            return;

        m_soundChannelsEnabled[channel - 1] = isEnabled;
        m_bus?.SetSoundChannelEnabled(channel, isEnabled);
    }

    public void SaveScreenshot(FileInfo tgaFile)
    {
        if (tgaFile == null)
            throw new ArgumentNullException(nameof(tgaFile));
        var ppu = m_bus?.PPU ?? throw new InvalidOperationException("Game Boy hardware is not initialized.");
        ppu.Dump(tgaFile);
    }

    public void ExportTileMap(FileInfo tgaFile)
    {
        if (tgaFile == null)
            throw new ArgumentNullException(nameof(tgaFile));
        var ppu = m_bus?.PPU ?? throw new InvalidOperationException("Game Boy hardware is not initialized.");
        ppu.DumpTileMap(tgaFile);
    }

    public void DumpCpuHistory() =>
        m_cpu?.InstructionLogger.DumpToConsole();

    public void SetCpuHistoryTracking(bool isEnabled)
    {
        m_isCpuHistoryTracked = isEnabled;
        if (m_cpu != null)
            m_cpu.InstructionLogger.IsEnabled = isEnabled;
    }
    
    public void SetSoundEnabled(bool isEnabled)
    {
        m_isUserSoundEnabled = isEnabled;
        UpdateSoundEnabled();
    }

    public void SetHardwareLowPassFilterEnabled(bool isEnabled) =>
        m_audioSink?.SetLowPassFilterEnabled(isEnabled);

    private void UpdateSoundEnabled()
    {
        // Auto-mute whenever the emulation is not running at 100% speed to avoid mangled audio.
        var shouldEnableSound = m_isUserSoundEnabled && m_isRunningAtNormalSpeed;
        m_audioSink?.SetEnabled(shouldEnableSound);
    }

    private void ApplySoundChannelSettings()
    {
        if (m_bus == null)
            return;

        for (var channel = 1; channel <= m_soundChannelsEnabled.Length; channel++)
            m_bus.SetSoundChannelEnabled(channel, m_soundChannelsEnabled[channel - 1]);
    }
    
    public bool IsDebugBuild
    {
        get
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }
    }
    
    private static void WriteDisassemblyIfEnabled(FileInfo romFile, byte[] romData, string cartridgeKey)
    {
        if (!WriteDisassemblyOnLoad || romFile == null || romData == null || romData.Length == 0)
            return;

        try
        {
            var asmFile = BuildAsmFilePath(romFile, cartridgeKey);
            var lines = Disassembler.DisassembleRom(romData);
            File.WriteAllLines(asmFile.FullName, lines);
            Logger.Instance.Info($"Disassembled ROM to '{asmFile.FullName}'.");
        }
        catch (Exception ex)
        {
            Logger.Instance.Warn($"Failed to write ROM disassembly: {ex.Message}");
        }
    }

    private static FileInfo BuildAsmFilePath(FileInfo romFile, string cartridgeKey)
    {
        var directory = romFile.DirectoryName ?? Environment.CurrentDirectory;
        var baseName = romFile.Extension.Equals(".zip", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(cartridgeKey)
            ? Path.GetFileNameWithoutExtension(cartridgeKey)
            : Path.GetFileNameWithoutExtension(romFile.Name);

        return new FileInfo(Path.Combine(directory, $"{baseName}.asm"));
    }

    private void ShutdownCpuThread()
    {
        m_shutdownRequested = true;
        m_cpuThread?.Join();
        m_cpuThread = null;
        m_shutdownRequested = false;
    }

    private void RecreateHardware()
    {
        var debuggers = m_cpu?.Debuggers;
        DisposeHardware();
        CreateHardware();
        debuggers?.ForEach(d => m_cpu.AddDebugger(d));
    }

    private void CreateHardware()
    {
        m_bus = new Bus(0x10000, Bus.BusType.GameBoy, Joypad, m_audioSink);
        m_bus.SetMode(Mode);
        ApplySoundChannelSettings();
        m_bus.PPU.LcdEmulationEnabled = m_lcdEmulationEnabled;
        m_cpu = new Cpu(m_bus)
        {
            InstructionLogger =
            {
                IsEnabled = m_isCpuHistoryTracked && IsDebugBuild
            }
        };
        m_bus.PPU.FrameRendered += OnFrameRendered;
    }

    private void DisposeHardware()
    {
        if (m_bus?.PPU != null)
            m_bus.PPU.FrameRendered -= OnFrameRendered;

        m_bus?.Dispose();
        m_bus = null;
        m_cpu = null;
    }

    private long ResetBusClock()
    {
        m_bus?.ResetClock();
        return 0;
    }

    public int GetStateSize() =>
        m_cpu?.GetStateSize() ?? 0;

    public void CaptureState(MachineState state, Span<byte> frameBuffer)
    {
        if (state == null)
            throw new ArgumentNullException(nameof(state));
        if (frameBuffer.Length != PPU.FrameWidth * PPU.FrameHeight * 4)
            throw new ArgumentException("Frame buffer size mismatch.", nameof(frameBuffer));
        if (m_cpu == null)
            throw new InvalidOperationException("Game Boy hardware is not initialized.");
        if (m_bus?.PPU == null)
            throw new InvalidOperationException("PPU is not available for state capture.");

        lock (m_cpuStepLock)
        {
            m_cpu.SaveState(state);
            m_bus.PPU.CopyFrameBuffer(frameBuffer);
        }
    }

    public void LoadState(MachineState state)
    {
        if (state == null)
            throw new ArgumentNullException(nameof(state));
        if (m_cpu == null)
            throw new InvalidOperationException("Game Boy hardware is not initialized.");

        lock (m_cpuStepLock)
            m_cpu.LoadState(state);

        RefreshDisplay();
        m_relativeSpeedRaw = 1.0;
        m_lastFrameClockTicks = (long)(m_bus?.ClockTicks ?? 0);
        m_lastFrameCpuTicks = (long)(m_bus?.CpuClockTicks ?? 0);
        m_frameStopwatch.Restart();
        m_clockSync.Resync();
    }

    private void RefreshDisplay()
    {
        if (m_bus?.PPU == null)
            return;

        m_frameBufferScratch ??= new byte[PPU.FrameWidth * PPU.FrameHeight * 4];
        m_bus.PPU.CopyFrameBuffer(m_frameBufferScratch);
        if (m_screen.Update(m_frameBufferScratch))
            DisplayUpdated?.Invoke(this, EventArgs.Empty);
    }

    private double GetEffectiveCpuHz() =>
        m_bus?.IsDoubleSpeed == true ? Cpu.Hz * 2.0 : Cpu.Hz;

    [Conditional("DEBUG")]
    private static void LogFrameTiming(long clockTicksDelta)
    {
        if (clockTicksDelta <= 0)
            return;
        const int expectedTicksPerFrame = 70224;
        const int tolerance = 32;
        if (Math.Abs(clockTicksDelta - expectedTicksPerFrame) > tolerance)
            Logger.Instance.Info($"Frame timing drift: {clockTicksDelta} ticks (expected {expectedTicksPerFrame}).");
    }


    private void ApplyHardwareMode(Cartridge cartridge)
    {
        Mode = DetermineEffectiveMode(cartridge, m_requestedMode);
        m_bus?.SetMode(Mode);
        if (m_screen != null)
            m_screen.Mode = Mode;
    }

    private static GameBoyMode DetermineEffectiveMode(Cartridge cartridge, GameBoyMode requestedMode)
    {
        if (requestedMode == GameBoyMode.Cgb || cartridge.IsCgbOnly)
            return GameBoyMode.Cgb;
        return GameBoyMode.Dmg;
    }
}
