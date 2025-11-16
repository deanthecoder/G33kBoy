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
using Avalonia;
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

    public MainWindowViewModel()
    {
        GameBoy = new GameBoy();
    }

    public void LoadGameRom()
    {
        // todo
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
        // todo
    }
    
    public void OpenProjectPage() =>
        new Uri("https://github.com/deanthecoder/G33kBoy").Open();

    public void ExportTileMap()
    {
        var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory).ToDir();
        var file = desktopPath.GetFile("TileMap.tga");
        GameBoy.ExportTileMap(file);
    }

    public void Dispose() =>
        GameBoy.Dispose();
}
