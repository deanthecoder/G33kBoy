// Code authored by Dean Edis (DeanTheCoder).
// Anyone is free to copy, modify, use, compile, or distribute this software,
// either in source code form or as a compiled binary, for any purpose.
// 
// If you modify the code, please retain this copyright header,
// and consider contributing back to the repository or letting us know
// about your modifications. Your contributions are valued!
// 
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.

using System.Text;
using DTC.Core.Extensions;
using DTC.Core.UnitTesting;
using DTC.SM83;
using DTC.SM83.Extensions;

namespace UnitTests;

[TestFixture, Parallelizable(ParallelScope.All)]
public class BlarggTests : TestsBase
{
    private const ulong OneSecondTicks = 4_194_304; // 4.194304 MHz DMG clock.

    public static IEnumerable<TestCaseData> CpuTestRomFiles =>
        ProjectDir.GetFiles("../external/blargg-test-roms/cpu_instrs/individual/*.gb")
            .Select(f => new TestCaseData(f).SetName(f.Name));

    public static IEnumerable<TestCaseData> SoundTestRomFiles =>
        ProjectDir.GetDir("../external/blargg-test-roms/dmg_sound/rom_singles")
            .TryGetFiles("*.gb")
            .OrderBy(f => f.Name)
            .Select(f => new TestCaseData(f).SetName($"Sound_{f.Name}"));

    [TestCaseSource(nameof(CpuTestRomFiles))]
    public void RunCpuRoms(FileInfo romFile)
    {
        using var bus = new Bus(0x10000, Bus.BusType.Minimal);
        var cpu = new Cpu(bus);
        cpu.LoadRom(new Cartridge(romFile.ReadAllBytes()));
        cpu.SkipBootRom();

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

    [TestCaseSource(nameof(SoundTestRomFiles))]
    [NonParallelizable]
    public void RunSoundRoms(FileInfo romFile)
    {
        Assert.That(romFile, Does.Exist, $"Missing Blargg sound ROM at {romFile.FullName}");

        using var bus = new Bus(0x10000, Bus.BusType.GameBoy);
        var cpu = new Cpu(bus);
        cpu.LoadRom(new Cartridge(romFile.ReadAllBytes()));
        cpu.SkipBootRom();

        var serialBus = new SerialDevice();
        bus.Attach(serialBus);

        const ulong timeoutTicks = OneSecondTicks * 10;
        const ushort statusAddr = 0xA000;      // 0x80 while running; result code (0 = pass) when done.
        const ushort signatureAddr = 0xA001;   // 0xDE,0xB0,0x61 indicates valid test output.
        const ushort outputAddr = 0xA004;

        byte status = 0x80;
        var hasSignature = false;
        var hasSeenRunning = false;
        while (bus.ClockTicks < timeoutTicks)
        {
            cpu.Step();

            status = bus.Read8(statusAddr);
            hasSignature = HasSignature(bus, signatureAddr);
            if (hasSignature && status == 0x80)
                hasSeenRunning = true;

            if (hasSeenRunning && status != 0x80)
                break;
        }

        var output = hasSignature ? ReadOutput(bus, outputAddr) : string.Empty;

        Assert.That(hasSignature, Is.True, $"Sound test never wrote signature before timeout ({bus.ClockTicks}/{timeoutTicks} T ticks). Serial output: {serialBus.Output}");
        Assert.That(hasSeenRunning, Is.True, $"Sound test never entered running state (0x80) within {timeoutTicks} T ticks. Output: {output} Serial output: {serialBus.Output}");
        Assert.That(status, Is.Not.EqualTo((byte)0x80), $"Sound test did not complete within {timeoutTicks} T ticks. Output: {output} Serial output: {serialBus.Output}");
        Assert.That(status, Is.EqualTo((byte)0x00), $"Sound test failed with code {status}. Output: {output} Serial output: {serialBus.Output}");
    }

    private static bool HasSignature(Bus bus, ushort signatureAddr) =>
        bus.Read8(signatureAddr) == 0xDE &&
        bus.Read8((ushort)(signatureAddr + 1)) == 0xB0 &&
        bus.Read8((ushort)(signatureAddr + 2)) == 0x61;

    private static string ReadOutput(Bus bus, ushort startAddr)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < 512; i++)
        {
            var value = bus.Read8((ushort)(startAddr + i));
            if (value == 0x00)
                break;
            builder.Append((char)value);
        }

        return builder.ToString();
    }
}
