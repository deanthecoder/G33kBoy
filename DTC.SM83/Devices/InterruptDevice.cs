// Code authored by Dean Edis (DeanTheCoder).
// Anyone is free to copy, modify, use, compile, or distribute this software,
// either in source code form or as a compiled binary, for any purpose.
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
/// Represents the interrupt mask at 0xFF0F.
/// </summary>
public class InterruptDevice : IMemDevice
{
    private byte m_if;
    
    public ushort FromAddr => 0xFF0F;
    public ushort ToAddr => 0xFF0F;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte Read8(ushort addr) => (byte)(m_if | 0xE0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write8(ushort addr, byte value) => m_if = (byte)(value & 0x1F);

    public enum InterruptType
    {
        VBlank = 1 << 0,
        Stat = 1 << 1,
        Timer = 1 << 2,
        Serial = 1 << 3,
        Joypad = 1 << 4
    }

    public void Raise(InterruptType requested) =>
        m_if |= (byte)requested;

    public int GetStateSize() => sizeof(byte);

    public void SaveState(ref StateWriter writer) =>
        writer.WriteByte(m_if);

    public void LoadState(ref StateReader reader) =>
        m_if = reader.ReadByte();
}
