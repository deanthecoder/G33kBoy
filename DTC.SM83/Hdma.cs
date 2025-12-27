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

namespace DTC.SM83;

/// <summary>
/// Handles CGB general DMA (GDMA) and HBlank DMA (HDMA) transfers.
/// </summary>
public sealed class Hdma
{
    private const int BlockSize = 0x10;
    private readonly Bus m_bus;
    private byte m_hdma1;
    private byte m_hdma2;
    private byte m_hdma3;
    private byte m_hdma4;
    private bool m_hblankMode;
    private int m_remainingBlocks;
    private ushort m_source;
    private ushort m_dest;

    public Hdma(Bus bus)
    {
        m_bus = bus ?? throw new ArgumentNullException(nameof(bus));
    }

    public bool IsActive => m_remainingBlocks > 0 && m_hblankMode;

    public byte ReadHdma1() => m_hdma1;
    public byte ReadHdma2() => m_hdma2;
    public byte ReadHdma3() => m_hdma3;
    public byte ReadHdma4() => m_hdma4;

    public byte ReadHdma5()
    {
        if (m_remainingBlocks == 0)
            return 0xFF;
        var length = (byte)((m_remainingBlocks - 1) & 0x7F);
        return (byte)((m_hblankMode ? 0x00 : 0x80) | length);
    }

    public void WriteHdma1(byte value) => m_hdma1 = value;
    public void WriteHdma2(byte value) => m_hdma2 = (byte)(value & 0xF0);
    public void WriteHdma3(byte value) => m_hdma3 = (byte)(value & 0x1F);
    public void WriteHdma4(byte value) => m_hdma4 = (byte)(value & 0xF0);

    public void WriteHdma5(byte value)
    {
        var requestedBlocks = (value & 0x7F) + 1;
        var requestHblank = (value & 0x80) != 0;

        if (m_remainingBlocks > 0 && m_hblankMode && !requestHblank)
        {
            // Stop an active HBlank DMA transfer.
            m_remainingBlocks = 0;
            return;
        }

        m_hblankMode = requestHblank;
        m_remainingBlocks = requestedBlocks;
        LatchAddresses();

        if (!m_hblankMode)
        {
            while (m_remainingBlocks > 0)
                TransferBlock();
            m_remainingBlocks = 0;
        }
    }

    public void OnHBlank()
    {
        if (m_remainingBlocks == 0 || !m_hblankMode)
            return;
        TransferBlock();
        if (m_remainingBlocks == 0)
            return;
    }

    private void TransferBlock()
    {
        for (var i = 0; i < BlockSize; i++)
        {
            var data = m_bus.UncheckedRead(m_source++);
            m_bus.UncheckedWrite(m_dest++, data);
        }
        m_remainingBlocks--;
        UpdateRegistersFromAddresses();
    }

    private void LatchAddresses()
    {
        m_source = (ushort)((m_hdma1 << 8) | m_hdma2);
        m_source = (ushort)(m_source & 0xFFF0);

        m_dest = (ushort)(0x8000 | (m_hdma3 << 8) | m_hdma4);
        m_dest = (ushort)(m_dest & 0x9FF0);
    }

    private void UpdateRegistersFromAddresses()
    {
        m_hdma1 = (byte)(m_source >> 8);
        m_hdma2 = (byte)(m_source & 0xF0);

        var dest = (ushort)(m_dest - 0x8000);
        m_hdma3 = (byte)((dest >> 8) & 0x1F);
        m_hdma4 = (byte)(dest & 0xF0);
    }
}
