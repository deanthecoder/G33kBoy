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

namespace DTC.SM83.Debuggers;

public sealed class PcBreakpointDebugger : CpuDebuggerBase
{
    private readonly ushort m_breakAddress;
    private readonly Action m_action;

    public PcBreakpointDebugger(ushort breakAddress, Action action)
    {
        m_breakAddress = breakAddress;
        m_action = action;
    }

    public override void BeforeInstruction(Cpu cpu, ushort opcodeAddress, byte opcode)
    {
        if (opcodeAddress != m_breakAddress)
            return;

        cpu.InstructionLogger.Write(() => $"[Debugger] PC hit {opcodeAddress:X4} (opcode {opcode:X2}).");
        m_action?.Invoke();
    }
}