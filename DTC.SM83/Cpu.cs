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
using System.Runtime.CompilerServices;
using DTC.Core;
using DTC.SM83.Debuggers;
using DTC.SM83.Instructions;

namespace DTC.SM83;

public class Cpu
{
    public const double Hz = 4194304.0;
    
    private string m_instructionState;
    private byte m_fetchedOpcode;
    private readonly List<ICpuDebugger> m_debuggers = new();
    private ushort m_currentInstructionAddress;

#if DEBUG
    private int m_nopStreak;
    private const int NopStreakThreshold = 64;
#endif

    public InstructionLogger InstructionLogger { get; } = new();

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
    public byte IF => (byte)(Bus.UncheckedRead(0xFF0F) & 0x1F);

    /// <summary>
    /// The address of the instruction currently being executed.
    /// </summary>
    public ushort CurrentInstructionAddress => m_currentInstructionAddress;

    public Cpu(Bus bus)
    {
        Bus = bus ?? throw new ArgumentNullException(nameof(bus));
        Bus.SetInstructionLogger(InstructionLogger);

        // Pre-load first opcode.
        Fetch8();
    }

    /// <summary>
    /// Registers a debugger that will be notified of CPU activity.
    /// </summary>
    /// <example>
    /// cpu.AddDebugger(new PcBreakpointDebugger(0x0150, breakIntoIde: true));
    /// cpu.AddDebugger(new MemoryWriteDebugger(0xC123, 0x42));
    /// cpu.AddDebugger(new IncrementingCounterDebugger(0x00, 0x05, breakWhenResolved: true));
    /// </example>
    public void AddDebugger(ICpuDebugger debugger)
    {
        if (debugger == null)
            throw new ArgumentNullException(nameof(debugger));

        m_debuggers.Add(debugger);
    }

    public void Step()
    {
        m_currentInstructionAddress = (ushort)(Reg.PC - 1);
        var isDebugMode = InstructionLogger.IsEnabled;
#if DEBUG
        if (!IsHalted)
        {
            if (m_fetchedOpcode == 0x00)
            {
                m_nopStreak++;
                if (m_nopStreak == NopStreakThreshold)
                {
                    Logger.Instance.Warn($"[WATCHDOG] {NopStreakThreshold} consecutive NOPs executed. PC={Reg.PC:X4} IE={IE:X2} IF={IF:X2} IME={IME}.");
                    InstructionLogger?.DumpToConsole();
                }
            }
            else
            {
                m_nopStreak = 0;
            }
        }
#endif
        if (!IsHalted)
        {
            NotifyBeforeInstruction(m_currentInstructionAddress, m_fetchedOpcode);

            try
            {
#if DEBUG
                // Runtime checks to help identify potential bugs.
                if (Bus.IsUninitializedWorkRam(Reg.PC))
                    Logger.Instance.Warn($"Executing from uninitialized WRAM at {Reg.PC:X4} - This is probably a CPU/interrupt/RET bug.");
                else if (Bus.IsOamOrUnusable(Reg.PC))
                    Logger.Instance.Warn($"Executing from OAM/unusable region at {Reg.PC:X4} - This is not typical.");
                else if (Bus.IsIo(Reg.PC))
                    Logger.Instance.Warn($"Executing from IO region at {Reg.PC:X4} - This is not typical.");
#endif

                // Decode instruction.
                Instruction instruction;
                if (m_fetchedOpcode == 0xCB)
                {
                    var opcode = Fetch8();
                    instruction = PrefixedInstructions.Table[opcode];
                    if (instruction == null)
                        throw new InvalidOperationException($"Opcode CB {opcode:X2} has null instruction.");
                }
                else
                {
                    instruction = Instructions.Instructions.Table[m_fetchedOpcode];
                    if (instruction == null)
                        throw new InvalidOperationException($"Opcode {m_fetchedOpcode:X2} has null instruction.");
                }

                if (isDebugMode && m_instructionState != null)
                    InstructionLogger?.Write(() => m_instructionState.Replace("xxx", $"{instruction,-12}"));

                // Execute instruction.
                instruction.Execute(this);
            }
            catch (Exception ex)
            {
                InstructionLogger?.Write(() => $"Exception: {ex.Message} (Halting...)");
                InstructionLogger?.DumpToConsole();
                IsHalted = true;
                throw;
            }
        }

        // Check/wake/service interrupts
        HandleInterrupts();

        // Enable interrupts if necessary.
        if (PendingIME)
        {
            IME = true;
            PendingIME = false;
        }

        // Fetch opcode.
        if (IsHalted)
        {
            Bus.AdvanceM(); // Re-queue the HALT instruction.
            NotifyAfterStep();
            return;
        }

        if (isDebugMode)
            m_instructionState = $"xxx  {Bus.Read8(Reg.PC):X2} {Bus.Read8((ushort)(Reg.PC + 1)):X2} {Bus.Read8((ushort)(Reg.PC + 2)):X2}│{Reg,-32}│{Reg.FlagsAsString()}";
        Fetch8();
        NotifyAfterStep();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void InternalWaitM() =>
        Bus.AdvanceM();

    /// <summary>
    /// Check and service pending interrupts if enabled.
    /// </summary>
    private void HandleInterrupts()
    {
        var ie = IE;
        var iff = IF;

        // Wake from HALT if any interrupt is requested, even if masked.
        if (IsHalted && iff != 0)
            IsHalted = false;

        var pending = (byte)(ie & iff & 0x1F);
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
            InstructionLogger?.Write(() => "Service VBlank interrupt");
            Service(0x0040, 0x01);
            return;
        }
        
        if ((pending & 0x02) != 0)
        {
            // LCD STAT
            InstructionLogger?.Write(() => "Service LCD STAT interrupt");
            Service(0x0048, 0x02);
            return;
        }
        
        if ((pending & 0x04) != 0)
        {
            // Timer
            InstructionLogger?.Write(() => "Service Timer interrupt");
            Service(0x0050, 0x04);
            return;
        }
        
        if ((pending & 0x08) != 0)
        {
            // Serial
            InstructionLogger?.Write(() => "Service Serial interrupt");
            Service(0x0058, 0x08);
            return;
        }
        
        if ((pending & 0x10) != 0)
        {
            // Joypad
            InstructionLogger?.Write(() => "Service Joypad interrupt");
            Service(0x0060, 0x10);
        }
        
        return;

        // Interrupt priority: VBlank, STAT, Timer, Serial, Joypad
        void Service(ushort vector, byte bitMask)
        {
            PushPC();  // Cost: 8T (2M)

            // Clear IF bit
            Write8(0xFF0F, (byte)(IF & ~bitMask)); // Cost: 1T (1M)
            
            // Jump to vector
            Reg.PC = vector;
            
            // Advance the remaining 8T (2M) to ensure the interrupt takes 5M total.
            Bus.AdvanceM(); 
            Bus.AdvanceM(); 
        }
    }
    
    public void PushPC()
    {
        Write8((ushort)(Reg.SP - 1), (byte)(Reg.PC >> 8));
        Write8((ushort)(Reg.SP - 2), (byte)(Reg.PC & 0xFF));
        Reg.SP -= 2;
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
        Bus.AdvanceM();
        NotifyMemoryRead(addr, value);
        return value;
    }
    
    /// <summary>
    /// Write memory at address and advance the clock 4 ticks.
    /// </summary>
    public void Write8(ushort addr, byte value)
    {
        Bus.Write8(addr, value);
        Bus.AdvanceM();
        NotifyMemoryWrite(addr, value);
    }
    
    /// <summary>
    /// Write 16-bit word at address and advance the clock 4 + 4 ticks.
    /// </summary>
    public void Write16(ushort addr, ushort value)
    {
        Write8(addr, (byte)(value & 0xFF));
        Write8((ushort)(addr + 1), (byte)(value >> 8));   
    }

    [Conditional("DEBUG")]
    private void NotifyBeforeInstruction(ushort opcodeAddress, byte opcode)
    {
        if (m_debuggers.Count == 0)
            return;

        foreach (var debugger in m_debuggers)
            debugger.BeforeInstruction(this, opcodeAddress, opcode);
    }

    [Conditional("DEBUG")]
    private void NotifyAfterStep()
    {
        if (m_debuggers.Count == 0)
            return;

        foreach (var debugger in m_debuggers)
            debugger.AfterStep(this);
    }

    [Conditional("DEBUG")]
    private void NotifyMemoryRead(ushort address, byte value)
    {
        if (m_debuggers.Count == 0)
            return;

        foreach (var debugger in m_debuggers)
            debugger.OnMemoryRead(this, address, value);
    }

    [Conditional("DEBUG")]
    private void NotifyMemoryWrite(ushort address, byte value)
    {
        if (m_debuggers.Count == 0)
            return;

        foreach (var debugger in m_debuggers)
            debugger.OnMemoryWrite(this, address, value);
    }
}
