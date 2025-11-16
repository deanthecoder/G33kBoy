// Code authored by Dean Edis (DeanTheCoder).
// Anyone is free to copy, modify, use, compile, or distribute this software,
// either in source code form or as a compiled binary, for any purpose.
// 
// If you modify the code, please retain this copyright header,
// and consider contributing back to the repository or letting us know
// about your modifications. Your contributions are valued!
// 
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.
using JetBrains.Annotations;

namespace DTC.SM83.Devices;

/// <summary>
/// A mirror of the WRAM device.
/// </summary>
public class EchoRamDevice : IMemDevice
{
    private readonly WorkRamDevice m_wram;

    public ushort FromAddr => 0xE000;
    public ushort ToAddr => 0xFDFF;

    public EchoRamDevice([NotNull] WorkRamDevice wram)
    {
        m_wram = wram ?? throw new ArgumentNullException(nameof(wram));
    }

    public byte Read8(ushort addr)
    {
        // Remap from echo RAM range (0xE000-0xFDFF) to WRAM range (0xC000-0xDDFF)
        var wramAddr = (ushort)(addr - 0x2000);
        return m_wram.Read8(wramAddr);
    }

    public void Write8(ushort addr, byte value)
    {
        // Remap from echo RAM range (0xE000-0xFDFF) to WRAM range (0xC000-0xDDFF)
        var wramAddr = (ushort)(addr - 0x2000);
        m_wram.Write8(wramAddr, value);
    }
}