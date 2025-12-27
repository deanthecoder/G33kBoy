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

using System.Buffers.Binary;

namespace DTC.SM83.Snapshot;

/// <summary>
/// Reads primitive values from a fixed snapshot buffer.
/// </summary>
public struct StateReader
{
    private readonly byte[] m_buffer;

    public StateReader(byte[] buffer)
    {
        m_buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
        Offset = 0;
    }

    public int Offset { get; private set; }

    public byte ReadByte()
    {
        Ensure(1);
        return m_buffer[Offset++];
    }

    public bool ReadBool() =>
        ReadByte() != 0;

    public ushort ReadUInt16()
    {
        Ensure(2);
        var value = BinaryPrimitives.ReadUInt16LittleEndian(m_buffer.AsSpan(Offset, 2));
        Offset += 2;
        return value;
    }

    public int ReadInt32()
    {
        Ensure(4);
        var value = BinaryPrimitives.ReadInt32LittleEndian(m_buffer.AsSpan(Offset, 4));
        Offset += 4;
        return value;
    }

    public uint ReadUInt32()
    {
        Ensure(4);
        var value = BinaryPrimitives.ReadUInt32LittleEndian(m_buffer.AsSpan(Offset, 4));
        Offset += 4;
        return value;
    }

    public long ReadInt64()
    {
        Ensure(8);
        var value = BinaryPrimitives.ReadInt64LittleEndian(m_buffer.AsSpan(Offset, 8));
        Offset += 8;
        return value;
    }

    public ulong ReadUInt64()
    {
        Ensure(8);
        var value = BinaryPrimitives.ReadUInt64LittleEndian(m_buffer.AsSpan(Offset, 8));
        Offset += 8;
        return value;
    }

    public void ReadBytes(byte[] destination)
    {
        if (destination == null)
            throw new ArgumentNullException(nameof(destination));
        Ensure(destination.Length);
        Buffer.BlockCopy(m_buffer, Offset, destination, 0, destination.Length);
        Offset += destination.Length;
    }

    public void ReadBytes(Span<byte> destination)
    {
        Ensure(destination.Length);
        m_buffer.AsSpan(Offset, destination.Length).CopyTo(destination);
        Offset += destination.Length;
    }

    private void Ensure(int count)
    {
        if (Offset + count > m_buffer.Length)
            throw new InvalidOperationException("State buffer is too small.");
    }
}
