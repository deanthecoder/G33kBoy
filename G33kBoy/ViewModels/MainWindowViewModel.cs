// Code authored by Dean Edis (DeanTheCoder).
// Anyone is free to copy, modify, use, compile, or distribute this software,
// either in source code form or as a compiled binary, for any purpose.
//
// If you modify the code, please retain this copyright header,
// and consider contributing back to the repository or letting us know
// about your modifications. Your contributions are valued!
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.

using System;
using System.ComponentModel;
using System.IO;
using Avalonia;
using Avalonia.Media;
using DTC.Core;
using DTC.Core.Commands;
using DTC.Core.Extensions;
using DTC.Core.UI;
using DTC.Core.ViewModels;
using DTC.Emulation;
using DTC.Emulation.Audio;
using EmulationScreen = DTC.Emulation.LcdScreen;
using DTC.Emulation.Rom;
using DTC.Emulation.Snapshot;
using DTC.SM83;
using DTC.SM83.Devices;
using SnapshotFile = DTC.SM83.Snapshot.SnapshotFile;

namespace G33kBoy.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase, IDisposable
{
    private const int AudioSampleRateHz = 44100;
    private static readonly string[] RomExtensions = [".gb", ".gbc"];
    private string m_windowTitle;
    private bool m_isCpuHistoryTracked;
    private bool m_isSoundChannel1Enabled = true;
    private bool m_isSoundChannel2Enabled = true;
    private bool m_isSoundChannel3Enabled = true;
    private bool m_isSoundChannel4Enabled = true;
    private readonly GameBoyMachine m_machine;
    private readonly MachineRunner m_machineRunner;
    private readonly EmulatorViewModel m_emulator;
    private string m_loadedRomPath;
    private string m_currentRomTitle = "G33kBoy";

    public MruFiles Mru { get; }

    public IImage Display { get; }

    public SnapshotHistory SnapshotHistory { get; }

    public Joypad Joypad => m_machine.Joypad;

    public Settings Settings => Settings.Instance;

    public string WindowTitle
    {
        get => m_windowTitle ?? "G33kBoy";
        private set => SetField(ref m_windowTitle, value);
    }

    public bool IsRecording => m_emulator.IsRecording;

    public bool IsRecordingIndicatorOn => m_emulator.IsRecordingIndicatorOn;

    public event EventHandler DisplayUpdated;

    public bool IsDebugBuild =>
#if DEBUG
        true;
#else
        false;
#endif

    public bool IsSoundChannel1Enabled
    {
        get => m_isSoundChannel1Enabled;
        private set
        {
            if (!SetField(ref m_isSoundChannel1Enabled, value))
                return;
            m_machine.SetSoundChannelEnabled(1, value);
        }
    }

    public bool IsSoundChannel2Enabled
    {
        get => m_isSoundChannel2Enabled;
        private set
        {
            if (!SetField(ref m_isSoundChannel2Enabled, value))
                return;
            m_machine.SetSoundChannelEnabled(2, value);
        }
    }

    public bool IsSoundChannel3Enabled
    {
        get => m_isSoundChannel3Enabled;
        private set
        {
            if (!SetField(ref m_isSoundChannel3Enabled, value))
                return;
            m_machine.SetSoundChannelEnabled(3, value);
        }
    }

    public bool IsSoundChannel4Enabled
    {
        get => m_isSoundChannel4Enabled;
        private set
        {
            if (!SetField(ref m_isSoundChannel4Enabled, value))
                return;
            m_machine.SetSoundChannelEnabled(4, value);
        }
    }

    public bool IsCpuHistoryTracked
    {
        get => m_isCpuHistoryTracked;
        private set
        {
            if (!SetField(ref m_isCpuHistoryTracked, value))
                return;
            Settings.IsCpuHistoryTracked = value;
            m_machine.SetCpuHistoryTracking(value);
        }
    }

    public bool IsDisplayBlurEnabled => Settings.IsLcdEmulationEnabled && m_machine.Mode == GameBoyMode.Cgb;

    public bool IsDisplayBlurDisabled => !IsDisplayBlurEnabled;

    public bool IsDmgPaletteDefault => Settings.DmgPalette == DmgPalette.Default;

    public bool IsDmgPaletteSepia => Settings.DmgPalette == DmgPalette.Sepia;

    public bool IsDmgPaletteBlackAndWhite => Settings.DmgPalette == DmgPalette.BlackAndWhite;

    public bool IsDmgPaletteBlue => Settings.DmgPalette == DmgPalette.Blue;

    public bool IsDmgPaletteRed => Settings.DmgPalette == DmgPalette.Red;

    public bool IsDmgPaletteCyan => Settings.DmgPalette == DmgPalette.Cyan;

    public bool IsDmgPaletteMagenta => Settings.DmgPalette == DmgPalette.Magenta;

    public MainWindowViewModel()
    {
        Mru = new MruFiles().InitFromString(Settings.MruFiles);
        Mru.OpenRequested += (_, file) => LoadRomFromFile(file, addToMru: false);

        var screen = new EmulationScreen(PPU.FrameWidth, PPU.FrameHeight);
        var audioSink = new SoundDevice(AudioSampleRateHz);

        var descriptor = CreateMachineDescriptor();
        m_machine = new GameBoyMachine(descriptor, audioSink);
        m_machine.SetRequestedMode(Settings.IsCgbModePreferred ? GameBoyMode.Cgb : GameBoyMode.Dmg);

        m_machineRunner = new MachineRunner(m_machine, GetEffectiveCpuHz, e =>
        {
            Logger.Instance.Error($"Stopping CPU loop due to exception: {e.Message}");
        });
        m_emulator = new EmulatorViewModel(
            m_machine,
            m_machineRunner,
            audioSink,
            screen,
            GetVideoFrameRate,
            () => m_currentRomTitle,
            GetEffectiveCpuHz);
        m_emulator.DisplayUpdated += (_, _) => DisplayUpdated?.Invoke(this, EventArgs.Empty);
        m_emulator.PropertyChanged += (_, e) => OnPropertyChanged(e.PropertyName);
        m_emulator.SetScreenEffectEnabled(false);
        Display = m_emulator.Display;
        SnapshotHistory = m_emulator.SnapshotHistory;
        Settings.PropertyChanged += OnSettingsPropertyChanged;
        IsCpuHistoryTracked = Settings.IsCpuHistoryTracked;
        ApplyDisplayVisibilitySettings();
        ApplySoundEnabledSetting();
        ApplyHardwareLowPassFilterSetting();
        ApplySoundChannelSettings();
    }

    public void ToggleAmbientBlur() =>
        Settings.IsAmbientBlurred = !Settings.IsAmbientBlurred;

    public void ToggleBackgroundVisibility()
    {
        Settings.IsBackgroundVisible = !Settings.IsBackgroundVisible;
        ApplyDisplayVisibilitySettings();
    }

    public void ToggleSpriteVisibility()
    {
        Settings.AreSpritesVisible = !Settings.AreSpritesVisible;
        ApplyDisplayVisibilitySettings();
    }

    public void ToggleLcdEmulation()
    {
        Settings.IsLcdEmulationEnabled = !Settings.IsLcdEmulationEnabled;
        ApplyDisplayVisibilitySettings();
    }

    public void SetDmgPaletteDefault() => SetDmgPalette(DmgPalette.Default);

    public void SetDmgPaletteSepia() => SetDmgPalette(DmgPalette.Sepia);

    public void SetDmgPaletteBlackAndWhite() => SetDmgPalette(DmgPalette.BlackAndWhite);

    public void SetDmgPaletteBlue() => SetDmgPalette(DmgPalette.Blue);

    public void SetDmgPaletteRed() => SetDmgPalette(DmgPalette.Red);

    public void SetDmgPaletteCyan() => SetDmgPalette(DmgPalette.Cyan);

    public void SetDmgPaletteMagenta() => SetDmgPalette(DmgPalette.Magenta);

    public void ToggleCgbMode()
    {
        Settings.IsCgbModePreferred = !Settings.IsCgbModePreferred;
        m_machine.SetRequestedMode(Settings.IsCgbModePreferred ? GameBoyMode.Cgb : GameBoyMode.Dmg);
        ResetDevice();
    }

    public void ToggleSoundChannel1() =>
        IsSoundChannel1Enabled = !IsSoundChannel1Enabled;

    public void ToggleSoundChannel2() =>
        IsSoundChannel2Enabled = !IsSoundChannel2Enabled;

    public void ToggleSoundChannel3() =>
        IsSoundChannel3Enabled = !IsSoundChannel3Enabled;

    public void ToggleSoundChannel4() =>
        IsSoundChannel4Enabled = !IsSoundChannel4Enabled;

    public void ToggleHardwareLowPassFilter()
    {
        Settings.IsHardwareLowPassFilterEnabled = !Settings.IsHardwareLowPassFilterEnabled;
        ApplyHardwareLowPassFilterSetting();
    }

    public void ToggleRecording() =>
        m_emulator.ToggleRecording();

    public void StartRecording() =>
        m_emulator.StartRecording();

    public void StopRecording() =>
        m_emulator.StopRecording();

    public void LoadGameRom()
    {
        var keyBlocker = Joypad.CreatePressBlocker();
        var command = new FileOpenCommand("Load Game Boy ROM or Snapshot", "Game Boy Files", ["*.gb", "*.gbc", "*.zip", "*.sav"]);
        command.FileSelected += (_, info) =>
        {
            try
            {
                if (IsSnapshotFile(info))
                    LoadSnapshotFile(info);
                else
                    LoadRomFromFile(info, addToMru: true);
            }
            finally
            {
                keyBlocker.Dispose();
            }
        };
        command.Cancelled += (_, _) => keyBlocker.Dispose();
        command.Execute(null);
    }

    public void LoadRomFile(FileInfo romFile) =>
        LoadRomFromFile(romFile, addToMru: true);

    public void CloseCommand() =>
        Application.Current.GetMainWindow().Close();

    public void SaveScreenshot()
    {
        var keyBlocker = Joypad.CreatePressBlocker();
        var prefix = RomNameHelper.GetSafeFileBaseName(m_currentRomTitle, "G33kBoy");
        var defaultName = $"{prefix}.tga";
        var command = new FileSaveCommand("Save Screenshot", "TGA Files", ["*.tga"], defaultName);
        command.FileSelected += (_, info) =>
        {
            try
            {
                m_emulator.SaveScreenshot(info);
            }
            finally
            {
                keyBlocker.Dispose();
            }
        };
        command.Cancelled += (_, _) => keyBlocker.Dispose();
        command.Execute(null);
    }

    public void SaveSnapshot()
    {
        if (!m_machine.HasLoadedCartridge)
        {
            Logger.Instance.Warn("Unable to save snapshot: No ROM loaded.");
            return;
        }

        var keyBlocker = Joypad.CreatePressBlocker();
        var prefix = RomNameHelper.GetSafeFileBaseName(m_currentRomTitle, "G33kBoy");
        var defaultName = $"{prefix}.sav";
        var command = new FileSaveCommand("Save Snapshot", "G33kBoy Snapshots", ["*.sav"], defaultName);
        command.FileSelected += (_, info) =>
        {
            try
            {
                var snapshot = SnapshotHistory.CaptureSnapshotNow();
                if (snapshot == null)
                {
                    Logger.Instance.Warn("Unable to save snapshot: No active emulation state.");
                    return;
                }

                SnapshotFile.Save(info, snapshot);
            }
            finally
            {
                keyBlocker.Dispose();
            }
        };
        command.Cancelled += (_, _) => keyBlocker.Dispose();
        command.Execute(null);
    }

    public void OpenLog() =>
        Logger.Instance.File.OpenWithDefaultViewer();

    public void OpenProjectPage() =>
        new Uri("https://github.com/deanthecoder/G33kBoy").Open();

    public void ExportTileMap()
    {
        var keyBlocker = Joypad.CreatePressBlocker();
        var prefix = RomNameHelper.GetSafeFileBaseName(m_currentRomTitle, "G33kBoy");
        var command = new FileSaveCommand("Export Tile Map", "TGA Files", ["*.tga"], $"{prefix}-TileMap.tga");
        command.FileSelected += (_, info) =>
        {
            try
            {
                m_machine.Bus?.PPU?.DumpTileMap(info);
            }
            finally
            {
                keyBlocker.Dispose();
            }
        };
        command.Cancelled += (_, _) => keyBlocker.Dispose();
        command.Execute(null);
    }

    public void ResetDevice()
    {
        m_emulator.Reset();
        if (!string.IsNullOrEmpty(m_loadedRomPath))
            SnapshotHistory.ResetForRom(m_loadedRomPath);
        Logger.Instance.Info("CPU reset.");
    }

    public void DumpCpuHistory() =>
        m_machine.Cpu?.InstructionLogger.DumpToConsole();

    public void ReportCpuClockTicks() =>
        Console.WriteLine($"CPU clock ticks: {m_machine.CpuTicks}");

    public void TrackCpuHistory() =>
        IsCpuHistoryTracked = !IsCpuHistoryTracked;

    private void ApplyDisplayVisibilitySettings()
    {
        m_machine.SetBackgroundVisibility(Settings.IsBackgroundVisible);
        m_machine.SetSpriteVisibility(Settings.AreSpritesVisible);
        m_machine.SetDmgPalette(Settings.DmgPalette);
        m_machine.SetLcdEmulation(Settings.IsLcdEmulationEnabled);
        OnPropertyChanged(nameof(IsDisplayBlurEnabled));
        OnPropertyChanged(nameof(IsDisplayBlurDisabled));
    }

    private void ApplySoundEnabledSetting() =>
        m_emulator.AudioDevice.SetEnabled(Settings.IsSoundEnabled);

    private void ApplyHardwareLowPassFilterSetting() =>
        m_emulator.AudioDevice.SetLowPassFilterEnabled(Settings.IsHardwareLowPassFilterEnabled);

    private void ApplySoundChannelSettings()
    {
        m_machine.SetSoundChannelEnabled(1, IsSoundChannel1Enabled);
        m_machine.SetSoundChannelEnabled(2, IsSoundChannel2Enabled);
        m_machine.SetSoundChannelEnabled(3, IsSoundChannel3Enabled);
        m_machine.SetSoundChannelEnabled(4, IsSoundChannel4Enabled);
    }

    private void OnSettingsPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(Settings.IsSoundEnabled):
                ApplySoundEnabledSetting();
                return;
            case nameof(Settings.IsHardwareLowPassFilterEnabled):
                ApplyHardwareLowPassFilterSetting();
                return;
            case nameof(Settings.IsCpuHistoryTracked):
                IsCpuHistoryTracked = Settings.IsCpuHistoryTracked;
                return;
            case nameof(Settings.IsCgbModePreferred):
                m_machine.SetRequestedMode(Settings.IsCgbModePreferred ? GameBoyMode.Cgb : GameBoyMode.Dmg);
                OnPropertyChanged(nameof(IsDisplayBlurEnabled));
                OnPropertyChanged(nameof(IsDisplayBlurDisabled));
                return;
            case nameof(Settings.IsLcdEmulationEnabled):
                ApplyDisplayVisibilitySettings();
                return;
            case nameof(Settings.DmgPalette):
                ApplyDisplayVisibilitySettings();
                RaiseDmgPaletteChanged();
                return;
        }
    }

    private void SetDmgPalette(DmgPalette palette)
    {
        Settings.DmgPalette = palette;
    }

    private void RaiseDmgPaletteChanged()
    {
        OnPropertyChanged(nameof(IsDmgPaletteDefault));
        OnPropertyChanged(nameof(IsDmgPaletteSepia));
        OnPropertyChanged(nameof(IsDmgPaletteBlackAndWhite));
        OnPropertyChanged(nameof(IsDmgPaletteBlue));
        OnPropertyChanged(nameof(IsDmgPaletteRed));
        OnPropertyChanged(nameof(IsDmgPaletteCyan));
        OnPropertyChanged(nameof(IsDmgPaletteMagenta));
    }

    internal bool LoadRomFromFile(FileInfo romFile, bool addToMru)
    {
        if (romFile == null)
            return false;
        if (!romFile.Exists)
        {
            Logger.Instance.Warn($"Unable to load ROM '{romFile.FullName}': File not found.");
            return false;
        }

        var (romName, romData) = RomLoader.ReadRomData(romFile, RomExtensions);
        if (romData == null || romData.Length == 0)
        {
            Logger.Instance.Warn($"Unable to load ROM '{romFile.FullName}': No valid ROM data found.");
            return false;
        }

        var cartridge = new Cartridge(romData);
        var supportCheck = cartridge.IsSupported();
        if (!supportCheck.IsSupported)
        {
            DialogService.Instance.ShowMessage($"Unable to load ROM '{romFile.Name}'", supportCheck.Message);
            return false;
        }

        m_emulator.Stop();

        m_machine.LoadRom(romData, romName);

        if (addToMru)
            Mru.Add(romFile);
        Settings.LastRomFile = romFile;
        m_loadedRomPath = romFile.FullName;

        m_currentRomTitle = RomNameHelper.GetDisplayName(romName) ?? "G33kBoy";
        WindowTitle = RomNameHelper.BuildWindowTitle("G33kBoy", m_currentRomTitle);
        SnapshotHistory?.ResetForRom(m_loadedRomPath);
        m_emulator.Start();
        Logger.Instance.Info($"ROM loaded: {romName} ({romData.Length / 1024.0:0.#} KB)");
        OnPropertyChanged(nameof(IsDisplayBlurEnabled));
        OnPropertyChanged(nameof(IsDisplayBlurDisabled));
        return true;
    }

    private void LoadSnapshotFile(FileInfo snapshotFile)
    {
        if (snapshotFile == null)
            return;
        if (!snapshotFile.Exists)
        {
            Logger.Instance.Warn($"Unable to load snapshot '{snapshotFile.FullName}': File not found.");
            return;
        }

        try
        {
            var state = SnapshotFile.Load(snapshotFile, out var romPath);
            if (string.IsNullOrWhiteSpace(romPath))
            {
                Logger.Instance.Warn($"Unable to load snapshot '{snapshotFile.FullName}': ROM path missing.");
                return;
            }

            var romFile = new FileInfo(romPath);
            if (!romFile.Exists)
            {
                Logger.Instance.Warn($"Unable to restore snapshot: ROM '{romFile.FullName}' not found.");
                return;
            }

            if (!LoadRomFromFile(romFile, addToMru: true))
                return;
            m_machineRunner.LoadState(state);
        }
        catch (Exception ex)
        {
            Logger.Instance.Warn($"Unable to load snapshot '{snapshotFile.FullName}': {ex.Message}");
        }
    }

    private static bool IsSnapshotFile(FileInfo file) =>
        file != null && file.Extension.Equals(".sav", StringComparison.OrdinalIgnoreCase);

    public void Dispose()
    {
        Settings.MruFiles = Mru.AsString();
        Settings.PropertyChanged -= OnSettingsPropertyChanged;
        m_emulator.Dispose();
        m_emulator.Stop();
        m_machineRunner.Dispose();
        m_machine.Dispose();
        m_emulator.AudioDevice.Dispose();
    }

    private double GetEffectiveCpuHz() =>
        m_machine?.GetEffectiveCpuHz() ?? Cpu.Hz;

    private double GetVideoFrameRate() =>
        GetEffectiveCpuHz() / (456.0 * 154.0);

    private MachineDescriptor CreateMachineDescriptor() => new()
    {
        Name = "G33kBoy",
        CpuHz = GetEffectiveCpuHz(),
        VideoHz = 0,
        AudioSampleRateHz = AudioSampleRateHz,
        FrameWidth = PPU.FrameWidth,
        FrameHeight = PPU.FrameHeight
    };
}
