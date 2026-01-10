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
using Avalonia.Threading;
using DTC.Core;
using DTC.Core.Commands;
using DTC.Core.Extensions;
using DTC.Core.UI;
using DTC.Core.ViewModels;
using DTC.SM83;
using DTC.SM83.Snapshot;
using G33kBoy.Recording;
using Material.Icons;

namespace G33kBoy.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase, IDisposable
{
    private string m_windowTitle;
    private bool m_isSoundChannel1Enabled = true;
    private bool m_isSoundChannel2Enabled = true;
    private bool m_isSoundChannel3Enabled = true;
    private bool m_isSoundChannel4Enabled = true;
    private bool m_isCpuHistoryTracked;
    private RecordingSession m_recordingSession;
    private DispatcherTimer m_recordingIndicatorTimer;
    private bool m_isRecordingIndicatorOn;

    public GameBoy GameBoy { get; }
    public MruFiles Mru { get; }

    public Settings Settings => Settings.Instance;
    
    public string WindowTitle
    {
        get => m_windowTitle ?? "G33kBoy";
        private set => SetField(ref m_windowTitle, value);
    }
    private string m_currentRomTitle = "G33kBoy";

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
    
    public bool IsSoundChannel1Enabled
    {
        get => m_isSoundChannel1Enabled;
        private set => SetField(ref m_isSoundChannel1Enabled, value);
    }

    public bool IsSoundChannel2Enabled
    {
        get => m_isSoundChannel2Enabled;
        private set => SetField(ref m_isSoundChannel2Enabled, value);
    }

    public bool IsSoundChannel3Enabled
    {
        get => m_isSoundChannel3Enabled;
        private set => SetField(ref m_isSoundChannel3Enabled, value);
    }

    public bool IsSoundChannel4Enabled
    {
        get => m_isSoundChannel4Enabled;
        private set => SetField(ref m_isSoundChannel4Enabled, value);
    }

    public bool IsCpuHistoryTracked
    {
        get => m_isCpuHistoryTracked;
        private set
        {
            if (!SetField(ref m_isCpuHistoryTracked, value))
                return;
            Settings.IsCpuHistoryTracked = value;
            GameBoy.SetCpuHistoryTracking(value);
        }
    }

    public bool IsRecording => m_recordingSession?.IsRecording == true;

    public bool IsRecordingIndicatorOn
    {
        get => m_isRecordingIndicatorOn;
        private set => SetField(ref m_isRecordingIndicatorOn, value);
    }

    public bool IsDisplayBlurEnabled => Settings.IsLcdEmulationEnabled && GameBoy.Mode == GameBoyMode.Cgb;

    public bool IsDisplayBlurDisabled => !IsDisplayBlurEnabled;

    public bool IsDmgPaletteDefault => Settings.DmgPalette == DmgPalette.Default;

    public bool IsDmgPaletteSepia => Settings.DmgPalette == DmgPalette.Sepia;

    public bool IsDmgPaletteBlackAndWhite => Settings.DmgPalette == DmgPalette.BlackAndWhite;

    public bool IsDmgPaletteBlue => Settings.DmgPalette == DmgPalette.Blue;

    public bool IsDmgPaletteRed => Settings.DmgPalette == DmgPalette.Red;

    public bool IsDmgPaletteCyan => Settings.DmgPalette == DmgPalette.Cyan;

    public bool IsDmgPaletteMagenta => Settings.DmgPalette == DmgPalette.Magenta;

    public void ToggleCgbMode()
    {
        Settings.IsCgbModePreferred = !Settings.IsCgbModePreferred;
        GameBoy.SetRequestedMode(Settings.IsCgbModePreferred ? GameBoyMode.Cgb : GameBoyMode.Dmg);
        ResetDevice();
    }

    public void ToggleSoundChannel1()
    {
        IsSoundChannel1Enabled = !IsSoundChannel1Enabled;
        GameBoy.SetSoundChannelEnabled(1, IsSoundChannel1Enabled);
    }
    
    public void ToggleSoundChannel2()
    {
        IsSoundChannel2Enabled = !IsSoundChannel2Enabled;
        GameBoy.SetSoundChannelEnabled(2, IsSoundChannel2Enabled);
    }
    
    public void ToggleSoundChannel3()
    {
        IsSoundChannel3Enabled = !IsSoundChannel3Enabled;
        GameBoy.SetSoundChannelEnabled(3, IsSoundChannel3Enabled);
    }
    
    public void ToggleSoundChannel4()
    {
        IsSoundChannel4Enabled = !IsSoundChannel4Enabled;
        GameBoy.SetSoundChannelEnabled(4, IsSoundChannel4Enabled);
    }

    public void ToggleHardwareLowPassFilter()
    {
        Settings.IsHardwareLowPassFilterEnabled = !Settings.IsHardwareLowPassFilterEnabled;
        ApplyHardwareLowPassFilterSetting();
    }

    public void ToggleRecording()
    {
        if (IsRecording)
            StopRecording();
        else
            StartRecording();
    }

    public void StartRecording()
    {
        if (IsRecording)
            return;

        if (!RecordingSession.IsFfmpegAvailable(out _))
        {
            DialogService.Instance.ShowMessage(
                "Recording unavailable",
                "FFmpeg was not detected. Install FFmpeg and ensure it's on your PATH, then try again.",
                MaterialIconKind.Information);
            return;
        }

        try
        {
            m_recordingSession?.Dispose();
            m_recordingSession = new RecordingSession(GameBoy);
            m_recordingSession.Start();
            StartRecordingIndicator();
            OnPropertyChanged(nameof(IsRecording));
        }
        catch (Exception ex)
        {
            m_recordingSession?.Dispose();
            m_recordingSession = null;
            StopRecordingIndicator();
            DialogService.Instance.ShowMessage(
                "Unable to start recording",
                ex.Message,
                MaterialIconKind.Information);
        }
    }

    public void StopRecording()
    {
        if (!IsRecording)
            return;

        var session = m_recordingSession;
        m_recordingSession = null;
        StopRecordingIndicator();
        OnPropertyChanged(nameof(IsRecording));

        if (session == null)
            return;

        var progress = new ProgressToken();
        var busyDialog = DialogService.Instance.ShowBusy("Finalizing recording...", progress);
        var stopTask = session.StopAsync(progress);
        stopTask.ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Dispatcher.UIThread.Post(() => busyDialog.Dispose());
                Logger.Instance.Warn($"Recording failed: {task.Exception?.GetBaseException().Message}");
                session.Dispose();
                return;
            }

            var result = task.Result;
            if (result == null || result.TempFile?.Exists != true)
            {
                Dispatcher.UIThread.Post(() => busyDialog.Dispose());
                Logger.Instance.Warn("Recording failed to produce an output file.");
                session.Dispose();
                return;
            }

            Dispatcher.UIThread.Post(() =>
            {
                busyDialog.Dispose();
                var keyBlocker = GameBoy.Joypad.CreatePressBlocker();
                var prefix = SanitizeFileName(m_currentRomTitle);
                var defaultName = $"{prefix}.mp4";
                var command = new FileSaveCommand("Save Recording", "MP4 Files", ["*.mp4"], defaultName);
                command.FileSelected += (_, info) =>
                {
                    try
                    {
                        File.Move(result.TempFile.FullName, info.FullName, overwrite: true);
                        var fileInfo = new FileInfo(info.FullName);
                        var size = fileInfo.Exists ? fileInfo.Length.ToSize() : "unknown size";
                        Logger.Instance.Info($"Recording stopped. Saved to '{fileInfo.FullName}' ({result.Duration:hh\\:mm\\:ss}, {size}).");
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Warn($"Failed to save recording: {ex.Message}");
                    }
                    finally
                    {
                        session.Dispose();
                        keyBlocker.Dispose();
                    }
                };
                command.Cancelled += (_, _) =>
                {
                    var size = result.TempFile.Exists ? result.TempFile.Length.ToSize() : "unknown size";
                    Logger.Instance.Info($"Recording stopped. Discarded temp output ({result.Duration:hh\\:mm\\:ss}, {size}).");
                    session.Dispose();
                    keyBlocker.Dispose();
                };
                command.Execute(null);
            });
        });
    }

    public MainWindowViewModel()
    {
        Mru = new MruFiles().InitFromString(Settings.MruFiles);
        Mru.OpenRequested += (_, file) => LoadRomFile(file, addToMru: false);

        GameBoy = new GameBoy();
        Settings.PropertyChanged += OnSettingsPropertyChanged;
        GameBoy.SetRequestedMode(Settings.IsCgbModePreferred ? GameBoyMode.Cgb : GameBoyMode.Dmg);
        GameBoy.RomLoaded += (_, title) =>
            Dispatcher.UIThread.Post(() =>
            {
                m_currentRomTitle = string.IsNullOrWhiteSpace(title) ? "G33kBoy" : title;
                WindowTitle = string.IsNullOrWhiteSpace(title) ? "G33kBoy" : $"G33kBoy - {title}";
                ApplyDisplayVisibilitySettings();
        });
        IsCpuHistoryTracked = Settings.IsCpuHistoryTracked;
        ApplyDisplayVisibilitySettings();
        ApplySoundEnabledSetting();
        ApplyHardwareLowPassFilterSetting();
        ApplySoundChannelSettings();
    }

    public void LoadGameRom()
    {
        var keyBlocker = GameBoy.Joypad.CreatePressBlocker();
        var command = new FileOpenCommand("Load Game Boy ROM or Snapshot", "Game Boy Files", ["*.gb", "*.gbc", "*.zip", "*.sav"]);
        command.FileSelected += (_, info) =>
        {
            try
            {
                if (IsSnapshotFile(info))
                    LoadSnapshotFile(info);
                else
                    LoadRomFile(info);
            }
            finally
            {
                keyBlocker.Dispose();
            }
        };
        command.Cancelled += (_, _) => keyBlocker.Dispose();
        command.Execute(null);
    }
    
    public void CloseCommand() =>
        Application.Current.GetMainWindow().Close();

    public void SaveScreenshot()
    {
        var keyBlocker = GameBoy.Joypad.CreatePressBlocker();
        var prefix = SanitizeFileName(m_currentRomTitle);
        var defaultName = $"{prefix}.tga";
        var command = new FileSaveCommand("Save Screenshot", "TGA Files", ["*.tga"], defaultName);
        command.FileSelected += (_, info) =>
        {
            try
            {
                GameBoy.SaveScreenshot(info);
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
        if (!GameBoy.HasLoadedCartridge)
        {
            Logger.Instance.Warn("Unable to save snapshot: No ROM loaded.");
            return;
        }

        var keyBlocker = GameBoy.Joypad.CreatePressBlocker();
        var prefix = SanitizeFileName(m_currentRomTitle);
        var defaultName = $"{prefix}.sav";
        var command = new FileSaveCommand("Save Snapshot", "G33kBoy Snapshots", ["*.sav"], defaultName);
        command.FileSelected += (_, info) =>
        {
            try
            {
                var snapshot = GameBoy.SnapshotHistory.CaptureSnapshotNow();
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
        var keyBlocker = GameBoy.Joypad.CreatePressBlocker();
        var prefix = SanitizeFileName(m_currentRomTitle);
        var command = new FileSaveCommand("Export Tile Map", "TGA Files", ["*.tga"], $"{prefix}-TileMap.tga");
        command.FileSelected += (_, info) =>
        {
            try
            {
                GameBoy.ExportTileMap(info);
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
        var romFile = Settings.LastRomFile;
        if (romFile == null)
            return;

        if (!romFile.Exists)
        {
            Logger.Instance.Warn($"Unable to reset: ROM '{romFile.FullName}' not found.");
            return;
        }

        LoadRomFile(romFile);
    }

    public void DumpCpuHistory() =>
        GameBoy.DumpCpuHistory();

    public void ReportCpuClockTicks() =>
        Console.WriteLine($"CPU clock ticks: {GameBoy.CpuClockTicks}");

    public void TrackCpuHistory() =>
        IsCpuHistoryTracked = !IsCpuHistoryTracked;

    private void ApplyDisplayVisibilitySettings()
    {
        GameBoy.SetBackgroundVisibility(Settings.IsBackgroundVisible);
        GameBoy.SetSpriteVisibility(Settings.AreSpritesVisible);
        GameBoy.SetDmgPalette(Settings.DmgPalette);
        GameBoy.SetLcdEmulation(Settings.IsLcdEmulationEnabled);
        OnPropertyChanged(nameof(IsDisplayBlurEnabled));
        OnPropertyChanged(nameof(IsDisplayBlurDisabled));
    }
    
    private void ApplySoundEnabledSetting() =>
        GameBoy.SetSoundEnabled(Settings.IsSoundEnabled);

    private void ApplyHardwareLowPassFilterSetting() =>
        GameBoy.SetHardwareLowPassFilterEnabled(Settings.IsHardwareLowPassFilterEnabled);
    
    private void ApplySoundChannelSettings()
    {
        GameBoy.SetSoundChannelEnabled(1, IsSoundChannel1Enabled);
        GameBoy.SetSoundChannelEnabled(2, IsSoundChannel2Enabled);
        GameBoy.SetSoundChannelEnabled(3, IsSoundChannel3Enabled);
        GameBoy.SetSoundChannelEnabled(4, IsSoundChannel4Enabled);
    }

    private void StartRecordingIndicator()
    {
        m_recordingIndicatorTimer ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        m_recordingIndicatorTimer.Stop();
        m_recordingIndicatorTimer.Tick -= OnRecordingIndicatorTick;
        m_recordingIndicatorTimer.Tick += OnRecordingIndicatorTick;
        IsRecordingIndicatorOn = true;
        m_recordingIndicatorTimer.Start();
    }

    private void StopRecordingIndicator()
    {
        if (m_recordingIndicatorTimer != null)
        {
            m_recordingIndicatorTimer.Tick -= OnRecordingIndicatorTick;
            m_recordingIndicatorTimer.Stop();
        }

        IsRecordingIndicatorOn = false;
    }

    private void OnRecordingIndicatorTick(object sender, EventArgs e)
    {
        if (!IsRecording)
        {
            OnPropertyChanged(nameof(IsRecording));
            StopRecordingIndicator();
            return;
        }

        IsRecordingIndicatorOn = !IsRecordingIndicatorOn;
    }

    private void OnSettingsPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Settings.IsSoundEnabled))
            ApplySoundEnabledSetting();
        else if (e.PropertyName == nameof(Settings.IsHardwareLowPassFilterEnabled))
            ApplyHardwareLowPassFilterSetting();
        else if (e.PropertyName == nameof(Settings.IsCpuHistoryTracked))
            IsCpuHistoryTracked = Settings.IsCpuHistoryTracked;
        else if (e.PropertyName == nameof(Settings.IsCgbModePreferred))
        {
            GameBoy.SetRequestedMode(Settings.IsCgbModePreferred ? GameBoyMode.Cgb : GameBoyMode.Dmg);
            OnPropertyChanged(nameof(IsDisplayBlurEnabled));
            OnPropertyChanged(nameof(IsDisplayBlurDisabled));
        }
        else if (e.PropertyName == nameof(Settings.IsLcdEmulationEnabled))
            ApplyDisplayVisibilitySettings();
        else if (e.PropertyName == nameof(Settings.DmgPalette))
        {
            ApplyDisplayVisibilitySettings();
            RaiseDmgPaletteChanged();
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

    internal void LoadRomFile(FileInfo romFile, bool addToMru = true)
    {
        if (romFile == null)
            return;
        if (!romFile.Exists)
        {
            Logger.Instance.Warn($"Unable to load ROM '{romFile.FullName}': File not found.");
            return;
        }

        if (addToMru)
            Mru.Add(romFile);

        GameBoy.PowerOnAsync(romFile);
        Settings.LastRomFile = romFile;
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

            LoadRomFile(romFile);
            GameBoy.LoadState(state);
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
        m_recordingSession?.Dispose();
        m_recordingSession = null;
        StopRecordingIndicator();
        GameBoy.Dispose();
    }

    private static string SanitizeFileName(string input) =>
        string.IsNullOrWhiteSpace(input) ? "G33kBoy" : input.ToSafeFileName();
}
