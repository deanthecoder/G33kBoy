// Code authored by Dean Edis (DeanTheCoder).
// Anyone is free to copy, modify, use, compile, or distribute this software,
// either in source code form or as a compiled binary, for any non-commercial
// purpose.
//
// If you modify the code, please retain this copyright header,
// and consider contributing back to the repository or letting us know
// about your modifications. Your contributions are valued!
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.

using System.Diagnostics.CodeAnalysis;

namespace DTC.SM83;

/// <summary>
/// The GameBoy timer, handling access to memory 0xFF04-0xFF07
/// </summary>
public class TimerDevice : IMemDevice
{
    private readonly InterruptDevice m_interruptDevice;

    /// <summary>
    /// Latched TAC low 3 bits (enable + clock select).
    /// </summary>
    /// <remarks>
    /// Mask to 0x07 on write; readback exposes 0xF8 | m_tac via FF07.
    /// </remarks>
    private byte m_tac;

    /// <summary>
    /// Accumulated T-cycles since the last DIV increment (increments every 256 T).
    /// </summary>
    private ulong m_divCycleCount;

    /// <summary>
    /// Accumulated T-cycles toward the next TIMA tick (when enabled).
    /// </summary>
    private ulong m_timaCycleCount;

    /// <summary>
    /// Current TIMA period in T-cycles (00=1024, 01=16, 10=64, 11=256).
    /// </summary>
    private ulong m_timaPeriod = 1024;

    /// <summary>
    /// True when TIMA overflow occurred and a delayed reload/IRQ is pending.
    /// </summary>
    private bool m_pendingReload;

    /// <summary>
    /// Reload/IRQ delay in T-cycles; 4 T-cycles equals one M-cycle.
    /// </summary>
    private long m_reloadDelayT;

    /// <summary>
    /// Inclusive start address of this device's MMIO range (FF04).
    /// </summary>
    public ushort FromAddr => 0xFF04;

    /// <summary>
    /// Inclusive end address of this device's MMIO range (FF07).
    /// </summary>
    public ushort ToAddr => 0xFF07;

    /// <summary>
    /// DIV - Divider Register (FF04).
    /// </summary>
    /// <remarks>
    /// Increments at 16384 Hz; any write resets to 0.
    /// </remarks>
    private byte DIV { get; set; }

    /// <summary>
    /// TIMA - Timer Counter (FF05).
    /// </summary>
    /// <remarks>
    /// Increments per TAC rate and reloads from TMA on overflow.
    /// </remarks>
    private byte TIMA { get; set; }

    /// <summary>
    /// TMA - Timer Modulo (FF06).
    /// </summary>
    /// <remarks>
    /// Value loaded into TIMA after overflow.
    /// </remarks>
    private byte TMA { get; set; }

    /// <summary>
    /// TAC - Timer Control (FF07).
    /// </summary>
    /// <remarks>
    /// Bit 2 enables timer; bits 1–0 select input clock. Periods: 00→1024T, 01→16T, 10→64T, 11→256T. Upper bits read as 1.
    /// </remarks>
    private byte TAC
    {
        get => m_tac;
        set
        {
            var maskedValue = (byte)(value & 0x07);
            if (m_tac == maskedValue)
                return;
            m_tac = maskedValue;
            m_timaPeriod = (m_tac & 0x03) switch
            {
                0x00 => 1024, // 4096 Hz
                0x01 => 16,   // 262144 Hz
                0x10 => 64,   // 65536 Hz
                0x11 => 256,  // 16384 Hz
                _ => 1024
            };
        }
    }

    /// <summary>
    /// Creates a Game Boy timer device bound to the shared interrupt flags.
    /// </summary>
    public TimerDevice([NotNull] InterruptDevice interruptDevice)
    {
        m_interruptDevice = interruptDevice ?? throw new ArgumentNullException(nameof(interruptDevice));
    }

    /// <summary>
    /// Reads a timer register (FF04–FF07). Returns 0xFF for unmapped addresses.
    /// </summary>
    public byte Read8(ushort addr) =>
        addr switch
        {
            0xFF04 => DIV,
            0xFF05 => TIMA,
            0xFF06 => TMA,
            0xFF07 => (byte)(0xF8 | m_tac), // Top bits set as 0x11111...
            _ => 0xFF
        };

    /// <summary>
    /// Writes a timer register; writing FF04 resets DIV and the internal divider.
    /// </summary>
    public void Write8(ushort addr, byte value)
    {
        switch (addr)
        {
            // DIV
            case 0xFF04:
                DIV = 0;
                m_divCycleCount = 0;
                break;
            
            // TIMA
            case 0xFF05:
                TIMA = value;
                break;
            
            // TMA
            case 0xFF06:
                TMA = value;
                break;
            
            // TAC
            case 0xFF07:
                TAC = (byte) (value | 0xF8);
                break;
        }
    }

    /// <summary>
    /// Advances the timer by the specified T-cycles, ticking DIV/TIMA and scheduling delayed reload/IRQ.
    /// </summary>
    /// <remarks>
    /// Call with every elapsed T-cycle chunk to keep timing correct.
    /// </remarks>
    public void AdvanceT(ulong tCycles)
    {
        // DIV increments @ 16384 Hz (every 256 T-cycles)
        m_divCycleCount += tCycles;
        while (m_divCycleCount >= 256)
        {
            m_divCycleCount -= 256;
            DIV++;
        }
        
        // If enabled, TIMA increments based on TAC.
        if ((TAC & 0x04) != 0)
        {
            m_timaCycleCount += tCycles;
            while (m_timaCycleCount >= m_timaPeriod)
            {
                m_timaCycleCount -= m_timaPeriod;
                if (TIMA == 0xFF)
                {
                    // Overflow: Delay raising the timer interrupt by 1 M-cycle.
                    m_pendingReload = true;
                    m_reloadDelayT = 4;
                }
                else
                {
                    TIMA++;
                }
            }
        }
        
        if (m_pendingReload)
        {
            m_reloadDelayT -= (long)tCycles;
            if (m_reloadDelayT <= 0)
            {
                m_pendingReload = false;
                TIMA = TMA;
                
                // IF |= 0x04 (Timer): raise Timer interrupt after the 1 M-cycle reload delay.
                m_interruptDevice.Write8((byte)(m_interruptDevice.Read8() | 0x04));
            }
        }
    }
}