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

using DTC.Core.UnitTesting;
using DTC.SM83;

namespace UnitTests;

public class MemoryTests : TestsBase
{
    [Test]
    public void CheckDefaultMemoryIsZeroed()
    {
        var memory = new Memory(0xFFFF);

        Assert.That(memory[0x1234], Is.Zero);
    }

    [Test]
    public void CheckIndexerReadsBackWrittenValue()
    {
        var memory = new Memory(0xFFFF);
        const ushort address = 0x2345;
        const byte value = 0x7A;

        memory[address] = value;

        Assert.That(memory[address], Is.EqualTo(value));
    }

    [Test]
    public void CheckWrite16StoresLittleEndianBytes()
    {
        var memory = new Memory(0xFFFF);
        const ushort address = 0x3456;

        memory.Write16(address, 0xABCD);

        Assert.That(memory[address], Is.EqualTo(0xCD));
        Assert.That(memory[address + 1], Is.EqualTo(0xAB));
    }

    [Test]
    public void CheckRead16CombinesLittleEndianBytes()
    {
        var memory = new Memory(0xFFFF);
        const ushort address = 0x4567;

        memory[address] = 0xEF;
        memory[address + 1] = 0x12;

        Assert.That(memory.Read16(address), Is.EqualTo(0x12EF));
    }

    [Test]
    public void CheckEqualityOperatorWithIdenticalMemory()
    {
        var memory1 = new Memory(1024);
        var memory2 = new Memory(1024);

        memory1[0x100] = 0x42;
        memory2[0x100] = 0x42;

        Assert.That(memory1, Is.EqualTo(memory2));
    }

    [Test]
    public void CheckEqualityOperatorWithDifferentMemory()
    {
        var memory1 = new Memory(1024);
        var memory2 = new Memory(1024);

        memory1[0x100] = 0x42;
        memory2[0x100] = 0x99;

        Assert.That(memory1, Is.Not.EqualTo(memory2));
    }
}
