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

using DTC.SM83;

namespace UnitTests;

public class PaletteRegressionTests
{
    private const ushort Bgpi = 0xFF68;
    private const ushort Bgpd = 0xFF69;
    private const ushort Obpi = 0xFF6A;
    private const ushort Obpd = 0xFF6B;

    /// <summary>
    /// Regression test for a GBC palette-read auto-increment bug that made the background in
    /// Alone in the Dark: The New Nightmare render with incorrect (fleshy) colors. BGPD/OBPD
    /// reads must not advance the palette index even when auto-increment is enabled.
    /// </summary>
    [Test]
    public void CgbPaletteReadsDoNotAutoIncrement()
    {
        using var bus = new Bus(0x10000, Bus.BusType.GameBoy);
        bus.SetMode(GameBoyMode.Cgb);

        var bgData = CreatePattern(0x11);
        var objData = CreatePattern(0xA7);

        WritePalette(bus, Bgpi, Bgpd, bgData);
        WritePalette(bus, Obpi, Obpd, objData);

        AssertPaletteReadStable(bus, Bgpi, Bgpd, bgData, 0x12);
        AssertPaletteReadStable(bus, Obpi, Obpd, objData, 0x2C);
    }

    private static void WritePalette(Bus bus, ushort indexRegister, ushort dataRegister, byte[] data)
    {
        bus.UncheckedWrite(indexRegister, 0x80);
        for (var i = 0; i < data.Length; i++)
            bus.UncheckedWrite(dataRegister, data[i]);
    }

    private static void AssertPaletteReadStable(Bus bus, ushort indexRegister, ushort dataRegister, byte[] data, byte index)
    {
        bus.UncheckedWrite(indexRegister, (byte)(0x80 | (index & 0x3F)));

        var first = bus.UncheckedRead(dataRegister);
        var indexAfterFirst = bus.UncheckedRead(indexRegister);

        var second = bus.UncheckedRead(dataRegister);
        var indexAfterSecond = bus.UncheckedRead(indexRegister);

        Assert.That(first, Is.EqualTo(data[index & 0x3F]));
        Assert.That(second, Is.EqualTo(data[index & 0x3F]));
        Assert.That(indexAfterFirst & 0x3F, Is.EqualTo(index & 0x3F));
        Assert.That(indexAfterSecond & 0x3F, Is.EqualTo(index & 0x3F));
        Assert.That(indexAfterFirst & 0x80, Is.EqualTo(0x80));
        Assert.That(indexAfterSecond & 0x80, Is.EqualTo(0x80));
    }

    private static byte[] CreatePattern(byte seed)
    {
        var data = new byte[0x40];
        for (var i = 0; i < data.Length; i++)
            data[i] = (byte)(seed + i * 3);
        return data;
    }
}
