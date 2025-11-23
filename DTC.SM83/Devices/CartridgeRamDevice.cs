// Code authored by Dean Edis (DeanTheCoder).
// Anyone is free to copy, modify, use, compile, or distribute this software,
// either in source code form or as a compiled binary, for any purpose.
//
// If you modify the code, please retain this copyright header,
// and consider contributing back to the repository or letting us know
// about your modifications. Your contributions are valued!
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.

namespace DTC.SM83.Devices;

/// <summary>
/// Exposes battery-backed cartridge RAM via the active MBC. (0xA000 - 0xBFFF)
/// </summary>
public sealed class CartridgeRamDevice : IMemDevice
{
    private readonly IMemoryBankController m_controller;

    public ushort FromAddr => 0xA000;
    public ushort ToAddr => 0xBFFF;

    public CartridgeRamDevice(IMemoryBankController controller)
    {
        m_controller = controller ?? throw new ArgumentNullException(nameof(controller));
    }

    public byte Read8(ushort addr) =>
        m_controller.ReadRam(addr);

    public void Write8(ushort addr, byte value) =>
        m_controller.WriteRam(addr, value);

    public byte[] GetSnapshot() =>
        m_controller.GetRamSnapshot();

    public void LoadSnapshot(ReadOnlySpan<byte> data) =>
        m_controller.LoadRamSnapshot(data);
}
