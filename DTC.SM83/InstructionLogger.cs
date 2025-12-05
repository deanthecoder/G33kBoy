// Code authored by Dean Edis (DeanTheCoder).
// Anyone is free to copy, modify, use, compile, or distribute this software,
// either in source code form or as a compiled binary, for any purpose.
//
// If you modify the code, please retain this copyright header,
// and consider contributing back to the repository or letting us know
// about your modifications. Your contributions are valued!
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.
using DTC.Core;
using DTC.Core.Extensions;

namespace DTC.SM83;

/// <summary>
/// Collects a rolling history of CPU instructions and related hardware events.
/// </summary>
public class InstructionLogger
{
    private readonly CircularBuffer<string> m_instructionLog = new(2048);
    private readonly Lock m_instructionLogLock = new();

    public bool IsEnabled { get; set; }

    public void Write(Func<string> message)
    {
        if (!IsEnabled)
            return;

        lock (m_instructionLogLock)
            m_instructionLog.Write(message());
    }

    public void DumpToConsole()
    {
        lock (m_instructionLogLock)
        {
            Console.WriteLine($"----- CPU instruction history @ {DateTime.Now:O} -----");
            m_instructionLog.ForEach(o => Console.WriteLine(o));
        }
    }
}
