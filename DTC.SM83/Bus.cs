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

/// <summary>
/// Manages memory access across multiple memory devices through a unified address space.
/// </summary>
public class Bus
{
    private readonly IMemDevice[] m_devices = new IMemDevice[0x10000];
    
    public void Attach(IMemDevice device, int start, int end)
    {
        for (var i = start; i <= end; i++)
            m_devices[i] = device;
    }
    
    public byte Read8(ushort addr) =>
        m_devices[addr]?.Read8(addr) ?? 0x00;
    
    public void Write8(ushort addr, byte value) =>
        m_devices[addr]?.Write8(addr, value);
}