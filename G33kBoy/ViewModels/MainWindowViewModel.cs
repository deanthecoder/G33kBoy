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

namespace G33kBoy.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase, IDisposable
{
    private ClockSync.Speed m_emulationSpeed;
    private string m_windowTitle;
    private bool m_isAutoFireEnabled;
    private bool m_isSoundChannel1Enabled = true;
    private bool m_isSoundChannel2Enabled = true;
    private bool m_isSoundChannel3Enabled = true;
    private bool m_isSoundChannel4Enabled = true;

    public GameBoy GameBoy { get; }
    public MruFiles Mru { get; }

    public Settings Settings => Settings.Instance;

#if DEBUG
    public bool IsDebugBuild => true;
#else
    public bool IsDebugBuild => false;
#endif
    
    public string WindowTitle
    {
        get => m_windowTitle ?? "G33kBoy";
        private set => SetField(ref m_windowTitle, value);
    }
    private string m_currentRomTitle = "G33kBoy";

    public ClockSync.Speed EmulationSpeed
    {
        get => m_emulationSpeed;
        set
        {
            if (!SetField(ref m_emulationSpeed, value))
                return;
            GameBoy.SetSpeed(value);
        }
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

    public bool IsAutoFireEnabled
    {
        get => m_isAutoFireEnabled;
        private set => SetField(ref m_isAutoFireEnabled, value);
    }

    public void ClearAllGameData()
    {
        DialogService.Instance.Warn(
            "Clear All Saved ROM Data",
            "Are you sure you want to clear all saved ROM data?",
            "Cancel",
            "Clear and Reset Device",
            confirmed =>
            {
                if (!confirmed)
                    return;
                GameBoy.ClearAllGameData();
                ResetDevice();
            });
    }

    public void ToggleAutoFire()
    {
        IsAutoFireEnabled = !IsAutoFireEnabled;
        GameBoy.SetAutoFireEnabled(IsAutoFireEnabled);
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

    public MainWindowViewModel()
    {
        Mru = new MruFiles().InitFromString(Settings.MruFiles);
        Mru.OpenRequested += (_, file) => LoadRomFile(file, addToMru: false);

        GameBoy = new GameBoy(Settings.Instance);
        Settings.PropertyChanged += OnSettingsPropertyChanged;
        GameBoy.RomLoaded += (_, title) =>
            Dispatcher.UIThread.Post(() =>
            {
                m_currentRomTitle = string.IsNullOrWhiteSpace(title) ? "G33kBoy" : title;
                WindowTitle = string.IsNullOrWhiteSpace(title) ? "G33kBoy" : $"G33kBoy - {title}";
                ApplyDisplayVisibilitySettings();
            });
        ApplyDisplayVisibilitySettings();
        ApplySoundEnabledSetting();
        ApplySoundChannelSettings();
    }

    public void LoadGameRom()
    {
        var keyBlocker = GameBoy.Joypad.CreatePressBlocker();
        var command = new FileOpenCommand("Load Game Boy ROM", "Game Boy ROMs", ["*.gb", "*.zip"]);
        command.FileSelected += (_, info) =>
        {
            try
            {
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
    
    public void RotateEmulationSpeed()
    {
        switch (EmulationSpeed)
        {
            case ClockSync.Speed.Actual:
                EmulationSpeed = ClockSync.Speed.Fast;
                break;
            case ClockSync.Speed.Fast:
                EmulationSpeed = ClockSync.Speed.Maximum;
                break;
            case ClockSync.Speed.Maximum:
                EmulationSpeed = ClockSync.Speed.Pause;
                break;
            case ClockSync.Speed.Pause:
                EmulationSpeed = ClockSync.Speed.Actual;
                break;
        }
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

    private void ApplyDisplayVisibilitySettings()
    {
        GameBoy.SetBackgroundVisibility(Settings.IsBackgroundVisible);
        GameBoy.SetSpriteVisibility(Settings.AreSpritesVisible);
        GameBoy.SetLcdEmulation(Settings.IsLcdEmulationEnabled);
    }
    
    private void ApplySoundEnabledSetting() =>
        GameBoy.SetSoundEnabled(Settings.IsSoundEnabled);
    
    private void ApplySoundChannelSettings()
    {
        GameBoy.SetSoundChannelEnabled(1, IsSoundChannel1Enabled);
        GameBoy.SetSoundChannelEnabled(2, IsSoundChannel2Enabled);
        GameBoy.SetSoundChannelEnabled(3, IsSoundChannel3Enabled);
        GameBoy.SetSoundChannelEnabled(4, IsSoundChannel4Enabled);
    }

    private void OnSettingsPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Settings.IsSoundEnabled))
            ApplySoundEnabledSetting();
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

        // Auto-fire is not persisted and resets per cartridge load.
        if (IsAutoFireEnabled)
            ToggleAutoFire();

        if (addToMru)
            Mru.Add(romFile);

        GameBoy.PowerOnAsync(romFile);
        Settings.LastRomFile = romFile;
    }

    public void Dispose()
    {
        Settings.MruFiles = Mru.AsString();
        Settings.PropertyChanged -= OnSettingsPropertyChanged;
        GameBoy.Dispose();
    }

    private static string SanitizeFileName(string input) =>
        string.IsNullOrWhiteSpace(input) ? "G33kBoy" : input.ToSafeFileName();
}
