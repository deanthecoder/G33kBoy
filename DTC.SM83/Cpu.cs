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
namespace DTC.SM83;

public class Cpu
{
    private byte m_fetchedOpcode;
    private ulong m_fetchStartTime;

    public Memory Ram { get; }
    public Registers Reg { get; private set; }

    public Cpu(Memory ram)
    {
        Ram = ram;
        Reset();
    }
    
    public void Reset()
    {
        Reg = new Registers();
        Ram.Clock.Reset();

        // Pre-load first opcode.
        m_fetchStartTime = Ram.Clock.Ticks;
        Fetch8();
    }

    /// <summary>
    /// Fetch byte at PC, and advance PC.
    /// </summary>
    public byte Fetch8() =>
        m_fetchedOpcode = Ram.Read8(Reg.PC++);

    /// <summary>
    /// Fetch word at PC, and advance PC (x2).
    /// </summary>
    public ushort Fetch16() =>
        (ushort)(Fetch8() | (Fetch8() << 8));

    public void Step()
    {
        // Decode instruction.
        var instruction = m_fetchedOpcode == 0xCB ? PrefixedInstructions.Table[Fetch8()] : Instructions.Table[m_fetchedOpcode];
        if (instruction == null)
            throw new InvalidOperationException($"Opcode {m_fetchedOpcode:X2} has null instruction.");
        
        // Execute instruction.
        var expectedTickDuration = instruction.Execute(this);
        var executeEndTime = Ram.Clock.Ticks;
        var actualTickDuration = executeEndTime - m_fetchStartTime;
        if (actualTickDuration != expectedTickDuration)
        {
            // todo - Remove this once all instructions are implemented correctly.
            throw new InvalidOperationException($"Instruction {instruction.Mnemonic} expected duration of {expectedTickDuration} CPU cycles, but {actualTickDuration} CPU ticks actually elapsed.");
        }

        // Fetch opcode.
        m_fetchStartTime = Ram.Clock.Ticks;
        Fetch8();
    }
    
    public void InternalWaitM(ulong m = 1) =>
        Ram.Clock.AdvanceT(4 * m);
}