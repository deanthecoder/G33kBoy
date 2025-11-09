// Code authored by Dean Edis (DeanTheCoder).
// Anyone is free to copy, modify, use, compile, or distribute this software,
// either in source code form or as a compiled binary, for any purpose.
// 
// If you modify the code, please retain this copyright header,
// and consider contributing back to the repository or letting us know
// about your modifications. Your contributions are valued!
// 
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.

using DTC.SM83.Devices;

namespace DTC.SM83.Extensions;

public static class MemDeviceExtensions
{
    /// <summary>
    /// Load a block of data into the target bus/device.
    /// </summary>
    public static void Load(this IMemDevice bus, ushort addr, byte[] data)
    {
        foreach (var b in data)
            bus.Write8(addr++, b);
    }
    
    public static ushort Read16(this IMemDevice bus, ushort addr) =>
        (ushort)(bus.Read8(addr) | (bus.Read8((ushort)(addr + 1)) << 8));
}