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

using System.Buffers;
using System.Runtime.CompilerServices;
using DTC.SM83.Devices;

namespace DTC.SM83;

/// <summary>
/// Manages memory access across multiple memory devices through a unified address space.
/// </summary>
public sealed class Bus : IMemDevice, IDisposable
{
    private readonly byte[] m_ram;
    private readonly IMemDevice[] m_devices;
    private readonly TimerDevice m_timer;
    private readonly IoDevice m_ioDevice;

    public ushort FromAddr => 0x0000;
    public ushort ToAddr => 0xFFFF;

    public BootRom BootRom { get; }
    public PPU PPU { get; }
    public InterruptDevice InterruptDevice { get; }

    /// <summary>
    /// The number of T cycles elapsed since boot. (4T = 1M)
    /// </summary>
    public ulong ClockTicks { get; private set; }

    /// <summary>
    /// The type of bus to create.
    /// </summary>
    public enum BusType
    {
        /// <summary>
        /// Bare bones CPU implementation, useful only for disassembly in unit tests.
        /// </summary>
        Trivial,

        /// <summary>
        /// Has interrupt handling capabilities.
        /// </summary>
        Minimal,

        /// <summary>
        /// Full GameBoy implementation with all devices.
        /// </summary>
        GameBoy
    }

    public Bus(int bytesToAllocate, BusType busType)
    {
        m_devices = ArrayPool<IMemDevice>.Shared.Rent(bytesToAllocate);
        Array.Clear(m_devices);
        m_ram = ArrayPool<byte>.Shared.Rent(bytesToAllocate);
        Array.Clear(m_ram);

        VramDevice vram = null;
        OamDevice oam = null;
        if (busType == BusType.GameBoy)
        {
            // The GameBoy boot ROM.
            BootRom = new BootRom();
            Attach(BootRom);
            
            // VRAM (0x8000 - 0x9FFF)
            vram = new VramDevice();
            Attach(vram);

            // OAM(/Sprites) (0xFE00 - 0xFE9F)
            oam = new OamDevice();
            Attach(oam);

            // IO (0xFF00 - 0xFF7F)
            m_ioDevice = new IoDevice(this, BootRom);
            Attach(m_ioDevice);
        }

        if (busType != BusType.Trivial)
        {
            // The timer (0xFF04-0xFF07), firing interrupts when internal timers elapse.
            // Note: This address range overrides a section of the IO device.
            InterruptDevice = new InterruptDevice();
            m_timer = new TimerDevice(InterruptDevice);
            Attach(m_timer);
        }

        if (busType != BusType.Trivial)
        {
            // Represents the interrupt mask at 0xFF0F.
            Attach(InterruptDevice);
        }

        if (busType == BusType.GameBoy)
        {
            // Pixel Processing Unit
            PPU = new PPU(m_ioDevice, vram, InterruptDevice, oam!);
        }
    }

    /// <summary>
    /// Attach a device to satisfy requests to a defined memory range.
    /// </summary>
    public void Attach(IMemDevice device) =>
        Array.Fill(m_devices, device, device.FromAddr, device.ToAddr - device.FromAddr + 1);

    public byte Read8(ushort addr) =>
        !BlockReadWrite(addr) ? UncheckedRead(addr) : (byte)0xFF;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte UncheckedRead(ushort addr) =>
        m_devices[addr]?.Read8(addr) ?? m_ram[addr];

    public void Write8(ushort addr, byte value)
    {
        if (!BlockReadWrite(addr))
            UncheckedWrite(addr, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UncheckedWrite(ushort addr, byte value)
    {
        if (m_devices[addr] == null)
            m_ram[addr] = value;
        else
            m_devices[addr].Write8(addr, value);
    }

    /// <summary>
    /// Read/writes to most addresses are blocked while a DMA transfer is active.
    /// </summary>
    /// <remarks>
    /// Access to HRAM is always allowed.
    /// </remarks>
    private bool BlockReadWrite(ushort addr)
    {
        if (m_ioDevice?.IsDMATransferActive != true)
            return false;
        var isBlockedRegion = addr < 0xFF00; // 0xFF00: IO, HRAM, IE regions.
        return isBlockedRegion;
    }

    /// <summary>
    /// Advance the clock by T cycles (4T = 1M).
    /// </summary>
    public void AdvanceT(ulong tCycles)
    {
        ClockTicks += tCycles;
        
        // Update the devices.
        m_timer?.AdvanceT(tCycles);
        PPU?.AdvanceT(tCycles);
    }

    public void ResetClock() =>
        ClockTicks = 0;

    public void Dispose()
    {
        ArrayPool<IMemDevice>.Shared.Return(m_devices);
        ArrayPool<byte>.Shared.Return(m_ram);
    }
}