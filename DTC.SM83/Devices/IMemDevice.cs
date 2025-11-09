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
/// Represents a memory device that can be read from and written to.
/// </summary>
public interface IMemDevice
{
    ushort FromAddr { get; }
    ushort ToAddr { get; }
    
    byte Read8(ushort addr);
    void Write8(ushort addr, byte value);
}