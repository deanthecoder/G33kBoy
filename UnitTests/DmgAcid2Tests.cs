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

[TestFixture]
public class DmgAcid2Tests : TestsBase
{
    private const ulong OneSecondTicks = 4_194_304; // 4.194304 MHz DMG clock.

    private static FileInfo RomFile =>
        ProjectDir.GetFile("../external/dmg-acid2.gb");

    [Test]
    public void Run()
    {
        var romFile = RomFile;
        Assert.That(romFile, Does.Exist, $"Missing dmg-acid2 ROM at {romFile.FullName}");

        using var bus = new Bus(0x10000, Bus.BusType.GameBoy);
        var cpu = new Cpu(bus);
        cpu.LoadRom(new Cartridge(romFile.ReadAllBytes()));
        cpu.SkipBootRom();

        string bufferHash = null;
        bus.PPU.FrameRendered += (_, frameBuffer) => bufferHash = frameBuffer.GetMd5Hex();

        const string expectedHash = "0969441DC37EE6A3BB623D456A4DFD8D";
        while (bufferHash != expectedHash && bus.ClockTicks < OneSecondTicks)
            cpu.Step();

        Assert.That(bufferHash, Is.Not.Null, $"No frame rendered within {OneSecondTicks} T ticks.");
        if (bufferHash != expectedHash)
        {
            bus.PPU.Dump(Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                .ToDir()
                .GetFile("acid-test-fail.tga"));
        }
        Assert.That(bufferHash, Is.EqualTo(expectedHash));
    }
}
