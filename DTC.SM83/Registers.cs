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

// ReSharper disable NonReadonlyMemberInGetHashCode

using System.Runtime.CompilerServices;

namespace DTC.SM83;

public sealed class Registers
{
    public byte A { get; set; }
    public byte F => (byte)((Zf ? 0x80 : 0) | (Nf ? 0x40 : 0) | (Hf ? 0x20 : 0) | (Cf ? 0x10 : 0));
    public byte B { get; set; }
    public byte C { get; set; }
    public byte D { get; set; }
    public byte E { get; set; }
    public byte H { get; set; }
    public byte L { get; set; }

    public ushort BC
    {
        get => (ushort)((B << 8) | C);
        set
        {
            B = (byte)(value >> 8);
            C = (byte)value;
        }
    }
    
    public ushort DE
    {
        get => (ushort)((D << 8) | E);
        set
        {
            D = (byte)(value >> 8);
            E = (byte)value;
        }
    }

    public ushort HL
    {
        get => (ushort)((H << 8) | L);
        set
        {
            H = (byte)(value >> 8);
            L = (byte)value;
        }
    }

    public ushort SP { get; set; } = 0xFFFE;
    public ushort PC { get; set; }
    
    /// <summary>
    /// Zero flag.
    /// </summary>
    public bool Zf { get; set; }
    
    /// <summary>
    /// Subtract flag.
    /// </summary>
    public bool Nf { get; set; }
    
    /// <summary>
    /// Half-carry flag.
    /// </summary>
    public bool Hf { get; set; }
    
    /// <summary>
    /// Carry flag.
    /// </summary>
    public bool Cf { get; set; }

    public void CopyTo(Registers cpuReg)
    {
        cpuReg.A = A;
        cpuReg.B = B;
        cpuReg.C = C;
        cpuReg.D = D;
        cpuReg.E = E;
        cpuReg.H = H;
        cpuReg.L = L;
        cpuReg.SP = SP;
        cpuReg.PC = PC;
        cpuReg.Zf = Zf;
        cpuReg.Nf = Nf;
        cpuReg.Hf = Hf;
        cpuReg.Cf = Cf;
    }

    public static bool operator ==(Registers left, Registers right)
    {
        if (ReferenceEquals(left, right))
            return true;
        if (left is null || right is null)
            return false;
        return left.A == right.A &&
               left.B == right.B &&
               left.C == right.C &&
               left.D == right.D &&
               left.E == right.E &&
               left.H == right.H &&
               left.L == right.L &&
               left.SP == right.SP &&
               left.PC == right.PC &&
               left.Zf == right.Zf &&
               left.Nf == right.Nf &&
               left.Hf == right.Hf &&
               left.Cf == right.Cf;
    }

    public static bool operator !=(Registers left, Registers right) => !(left == right);

    public override bool Equals(object obj) => obj is Registers other && this == other;

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(A);
        hash.Add(B);
        hash.Add(C);
        hash.Add(D);
        hash.Add(E);
        hash.Add(H);
        hash.Add(L);
        hash.Add(SP);
        hash.Add(PC);
        hash.Add(Zf);
        hash.Add(Nf);
        hash.Add(Hf);
        hash.Add(Cf);
        return hash.ToHashCode();
    }
    
    public override string ToString() =>
        $"A:{A:X2} F:{F:X2} B:{B:X2} C:{C:X2} D:{D:X2} E:{E:X2} H:{H:X2} L:{L:X2} SP:{SP:X4} PC:{PC:X4}";

    /// <summary>
    /// Call before an add/inc math operation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetHfForInc(byte value, byte inc = 1) =>
        Hf = (value & 0x0F) + (inc & 0x0F) > 0x0F;

    /// <summary>
    /// Call before a sub/dec math operation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetHfForDec(byte value, byte dec = 1) =>
        Hf = (value & 0x0F) < (dec & 0x0F);
}