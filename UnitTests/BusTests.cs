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

using DTC.Core.UnitTesting;
using DTC.SM83;

namespace UnitTests;

public class BusTests : TestsBase
{
    [Test]
    public void CheckDefaultMemoryIsZeroed()
    {
        using var memory = new Bus(0x2000, attachDevices: false);

        Assert.That(memory.Read8(0x1234), Is.Zero);
    }

    [Test]
    public void CheckIndexerReadsBackWrittenValue()
    {
        using var memory = new Bus(0x4000, attachDevices: false);
        const ushort address = 0x2345;
        const byte value = 0x7A;

        memory.Write8(address, value);

        Assert.That(memory.Read8(address), Is.EqualTo(value));
    }
}
