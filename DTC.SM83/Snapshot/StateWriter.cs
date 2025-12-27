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
/// Writes primitive values into a fixed snapshot buffer.
/// </summary>
public struct StateWriter
{
    private readonly byte[] m_buffer;

    public StateWriter(byte[] buffer)
    {
        m_buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
        Offset = 0;
    }

    public int Offset { get; private set; }

    public void WriteByte(byte value)
    {
        Ensure(1);
        m_buffer[Offset++] = value;
    }

    public void WriteBool(bool value) =>
        WriteByte(value ? (byte)1 : (byte)0);

    public void WriteUInt16(ushort value)
    {
        Ensure(2);
        BinaryPrimitives.WriteUInt16LittleEndian(m_buffer.AsSpan(Offset, 2), value);
        Offset += 2;
    }

    public void WriteInt32(int value)
    {
        Ensure(4);
        BinaryPrimitives.WriteInt32LittleEndian(m_buffer.AsSpan(Offset, 4), value);
        Offset += 4;
    }

    public void WriteUInt32(uint value)
    {
        Ensure(4);
        BinaryPrimitives.WriteUInt32LittleEndian(m_buffer.AsSpan(Offset, 4), value);
        Offset += 4;
    }

    public void WriteInt64(long value)
    {
        Ensure(8);
        BinaryPrimitives.WriteInt64LittleEndian(m_buffer.AsSpan(Offset, 8), value);
        Offset += 8;
    }

    public void WriteUInt64(ulong value)
    {
        Ensure(8);
        BinaryPrimitives.WriteUInt64LittleEndian(m_buffer.AsSpan(Offset, 8), value);
        Offset += 8;
    }

    public void WriteBytes(byte[] data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));
        Ensure(data.Length);
        Buffer.BlockCopy(data, 0, m_buffer, Offset, data.Length);
        Offset += data.Length;
    }

    public void WriteBytes(ReadOnlySpan<byte> data)
    {
        Ensure(data.Length);
        data.CopyTo(m_buffer.AsSpan(Offset, data.Length));
        Offset += data.Length;
    }

    private void Ensure(int count)
    {
        if (Offset + count > m_buffer.Length)
            throw new InvalidOperationException("State buffer is too small.");
    }
}
