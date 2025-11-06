// Code authored by Dean Edis (DeanTheCoder).
// Anyone is free to copy, modify, use, compile, or distribute this software,
// either in source code form or as a compiled binary, for any purpose.
// 
// If you modify the code, please retain this copyright header,
// and consider contributing back to the repository or letting us know
// about your modifications. Your contributions are valued!
// 
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.
using DTC.Core.UnitTesting;

namespace UnitTests;

[TestFixture]
public class BlarggTests : TestsBase
{
    public static IEnumerable<FileInfo> CpuTestRomFiles =>
        ProjectDir.GetFiles("../external/blargg-test-roms/cpu_instrs/individual/*.gb");

    [Test, Sequential]
    public void RunCpuRoms([ValueSource(nameof(CpuTestRomFiles))] FileInfo romFile)
    {
        
    }
}