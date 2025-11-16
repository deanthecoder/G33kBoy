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

// ReSharper disable NonReadonlyMemberInGetHashCode

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DTC.SM83;

[StructLayout(LayoutKind.Explicit)]
public sealed class Registers
{
    [FieldOffset(0)] public byte A;
    public byte F => (byte)((Zf ? 0x80 : 0) | (Nf ? 0x40 : 0) | (Hf ? 0x20 : 0) | (Cf ? 0x10 : 0));
    [FieldOffset(2)] public byte B;
    [FieldOffset(1)] public byte C;
    [FieldOffset(4)] public byte D;
    [FieldOffset(3)] public byte E;
    [FieldOffset(6)] public byte H;
    [FieldOffset(5)] public byte L;

    [FieldOffset(1)] public ushort BC;
    [FieldOffset(3)] public ushort DE;
    [FieldOffset(5)] public ushort HL;

    [FieldOffset(7)] public ushort SP = 0xFFFE;
    [FieldOffset(9)] public ushort PC;

    /// <summary>
    /// Zero flag.
    /// </summary>
    [FieldOffset(11)] public bool Zf;

    /// <summary>
    /// Subtract flag.
    /// </summary>
    [FieldOffset(12)] public bool Nf;

    /// <summary>
    /// Half-carry flag.
    /// </summary>
    [FieldOffset(13)] public bool Hf;

    /// <summary>
    /// Carry flag.
    /// </summary>
    [FieldOffset(14)] public bool Cf;

    public void CopyTo(Registers cpuReg)
    {
        cpuReg.A = A;
        cpuReg.BC = BC;
        cpuReg.DE = DE;
        cpuReg.HL = HL;
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
               left.F == right.F &&
               left.BC == right.BC &&
               left.DE == right.DE &&
               left.HL == right.HL &&
               left.SP == right.SP &&
               left.PC == right.PC;
    }

    public static bool operator !=(Registers left, Registers right) => !(left == right);

    public override bool Equals(object obj) => obj is Registers other && this == other;

    public override int GetHashCode() =>
        ToString().GetHashCode();

    public override string ToString() =>
        $"A:{A:X2} F:{F:X2} BC:{B:X2}{C:X2} DE:{D:X2}{E:X2} HL:{H:X2}{L:X2} SP:{SP:X4} PC:{PC:X4}";

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
    
    public string FlagsAsString() =>
        $"{(Zf ? "Z" : "z")}{(Nf ? "N" : "n")}{(Hf ? "H" : "h")}{(Cf ? "C" : "c")}";
}