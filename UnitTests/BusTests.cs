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
        using var memory = new Bus(0x2000, Bus.BusType.Trivial);

        Assert.That(memory.Read8(0x1234), Is.Zero);
    }

    [Test]
    public void CheckIndexerReadsBackWrittenValue()
    {
        using var memory = new Bus(0x4000, Bus.BusType.Trivial);
        const ushort address = 0x2345;
        const byte value = 0x7A;

        memory.Write8(address, value);

        Assert.That(memory.Read8(address), Is.EqualTo(value));
    }

    [Test]
    public void TracksWritesToWorkRam()
    {
        using var memory = new Bus(0x10000, Bus.BusType.Trivial);
        const ushort address = 0xC234;

        Assert.That(memory.IsUninitializedWorkRam(address), Is.True);

        memory.Write8(address, 0x11);

        Assert.That(memory.IsUninitializedWorkRam(address), Is.False);
    }

    [Test]
    public void TracksWritesAcrossWorkRamEcho()
    {
        using var memory = new Bus(0x10000, Bus.BusType.Trivial);
        const ushort wramAddr = 0xC345;
        const ushort echoAddr = 0xE345;

        memory.Write8(wramAddr, 0x22);

        Assert.That(memory.IsUninitializedWorkRam(echoAddr), Is.False);
    }

    [Test]
    public void DetectsOamAndIoRanges()
    {
        using var memory = new Bus(0x10000, Bus.BusType.Trivial);

        Assert.That(Bus.IsOamOrUnusable(0xFE00), Is.True);
        Assert.That(Bus.IsOamOrUnusable(0xFEFF), Is.True);
        Assert.That(Bus.IsOamOrUnusable(0xFDFF), Is.False);

        Assert.That(Bus.IsIo(0xFF00), Is.True);
        Assert.That(Bus.IsIo(0xFF7F), Is.True);
        Assert.That(Bus.IsIo(0xFF80), Is.False);
    }
}
