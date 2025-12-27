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

using System.Runtime.CompilerServices;
using DTC.SM83.Snapshot;

namespace DTC.SM83.Devices;

/// <summary>
/// Base class for generic RAM devices with configurable memory ranges and usability.
/// </summary>
public abstract class RamDeviceBase : IMemDevice
{
    protected readonly byte[] m_data;
    private readonly bool m_isUsable;

    public ushort FromAddr { get; }
    public ushort ToAddr { get; }

    protected RamDeviceBase(ushort fromAddr, ushort toAddr, bool isUsable)
    {
        FromAddr = fromAddr;
        ToAddr = toAddr;
        m_isUsable = isUsable;
        m_data = new byte[toAddr - fromAddr + 1];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte Read8(ushort addr)
    {
        if (!m_isUsable)
            return 0xFF; // Ignore reads from unusable RAM.

        var idx = addr - FromAddr;
        return m_data[idx];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write8(ushort addr, byte value)
    {
        if (!m_isUsable)
            return; // Ignore writes to unusable RAM.

        var idx = addr - FromAddr;
        m_data[idx] = value;
    }
    
    public bool Contains(ushort addr) =>
        addr >= FromAddr && addr <= ToAddr;

    internal int GetStateSize() =>
        sizeof(int) + m_data.Length;

    internal void SaveState(ref StateWriter writer)
    {
        writer.WriteInt32(m_data.Length);
        writer.WriteBytes(m_data);
    }

    internal void LoadState(ref StateReader reader)
    {
        var length = reader.ReadInt32();
        if (length != m_data.Length)
            throw new InvalidOperationException($"RAM size mismatch. Expected {m_data.Length}, got {length}.");
        reader.ReadBytes(m_data);
    }
}
