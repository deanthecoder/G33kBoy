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

    /// <summary>
    /// Reference to the system bus for memory and IO operations.
    /// </summary>
    public Bus Bus { get; }
    public Registers Reg { get; } = new Registers();
    
    public bool IsHalted { get; set; }
    
    /// <summary>
    /// Allows implementation of the delayed HALT bug behavior.
    /// </summary>
    /// <remarks>
    /// When true, PC does not advance on next opcode fetch.
    /// </remarks>
    public bool HaltBug { get; set; }
    
    /// <summary>
    /// Global interrupt enable flag.
    /// </summary>
    public bool IME { get; set; }
    
    /// <summary>
    /// Indicates if interrupt enable is pending (delayed effect).
    /// </summary>
    /// <remarks>
    /// Used to implement EI instruction delay.
    /// </remarks>
    public bool PendingIME { get; set; }

    /// <summary>
    /// Interrupt enable register value.
    /// </summary>
    /// <remarks>
    /// Read from the interrupt device via the bus.
    /// </remarks>
    public byte IE => Bus.UncheckedRead(0xFFFF);

    /// <summary>
    /// Interrupt flag register value.
    /// </summary>
    /// <remarks>
    /// Indicates which interrupts are requested.
    /// </remarks>
    public byte IF => Bus.UncheckedRead(0xFF0F);

    public Cpu(Bus bus)
    {
        Bus = bus ?? throw new ArgumentNullException(nameof(bus));

        // Pre-load first opcode.
        Fetch8();
    }

    public void Step()
    {
        // Decode instruction.
        var instruction = m_fetchedOpcode == 0xCB ? PrefixedInstructions.Table[Fetch8()] : Instructions.Table[m_fetchedOpcode];
        if (instruction == null)
            throw new InvalidOperationException($"Opcode {m_fetchedOpcode:X2} has null instruction.");
        
        // Execute instruction.
        instruction.Execute(this);

        // Enable interrupts if necessary.
        if (PendingIME)
        {
            IME = true;
            PendingIME = false;
        }

        // Check/wake/service interrupts
        HandleInterrupts();

        // Fetch opcode.
        if (IsHalted)
            Bus.AdvanceT(4); // Re-queue the HALT instruction. 
        else
            Fetch8();
    }
    
    public void InternalWaitM(ulong m = 1) =>
        Bus.AdvanceT(4 * m);

    /// <summary>
    /// Check and service pending interrupts if enabled.
    /// </summary>
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
        }
        
        return;

        // Interrupt priority: VBlank, STAT, Timer, Serial, Joypad
        void Service(ushort vector, byte bitMask)
        {
            PushPC();

            // Clear IF bit
            Write8(0xFF0F, (byte)(IF & ~bitMask));
            
            // Jump to vector
            Reg.PC = vector;
            
            // Interrupt entry takes 5 M-cycles
            Bus.AdvanceT(20);
        }
    }
    
    public void PushPC()
    {
        Write8(--Reg.SP, (byte)(Reg.PC >> 8));
        Write8(--Reg.SP, (byte)(Reg.PC & 0xFF));
    }

    /// <summary>
    /// Fetch byte at PC, and advance PC.
    /// </summary>
    public byte Fetch8()
    {
        var imm = Read8(Reg.PC);

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

    /// <summary>
    /// Read memory at address and advance the clock 4 ticks.
    /// </summary>
    public byte Read8(ushort addr)
    {
        var value = Bus.Read8(addr);
        Bus.AdvanceT(4);
        return value;
    }
    
    /// <summary>
    /// Read a 16-bit word from memory and advance clock by 8 ticks.
    /// </summary>
    /// <param name="addr">Memory address to read.</param>
    /// <returns>Word value read.</returns>
    public ushort Read16(ushort addr) =>
        (ushort)(Read8(addr) | (Read8((ushort)(addr + 1)) << 8));
    
    /// <summary>
    /// Write memory at address and advance the clock 4 ticks.
    /// </summary>
    public void Write8(ushort addr, byte value)
    {
        Bus.Write8(addr, value);
        Bus.AdvanceT(4);
    }
    
    /// <summary>
    /// Write 16-bit word at address and advance the clock 4 + 4 ticks.
    /// </summary>
    public void Write16(ushort addr, ushort value)
    {
        Write8(addr, (byte)(value & 0xFF));
        Write8((ushort)(addr + 1), (byte)(value >> 8));   
    }
}
