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

namespace DTC.SM83;

public class Memory
{
    private readonly byte[] m_ram;
    
    public Clock Clock { get; }

    public int Length => m_ram.Length;

    public Memory(int size, Clock clock)
    {
        Clock = clock;
        m_ram = new byte[size];
    }
    
    /// <summary>
    /// Peek memory at address without advancing the clock.
    /// </summary>
    /// <param name="address"></param>
    /// <returns></returns>
    public byte Peek8(ushort address) =>
        m_ram[address];

    /// <summary>
    /// Peek memory at address and advance the clock 4 ticks.
    /// </summary>
    public byte Read8(ushort address)
    {
        Clock.AdvanceT(4);
        return m_ram[address];
    }
    
    /// <summary>
    /// Write memory at address and advance the clock 4 ticks.
    /// </summary>
    public void Write8(ushort address, byte value)
    {
        Clock.AdvanceT(4);
        m_ram[address] = value;
    }

    /// <summary>
    /// Read 16-bit word at address and advance the clock 8 ticks.
    /// </summary>
    public ushort Read16(ushort address) =>
        (ushort)(Read8(address) | (ushort)(Read8((ushort)(address + 1)) << 8));

    /// <summary>
    /// Write 16-bit word at address and advance the clock 8 ticks.
    /// </summary>
    public void Write16(ushort address, ushort value)
    {
        Write8(address, (byte)(value & 0xFF));
        Write8((ushort)(address + 1), (byte)(value >> 8));   
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