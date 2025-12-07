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
using Material.Icons;

namespace DTC.SM83;

public sealed class GameBoy : IDisposable
{
    private static readonly TimeSpan CartRamPersistInterval = TimeSpan.FromSeconds(5);
    private readonly ClockSync m_clockSync;
    private readonly IGameDataStore m_gameDataStore;
    private readonly Stopwatch m_ramPersistStopwatch = new();
    private readonly LcdScreen m_screen;
    private readonly Stopwatch m_frameStopwatch = Stopwatch.StartNew();
    private Bus m_bus;
    private Cpu m_cpu;
    private string m_cartridgeKey;
    private Thread m_cpuThread;
    private bool m_shutdownRequested;
    private Cartridge m_loadedCartridge;
    private bool m_lcdEmulationEnabled = true;
    private readonly SoundDevice m_audioSink;
    private readonly bool[] m_soundChannelsEnabled = [true, true, true, true];
    private bool m_isUserSoundEnabled = true;
    private bool m_isRunningAtNormalSpeed = true;
    private bool m_isCpuHistoryTracked;

    public event EventHandler<string> RomLoaded;
    public event EventHandler DisplayUpdated;

    public WriteableBitmap Display => m_screen.Display;

    public double RelativeSpeed { get; private set; }

    public Joypad Joypad { get; }

    public bool WriteDisassemblyOnLoad { get; set; }

    public GameBoy(IGameDataStore gameDataStore = null)
    {
        m_gameDataStore = gameDataStore;
        Joypad = new Joypad();
        m_audioSink = new SoundDevice(44100);
        CreateHardware();
        m_screen = new LcdScreen(PPU.FrameWidth, PPU.FrameHeight);

        m_clockSync = new ClockSync(Cpu.Hz, () => (long)(m_bus?.ClockTicks ?? 0), ResetBusClock);

        // WriteDisassemblyOnLoad = true;
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
        SetAutoFireEnabled(false); // Reset auto-fire on cartridge change.

        m_cartridgeKey = cartridgeKey;
        m_loadedCartridge = cartridge;
        RomLoaded?.Invoke(this, m_loadedCartridge.Title);
        m_cpu.LoadRom(m_loadedCartridge);

        WriteDisassemblyIfEnabled(romFile, romData, cartridgeKey);
        RestoreSavedGameData();

        m_audioSink?.Start();
        
        m_ramPersistStopwatch.Restart();
        m_cpuThread = new Thread(RunLoop) { Name = "GameBoy CPU" };
        m_cpuThread.Start();
    }

    private void RunLoop()
    {
        m_shutdownRequested = false;
        
        Logger.Instance.Info("CPU loop started.");
        var canPersistGameData = CanPersistGameData;

        try
        {
            while (!m_shutdownRequested)
            {
                // Sync the clock speed.
                m_clockSync.SyncWithRealTime();
            
                m_cpu.Step();
                PersistCartRamIfDue(canPersistGameData);
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
            if (!entry.Name.EndsWith(".gb", StringComparison.OrdinalIgnoreCase))
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
        var didUpdate = m_screen.Update(frameBuffer);

        // Calculate relative speed based on frame frequency (60Hz = 100%)
        var elapsedMs = m_frameStopwatch.Elapsed.TotalMilliseconds;
        if (elapsedMs > 0)
        {
            var currentFrequency = 1000.0 / elapsedMs;
            var speedPercentage = currentFrequency / 60.0;

            // Apply filtering to smooth out the value
            RelativeSpeed = Math.Round(speedPercentage / 0.2) * 0.2;
        }
        m_frameStopwatch.Restart();

        if (didUpdate)
            DisplayUpdated?.Invoke(this, EventArgs.Empty);
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

    public void SetAutoFireEnabled(bool isEnabled) =>
        Joypad.AutoFireEnabled = isEnabled;
    
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
    
    public void ClearAllGameData() =>
        m_gameDataStore?.ClearAllGameData();
    
    public void SetSoundEnabled(bool isEnabled)
    {
        m_isUserSoundEnabled = isEnabled;
        UpdateSoundEnabled();
    }

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

    private bool CanPersistGameData =>
        m_gameDataStore != null &&
        m_loadedCartridge?.SupportsBattery == true &&
        !string.IsNullOrEmpty(m_cartridgeKey) &&
        m_bus?.CartridgeRam != null;

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

    private void PersistCartRamIfDue(bool canPersistGameData)
    {
        if (!canPersistGameData)
            return;

        if (!m_ramPersistStopwatch.IsRunning)
            m_ramPersistStopwatch.Start();

        if (m_ramPersistStopwatch.Elapsed < CartRamPersistInterval)
            return;

        m_ramPersistStopwatch.Restart();
        SaveCartRamState();
    }

    private void SaveCartRamState()
    {
        try
        {
            var cartRam = m_bus?.CartridgeRam;
            if (cartRam == null)
                return;

            var snapshot = cartRam.GetSnapshot();
            m_gameDataStore?.SaveGameData(m_cartridgeKey, snapshot);
        }
        catch (Exception ex)
        {
            Logger.Instance.Warn($"Failed to persist cartridge RAM: {ex.Message}");
        }
    }

    private void RestoreSavedGameData()
    {
        if (!CanPersistGameData)
            return;

        var snapshot = m_gameDataStore?.LoadGameData(m_cartridgeKey);
        if (snapshot == null || snapshot.Length == 0)
            return;

        try
        {
            var cartRam = m_bus?.CartridgeRam;
            if (cartRam == null)
                return;

            cartRam.LoadSnapshot(snapshot);
            Logger.Instance.Info($"Restored {snapshot.Length} bytes of cartridge RAM for '{m_cartridgeKey}'.");
        }
        catch (Exception ex)
        {
            Logger.Instance.Warn($"Failed to restore cartridge RAM: {ex.Message}");
        }
    }

    private void WriteDisassemblyIfEnabled(FileInfo romFile, byte[] romData, string cartridgeKey)
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
        DisposeHardware();
        CreateHardware();
    }

    private void CreateHardware()
    {
        m_bus = new Bus(0x10000, Bus.BusType.GameBoy, Joypad, m_audioSink);
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
}
