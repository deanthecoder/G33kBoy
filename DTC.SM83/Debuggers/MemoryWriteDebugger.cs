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

public sealed class MemoryWriteDebugger : CpuDebuggerBase
{
    private readonly ushort m_targetAddress;
    private readonly byte? m_targetValue;
    private readonly Action<byte> m_onWrite;

    public MemoryWriteDebugger(ushort targetAddress, byte targetValue, Action<byte> onWrite)
    {
        m_targetAddress = targetAddress;
        m_targetValue = targetValue;
        m_onWrite = onWrite;
    }

    public MemoryWriteDebugger(ushort targetAddress, Action<byte> onWrite)
    {
        m_targetAddress = targetAddress;
        m_targetValue = null;
        m_onWrite = onWrite;
    }

    public override void OnMemoryWrite(Cpu cpu, ushort address, byte value)
    {
        if (address != m_targetAddress)
            return;

        if (m_targetValue.HasValue && value != m_targetValue.Value)
            return;
        
        cpu.InstructionLogger.Write(() => $"[Debugger] Write {address:X4} <= {value:X2} at PC {cpu.CurrentInstructionAddress:X4}.");
        m_onWrite?.Invoke(value);
    }
}
