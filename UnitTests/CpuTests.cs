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
using DTC.SM83.Devices;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace UnitTests;

[TestFixture, Parallelizable(ParallelScope.All)]
public class CpuTests : TestsBase
{
    public static InstructionTests[] AllTests { get; } = LoadTests();

    private static InstructionTests[] LoadTests() =>
        ProjectDir
            .GetDir("../external/GameboyCPUTests/v2/")
            .TryGetFiles("*.json")
            .Select(InstructionTests.LoadFromFile)
            .ToArray();

    [Test]
    public void RunTests([ValueSource(nameof(AllTests))] InstructionTests test) =>
        test.Run();

    /// <summary>
    /// All all tests for a single instruction. 
    /// </summary>
    public class InstructionTests
    {
        private readonly string m_name;
        private readonly SingleTest[] m_tests;

        private InstructionTests(string name, [NotNull] SingleTest[] tests)
        {
            m_name = name;
            m_tests = tests ?? throw new ArgumentNullException(nameof(tests));
        }

        public static InstructionTests LoadFromFile(FileInfo testJson)
        {
            var json = testJson.ReadAllText();
            var testCases = JsonConvert.DeserializeObject<TestCaseDto[]>(json) ?? [];
            var singleTests = testCases.Select(testCase => new SingleTest(testCase)).ToArray();
            return new InstructionTests(testJson.LeafName(), singleTests);
        }

        public void Run()
        {
            foreach (var test in m_tests)
            {
                using var bus = new Bus(0x10000, Bus.BusType.Trivial);
                var cpu = new Cpu(bus);
        
                var prepared = test.Prepare();
                foreach (var memState in prepared.InitialMem)
                    bus.Write8((ushort)memState[0], (byte)memState[1]);
                
                prepared.InitialRegs.CopyTo(cpu.Reg);
                cpu.Reg.PC--;
                cpu.Bus.ResetClock();
                cpu.Fetch8();
        
                cpu.Step();

                Assert.That(cpu.Reg, Is.EqualTo(prepared.FinalRegs), test.GetMnemonic);
                var comparison = GetMemoryComparisonMessage(cpu.Bus, prepared.FinalMem);
                Assert.That(comparison, Is.Empty, test.GetMnemonic);
            }
        }

        private static string GetMemoryComparisonMessage(IMemDevice actualRam, int[][] expectedValues)
        {
            foreach (var v in expectedValues)
            {
                var addr = v[0];
                var expectedByte = v[1];
                var actualByte = actualRam.Read8((ushort)addr);
                if (expectedByte != actualByte)
                    return $"Memory at 0x{addr:X4} ({addr}) is not equal. Expected 0x{expectedByte:X2} ({expectedByte}), got 0x{actualByte:X2} ({actualByte}).";
            }

            return string.Empty;   
        }

        public override string ToString() => m_name;
    }

    public sealed class TestCaseDto
    {
        [JsonProperty("name")] public string Name { get; set; } = string.Empty;

        [JsonProperty("initial")] public CpuStateDto Initial { get; set; } = new CpuStateDto();

        [JsonProperty("final")] public CpuStateDto Final { get; set; } = new CpuStateDto();
    }

    public sealed class CpuStateDto
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

    /// <summary>
    /// A test for a single setup of a single instruction.
    /// </summary>
    public class SingleTest
    {
        private readonly CpuStateDto m_initialState;
        private readonly CpuStateDto m_finalState;
        private readonly string m_name;

        public SingleTest(TestCaseDto testCase)
        {
            m_initialState = testCase.Initial;
            m_finalState = testCase.Final;
            m_name = testCase.Name;
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

        public string GetMnemonic()
        {
            var pc = (ushort)m_initialState.Pc - 1;
            var ram = m_initialState.Ram;
            var maxRamAddress = ram.Max(entry => entry[0]);
            var bytesToAllocate = maxRamAddress - pc + 1;
            var mem = new Bus(bytesToAllocate, Bus.BusType.Trivial);
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
    }
}
