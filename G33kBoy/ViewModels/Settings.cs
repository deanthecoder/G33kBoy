// Code authored by Dean Edis (DeanTheCoder).
// Anyone is free to copy, modify, use, compile, or distribute this software,
// either in source code form or as a compiled binary, for any purpose.
//
// If you modify the code, please retain this copyright header,
// and consider contributing back to the repository or letting us know
// about your modifications. Your contributions are valued!
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.

using System.IO;
using DTC.Core.Settings;

namespace G33kBoy.ViewModels;

/// <summary>
/// Application settings.
/// </summary>
public class Settings : UserSettingsBase
{
    public static Settings Instance { get; } = new Settings();
    
    public bool IsSoundEnabled
    {
        get => Get<bool>();
        set => Set(value);
    }

    public bool IsHardwareLowPassFilterEnabled
    {
        get => Get<bool>();
        set => Set(value);
    }

    public bool IsAmbientBlurred
    {
        get => Get<bool>();
        set => Set(value);
    }

    public bool IsBackgroundVisible
    {
        get => Get<bool>();
        set => Set(value);
    }

    public bool AreSpritesVisible
    {
        get => Get<bool>();
        set => Set(value);
    }

    public bool IsLcdEmulationEnabled
    {
        get => Get<bool>();
        set => Set(value);
    }

    public bool IsDmgSepiaEnabled
    {
        get => Get<bool>();
        set => Set(value);
    }

    public bool IsCpuHistoryTracked
    {
        get => Get<bool>();
        set => Set(value);
    }

    public bool IsCgbModePreferred
    {
        get => Get<bool>();
        set => Set(value);
    }

    public string MruFiles
    {
        get => Get<string>();
        set => Set(value);
    }

    public FileInfo LastRomFile
    {
        get => Get<FileInfo>();
        set => Set(value);
    }
    
    protected override void ApplyDefaults()
    {
        IsAmbientBlurred = true;
        IsSoundEnabled = true;
        IsHardwareLowPassFilterEnabled = true;
        IsBackgroundVisible = true;
        IsLcdEmulationEnabled = true;
        IsDmgSepiaEnabled = false;
        AreSpritesVisible = true;
        IsCpuHistoryTracked = false;
        IsCgbModePreferred = true;
        MruFiles = string.Empty;
        LastRomFile = null;
    }
}
