// Code authored by Dean Edis (DeanTheCoder).
// Anyone is free to copy, modify, use, compile, or distribute this software,
// either in source code form or as a compiled binary, for any non-commercial
// purpose.
//
// If you modify the code, please retain this copyright header,
// and consider contributing back to the repository or letting us know
// about your modifications. Your contributions are valued!
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.

using DTC.Core.UnitTesting;
using DTC.SM83;

namespace UnitTests;

public class RegisterTests : TestsBase
{
    [Test]
    public void CheckDefaultValues()
    {
        var reg = new Registers();
        
        Assert.That(reg.A, Is.Zero);
        Assert.That(reg.F, Is.Zero);
        Assert.That(reg.B, Is.Zero);
        Assert.That(reg.C, Is.Zero);
        Assert.That(reg.D, Is.Zero);
        Assert.That(reg.E, Is.Zero);
        Assert.That(reg.H, Is.Zero);
        Assert.That(reg.L, Is.Zero);

        Assert.That(reg.SP, Is.EqualTo(0xFFFE));
        Assert.That(reg.PC, Is.Zero);

        Assert.That(reg.Zf, Is.False);
        Assert.That(reg.Nf, Is.False);
        Assert.That(reg.Hf, Is.False);
        Assert.That(reg.Cf, Is.False);
    }

    [Test]
    public void CheckSettingZeroFlag()
    {
        var reg = new Registers
        {
            Zf = true
        };

        Assert.That(reg.Zf, Is.True);
        Assert.That(reg.F, Is.EqualTo(0b10000000));
    }

    [Test]
    public void CheckSettingNegativeFlag()
    {
        var reg = new Registers
        {
            Nf = true
        };

        Assert.That(reg.Nf, Is.True);
        Assert.That(reg.F, Is.EqualTo(0b01000000));
    }

    [Test]
    public void CheckSettingHalfCarryFlag()
    {
        var reg = new Registers
        {
            Hf = true
        };

        Assert.That(reg.Hf, Is.True);
        Assert.That(reg.F, Is.EqualTo(0b00100000));
    }

    [Test]
    public void CheckSettingCarryFlag()
    {
        var reg = new Registers
        {
            Cf = true
        };

        Assert.That(reg.Cf, Is.True);
        Assert.That(reg.F, Is.EqualTo(0b00010000));
    }
    
    [Test]
    public void CheckReadingBcRegisterPair()
    {
        var reg = new Registers
        {
            B = 0x12,
            C = 0x34
        };

        Assert.That(reg.BC, Is.EqualTo(0x1234));
    }

    [Test]
    public void CheckReadingDeRegisterPair()
    {
        var reg = new Registers
        {
            D = 0x56,
            E = 0x78
        };

        Assert.That(reg.DE, Is.EqualTo(0x5678));
    }

    [Test]
    public void CheckReadingHlRegisterPair()
    {
        var reg = new Registers
        {
            H = 0x9A,
            L = 0xBC
        };

        Assert.That(reg.HL, Is.EqualTo(0x9ABC));
    }

    [Test]
    public void CheckEqualityOperatorWithIdenticalRegisters()
    {
        var reg1 = new Registers { A = 0x12, B = 0x34, SP = 0xFFFE, Zf = true };
        var reg2 = new Registers { A = 0x12, B = 0x34, SP = 0xFFFE, Zf = true };

        Assert.That(reg1, Is.EqualTo(reg2));
    }

    [Test]
    public void CheckEqualityOperatorWithDifferentRegisters()
    {
        var reg1 = new Registers { A = 0x12, B = 0x34 };
        var reg2 = new Registers { A = 0x12, B = 0x99 };

        Assert.That(reg1, Is.Not.EqualTo(reg2));
    }
}