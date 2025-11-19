// Code authored by Dean Edis (DeanTheCoder).
// Anyone is free to copy, modify, use, compile, or distribute this software,
// either in source code form or as a compiled binary, for any purpose.
//
// If you modify the code, please retain this copyright header,
// and consider contributing back to the repository or letting us know
// about your modifications. Your contributions are valued!
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.

namespace DTC.SM83;

/// <summary>
/// Contract for persisting and retrieving cartridge RAM snapshots.
/// </summary>
public interface IGameDataStore
{
    /// <summary>
    /// Look up the serialized entry for the given cartridge file name.
    /// </summary>
    /// <param name="cartridgeFileName">File name (no directory) identifying the cartridge.</param>
    /// <returns>The stored data blob, or null if no entry exists.</returns>
    byte[] LoadGameData(string cartridgeFileName);

    /// <summary>
    /// Save or replace the serialized entry for the given cartridge file name.
    /// </summary>
    /// <param name="cartridgeFileName">File name (no directory) identifying the cartridge.</param>
    /// <param name="data">Raw data to persist (implementations can encode as needed).</param>
    void SaveGameData(string cartridgeFileName, byte[] data);

    /// <summary>
    /// Remove any stored entry that matches the given cartridge file name.
    /// </summary>
    /// <param name="cartridgeFileName">File name (no directory) identifying the cartridge.</param>
    void ClearGameData(string cartridgeFileName);
}
