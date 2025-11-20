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

namespace DTC.SM83.Devices;

/// <summary>
/// Represents the cartridge RAM device (0xA000 - 0xBFFF).
/// </summary>
public class CartRamDevice : RamDeviceBase
{
    public CartRamDevice() : base(0xA000, 0xBFFF, isUsable: true)
    {
    }

    /// <summary>
    /// Clone the current RAM contents so the caller can serialize them safely.
    /// </summary>
    public byte[] GetSnapshot() =>
        (byte[])m_data.Clone();

    /// <summary>
    /// Load RAM contents from the supplied snapshot. Pads with zeros if <paramref name="data"/> is shorter.
    /// </summary>
    public void LoadSnapshot(ReadOnlySpan<byte> data)
    {
        var destination = m_data.AsSpan();
        var bytesToCopy = Math.Min(data.Length, destination.Length);
        data[..bytesToCopy].CopyTo(destination);
        if (bytesToCopy < destination.Length)
            destination[bytesToCopy..].Clear();
    }

    public void Clear() =>
        Array.Clear(m_data);
}
