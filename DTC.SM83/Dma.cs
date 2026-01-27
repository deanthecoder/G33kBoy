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

using DTC.Emulation.Snapshot;

namespace DTC.SM83;

/// <summary>
/// Handles OAM DMA transfers.
/// </summary>
public class Dma
{
    private const int TransferLength = 0xA0;
    private const int CyclesPerByte = 4;

    private readonly Bus m_bus;

    private ushort m_sourceAddr;
    private ushort m_destAddr;
    private int m_bytesRemaining;
    private ulong m_cycleBudget;

    public Dma(Bus bus)
    {
        m_bus = bus ?? throw new ArgumentNullException(nameof(bus));
    }

    public InstructionLogger InstructionLogger { get; set; }

    /// <summary>
    /// True while a DMA transfer is underway.
    /// </summary>
    public bool IsTransferActive { get; private set; }

    /// <summary>
    /// Allows us to turn of DMA transfers whilst the hardware ROM/RAM content is first initialized.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Begin a new DMA transfer from the supplied high byte page.
    /// </summary>
    public void Start(byte sourceHighByte)
    {
        if (!IsEnabled)
            return;
        m_sourceAddr = (ushort)(sourceHighByte << 8);
        m_destAddr = 0xFE00;
        m_bytesRemaining = TransferLength;
        m_cycleBudget = 0;
        IsTransferActive = true;

        InstructionLogger?.Write(() => $"DMA start: source={m_sourceAddr:X4} dest={m_destAddr:X4} length={TransferLength}");
    }

    /// <summary>
    /// Advance the DMA engine by the given T-cycles, copying bytes in 4T steps.
    /// </summary>
    public void AdvanceT(ulong tCycles)
    {
        if (!IsTransferActive)
            return;

        m_cycleBudget += tCycles;
        while (m_cycleBudget >= CyclesPerByte && IsTransferActive)
        {
            m_cycleBudget -= CyclesPerByte;

            var sourceAddr = m_sourceAddr++;
            var destAddr = m_destAddr++;
            var data = m_bus.UncheckedRead(sourceAddr);
            m_bus.UncheckedWrite(destAddr, data);

            if (--m_bytesRemaining == 0)
            {
                IsTransferActive = false;
                m_cycleBudget = 0;
                InstructionLogger?.Write(() => "DMA complete");
            }
        }
    }

    public int GetStateSize() =>
        sizeof(byte) * 2 + // IsTransferActive, IsEnabled
        sizeof(ushort) * 2 + // m_sourceAddr, m_destAddr
        sizeof(int) + // m_bytesRemaining
        sizeof(ulong); // m_cycleBudget

    public void SaveState(ref StateWriter writer)
    {
        writer.WriteBool(IsTransferActive);
        writer.WriteBool(IsEnabled);
        writer.WriteUInt16(m_sourceAddr);
        writer.WriteUInt16(m_destAddr);
        writer.WriteInt32(m_bytesRemaining);
        writer.WriteUInt64(m_cycleBudget);
    }

    public void LoadState(ref StateReader reader)
    {
        IsTransferActive = reader.ReadBool();
        IsEnabled = reader.ReadBool();
        m_sourceAddr = reader.ReadUInt16();
        m_destAddr = reader.ReadUInt16();
        m_bytesRemaining = reader.ReadInt32();
        m_cycleBudget = reader.ReadUInt64();
    }
}
