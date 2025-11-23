// Code authored by Dean Edis (DeanTheCoder).
// Anyone is free to copy, modify, use, compile, or distribute this software,
// either in source code form or as a compiled binary, for any purpose.
//
// If you modify the code, please retain this copyright header,
// and consider contributing back to the repository or letting us know
// about your modifications. Your contributions are valued!
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.

namespace DTC.SM83;

/// <summary>
/// Contract for cartridge memory controllers (MBCs) that handle banked ROM/RAM access.
/// </summary>
public interface IMemoryBankController
{
    bool HasRam { get; }

    byte ReadRom(ushort addr);
    void WriteRom(ushort addr, byte value);

    byte ReadRam(ushort addr);
    void WriteRam(ushort addr, byte value);

    byte[] GetRamSnapshot();
    void LoadRamSnapshot(ReadOnlySpan<byte> data);
}
