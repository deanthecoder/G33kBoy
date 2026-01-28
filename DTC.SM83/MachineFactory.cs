// Code authored by Dean Edis (DeanTheCoder).
// Anyone is free to copy, modify, use, compile, or distribute this software,
// either in source code form or as a compiled binary, for any purpose.
//
// If you modify the code, please retain this copyright header,
// and consider contributing back to the repository or letting us know
// about your modifications. Your contributions are valued!
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.

using DTC.Emulation;
using DTC.Emulation.Audio;
using DTC.SM83.Devices;

namespace DTC.SM83;

/// <summary>
/// Creates Game Boy hardware components in one explicit location.
/// </summary>
internal sealed class MachineFactory : MachineFactoryBase<Bus, Cpu, PPU, ApuDevice, Joypad>
{
    private readonly SoundDevice m_audioSink;
    private readonly GameBoyMode m_mode;
    private readonly bool m_isCpuHistoryTracked;
    private readonly Joypad m_existingJoypad;

    public MachineFactory(SoundDevice audioSink, GameBoyMode mode, bool isCpuHistoryTracked, Joypad existingJoypad = null)
    {
        m_audioSink = audioSink ?? throw new ArgumentNullException(nameof(audioSink));
        m_mode = mode;
        m_isCpuHistoryTracked = isCpuHistoryTracked;
        m_existingJoypad = existingJoypad;
    }

    protected override Joypad CreateInput() => m_existingJoypad ?? new Joypad();

    protected override Bus CreateBus()
    {
        var bus = new Bus(0x10000, Bus.BusType.GameBoy, Input, m_audioSink);
        bus.SetMode(m_mode);
        return bus;
    }

    protected override Cpu CreateCpu(Bus bus)
    {
        var cpu = new Cpu(bus);
        cpu.InstructionLogger.IsEnabled = m_isCpuHistoryTracked;
        return cpu;
    }

    protected override PPU CreateVideo(Bus bus) => bus.PPU;

    protected override ApuDevice CreateAudio(Bus bus) => bus.APU;
}
