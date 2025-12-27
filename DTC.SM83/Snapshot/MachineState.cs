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

namespace DTC.SM83.Snapshot;

/// <summary>
/// Holds a serialized snapshot of CPU and bus state.
/// </summary>
public sealed class MachineState
{
    public const uint Magic = 0x54534247; // "GBST" (little-endian)
    public const ushort Version = 1;

    private readonly byte[] m_data;

    public MachineState(int size)
    {
        if (size <= 0)
            throw new ArgumentOutOfRangeException(nameof(size));
        m_data = new byte[size];
    }

    public int Size => m_data.Length;
    public string RomPath { get; set; }

    internal StateWriter CreateWriter() => new StateWriter(m_data);
    internal StateReader CreateReader() => new StateReader(m_data);
}
