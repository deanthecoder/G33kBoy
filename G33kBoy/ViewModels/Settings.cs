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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DTC.Core;
using DTC.Core.Extensions;
using DTC.Core.Settings;
using DTC.SM83;
using JetBrains.Annotations;

namespace G33kBoy.ViewModels;

/// <summary>
/// Application settings.
/// </summary>
public class Settings : UserSettingsBase, IGameDataStore
{
    public static Settings Instance { get; } = new Settings();
    
    public bool IsSoundEnabled
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

    /// <summary>
    /// Serialized game data entries (<c>FileName|Payload</c>).
    /// </summary>
    [UsedImplicitly]
    public string[] GameDataStates
    {
        get => Get<string[]>();
        set => Set(value ?? []);
    }

    protected override void ApplyDefaults()
    {
        IsAmbientBlurred = true;
        IsSoundEnabled = true;
        IsBackgroundVisible = true;
        IsLcdEmulationEnabled = true;
        AreSpritesVisible = true;
        GameDataStates = [];
        MruFiles = string.Empty;
        LastRomFile = null;
    }

    public byte[] LoadGameData(string cartridgeFileName)
    {
        if (string.IsNullOrWhiteSpace(cartridgeFileName))
            return null;

        var prefix = BuildPrefix(cartridgeFileName);
        var entry = GameDataStates.FirstOrDefault(e =>
            e.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrEmpty(entry) || entry.Length <= prefix.Length)
            return null;

        var encoded = entry[prefix.Length..];
        if (string.IsNullOrWhiteSpace(encoded))
            return null;

        try
        {
            var compressed = Convert.FromBase64String(encoded);
            return compressed.Decompress();
        }
        catch (FormatException ex)
        {
            Logger.Instance.Warn($"Stored game data for '{cartridgeFileName}' is invalid: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Logger.Instance.Warn($"Stored game data for '{cartridgeFileName}' is invalid: {ex.Message}");
            return null;
        }
    }

    public void SaveGameData(string cartridgeFileName, byte[] data)
    {
        if (string.IsNullOrWhiteSpace(cartridgeFileName) || data == null)
            return;

        var prefix = BuildPrefix(cartridgeFileName);
        var compressed = data.Compress();
        var encoded = Convert.ToBase64String(compressed);
        var entry = $"{prefix}{encoded}";

        var entries = new List<string>(GameDataStates);
        var existingIndex = entries.FindIndex(e =>
            e.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        if (existingIndex >= 0)
            entries[existingIndex] = entry;
        else
            entries.Add(entry);

        GameDataStates = entries.ToArray();
        Save();
    }
    
    public void ClearAllGameData()
    {
        GameDataStates = [];
        Save();
    }

    private static string BuildPrefix(string cartridgeFileName) =>
        $"{cartridgeFileName}|";
}
