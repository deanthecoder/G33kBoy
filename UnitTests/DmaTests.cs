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

using DTC.SM83;

namespace UnitTests;

[TestFixture]
public class DmaTests
{
    /// <summary>
    /// Regression test for OAM DMA CPU stalling to avoid RST $38 loops corrupting HDMA regs (Robocop tilemap issue).
    /// </summary>
    [Test]
    public void CpuDoesNotExecuteDuringOamDma()
    {
        using var bus = new Bus(0x10000, Bus.BusType.Minimal);
        var cpu = new Cpu(bus);
        cpu.Reg.PC = 0x0000;
        bus.UncheckedWrite(0x0000, 0x00); // NOP
        bus.UncheckedWrite(0x0001, 0x00); // NOP
        bus.UncheckedWrite(0x0002, 0x00); // NOP
        cpu.Fetch8(); // Prime first opcode for Step().

        bus.Dma.Start(0x80); // Start OAM DMA from 0x8000.

        Assert.That(bus.Dma.IsTransferActive, Is.True);
        var pcAfterDmaStart = cpu.Reg.PC;
        var spAfterDmaStart = cpu.Reg.SP;

        for (var i = 0; i < 200 && bus.Dma.IsTransferActive; i++)
            cpu.Step();

        Assert.That(cpu.Reg.PC, Is.EqualTo(pcAfterDmaStart));
        Assert.That(cpu.Reg.SP, Is.EqualTo(spAfterDmaStart));

        while (bus.Dma.IsTransferActive)
            cpu.Step();

        cpu.Step(); // Exit DMA stall and prefetch next opcode.
        var pcAfterResume = cpu.Reg.PC;
        cpu.Step(); // Execute NOP.

        Assert.That(cpu.Reg.PC, Is.GreaterThan(pcAfterResume));
        Assert.That(cpu.Reg.PC, Is.GreaterThan(pcAfterDmaStart));
    }
}
