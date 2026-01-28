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
using DTC.Emulation.Snapshot;
using DTC.SM83.Extensions;

namespace DTC.SM83.Devices;

/// <summary>
/// Wires together Game Boy devices into an emulated machine.
/// </summary>
public sealed class GameBoyMachine : IMachine, IMachineSnapshotter, IDisposable
{
    private readonly SoundDevice m_audioSink;
    private readonly VideoSourceProxy m_videoSource = new();
    private readonly bool[] m_soundChannelsEnabled = [true, true, true, true];
    private Bus m_bus;
    private Cpu m_cpu;
    private Cartridge m_loadedCartridge;
    private byte[] m_loadedRomData;
    private string m_loadedRomName;
    private bool m_lcdEmulationEnabled = true;
    private bool m_backgroundVisible = true;
    private bool m_spritesVisible = true;
    private DmgPalette m_dmgPalette = DmgPalette.Default;
    private bool m_isCpuHistoryTracked;
    private GameBoyMode m_requestedMode = GameBoyMode.Cgb;

    public GameBoyMachine(IMachineDescriptor descriptor, SoundDevice audioSink)
    {
        Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        m_audioSink = audioSink ?? throw new ArgumentNullException(nameof(audioSink));
        CreateHardware();
    }

    public IMachineDescriptor Descriptor { get; private set; }

    public string Name => Descriptor?.Name ?? "G33kBoy";

    public long CpuTicks => (long)(m_bus?.CpuClockTicks ?? 0);

    public bool HasLoadedCartridge => m_loadedCartridge != null;

    public IVideoSource Video => m_videoSource;

    public IAudioSource Audio => m_bus?.APU;

    public IMachineSnapshotter Snapshotter => this;

    public Bus Bus => m_bus;

    public Cpu Cpu => m_cpu;

    public Joypad Joypad { get; private set; }

    public GameBoyMode Mode { get; private set; } = GameBoyMode.Dmg;

    public void UpdateDescriptor(IMachineDescriptor descriptor)
    {
        Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
    }

    public void Reset()
    {
        if (m_loadedRomData == null || m_loadedRomData.Length == 0)
            return;

        LoadRom(m_loadedRomData, m_loadedRomName);
    }

    public void LoadRom(byte[] romData, string romName)
    {
        if (romData == null || romData.Length == 0)
            throw new ArgumentException("ROM data is empty.", nameof(romData));

        var cartridge = new Cartridge(romData);
        var supportCheck = cartridge.IsSupported();
        if (!supportCheck.IsSupported)
            throw new InvalidOperationException(supportCheck.Message ?? "Cartridge type is not supported.");

        m_loadedCartridge = cartridge;
        m_loadedRomData = romData;
        m_loadedRomName = romName;

        RecreateHardware();
        ApplyHardwareMode(cartridge);
        m_cpu.LoadRom(cartridge);
        ApplySoundChannelSettings();
        ApplyDisplaySettings();
    }

    public void StepCpu() => m_cpu?.Step();

    public void AdvanceDevices(long deltaTicks)
    {
    }

    public bool TryConsumeInterrupt() => false;

    public void RequestInterrupt()
    {
    }

    public void SetInputActive(bool isActive) =>
        Joypad.SetInputEnabled(isActive);

    public void SetBackgroundVisibility(bool isVisible)
    {
        m_backgroundVisible = isVisible;
        if (m_bus?.PPU != null)
            m_bus.PPU.BackgroundVisible = isVisible;
    }

    public void SetSpriteVisibility(bool isVisible)
    {
        m_spritesVisible = isVisible;
        if (m_bus?.PPU != null)
            m_bus.PPU.SpritesVisible = isVisible;
    }

    public void SetLcdEmulation(bool isEnabled)
    {
        m_lcdEmulationEnabled = isEnabled;
        if (m_bus?.PPU != null)
            m_bus.PPU.LcdEmulationEnabled = isEnabled;
    }

    public void SetDmgPalette(DmgPalette palette)
    {
        if (m_dmgPalette == palette)
            return;

        m_dmgPalette = palette;
        if (m_bus?.PPU != null)
            m_bus.PPU.DmgPalette = palette;
    }

    public void SetSoundChannelEnabled(int channel, bool isEnabled)
    {
        if (channel is < 1 or > 4)
            return;

        m_soundChannelsEnabled[channel - 1] = isEnabled;
        m_bus?.SetSoundChannelEnabled(channel, isEnabled);
    }

    public void SetCpuHistoryTracking(bool isEnabled)
    {
        m_isCpuHistoryTracked = isEnabled;
        if (m_cpu != null)
            m_cpu.InstructionLogger.IsEnabled = isEnabled;
    }

    public void SetRequestedMode(GameBoyMode mode)
    {
        if (m_requestedMode == mode)
            return;

        m_requestedMode = mode;
        if (m_loadedCartridge != null)
            ApplyHardwareMode(m_loadedCartridge);
    }

    public double GetEffectiveCpuHz() =>
        m_bus?.IsDoubleSpeed == true ? Cpu.Hz * 2.0 : Cpu.Hz;

    public void Dispose()
    {
        DisposeHardware();
        m_videoSource.SetSource(null);
        Joypad.Dispose();
    }

    private void CreateHardware()
    {
        var factory = new MachineFactory(m_audioSink, Mode, m_isCpuHistoryTracked, Joypad);
        factory.Build();
        Joypad = factory.Input;
        m_bus = factory.Bus;
        m_cpu = factory.Cpu;
        m_videoSource.SetSource(factory.Video);
        ApplySoundChannelSettings();
        ApplyDisplaySettings();
    }

    private void RecreateHardware()
    {
        var debuggers = m_cpu?.Debuggers;
        DisposeHardware();
        CreateHardware();
        if (debuggers == null)
            return;
        foreach (var debugger in debuggers)
            m_cpu.AddDebugger(debugger);
    }

    private void DisposeHardware()
    {
        m_bus?.Dispose();
        m_bus = null;
        m_cpu = null;
    }

    private void ApplyHardwareMode(Cartridge cartridge)
    {
        Mode = DetermineEffectiveMode(cartridge, m_requestedMode);
        m_bus?.SetMode(Mode);
    }

    private void ApplySoundChannelSettings()
    {
        if (m_bus == null)
            return;

        for (var channel = 1; channel <= m_soundChannelsEnabled.Length; channel++)
            m_bus.SetSoundChannelEnabled(channel, m_soundChannelsEnabled[channel - 1]);
    }

    private void ApplyDisplaySettings()
    {
        var ppu = m_bus?.PPU;
        if (ppu == null)
            return;

        ppu.BackgroundVisible = m_backgroundVisible;
        ppu.SpritesVisible = m_spritesVisible;
        ppu.LcdEmulationEnabled = m_lcdEmulationEnabled;
        ppu.DmgPalette = m_dmgPalette;
    }

    private static GameBoyMode DetermineEffectiveMode(Cartridge cartridge, GameBoyMode requestedMode)
    {
        if (requestedMode == GameBoyMode.Cgb || cartridge.IsCgbOnly)
            return GameBoyMode.Cgb;
        return GameBoyMode.Dmg;
    }

    private sealed class VideoSourceProxy : IVideoSource
    {
        private PPU m_source;

        public int FrameWidth => PPU.FrameWidth;

        public int FrameHeight => PPU.FrameHeight;

        public event EventHandler<byte[]> FrameRendered;

        public void CopyFrameBuffer(Span<byte> frameBuffer)
        {
            if (m_source == null)
                return;
            m_source.CopyFrameBuffer(frameBuffer);
        }

        public void SetSource(PPU source)
        {
            if (m_source != null)
                m_source.FrameRendered -= OnFrameRendered;
            m_source = source;
            if (m_source != null)
                m_source.FrameRendered += OnFrameRendered;
        }

        private void OnFrameRendered(object sender, byte[] frameBuffer) =>
            FrameRendered?.Invoke(this, frameBuffer);
    }

    int IMachineSnapshotter.GetStateSize() =>
        m_cpu?.GetStateSize() ?? 0;

    void IMachineSnapshotter.Save(MachineState state, Span<byte> frameBuffer)
    {
        if (state == null)
            throw new ArgumentNullException(nameof(state));
        if (m_cpu == null || m_bus?.PPU == null)
            throw new InvalidOperationException("Game Boy hardware is not initialized.");

        m_cpu.SaveState(state);
        m_bus.PPU.CopyFrameBuffer(frameBuffer);
    }

    void IMachineSnapshotter.Load(MachineState state)
    {
        if (state == null)
            throw new ArgumentNullException(nameof(state));
        if (m_cpu == null)
            throw new InvalidOperationException("Game Boy hardware is not initialized.");

        m_cpu.LoadState(state);
    }
}
