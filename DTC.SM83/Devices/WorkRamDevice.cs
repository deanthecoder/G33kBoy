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
/// Represents the working RAM device (0xC000 - 0xDFFF).
/// </summary>
public class WorkRamDevice : RamDeviceBase
{
    public WorkRamDevice() : base(0xC000, 0xDFFF, isUsable: true)
    {
    }
}
