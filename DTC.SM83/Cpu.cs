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
    
    public bool IsHalted { get; set; }
    
    /// <summary>
    /// 
    /// </summary>
    public bool HaltBug { get; set; }
    
    /// <summary>
    /// Whether interrupts are globally enabled.
    /// </summary>
    public bool IME { get; set; }
    
    /// <summary>
    /// Whether interrupts are pending.
    /// </summary>
    /// <remarks>
    /// Used to handle EI delay.
    /// </remarks>
    public bool PendingIME { get; set; }
    
    /// <summary>
    /// Interrupt enable mask.
    /// </summary>
    public byte IE => Ram.Read8(0xFFFF);
    
    /// <summary>
    /// Interrupt request flags.
    /// </summary>
    public byte IF => Ram.Read8(0xFF0F);

    public Cpu(Memory ram)
    {
        Ram = ram;
        Reset();
    }
    
    public void Reset()
    {
        Reg = new Registers();
        Ram.Clock.Reset();
        
        IsHalted = false;
        HaltBug = false;
        IME = false;
        PendingIME = false;

        // Pre-load first opcode.
        m_fetchStartTime = Ram.Clock.Ticks;
        Fetch8();
    }

    /// <summary>
    /// Fetch byte at PC, and advance PC.
    /// </summary>
    public byte Fetch8()
    {
        var imm = Ram.Read8(Reg.PC);
        if (HaltBug)
            HaltBug = false; // consume the bug: don't advance PC this time
        else
            Reg.PC++;
        
        m_fetchedOpcode = imm;
        return m_fetchedOpcode;
    }
    
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

        // Enable interrupts if necessary.
        if (PendingIME)
        {
            IME = true;
            PendingIME = false;
        }

        // Check/wake/service interrupts
        HandleInterrupts();

        // Fetch opcode.
        m_fetchStartTime = Ram.Clock.Ticks;
        if (IsHalted)
            Ram.Clock.AdvanceT(4); // Re-queue the HALT instruction. 
        else
            Fetch8();
    }
    
    public void InternalWaitM(ulong m = 1) =>
        Ram.Clock.AdvanceT(4 * m);

    private void HandleInterrupts()
    {
        var ie = IE;
        var iff = IF;
        var pending = (byte)(ie & iff);
        if (pending == 0)
            return;

        // Wake from HALT if we were halted
        IsHalted = false;

        // Only service when interrupts are enabled.
        if (!IME)
            return; // Interrupts are disabled.

        // Disable nested interrupts
        IME = false;

        if ((pending & 0x01) != 0)
        {
            // VBlank
            Service(0x0040, 0x01);
            return;
        }
        
        if ((pending & 0x02) != 0)
        {
            // LCD STAT
            Service(0x0048, 0x02);
            return;
        }
        
        if ((pending & 0x04) != 0)
        {
            // Timer
            Service(0x0050, 0x04);
            return;
        }
        
        if ((pending & 0x08) != 0)
        {
            // Serial
            Service(0x0058, 0x08);
            return;
        }
        
        if ((pending & 0x10) != 0)
        {
            // Joypad
            Service(0x0060, 0x10);
            return;
        }
        
        return;

        // Interrupt priority: VBlank, STAT, Timer, Serial, Joypad
        void Service(ushort vector, byte bitMask)
        {
            PushPC();

            // Clear IF bit
            Ram.Write8(0xFF0F, (byte)(IF & ~bitMask));
            
            // Jump to vector
            Reg.PC = vector;
            
            // Interrupt entry takes 5 M-cycles
            Ram.Clock.AdvanceT(20);
        }
    }
    
    public void PushPC()
    {
        Ram.Write8(--Reg.SP, (byte)(Reg.PC >> 8));
        Ram.Write8(--Reg.SP, (byte)(Reg.PC & 0xFF));
    }
}