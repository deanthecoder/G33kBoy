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

public abstract class CpuDebuggerBase : ICpuDebugger
{
    public virtual void BeforeInstruction(Cpu cpu, ushort opcodeAddress, byte opcode)
    {
    }

    public virtual void AfterStep(Cpu cpu)
    {
    }

    public virtual void OnMemoryWrite(Cpu cpu, ushort address, byte value)
    {
    }
}