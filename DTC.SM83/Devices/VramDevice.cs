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

namespace DTC.SM83.Devices;

/// <summary>
/// Represents the device for VRAM.
/// </summary>
public class VramDevice : IMemDevice
{
    private readonly byte[] m_data = new byte[0x2000];
    
    public ushort FromAddr => 0x8000;
    public ushort ToAddr => 0x9FFF;

    public byte Read8(ushort addr)
    {
        var idx = addr - FromAddr;
        return m_data[idx];
    }

    public void Write8(ushort addr, byte value)
    {
        var idx = addr - FromAddr;
        m_data[idx] = value;
    }
}