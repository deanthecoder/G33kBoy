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

namespace DTC.SM83.Debuggers;

public sealed class MemoryReadDebugger : CpuDebuggerBase
{
    private readonly ushort m_targetAddress;
    private readonly byte? m_targetValue;
    private bool m_pendingDump;
    private ulong m_dumpAtClock;
    private readonly Action<byte> m_onRead;

    public MemoryReadDebugger(ushort targetAddress, byte targetValue, Action<byte> onRead)
    {
        m_targetAddress = targetAddress;
        m_targetValue = targetValue;
        m_onRead = onRead;
    }

    public MemoryReadDebugger(ushort targetAddress, Action<byte> onRead)
    {
        m_targetAddress = targetAddress;
        m_targetValue = null;
        m_onRead = onRead;
    }

    public override void AfterStep(Cpu cpu)
    {
        if (!m_pendingDump)
            return;

        if (cpu.Bus.ClockTicks >= m_dumpAtClock)
        {
            m_pendingDump = false;
            cpu.InstructionLogger.DumpToConsole();
        }
    }

    public override void OnMemoryRead(Cpu cpu, ushort address, byte value)
    {
        if (address != m_targetAddress)
            return;

        if (m_targetValue.HasValue && value != m_targetValue.Value)
            return;

        cpu.InstructionLogger.Write(() => $"[Debugger] Read {address:X4} => {value:X2} at PC {cpu.CurrentInstructionAddress:X4}.");

        m_pendingDump = true;
        m_dumpAtClock = cpu.Bus.ClockTicks + 96; // Dump the CPU history after a few more cycles.
        m_onRead?.Invoke(value);
    }
}
