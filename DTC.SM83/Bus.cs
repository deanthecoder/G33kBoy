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
using DTC.SM83.Snapshot;

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
    private readonly InterruptDevice m_interruptDevice;
    private readonly HramDevice m_hramDevice;
    private readonly OamDevice m_oam;
    private readonly JoypadDevice m_joypadDevice;
    private CartridgeRamDevice m_cartridgeRam;
    private int m_hdmaStallMCycles;

    public ushort FromAddr => 0x0000;
    public ushort ToAddr => 0xFFFF;

    public BootRom BootRom { get; }
    public PPU PPU { get; }
    public ApuDevice APU { get; }
    public Dma Dma { get; }
    public Hdma Hdma { get; }

    public VramDevice Vram { get; }

    public WorkRamDevice WorkRam { get; }

    public CartridgeRomDevice CartridgeRom { get; private set; }
    public GameBoyMode Mode { get; private set; } = GameBoyMode.Dmg;
    public bool IsDoubleSpeed { get; private set; }
    public BusType Type { get; }

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
        Type = busType;
        m_devices = ArrayPool<IMemDevice>.Shared.Rent(bytesToAllocate);
        Array.Clear(m_devices);
        m_ram = ArrayPool<byte>.Shared.Rent(bytesToAllocate);
        Array.Clear(m_ram);
        Dma = new Dma(this);

        VramDevice vram = null;
        if (busType == BusType.GameBoy)
        {
            BootRom = new BootRom();

            // V(ideo)RAM (0x8000 - 0x9FFF)
            Vram = new VramDevice();
            vram = Vram;
            Attach(Vram);
            
            // Ram bank 0 (0xC000 - 0xDFFF)
            WorkRam = new WorkRamDevice();
            Attach(WorkRam);
            
            // Echo of WRAM (0xE000 - 0xFDFF)
            Attach(new EchoRamDevice(WorkRam));

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
            Hdma = new Hdma(this);
            PPU = new PPU(m_ioDevice, vram, m_interruptDevice, m_oam!);
            PPU.HBlankStarted += () => Hdma?.OnHBlank();
        }
    }

    public void SetMode(GameBoyMode mode)
    {
        if (Mode == mode)
            return;
        Mode = mode;
        IsDoubleSpeed = false;
        BootRom?.SetMode(mode);
        m_ioDevice?.SetMode(mode);
        Vram?.SetCurrentBank(0);
        WorkRam?.SetCurrentBank(1);
        PPU?.SetMode(mode);
    }

    public void SetDoubleSpeed(bool isDoubleSpeed)
    {
        IsDoubleSpeed = isDoubleSpeed;
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

        m_cartridgeRam = m_memoryBankController.HasRam ? new CartridgeRamDevice(m_memoryBankController) : null;
        if (m_cartridgeRam != null)
            Attach(m_cartridgeRam);

        BootRom?.PrimeCartridgeData(cartridge.RomData);

        // Boot ROM must overlay the cartridge; re-attach to ensure priority.
        if (BootRom != null)
            Attach(BootRom);
    }

    public void SetSoundChannelEnabled(int channel, bool isEnabled) =>
        APU?.SetChannelEnabled(channel, isEnabled);

    public byte Read8(ushort addr)
    {
        if (IsOamBugAddress(addr))
            PPU?.ApplyOamCorruption(OamCorruptionType.Read);

        return GetMemoryAccess(addr) == MemoryAccess.Allow ? UncheckedRead(addr) : (byte)0xFF;
    }

    /// <summary>
    /// Read memory while forcing a specific DMG OAM corruption type, if applicable.
    /// </summary>
    internal byte Read8WithOamCorruption(ushort addr, OamCorruptionType type)
    {
        if (IsOamBugAddress(addr))
            PPU?.ApplyOamCorruption(type);

        return GetMemoryAccess(addr) == MemoryAccess.Allow ? UncheckedRead(addr) : (byte)0xFF;
    }

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

        if (IsOamBugAddress(addr))
            PPU?.ApplyOamCorruption(OamCorruptionType.Write);
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
    private MemoryAccess GetMemoryAccess(ushort addr)
    {
        if (Dma?.IsTransferActive == true)
        {
            var isSafeRegion = m_hramDevice?.Contains(addr) == true;
            if (!isSafeRegion)
                return MemoryAccess.Block;
        }

        var isVram = addr is >= 0x8000 and <= 0x9FFF;
        if (isVram && PPU is { CanAccessVram: false })
            return MemoryAccess.Block;

        // Sprite memory is blocked during certain cycles.
        var isOam = m_oam?.Contains(addr) == true;
        return isOam && PPU is {CanAccessOam: false} ? MemoryAccess.Block : MemoryAccess.Allow;
    }

    /// <summary>
    /// Trigger DMG OAM corruption for an address, without performing a bus read/write.
    /// </summary>
    internal void TriggerOamCorruption(ushort addr, OamCorruptionType type)
    {
        if (!IsOamBugAddress(addr))
            return;
        PPU?.ApplyOamCorruption(type);
    }

    private static bool IsOamBugAddress(ushort addr) =>
        addr is >= 0xFE00 and <= 0xFEFF;

    /// <summary>
    /// Advance the clock by one CPU M-cycle.
    /// </summary>
    public void AdvanceM()
    {
        AdvanceOneM();

        while (m_hdmaStallMCycles > 0)
        {
            m_hdmaStallMCycles--;
            AdvanceOneM();
        }
    }

    /// <summary>
    /// Request extra CPU M-cycle stalls to model CGB HDMA/GDMA bus blocking.
    /// </summary>
    internal void RequestHdmaCpuStall(int mCycles)
    {
        if (mCycles > 0)
            m_hdmaStallMCycles += mCycles;
    }

    private void AdvanceOneM()
    {
        const ulong cpuTicksPerM = 4;
        var masterTicksPerM = IsDoubleSpeed ? 2UL : 4UL;

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

    public void Dispose()
    {
        ArrayPool<IMemDevice>.Shared.Return(m_devices);
        ArrayPool<byte>.Shared.Return(m_ram);
    }

    public int GetStateSize()
    {
        var size =
            sizeof(byte) +        // Mode
            sizeof(byte) +        // IsDoubleSpeed
            sizeof(ulong) * 2 +   // ClockTicks, CpuClockTicks
            sizeof(int) +         // RAM length
            m_ram.Length;

        if (Type != BusType.Trivial)
        {
            size += m_timer.GetStateSize();
            size += m_interruptDevice.GetStateSize();
            size += sizeof(byte); // IE
        }

        size += Dma.GetStateSize();
        size += sizeof(byte); // has MBC

        if (m_memoryBankController is MemoryBankControllerBase mbc)
            size += mbc.GetStateSize();

        if (Type == BusType.GameBoy)
        {
            size += sizeof(byte); // boot ROM attached
            size += BootRom?.GetStateSize() ?? 0;
            size += Vram?.GetStateSize() ?? 0;
            size += WorkRam?.GetStateSize() ?? 0;
            size += m_oam?.GetStateSize() ?? 0;
            size += m_hramDevice?.GetStateSize() ?? 0;
            size += m_ioDevice?.GetStateSize() ?? 0;
            size += m_joypadDevice?.GetStateSize() ?? 0;
            size += Hdma?.GetStateSize() ?? 0;
            size += PPU?.GetStateSize() ?? 0;
        }

        return size;
    }

    public void SaveState(ref StateWriter writer)
    {
        writer.WriteByte((byte)Mode);
        writer.WriteBool(IsDoubleSpeed);
        writer.WriteUInt64(ClockTicks);
        writer.WriteUInt64(CpuClockTicks);
        writer.WriteInt32(m_ram.Length);
        writer.WriteBytes(m_ram);

        if (Type != BusType.Trivial)
        {
            m_timer.SaveState(ref writer);
            m_interruptDevice.SaveState(ref writer);
            writer.WriteByte(UncheckedRead(0xFFFF));
        }

        Dma.SaveState(ref writer);

        var hasMbc = m_memoryBankController is MemoryBankControllerBase;
        writer.WriteBool(hasMbc);
        if (hasMbc)
            ((MemoryBankControllerBase)m_memoryBankController).SaveState(ref writer);

        if (Type == BusType.GameBoy)
        {
            var bootRomAttached = BootRom != null && ReferenceEquals(m_devices[0x0000], BootRom);
            writer.WriteBool(bootRomAttached);
            BootRom?.SaveState(ref writer);
            Vram?.SaveState(ref writer);
            WorkRam?.SaveState(ref writer);
            m_oam?.SaveState(ref writer);
            m_hramDevice?.SaveState(ref writer);
            m_ioDevice?.SaveState(ref writer);
            m_joypadDevice?.SaveState(ref writer);
            Hdma?.SaveState(ref writer);
            PPU?.SaveState(ref writer);
        }
    }

    public void LoadState(ref StateReader reader)
    {
        Mode = (GameBoyMode)reader.ReadByte();
        IsDoubleSpeed = reader.ReadBool();
        ClockTicks = reader.ReadUInt64();
        CpuClockTicks = reader.ReadUInt64();

        var ramLength = reader.ReadInt32();
        if (ramLength != m_ram.Length)
            throw new InvalidOperationException($"State RAM size mismatch. Expected {m_ram.Length}, got {ramLength}.");
        reader.ReadBytes(m_ram);

        if (Type != BusType.Trivial)
        {
            m_timer.LoadState(ref reader);
            m_interruptDevice.LoadState(ref reader);
            UncheckedWrite(0xFFFF, reader.ReadByte());
        }

        Dma.LoadState(ref reader);

        var hasMbc = reader.ReadBool();
        if (hasMbc)
        {
            if (m_memoryBankController is MemoryBankControllerBase mbc)
                mbc.LoadState(ref reader);
            else
                throw new InvalidOperationException("State expects a cartridge controller, but none is loaded.");
        }
        else if (m_memoryBankController != null)
        {
            throw new InvalidOperationException("State does not include a cartridge controller, but one is loaded.");
        }

        if (Type == BusType.GameBoy)
        {
            var bootRomAttached = reader.ReadBool();
            if (BootRom != null)
            {
                if (bootRomAttached)
                    Attach(BootRom);
                else
                    Detach(BootRom, CartridgeRom);
            }

            BootRom?.LoadState(ref reader);
            Vram?.LoadState(ref reader);
            WorkRam?.LoadState(ref reader);
            m_oam?.LoadState(ref reader);
            m_hramDevice?.LoadState(ref reader);
            m_ioDevice?.LoadState(ref reader);
            m_joypadDevice?.LoadState(ref reader);
            Hdma?.LoadState(ref reader);
            PPU?.LoadState(ref reader);
        }
    }
}
