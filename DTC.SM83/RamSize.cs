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

public enum RamSize : byte
{
    None = 0x00,
    RamUnused1 = 0x01,
    Ram8K = 0x02,
    Ram32K = 0x03,
    Ram128K = 0x04,
    Ram64K = 0x05
}