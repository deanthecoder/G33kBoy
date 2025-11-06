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
using DTC.Core.Extensions;
using DTC.Core.UnitTesting;
using DTC.SM83;
using Newtonsoft.Json;

namespace UnitTests;

[TestFixture, Parallelizable(ParallelScope.All)]
public class CpuTests : TestsBase
{
    public static SingleTest[] UnprefixedTests { get; } = LoadTests(targetCb: false);
    public static SingleTest[] PrefixedTests { get; } = LoadTests(targetCb: true);

    private static SingleTest[] LoadTests(bool targetCb)
    {
        var testDir = ProjectDir.GetDir("../external/GameboyCPUTests/v2/");
        var testFiles = testDir.TryGetFiles("*.json").Where(o => o.Name.StartsWith("cb") == targetCb);
        return testFiles
            .SelectMany(SingleTest.LoadFromFile)
            .ToArray();
    }

    [Test]
    public void RunUnprefixedTests([ValueSource(nameof(UnprefixedTests))] SingleTest test) =>
        ProcessTest(test);

    [Test]
    public void RunPrefixedTests([ValueSource(nameof(PrefixedTests))] SingleTest test) =>
        ProcessTest(test);

    private static void ProcessTest(SingleTest test)
    {
        var bus = new Bus();
        bus.Attach(new Memory(0x10000), 0x0000, 0xFFFF);
        
        var prepared = test.Prepare();
        prepared.InitialMem.ForEach(o => bus.Write8((ushort)o[0], (byte)o[1]));
        var cpu = new Cpu(bus);
        prepared.InitialRegs.CopyTo(cpu.Reg);
        cpu.Reg.PC--;
        cpu.Clock.Reset();
        cpu.Fetch8();
        
        cpu.Step();

        Assert.That(cpu.Reg, Is.EqualTo(prepared.FinalRegs));
        var comparison = GetMemoryComparisonMessage(cpu.Ram, prepared.FinalMem);
        Assert.That(comparison, Is.Empty);
    }

    private static string GetMemoryComparisonMessage(Bus actual, int[][] expected)
    {
        foreach (var v in expected)
        {
            var addr = v[0];
            var expectedByte = v[1];
            var actualByte = actual.Read8((ushort)addr);
            if (expectedByte != actualByte)
                return $"Memory at 0x{addr:X4} ({addr}) is not equal. Expected 0x{expectedByte:X2} ({expectedByte}), got 0x{actualByte:X2} ({actualByte}).";
        }

        return string.Empty;   
    }

    public class SingleTest
    {
        private readonly CpuStateDto m_initialState;
        private readonly CpuStateDto m_finalState;
        private readonly string m_name;

        private SingleTest(TestCaseDto testCase)
        {
            m_initialState = testCase.Initial;
            m_finalState = testCase.Final;
            m_name = $"({testCase.Name}) {GetMnemonic(m_initialState)}";
        }

        public static IEnumerable<SingleTest> LoadFromFile(FileInfo testJson)
        {
            var json = testJson.ReadAllText();
            var testCases = JsonConvert.DeserializeObject<TestCaseDto[]>(json) ?? [];
            return testCases.Select(testCase => new SingleTest(testCase));
        }

        public PreparedTest Prepare()
        {
            var initialRegs = new Registers();
            var finalRegs = new Registers();

            Populate(initialRegs, m_initialState);
            Populate(finalRegs, m_finalState);

            return new PreparedTest(initialRegs, finalRegs, m_initialState.Ram, m_finalState.Ram);
        }

        private static void Populate(Registers registers, CpuStateDto dto)
        {
            registers.A = (byte)dto.A;
            registers.B = (byte)dto.B;
            registers.C = (byte)dto.C;
            registers.D = (byte)dto.D;
            registers.E = (byte)dto.E;
            registers.H = (byte)dto.H;
            registers.L = (byte)dto.L;
            registers.SP = (ushort)dto.Sp;
            registers.PC = (ushort)dto.Pc;

            var flags = (byte)dto.F;
            registers.Zf = (flags & 0x80) != 0;
            registers.Nf = (flags & 0x40) != 0;
            registers.Hf = (flags & 0x20) != 0;
            registers.Cf = (flags & 0x10) != 0;
        }

        private static string GetMnemonic(CpuStateDto initialState)
        {
            var pc = (ushort)initialState.Pc - 1;
            var ram = initialState.Ram;
            var maxRamAddress = ram.Max(entry => entry[0]);
            var bytesToAllocate = maxRamAddress - pc + 1;
            var mem = new Memory(bytesToAllocate);
            foreach (var ramByte in ram)
            {
                var addr = ramByte[0] - pc;
                if (addr >= 0)
                    mem.Write8((ushort)addr, (byte)ramByte[1]);
            }

            return Disassembler.GetMnemonic(mem, 0);
        }
        
        public override string ToString() => m_name;

        public readonly struct PreparedTest
        {
            public PreparedTest(Registers initialRegs, Registers finalRegs, int[][] initialMem, int[][] finalMem)
            {
                InitialRegs = initialRegs ?? throw new ArgumentNullException(nameof(initialRegs));
                FinalRegs = finalRegs ?? throw new ArgumentNullException(nameof(finalRegs));
                InitialMem = initialMem ?? throw new ArgumentNullException(nameof(initialMem));
                FinalMem = finalMem ?? throw new ArgumentNullException(nameof(finalMem));
            }

            public Registers InitialRegs { get; }
            public Registers FinalRegs { get; }
            public int[][] InitialMem { get; }
            public int[][] FinalMem { get; }
        }

        private sealed class TestCaseDto
        {
            [JsonProperty("name")] public string Name { get; set; } = string.Empty;

            [JsonProperty("initial")] public CpuStateDto Initial { get; set; } = new CpuStateDto();

            [JsonProperty("final")] public CpuStateDto Final { get; set; } = new CpuStateDto();
        }

        private sealed class CpuStateDto
        {
            [JsonProperty("a")] public int A { get; set; }

            [JsonProperty("b")] public int B { get; set; }

            [JsonProperty("c")] public int C { get; set; }

            [JsonProperty("d")] public int D { get; set; }

            [JsonProperty("e")] public int E { get; set; }

            [JsonProperty("f")] public int F { get; set; }

            [JsonProperty("h")] public int H { get; set; }

            [JsonProperty("l")] public int L { get; set; }

            [JsonProperty("pc")] public int Pc { get; set; }

            [JsonProperty("sp")] public int Sp { get; set; }

            [JsonProperty("ram")] public int[][] Ram { get; set; }
        }
    }
}
