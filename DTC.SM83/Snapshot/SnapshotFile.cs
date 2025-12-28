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

using System.Text;

namespace DTC.SM83.Snapshot;

public static class SnapshotFile
{
    private const uint FileMagic = 0x56534247; // "GBSV" (little-endian)
    private const ushort FileVersion = 1;

    public static void Save(FileInfo file, MachineState state)
    {
        if (file == null)
            throw new ArgumentNullException(nameof(file));
        if (state == null)
            throw new ArgumentNullException(nameof(state));

        var romPath = state.RomPath ?? string.Empty;
        var romPathBytes = Encoding.UTF8.GetBytes(romPath);

        using var stream = file.Open(FileMode.Create, FileAccess.Write, FileShare.None);
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: false);
        writer.Write(FileMagic);
        writer.Write(FileVersion);
        writer.Write((ushort)0);
        writer.Write(romPathBytes.Length);
        writer.Write(state.Size);
        writer.Write(romPathBytes);
        writer.Write(state.GetBuffer());
    }

    public static MachineState Load(FileInfo file, out string romPath)
    {
        if (file == null)
            throw new ArgumentNullException(nameof(file));

        using var stream = file.OpenRead();
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: false);
        var magic = reader.ReadUInt32();
        if (magic != FileMagic)
            throw new InvalidOperationException("Invalid snapshot file.");

        var version = reader.ReadUInt16();
        if (version != FileVersion)
            throw new InvalidOperationException($"Unsupported snapshot version {version}.");

        reader.ReadUInt16(); // reserved
        var romPathLength = reader.ReadInt32();
        var stateSize = reader.ReadInt32();
        if (romPathLength < 0 || stateSize <= 0)
            throw new InvalidOperationException("Snapshot file is corrupt.");

        var romPathBytes = reader.ReadBytes(romPathLength);
        if (romPathBytes.Length != romPathLength)
            throw new InvalidOperationException("Snapshot file is corrupt.");

        var stateBytes = reader.ReadBytes(stateSize);
        if (stateBytes.Length != stateSize)
            throw new InvalidOperationException("Snapshot file is corrupt.");

        romPath = Encoding.UTF8.GetString(romPathBytes);
        var state = new MachineState(stateSize)
        {
            RomPath = romPath
        };
        state.LoadBuffer(stateBytes);
        return state;
    }
}
