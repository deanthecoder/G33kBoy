// Code authored by Dean Edis (DeanTheCoder).
// Anyone is free to copy, modify, use, compile, or distribute this software,
// either in source code form or as a compiled binary, for any purpose.
// 
// If you modify the code, please retain this copyright header,
// and consider contributing back to the repository or letting us know
// about your modifications. Your contributions are valued!
// 
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.
using System.Text;
using DTC.SM83.Devices;

namespace UnitTests;

/// <summary>
/// Minimal bus to allow capturing of serial output, used by the Blargg tests when no PPU is implemented.
/// </summary>
internal class SerialDevice : IMemDevice
{
    public ushort FromAddr => 0xFF01;
    public ushort ToAddr => 0xFF02;

    /// <summary>
    /// Transfer data, Serial Control.
    /// </summary>
    private readonly byte[] m_data = new byte[2];

    private readonly StringBuilder m_output = new StringBuilder();
        
    public string Output => m_output.ToString();
        
    public byte Read8(ushort addr) => 0x00;

    public void Write8(ushort addr, byte value)
    {
        switch (addr)
        {
            case 0xFF01:
                // Transfer data.
                m_data[0] = value;
                return;
            case 0xFF02:
                // Serial Control.
                m_output.Append((char)m_data[0]);
                m_data[1] = 0x01;
                break;
        }
    }
}