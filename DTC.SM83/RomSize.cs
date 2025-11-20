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

// ReSharper disable InconsistentNaming
namespace DTC.SM83;

public enum RomSize : byte
{
    Rom32K = 0x00,
    Rom64K = 0x01,
    Rom128K = 0x02,
    Rom256K = 0x03,
    Rom512K = 0x04,
    Rom1M = 0x05,
    Rom2M = 0x06,
    Rom4M = 0x07,
    Rom8M = 0x08,
    Rom1_1M = 0x52,
    Rom1_2M = 0x53,
    Rom1_5M = 0x54
}