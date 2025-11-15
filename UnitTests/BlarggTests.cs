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
using DTC.SM83.Devices;
using DTC.SM83.Extensions;

namespace UnitTests;

[TestFixture, Parallelizable(ParallelScope.All)]
public class BlarggTests : TestsBase
{
    public static IEnumerable<FileInfo> CpuTestRomFiles =>
        ProjectDir.GetFiles("../external/blargg-test-roms/cpu_instrs/individual/*.gb");

    [Test, Sequential]
    public void RunCpuRoms([ValueSource(nameof(CpuTestRomFiles))] FileInfo romFile)
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

    /// <summary>
    /// Minimal bus to allow capturing of serial output, used by the Blargg tests when no PPU is implemented.
    /// </summary>
    private class SerialDevice : IMemDevice
    {
        public ushort FromAddr => 0xFF01;
        public ushort ToAddr => 0xFF02;

        /// <summary>
        /// Transfer data, Serial Control.
        /// </summary>
        private readonly byte[] m_data = new byte[2];

        private readonly StringBuilder m_output = new StringBuilder();
        
        public string Output => m_output.ToString();
        
        public byte Read8(ushort addr) => 0x00;

        public void Write8(ushort addr, byte value)
        {
            switch (addr)
            {
                case 0xFF01:
                    // Transfer data.
                    m_data[0] = value;
                    return;
                case 0xFF02:
                    // Serial Control.
                    m_output.Append((char)m_data[0]);
                    m_data[1] = 0x01;
                    break;
            }
        }
    }
}