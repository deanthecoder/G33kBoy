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
using DTC.Core.Extensions;
using DTC.Core.UnitTesting;
using DTC.SM83;
using Newtonsoft.Json;

namespace UnitTests;

[TestFixture]
public class CpuTests : TestsBase
{
    public static SingleTest[] Tests { get; } = LoadTests();

    private static SingleTest[] LoadTests()
    {
        var testDir = ProjectDir.GetDir("../external/GameboyCPUTests/v2/");
        var testFiles = testDir.TryGetFiles("*.json");
        return testFiles
            .SelectMany(SingleTest.LoadFromFile)
            .ToArray();
    }

    [Test]
    public void RunTests([ValueSource(nameof(Tests))] SingleTest test)
    {
        var cpu = new Cpu(test.InitialMem);
        test.InitialRegs.CopyTo(cpu.Reg);

        cpu.Step();

        Assert.That(cpu.Reg, Is.EqualTo(test.FinalRegs));
        Assert.That(cpu.Ram, Is.EqualTo(test.FinalMem));
    }

    public class SingleTest
    {
        private readonly string m_name;

        public Registers InitialRegs { get; } = new Registers();
        public Registers FinalRegs { get; } = new Registers();
        public Memory InitialMem { get; } = new Memory(0xFFFF);
        public Memory FinalMem { get; } = new Memory(0xFFFF);

        private SingleTest(string name)
        {
            m_name = name;
        }

        public static IEnumerable<SingleTest> LoadFromFile(FileInfo testJson)
        {
            var json = testJson.ReadAllText();
            var testCases = JsonConvert.DeserializeObject<TestCaseDto[]>(json) ?? [];
            foreach (var testCase in testCases)
            {
                var name = string.IsNullOrWhiteSpace(testCase.Name)
                    ? testJson.Name
                    : testCase.Name;

                var singleTest = new SingleTest(name);
                Populate(singleTest.InitialRegs, testCase.Initial);
                Populate(singleTest.FinalRegs, testCase.Final);
                Populate(singleTest.InitialMem, testCase.Initial);
                Populate(singleTest.FinalMem, testCase.Final);

                yield return singleTest;
            }
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

        private static void Populate(Memory memory, CpuStateDto dto)
        {
            if (dto.Ram is null)
                return;

            foreach (var entry in dto.Ram)
            {
                if (entry.Length < 2)
                    continue;

                var address = (ushort)entry[0];
                if (address >= 0xFFFF)
                    continue;

                memory[address] = (byte)entry[1];
            }
        }

        public override string ToString() => m_name;

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

            [JsonProperty("ram")] public int[][]? Ram { get; set; }
        }
    }
}
