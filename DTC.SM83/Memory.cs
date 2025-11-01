// Code authored by Dean Edis (DeanTheCoder).
// Anyone is free to copy, modify, use, compile, or distribute this software,
// either in source code form or as a compiled binary, for any non-commercial
// purpose.
//
// If you modify the code, please retain this copyright header,
// and consider contributing back to the repository or letting us know
// about your modifications. Your contributions are valued!
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.

namespace DTC.SM83;

public class Memory
{
    private readonly byte[] m_ram;

    public Memory(int size)
    {
        m_ram = new byte[size];
    }

    public byte this[ushort address]
    {
        get => m_ram[address];
        set => m_ram[address] = value;
    }

    public ushort Read16(ushort address) =>
        (ushort)((m_ram[address + 1] << 8) | m_ram[address]);

    public void Write16(ushort address, ushort value)
    {
        m_ram[address] = (byte)(value & 0xFF);
        m_ram[address + 1] = (byte)((value >> 8) & 0xFF);
    }

    public static bool operator ==(Memory left, Memory right)
    {
        if (ReferenceEquals(left, right))
            return true;
        if (left is null || right is null)
            return false;
        if (left.m_ram.Length != right.m_ram.Length)
            return false;
        return left.m_ram.SequenceEqual(right.m_ram);
    }

    public static bool operator !=(Memory left, Memory right) => !(left == right);

    public override bool Equals(object obj) => obj is Memory other && this == other;

    public override int GetHashCode() => m_ram.GetHashCode();
}