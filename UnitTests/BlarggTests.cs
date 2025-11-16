// Code authored by Dean Edis (DeanTheCoder).
// Anyone is free to copy, modify, use, compile, or distribute this software,
// either in source code form or as a compiled binary, for any purpose.
// 
// If you modify the code, please retain this copyright header,
// and consider contributing back to the repository or letting us know
// about your modifications. Your contributions are valued!
// 
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.

using DTC.Core.Extensions;
using DTC.Core.UnitTesting;
using DTC.SM83;
using DTC.SM83.Extensions;

namespace UnitTests;

[TestFixture, Parallelizable(ParallelScope.All)]
public class BlarggTests : TestsBase
{
    public static IEnumerable<TestCaseData> CpuTestRomFiles =>
        ProjectDir.GetFiles("../external/blargg-test-roms/cpu_instrs/individual/*.gb")
            .Select(f => new TestCaseData(f).SetName(f.Name));
    public static IEnumerable<TestCaseData> OAMTestRomFiles =>
        ProjectDir.GetFiles("../external/blargg-test-roms/oam_bug/rom_singles/*.gb")
            .Select(f => new TestCaseData(f).SetName(f.Name));

    [TestCaseSource(nameof(CpuTestRomFiles))]
    public void RunCpuRoms(FileInfo romFile)
    {
        using var bus = new Bus(0x10000, Bus.BusType.Minimal);
        var cpu =
            new Cpu(bus)
                .LoadRom(romFile.ReadAllBytes())
                .SkipBootRom();

        var serialBus = new SerialDevice();
        bus.Attach(serialBus);
        cpu.Reg.PC = 0x0100;

        while (true)
        {
            var oldPC = cpu.Reg.PC;
            cpu.Step();
            if (cpu.Reg.PC == oldPC && (serialBus.Output.Contains("Passed") || serialBus.Output.Contains("Failed")))
                break;
        }

        Assert.That(serialBus.Output, Does.Contain("Passed"));
    }
    
    [TestCaseSource(nameof(OAMTestRomFiles))]
    public void RunOAMRoms(FileInfo romFile)
    {
        using var bus = new Bus(0x10000, Bus.BusType.Minimal);
        var cpu =
            new Cpu(bus)
                .LoadRom(romFile.ReadAllBytes())
                .SkipBootRom();

        var serialBus = new SerialDevice();
        bus.Attach(serialBus);
        cpu.Reg.PC = 0x0100;

        while (true)
        {
            var oldPC = cpu.Reg.PC;
            cpu.Step();
            if (cpu.Reg.PC == oldPC && (serialBus.Output.Contains("Passed") || serialBus.Output.Contains("Failed")))
                break;
        }

        Assert.That(serialBus.Output, Does.Contain("Passed"));
    }
}