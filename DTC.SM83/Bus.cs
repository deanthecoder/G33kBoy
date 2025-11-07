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

namespace DTC.SM83;

/// <summary>
/// Manages memory access across multiple memory devices through a unified address space.
/// </summary>
public sealed class Bus : IMemDevice, IDisposable
{
    private readonly byte[] m_ram;
    private readonly IMemDevice[] m_devices;
    private readonly TimerDevice m_timer;

    public ushort FromAddr => 0x0000;
    public ushort ToAddr => 0xFFFF;

    public InterruptDevice InterruptDevice { get; }

    /// <summary>
    /// The number of T cycles elapsed since boot. (4T = 1M)
    /// </summary>
    public ulong ClockTicks { get; private set; }

    public Bus(int bytesToAllocate, bool attachDevices = true)
    {
        m_devices = ArrayPool<IMemDevice>.Shared.Rent(bytesToAllocate);
        Array.Clear(m_devices);
        m_ram = ArrayPool<byte>.Shared.Rent(bytesToAllocate);
        Array.Clear(m_ram);

        if (!attachDevices)
            return;

        // Represents the interrupt mask at 0xFF0F.
        InterruptDevice = new InterruptDevice();
        Attach(InterruptDevice);
        
        // The GameBoy timer, firing interrupts when internal timers elapse.
        m_timer = new TimerDevice(InterruptDevice);
        Attach(m_timer);
    }

    /// <summary>
    /// Attach a device to satisfy requests to a defined memort range.
    /// </summary>
    /// <param name="device"></param>
    public void Attach(IMemDevice device) =>
        Array.Fill(m_devices, device, device.FromAddr, device.ToAddr - device.FromAddr + 1);    
    
    public byte Read8(ushort addr) =>
        m_devices[addr]?.Read8(addr) ?? m_ram[addr];
    
    public void Write8(ushort addr, byte value)
    {
        if (m_devices[addr] == null)
            m_ram[addr] = value;
        else
            m_devices[addr].Write8(addr, value);
    }

    /// <summary>
    /// Advance the clock by T cycles (4T = 1M).
    /// </summary>
    public void AdvanceT(ulong tCycles)
    {
        ClockTicks += tCycles;
        m_timer?.AdvanceT(tCycles);
    }

    public void ResetClock() =>
        ClockTicks = 0;

    public void Dispose()
    {
        ArrayPool<IMemDevice>.Shared.Return(m_devices);
        ArrayPool<byte>.Shared.Return(m_ram);
    }
}