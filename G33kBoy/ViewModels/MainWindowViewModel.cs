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
using System.IO;
using Avalonia;
using DTC.Core.Commands;
using DTC.Core.Extensions;
using DTC.Core.ViewModels;
using DTC.SM83;

namespace G33kBoy.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase, IDisposable
{
    private ClockSync.Speed m_emulationSpeed;
    
    public GameBoy GameBoy { get; }

    public Settings Settings => Settings.Instance;

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

    public MainWindowViewModel()
    {
        GameBoy = new GameBoy(Settings.Instance);
        ApplyDisplayVisibilitySettings();
    }

    public void LoadGameRom()
    {
        var keyBlocker = GameBoy.Joypad.CreatePressBlocker();
        var command = new FileOpenCommand("Load Game Boy ROM", "Game Boy ROMs", ["*.gb"]);
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
        var desktopPath = GetDesktopDirectory();
        var fileName = $"G33kBoy-Screenshot-{DateTime.Now:yyyyMMdd-HHmmss}.tga";
        var file = desktopPath.GetFile(fileName);
        GameBoy.SaveScreenshot(file);
    }
    
    public void OpenProjectPage() =>
        new Uri("https://github.com/deanthecoder/G33kBoy").Open();

    public void ClearGameData() =>
        GameBoy.ClearGameData();

    public void ExportTileMap()
    {
        var desktopPath = GetDesktopDirectory();
        var file = desktopPath.GetFile("TileMap.tga");
        GameBoy.ExportTileMap(file);
    }

    private void ApplyDisplayVisibilitySettings()
    {
        GameBoy.SetBackgroundVisibility(Settings.IsBackgroundVisible);
        GameBoy.SetSpriteVisibility(Settings.AreSpritesVisible);
    }

    internal void LoadRomFile(FileInfo romFile)
    {
        if (romFile == null)
            return;

        GameBoy.PowerOnAsync(romFile);
        Settings.LastRomFile = romFile;
    }

    private static DirectoryInfo GetDesktopDirectory()
    {
        var candidates = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            Environment.CurrentDirectory
        };

        foreach (var candidate in candidates)
        {
            if (!string.IsNullOrWhiteSpace(candidate))
                return candidate.ToDir();
        }

        throw new InvalidOperationException("Unable to resolve a desktop directory.");
    }

    public void Dispose() =>
        GameBoy.Dispose();
}
