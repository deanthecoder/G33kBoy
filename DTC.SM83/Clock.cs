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
using System.Diagnostics;

namespace DTC.SM83;

[DebuggerDisplay("Ticks = {Ticks}")]
public class Clock
{
    public ulong Ticks { get; private set; }

    /// <summary>
    /// Advance the clock by t CPU ticks.
    /// </summary>
    /// <remarks>
    /// 4 ticks per CPU cycle (4T = 1M).
    /// </remarks>
    public void AdvanceT(ulong t)
    {
        Ticks += t;
    }
    public void Reset()
    {
        Ticks = 0;
    }
}