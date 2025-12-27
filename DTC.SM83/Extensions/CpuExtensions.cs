// Code authored by Dean Edis (DeanTheCoder).
// Anyone is free to copy, modify, use, compile, or distribute this software,
// either in source code form or as a compiled binary, for any purpose.
// 
// If you modify the code, please retain this copyright header,
// and consider contributing back to the repository or letting us know
// about your modifications. Your contributions are valued!
// 
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.

namespace DTC.SM83.Extensions;

public static class CpuExtensions
{
    /// <summary>
    /// Standard register values after the DMG boot ROM has completed.
    /// </summary>
    private static readonly Registers BootStateDmg = new Registers
    {
        A = 0x01,     // 0x01 => DMG
        BC = 0x0013,  // C = 0x13 => DMG
        DE = 0x00D8,
        HL = 0x014D,
        SP = 0xFFFE,
        PC = 0x0100,
        Zf = true,
        Nf = false,
        Hf = true,
        Cf = true
    };

    /// <summary>
    /// Standard register values after the CGB boot ROM has completed.
    /// </summary>
    private static readonly Registers BootStateCgb = new Registers
    {
        A = 0x11,
        BC = 0x0000,
        DE = 0xFF56,
        HL = 0x000D,
        SP = 0xFFFE,
        PC = 0x0100,
        Zf = true,
        Nf = false,
        Hf = false,
        Cf = false
    };

    /// <summary>
    /// Hardware register defaults that the boot ROM programs before handing control to the cartridge (DMG/MGB column).
    /// </summary>
    private static readonly (ushort Address, byte Value)[] BootIoDefaultsDmg =
    [
        (0xFF00, 0xCF), // P1
        (0xFF01, 0x00), // SB
        (0xFF02, 0x7E), // SC
        (0xFF04, 0xAB), // DIV
        (0xFF05, 0x00), // TIMA
        (0xFF06, 0x00), // TMA
        (0xFF07, 0xF8), // TAC
        (0xFF0F, 0xE1), // IF
        (0xFF10, 0x80), // NR10
        (0xFF11, 0xBF), // NR11
        (0xFF12, 0xF3), // NR12
        (0xFF13, 0xFF), // NR13
        (0xFF14, 0xBF), // NR14
        (0xFF16, 0x3F), // NR21
        (0xFF17, 0x00), // NR22
        (0xFF18, 0xFF), // NR23
        (0xFF19, 0xBF), // NR24
        (0xFF1A, 0x7F), // NR30
        (0xFF1B, 0xFF), // NR31
        (0xFF1C, 0x9F), // NR32
        (0xFF1D, 0xFF), // NR33
        (0xFF1E, 0xBF), // NR34
        (0xFF20, 0xFF), // NR41
        (0xFF21, 0x00), // NR42
        (0xFF22, 0x00), // NR43
        (0xFF23, 0xBF), // NR44
        (0xFF24, 0x77), // NR50
        (0xFF25, 0xF3), // NR51
        (0xFF26, 0xF1), // NR52
        (0xFF40, 0x91), // LCDC
        (0xFF41, 0x85), // STAT
        (0xFF42, 0x00), // SCY
        (0xFF43, 0x00), // SCX
        (0xFF44, 0x00), // LY
        (0xFF45, 0x00), // LYC
        (0xFF46, 0xFF), // DMA
        (0xFF47, 0xFC), // BGP
        (0xFF48, 0xFF), // OBP0
        (0xFF49, 0xFF), // OBP1
        (0xFF4A, 0x00), // WY
        (0xFF4B, 0x00), // WX
        (0xFF50, 0x01), // BOOT (disable boot ROM)
        (0xFFFF, 0x00)  // IE
    ];

    /// <summary>
    /// Hardware register defaults that the boot ROM programs before handing control to the cartridge (CGB column).
    /// </summary>
    private static readonly (ushort Address, byte Value)[] BootIoDefaultsCgb =
    [
        (0xFF00, 0xCF), // P1
        (0xFF01, 0x00), // SB
        (0xFF02, 0x7F), // SC
        (0xFF04, 0x00), // DIV (value ignored; write resets divider)
        (0xFF05, 0x00), // TIMA
        (0xFF06, 0x00), // TMA
        (0xFF07, 0xF8), // TAC
        (0xFF0F, 0xE1), // IF
        (0xFF10, 0x80), // NR10
        (0xFF11, 0xBF), // NR11
        (0xFF12, 0xF3), // NR12
        (0xFF13, 0xFF), // NR13
        (0xFF14, 0xBF), // NR14
        (0xFF16, 0x3F), // NR21
        (0xFF17, 0x00), // NR22
        (0xFF18, 0xFF), // NR23
        (0xFF19, 0xBF), // NR24
        (0xFF1A, 0x7F), // NR30
        (0xFF1B, 0xFF), // NR31
        (0xFF1C, 0x9F), // NR32
        (0xFF1D, 0xFF), // NR33
        (0xFF1E, 0xBF), // NR34
        (0xFF20, 0xFF), // NR41
        (0xFF21, 0x00), // NR42
        (0xFF22, 0x00), // NR43
        (0xFF23, 0xBF), // NR44
        (0xFF24, 0x77), // NR50
        (0xFF25, 0xF3), // NR51
        (0xFF26, 0xF1), // NR52
        (0xFF40, 0x91), // LCDC
        (0xFF41, 0x85), // STAT
        (0xFF42, 0x00), // SCY
        (0xFF43, 0x00), // SCX
        (0xFF44, 0x00), // LY
        (0xFF45, 0x00), // LYC
        (0xFF47, 0xFC), // BGP
        (0xFF48, 0xFF), // OBP0
        (0xFF49, 0xFF), // OBP1
        (0xFF4A, 0x00), // WY
        (0xFF4B, 0x00), // WX
        (0xFF4D, 0x7E), // KEY1
        (0xFF4F, 0xFE), // VBK
        (0xFF50, 0x01), // BOOT (disable boot ROM)
        (0xFF56, 0x3E), // RP
        (0xFF70, 0xF8), // SVBK
        (0xFFFF, 0x00)  // IE
    ];

    /// <summary>
    /// Mimic the state the CPU and IO registers are left in once the boot ROM hands off to the cartridge.
    /// </summary>
    public static Cpu SkipBootRom(this Cpu cpu, bool disableDevices = false)
    {
        if (cpu == null)
            throw new ArgumentNullException(nameof(cpu));

        if (cpu.Bus.BootRom != null)
        {
            cpu.Bus.Detach(cpu.Bus.BootRom, cpu.Bus.CartridgeRom);
            cpu.Bus.BootRom.Unload();
        }
        var isCgb = cpu.Bus.Mode == GameBoyMode.Cgb;
        (isCgb ? BootStateCgb : BootStateDmg).CopyTo(cpu.Reg);
        cpu.IME = true;
        cpu.PendingIME = false;
        cpu.IsHalted = false;

        if (disableDevices)
        {
            cpu.Bus.Dma.IsEnabled = false;
            cpu.Bus.APU.SuppressTriggers = true;
        }
        var defaults = isCgb ? BootIoDefaultsCgb : BootIoDefaultsDmg;
        foreach (var (address, value) in defaults)
            cpu.Bus.Write8(address, value);
        cpu.Bus.Dma.IsEnabled = false;
        if (cpu.Bus.APU != null)
            cpu.Bus.APU.SuppressTriggers = false;

        cpu.Bus.ResetClock();
        cpu.Fetch8(); // Prime the pipeline with the opcode at 0x0100.

        return cpu;
    }

    /// <summary>
    /// Load a ROM into the CPU's memory.
    /// </summary>
    public static void LoadRom(this Cpu cpu, Cartridge cartridge)
    {
        if (cpu == null)
            throw new ArgumentNullException(nameof(cpu));
        if (cartridge == null)
            throw new ArgumentNullException(nameof(cartridge));

        Console.WriteLine(cartridge);

        cpu.Bus.LoadCartridge(cartridge);
        cpu.Bus.BootRom?.Load();
    }
}
