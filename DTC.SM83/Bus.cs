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

using System.Buffers;
using System.Runtime.CompilerServices;
using DTC.SM83.Devices;
using DTC.SM83.HostDevices;
using DTC.SM83.MemoryBankControllers;

namespace DTC.SM83;

/// <summary>
/// Manages memory access across multiple memory devices through a unified address space.
/// </summary>
public sealed class Bus : IMemDevice, IDisposable
{
    private readonly byte[] m_ram;
    private readonly IMemDevice[] m_devices;
    private readonly bool[] m_written;
    private readonly TimerDevice m_timer;
    private readonly IoDevice m_ioDevice;
    private readonly InterruptDevice m_interruptDevice;
    private readonly HramDevice m_hramDevice;
    private readonly OamDevice m_oam;
    private readonly VramDevice m_vram;
    private readonly WorkRamDevice m_wram;
    private readonly Hdma m_hdma;
    private readonly JoypadDevice m_joypadDevice;
    private bool m_isDoubleSpeed;
    private GameBoyMode m_mode = GameBoyMode.Dmg;

    public ushort FromAddr => 0x0000;
    public ushort ToAddr => 0xFFFF;

    public BootRom BootRom { get; }
    public PPU PPU { get; }
    public ApuDevice APU { get; }
    public Dma Dma { get; }
    public Hdma Hdma => m_hdma;
    public VramDevice Vram => m_vram;
    public WorkRamDevice WorkRam => m_wram;
    public CartridgeRamDevice CartridgeRam { get; private set; }
    public CartridgeRomDevice CartridgeRom { get; private set; }
    public GameBoyMode Mode => m_mode;
    public bool IsDoubleSpeed => m_isDoubleSpeed;

    private IMemoryBankController m_memoryBankController;

    /// <summary>
    /// Master clock ticks (PPU dots / DMG T-cycles) elapsed since boot.
    /// </summary>
    public ulong ClockTicks { get; private set; }

    /// <summary>
    /// CPU T-cycles elapsed since boot. (4T = 1M)
    /// </summary>
    public ulong CpuClockTicks { get; private set; }

    /// <summary>
    /// The type of bus to create.
    /// </summary>
    public enum BusType
    {
        /// <summary>
        /// Bare-bones CPU implementation, useful only for disassembly in unit tests.
        /// </summary>
        Trivial,

        /// <summary>
        /// Has interrupt handling capabilities.
        /// </summary>
        Minimal,

        /// <summary>
        /// Full Game Boy implementation with all devices.
        /// </summary>
        GameBoy
    }

    private enum MemoryAccess
    {
        Allow,
        Block
    }

    public Bus(int bytesToAllocate, BusType busType, Joypad joypad = null, SoundDevice audioSink = null)
    {
        m_devices = ArrayPool<IMemDevice>.Shared.Rent(bytesToAllocate);
        Array.Clear(m_devices);
        m_ram = ArrayPool<byte>.Shared.Rent(bytesToAllocate);
        Array.Clear(m_ram);
        m_written = ArrayPool<bool>.Shared.Rent(bytesToAllocate);
        Array.Clear(m_written);
        Dma = new Dma(this);

        VramDevice vram = null;
        if (busType == BusType.GameBoy)
        {
            BootRom = new BootRom();

            // V(ideo)RAM (0x8000 - 0x9FFF)
            m_vram = new VramDevice();
            vram = m_vram;
            Attach(m_vram);
            
            // Ram bank 0 (0xC000 - 0xDFFF)
            m_wram = new WorkRamDevice();
            Attach(m_wram);
            
            // Echo of WRAM (0xE000 - 0xFDFF)
            Attach(new EchoRamDevice(m_wram));

            // OAM(/Sprites) (0xFE00 - 0xFE9F)
            m_oam = new OamDevice();
            Attach(m_oam);
            
            // Unusable/Reserved RAM (0xFEA0 - 0xFEFF)
            Attach(new UnusableRamDevice());

            // IO (0xFF00 - 0xFF7F)
            m_ioDevice = new IoDevice(this, BootRom);
            Attach(m_ioDevice);

            // Joypad (0xFF00)
            m_joypadDevice = new JoypadDevice(joypad);
            Attach(m_joypadDevice);
            
            // APU
            APU = new ApuDevice(audioSink);
            Attach(APU);

            // High RAM (0xFF80 - 0xFFFE)
            m_hramDevice = new HramDevice();
            Attach(m_hramDevice);

            // The GameBoy boot ROM (0x0000 - 0x00FF).
            Attach(BootRom);
        }

        if (busType != BusType.Trivial)
        {
            // The timer (0xFF04-0xFF07), firing interrupts when internal timers elapse.
            // Note: This address range overrides a section of the IO device.
            m_interruptDevice = new InterruptDevice();
            m_timer = new TimerDevice(m_interruptDevice);
            Attach(m_timer);

            // Represents the interrupt mask at 0xFF0F.
            Attach(m_interruptDevice);
            
            // Interrupt Enable device (0xFFFF).
            Attach(new InterruptEnableDevice());
        }

        if (busType == BusType.GameBoy)
        {
            // Pixel Processing Unit
            m_hdma = new Hdma(this);
            PPU = new PPU(m_ioDevice, vram, m_interruptDevice, m_oam!);
            PPU.HBlankStarted += () => m_hdma?.OnHBlank();
        }
    }

    public void SetMode(GameBoyMode mode)
    {
        if (m_mode == mode)
            return;
        m_mode = mode;
        m_isDoubleSpeed = false;
        BootRom?.SetMode(mode);
        m_ioDevice?.SetMode(mode);
        m_vram?.SetMode(mode);
        m_wram?.SetMode(mode);
        PPU?.SetMode(mode);
    }

    public void SetDoubleSpeed(bool isDoubleSpeed)
    {
        m_isDoubleSpeed = isDoubleSpeed;
    }

    public Joypad.JoypadButtons GetJoypadButtons() =>
        m_joypadDevice?.GetPressedButtons() ?? Joypad.JoypadButtons.None;

    public void ResetDivider() =>
        m_timer?.ResetDivider();

    public bool TryHandleSpeedSwitch() =>
        m_ioDevice?.TryHandleSpeedSwitch() == true;

    public void SetInstructionLogger(InstructionLogger instructionLogger)
    {
        if (Dma != null)
            Dma.InstructionLogger = instructionLogger;
        if (PPU != null)
            PPU.InstructionLogger = instructionLogger;
        if (APU != null)
            APU.InstructionLogger = instructionLogger;
    }

    /// <summary>
    /// Attach a device to satisfy requests to a defined memory range.
    /// </summary>
    public void Attach(IMemDevice device) =>
        Array.Fill(m_devices, device, device.FromAddr, device.ToAddr - device.FromAddr + 1);

    /// <summary>
    /// Detach a device, replacing its range with another device (or null).
    /// </summary>
    public void Detach(IMemDevice device, IMemDevice replacement)
    {
        if (device == null)
            return;
        Array.Fill(m_devices, replacement, device.FromAddr, device.ToAddr - device.FromAddr + 1);
    }

    /// <summary>
    /// Install the supplied cartridge and its memory controller.
    /// </summary>
    public void LoadCartridge(Cartridge cartridge)
    {
        m_memoryBankController = MemoryBankControllerFactory.Create(cartridge);

        CartridgeRom = new CartridgeRomDevice(m_memoryBankController);
        Attach(CartridgeRom);

        CartridgeRam = m_memoryBankController.HasRam ? new CartridgeRamDevice(m_memoryBankController) : null;
        if (CartridgeRam != null)
            Attach(CartridgeRam);

        BootRom?.PrimeCartridgeData(cartridge.RomData);

        // Boot ROM must overlay the cartridge; re-attach to ensure priority.
        if (BootRom != null)
            Attach(BootRom);
    }

    public void SetSoundChannelEnabled(int channel, bool isEnabled) =>
        APU?.SetChannelEnabled(channel, isEnabled);

    public byte Read8(ushort addr) =>
        GetMemoryAccess(addr) == MemoryAccess.Allow ? UncheckedRead(addr) : (byte)0xFF;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte UncheckedRead(ushort addr) =>
        m_devices[addr]?.Read8(addr) ?? m_ram[addr];

    public void Write8(ushort addr, byte value)
    {
        if (GetMemoryAccess(addr) == MemoryAccess.Allow)
        {
            UncheckedWrite(addr, value);
            return;
        }

        // DMG OAM bug approximation: writes during modes 2/3 corrupt a pair of bytes.
        if (PPU is { CanAccessOam: false } && m_oam?.Contains(addr) == true && Dma?.IsTransferActive != true)
        {
            var baseAddr = (ushort)(addr & 0xFFFE);
            if (m_oam.Contains(baseAddr))
            {
                MarkWritten(baseAddr);
                m_oam.Write8(baseAddr, value);

                var neighbor = (ushort)(baseAddr + 1);
                if (m_oam.Contains(neighbor))
                {
                    MarkWritten(neighbor);
                    m_oam.Write8(neighbor, value);
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UncheckedWrite(ushort addr, byte value)
    {
        MarkWritten(addr);

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
    private MemoryAccess GetMemoryAccess(ushort addr)
    {
        if (Dma?.IsTransferActive == true)
        {
            var isSafeRegion = m_hramDevice?.Contains(addr) == true;
            if (!isSafeRegion)
                return MemoryAccess.Block;
        }

        // Sprite memory is blocked during certain cycles.
        var isOam = m_oam?.Contains(addr) == true;
        return isOam && PPU is {CanAccessOam: false} ? MemoryAccess.Block : MemoryAccess.Allow;
    }

    /// <summary>
    /// Advance the clock by one CPU M-cycle.
    /// </summary>
    public void AdvanceM()
    {
        const ulong cpuTicksPerM = 4;
        var masterTicksPerM = m_isDoubleSpeed ? 2UL : 4UL;

        ClockTicks += masterTicksPerM;
        CpuClockTicks += cpuTicksPerM;
        Dma?.AdvanceT(cpuTicksPerM);
        m_timer?.AdvanceT(masterTicksPerM);
        APU?.AdvanceT(masterTicksPerM);
        PPU?.AdvanceT(masterTicksPerM);
    }

    public void ResetClock()
    {
        ClockTicks = 0;
        CpuClockTicks = 0;
    }

    public bool IsUninitializedWorkRam(ushort addr) =>
        addr is >= 0xC000 and <= 0xFDFF && !m_written[addr];
    public static bool IsOamOrUnusable(ushort addr) =>
        addr is >= 0xFE00 and <= 0xFEFF;
    public static bool IsIo(ushort addr) =>
        addr is >= 0xFF00 and <= 0xFF7F;

    public void Dispose()
    {
        ArrayPool<IMemDevice>.Shared.Return(m_devices);
        ArrayPool<byte>.Shared.Return(m_ram);
        ArrayPool<bool>.Shared.Return(m_written);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void MarkWritten(ushort addr)
    {
        m_written[addr] = true;

        // Keep WRAM and its echo in sync for write-tracking.
        if (addr is >= 0xC000 and <= 0xDFFF)
            m_written[addr + 0x2000] = true;
        else if (addr is >= 0xE000 and <= 0xFDFF)
            m_written[addr - 0x2000] = true;
    }
}
