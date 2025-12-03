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
    private const ulong TimeoutTicks = OneSecondTicks * 20;

    public static IEnumerable<TestCaseData> CpuTestRomFiles =>
        new[]
            {
                "cpu_instrs/individual/*.gb",
                "instr_timing/*.gb",
                "mem_timing/*.gb"
                }
            .SelectMany(o => ProjectDir.GetFiles($"../external/blargg-test-roms/{o}"))
            .OrderBy(f => f.Name)
            .Select(f => new TestCaseData(f).SetName(GetTestName(f)));

    public static IEnumerable<TestCaseData> MoreTestRomFiles =>
        new[]
            {
                "oam_bug/rom_singles/*.gb",
                "dmg_sound/rom_singles/*.gb"
            }
            .SelectMany(o => ProjectDir.GetFiles($"../external/blargg-test-roms/{o}"))
            .OrderBy(f => f.Name)
            .Select(f => new TestCaseData(f).SetName(GetTestName(f)));

    private static string GetTestName(FileInfo f)
    {
        var names = new List<string>();
        var d = f.Directory;
        while (d != null && d.Name != "blargg-test-roms")
        {
            if (d.Name != "rom_singles" && d.Name != "individual")
                names.Add(d.Name);
            d = d.Parent;
        }
        return string.Join('/', names) + '/' + f.Name;
    }

    /// <summary>
    /// These tests monitor the serial output.
    /// </summary>
    [TestCaseSource(nameof(CpuTestRomFiles))]
    public void RunTestRoms(FileInfo romFile)
    {
        using var bus = new Bus(0x10000, Bus.BusType.Minimal);
        var cpu = new Cpu(bus);
        cpu.LoadRom(new Cartridge(romFile.ReadAllBytes()));
        cpu.SkipBootRom();

        var serialBus = new SerialDevice();
        bus.Attach(serialBus);
        cpu.Reg.PC = 0x0100;

        while (bus.ClockTicks < TimeoutTicks)
        {
            var oldPC = cpu.Reg.PC;
            cpu.Step();
            if (cpu.Reg.PC == oldPC && (serialBus.Output.Contains("Passed") || serialBus.Output.Contains("Failed")))
                break;
        }

        Assert.That(bus.ClockTicks, Is.LessThanOrEqualTo(TimeoutTicks), $"Time-out. Serial output: {serialBus.Output}");
        Assert.That(serialBus.Output, Does.Contain("Passed"));
    }
    
    /// <summary>
    /// These tests monitor output using address 0xA004.
    /// </summary>
    [TestCaseSource(nameof(MoreTestRomFiles))]
    [NonParallelizable]
    public void RunMoreRoms(FileInfo romFile)
    {
        Assert.That(romFile, Does.Exist, $"Missing Blargg sound ROM at {romFile.FullName}");

        using var bus = new Bus(0x10000, Bus.BusType.GameBoy);
        var cpu = new Cpu(bus);
        cpu.LoadRom(new Cartridge(romFile.ReadAllBytes()));
        cpu.SkipBootRom();

        var serialBus = new SerialDevice();
        bus.Attach(serialBus);

        const ushort statusAddr = 0xA000;      // 0x80 while running; result code (0 = pass) when done.
        const ushort signatureAddr = 0xA001;   // 0xDE,0xB0,0x61 indicates valid test output.
        const ushort outputAddr = 0xA004;

        byte status = 0x80;
        var hasSignature = false;
        var hasSeenRunning = false;
        while (bus.ClockTicks < TimeoutTicks)
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

        Assert.That(hasSignature, Is.True, $"Time-out. Serial output: {serialBus.Output}");
        Assert.That(hasSeenRunning, Is.True, $"Test never entered running state (0x80) within {TimeoutTicks} T ticks. Output: {output} Serial output: {serialBus.Output}");
        Assert.That(status, Is.Not.EqualTo((byte)0x80), $"Test did not complete within {TimeoutTicks} T ticks. Output: {output} Serial output: {serialBus.Output}");
        Assert.That(status, Is.EqualTo((byte)0x00), $"Test failed with code {status}. Output: {output} Serial output: {serialBus.Output}");
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
