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

using System.Runtime.CompilerServices;

namespace DTC.SM83.Instructions;

/// <summary>
/// Initially auto-generated from https://gbdev.io/gb-opcodes/Opcodes.json
/// </summary>
public static class Instructions
{
    public static readonly Instruction[] Table =
    [
        new Instruction(
            "NOP", // 0x00
            static _ => { }),
        new Instruction(
            "LD BC,nn", // 0x01 nn nn
            static cpu =>
            {
                cpu.Reg.C = cpu.Fetch8();
                cpu.Reg.B = cpu.Fetch8();
            }
        ),
        new Instruction(
            "LD (BC),A", // 0x02
            static cpu => { cpu.Write8(cpu.Reg.BC, cpu.Reg.A); }
        ),
        new Instruction(
            "INC BC", // 0x03
            static cpu =>
            {
                cpu.Reg.BC++;
                cpu.InternalWaitM();
            }
        ),
        new Instruction(
            "INC B", // 0x04
            static cpu => { DoINC(cpu, ref cpu.Reg.B); }
        ),
        new Instruction(
            "DEC B", // 0x05
            static cpu => { DoDEC(cpu, ref cpu.Reg.B); }
        ),
        new Instruction(
            "LD B,nn", // 0x06 nn
            static cpu => { cpu.Reg.B = cpu.Fetch8(); }
        ),
        new Instruction(
            "RLCA", // 0x07
            static cpu =>
            {
                cpu.Reg.Cf = (cpu.Reg.A & 0x80) != 0;
                cpu.Reg.A = (byte)((cpu.Reg.A << 1) + (cpu.Reg.Cf ? 1 : 0));
                cpu.Reg.Zf = false;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = false;
            }
        ),
        new Instruction(
            "LD (a16),SP", // 0x08 nn nn
            static cpu => { cpu.Write16(cpu.Fetch16(), cpu.Reg.SP); }
        ),
        new Instruction(
            "ADD HL,BC", // 0x09
            static cpu =>
            {
                cpu.Reg.Hf = (cpu.Reg.HL & 0x0FFF) + (cpu.Reg.BC & 0x0FFF) > 0x0FFF;
                var sum = (uint)cpu.Reg.HL + cpu.Reg.BC;
                cpu.Reg.Cf = sum > 0xFFFF;
                cpu.Reg.HL = (ushort)sum;
                cpu.Reg.Nf = false;
                cpu.InternalWaitM();
            }
        ),
        new Instruction(
            "LD A,(BC)", // 0x0A
            static cpu => { cpu.Reg.A = cpu.Read8(cpu.Reg.BC); }
        ),
        new Instruction(
            "DEC BC", // 0x0B
            static cpu =>
            {
                cpu.Reg.BC--;
                cpu.InternalWaitM();
            }
        ),
        new Instruction(
            "INC C", // 0x0C
            static cpu => { DoINC(cpu, ref cpu.Reg.C); }
        ),
        new Instruction(
            "DEC C", // 0x0D
            static cpu => { DoDEC(cpu, ref cpu.Reg.C); }
        ),
        new Instruction(
            "LD C,nn", // 0x0E nn
            static cpu => { cpu.Reg.C = cpu.Fetch8(); }
        ),
        new Instruction(
            "RRCA", // 0x0F
            static cpu =>
            {
                cpu.Reg.Cf = (cpu.Reg.A & 0x01) != 0;
                cpu.Reg.A = (byte)((cpu.Reg.A >> 1) + (cpu.Reg.Cf ? 0x80 : 0x00));
                cpu.Reg.Zf = false;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = false;
            }
        ),
        new Instruction(
            "STOP nn", // 0x10 nn
            static cpu =>
            {
                cpu.Fetch8(); // Consume padding byte.
                if (!cpu.Bus.TryHandleSpeedSwitch())
                    cpu.EnterStop();
            }),
        new Instruction(
            "LD DE,nn", // 0x11 nn nn
            static cpu =>
            {
                cpu.Reg.E = cpu.Fetch8();
                cpu.Reg.D = cpu.Fetch8();
            }
        ),
        new Instruction(
            "LD (DE),A", // 0x12
            static cpu => { cpu.Write8(cpu.Reg.DE, cpu.Reg.A); }
        ),
        new Instruction(
            "INC DE", // 0x13
            static cpu =>
            {
                cpu.Reg.DE++;
                cpu.InternalWaitM();
            }
        ),
        new Instruction(
            "INC D", // 0x14
            static cpu => { DoINC(cpu, ref cpu.Reg.D); }
        ),
        new Instruction(
            "DEC D", // 0x15
            static cpu => { DoDEC(cpu, ref cpu.Reg.D); }
        ),
        new Instruction(
            "LD D,nn", // 0x16 nn
            static cpu => { cpu.Reg.D = cpu.Fetch8(); }
        ),
        new Instruction(
            "RLA", // 0x17
            static cpu =>
            {
                var cf = cpu.Reg.Cf;
                cpu.Reg.Cf = (cpu.Reg.A & 0x80) != 0;
                cpu.Reg.A = (byte)((cpu.Reg.A << 1) + (cf ? 1 : 0));
                cpu.Reg.Zf = false;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = false;
            }
        ),
        new Instruction(
            "JR e8", // 0x18 nn
            static cpu =>
            {
                var diff = (sbyte)cpu.Fetch8();
                cpu.Reg.PC = (ushort)(cpu.Reg.PC + diff);
                cpu.InternalWaitM();
            }
        ),
        new Instruction(
            "ADD HL,DE", // 0x19
            static cpu =>
            {
                cpu.Reg.Hf = (cpu.Reg.HL & 0x0FFF) + (cpu.Reg.DE & 0x0FFF) > 0x0FFF;
                var sum = (uint)cpu.Reg.HL + cpu.Reg.DE;
                cpu.Reg.Cf = sum > 0xFFFF;
                cpu.Reg.HL = (ushort)sum;
                cpu.Reg.Nf = false;
                cpu.InternalWaitM();
            }
        ),
        new Instruction(
            "LD A,(DE)", // 0x1A
            static cpu => { cpu.Reg.A = cpu.Read8(cpu.Reg.DE); }
        ),
        new Instruction(
            "DEC DE", // 0x1B
            static cpu =>
            {
                cpu.Reg.DE--;
                cpu.InternalWaitM();
            }
        ),
        new Instruction(
            "INC E", // 0x1C
            static cpu => { DoINC(cpu, ref cpu.Reg.E); }
        ),
        new Instruction(
            "DEC E", // 0x1D
            static cpu => { DoDEC(cpu, ref cpu.Reg.E); }
        ),
        new Instruction(
            "LD E,nn", // 0x1E nn
            static cpu => { cpu.Reg.E = cpu.Fetch8(); }
        ),
        new Instruction(
            "RRA", // 0x1F
            static cpu =>
            {
                var cf = cpu.Reg.Cf;
                cpu.Reg.Cf = (cpu.Reg.A & 0x01) != 0;
                cpu.Reg.A = (byte)((cpu.Reg.A >> 1) + (cf ? 0x80 : 0x00));
                cpu.Reg.Zf = false;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = false;
            }
        ),
        new Instruction(
            "JR NZ,e8", // 0x20 nn
            static cpu =>
            {
                var diff = (sbyte)cpu.Fetch8();
                if (cpu.Reg.Zf)
                    return;
                cpu.Reg.PC = (ushort)(cpu.Reg.PC + diff);
                cpu.InternalWaitM();
            }
        ),
        new Instruction(
            "LD HL,nn", // 0x21 nn nn
            static cpu =>
            {
                cpu.Reg.L = cpu.Fetch8();
                cpu.Reg.H = cpu.Fetch8();
            }
        ),
        new Instruction(
            "LD (HL+),A", // 0x22
            static cpu =>
            {
                cpu.Write8(cpu.Reg.HL, cpu.Reg.A);
                cpu.Reg.HL++;
            }
        ),
        new Instruction(
            "INC HL", // 0x23
            static cpu =>
            {
                cpu.Reg.HL++;
                cpu.InternalWaitM();
            }
        ),
        new Instruction(
            "INC H", // 0x24
            static cpu => { DoINC(cpu, ref cpu.Reg.H); }
        ),
        new Instruction(
            "DEC H", // 0x25
            static cpu => { DoDEC(cpu, ref cpu.Reg.H); }
        ),
        new Instruction(
            "LD H,nn", // 0x26 nn
            static cpu => { cpu.Reg.H = cpu.Fetch8(); }
        ),
        new Instruction(
            "DAA", // 0x27
            static cpu =>
            {
                if (cpu.Reg.Nf)
                {
                    var adjust = cpu.Reg.Hf ? 0x06 : 0x00;
                    if (cpu.Reg.Cf)
                        adjust |= 0x60;

                    cpu.Reg.A = (byte)(cpu.Reg.A - adjust);
                }
                else
                {
                    var adjust = cpu.Reg.Hf || (cpu.Reg.A & 0x0F) > 0x09 ? 0x06 : 0x00;
                    if (cpu.Reg.Cf || cpu.Reg.A > 0x99)
                    {
                        adjust |= 0x60;
                        cpu.Reg.Cf = true;
                    }

                    cpu.Reg.A = (byte)(cpu.Reg.A + adjust);
                }

                cpu.Reg.Zf = cpu.Reg.A == 0;
                cpu.Reg.Hf = false;
            }
        ),
        new Instruction(
            "JR Z,e8", // 0x28 nn
            static cpu =>
            {
                var diff = (sbyte)cpu.Fetch8();
                if (!cpu.Reg.Zf)
                    return;
                cpu.Reg.PC = (ushort)(cpu.Reg.PC + diff);
                cpu.InternalWaitM();
            }
        ),
        new Instruction(
            "ADD HL,HL", // 0x29
            static cpu =>
            {
                cpu.Reg.Hf = (cpu.Reg.HL & 0x0FFF) + (cpu.Reg.HL & 0x0FFF) > 0x0FFF;
                var sum = (uint)cpu.Reg.HL + cpu.Reg.HL;
                cpu.Reg.Cf = sum > 0xFFFF;
                cpu.Reg.HL = (ushort)sum;
                cpu.Reg.Nf = false;
                cpu.InternalWaitM();
            }
        ),
        new Instruction(
            "LD A,(HL+)", // 0x2A
            static cpu =>
            {
                cpu.Reg.A = cpu.Read8(cpu.Reg.HL);
                cpu.Reg.HL++;
            }
        ),
        new Instruction(
            "DEC HL", // 0x2B
            static cpu =>
            {
                cpu.Reg.HL--;
                cpu.InternalWaitM();
            }
        ),
        new Instruction(
            "INC L", // 0x2C
            static cpu => { DoINC(cpu, ref cpu.Reg.L); }
        ),
        new Instruction(
            "DEC L", // 0x2D
            static cpu => { DoDEC(cpu, ref cpu.Reg.L); }
        ),
        new Instruction(
            "LD L,nn", // 0x2E nn
            static cpu => { cpu.Reg.L = cpu.Fetch8(); }
        ),
        new Instruction(
            "CPL", // 0x2F
            static cpu =>
            {
                cpu.Reg.A = (byte)~cpu.Reg.A;
                cpu.Reg.Nf = true;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "JR NC,e8", // 0x30 nn
            static cpu =>
            {
                var diff = (sbyte)cpu.Fetch8();
                if (cpu.Reg.Cf)
                    return;
                cpu.Reg.PC = (ushort)(cpu.Reg.PC + diff);
                cpu.InternalWaitM();
            }
        ),
        new Instruction(
            "LD SP,nn", // 0x31 nn nn
            static cpu => { cpu.Reg.SP = cpu.Fetch16(); }
        ),
        new Instruction(
            "LD (HL-),A", // 0x32
            static cpu =>
            {
                cpu.Write8(cpu.Reg.HL, cpu.Reg.A);
                cpu.Reg.HL--;
            }
        ),
        new Instruction(
            "INC SP", // 0x33
            static cpu =>
            {
                cpu.Reg.SP++;
                cpu.InternalWaitM();
            }
        ),
        new Instruction(
            "INC (HL)", // 0x34
            static cpu =>
            {
                var value = cpu.Read8(cpu.Reg.HL);
                DoINC(cpu, ref value);
                cpu.Write8(cpu.Reg.HL, value);
            }
        ),
        new Instruction(
            "DEC (HL)", // 0x35
            static cpu =>
            {
                var value = cpu.Read8(cpu.Reg.HL);
                DoDEC(cpu, ref value);
                cpu.Write8(cpu.Reg.HL, value);
            }
        ),
        new Instruction(
            "LD (HL),nn", // 0x36 nn
            static cpu => { cpu.Write8(cpu.Reg.HL, cpu.Fetch8()); }
        ),
        new Instruction(
            "SCF", // 0x37
            static cpu =>
            {
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = false;
                cpu.Reg.Cf = true;
            }
        ),
        new Instruction(
            "JR C,e8", // 0x38 nn
            static cpu =>
            {
                var diff = (sbyte)cpu.Fetch8();
                if (!cpu.Reg.Cf)
                    return;
                cpu.Reg.PC = (ushort)(cpu.Reg.PC + diff);
                cpu.InternalWaitM();
            }
        ),
        new Instruction(
            "ADD HL,SP", // 0x39
            static cpu =>
            {
                cpu.Reg.Hf = (cpu.Reg.HL & 0x0FFF) + (cpu.Reg.SP & 0x0FFF) > 0x0FFF;
                var sum = (uint)cpu.Reg.HL + cpu.Reg.SP;
                cpu.Reg.Cf = sum > 0xFFFF;
                cpu.Reg.HL = (ushort)sum;
                cpu.Reg.Nf = false;
                cpu.InternalWaitM();
            }
        ),
        new Instruction(
            "LD A,(HL-)", // 0x3A
            static cpu =>
            {
                cpu.Reg.A = cpu.Read8(cpu.Reg.HL);
                cpu.Reg.HL--;
            }
        ),
        new Instruction(
            "DEC SP", // 0x3B
            static cpu =>
            {
                cpu.Reg.SP--;
                cpu.InternalWaitM();
            }
        ),
        new Instruction(
            "INC A", // 0x3C
            static cpu => { DoINC(cpu, ref cpu.Reg.A); }
        ),
        new Instruction(
            "DEC A", // 0x3D
            static cpu => { DoDEC(cpu, ref cpu.Reg.A); }
        ),
        new Instruction(
            "LD A,nn", // 0x3E nn
            static cpu => { cpu.Reg.A = cpu.Fetch8(); }
        ),
        new Instruction(
            "CCF", // 0x3F
            static cpu =>
            {
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = false;
                cpu.Reg.Cf = !cpu.Reg.Cf;
            }
        ),
        new Instruction(
            "LD B,B", // 0x40
            static _ => { }),
        new Instruction(
            "LD B,C", // 0x41
            static cpu => { cpu.Reg.B = cpu.Reg.C; }
        ),
        new Instruction(
            "LD B,D", // 0x42
            static cpu => { cpu.Reg.B = cpu.Reg.D; }
        ),
        new Instruction(
            "LD B,E", // 0x43
            static cpu => { cpu.Reg.B = cpu.Reg.E; }
        ),
        new Instruction(
            "LD B,H", // 0x44
            static cpu => { cpu.Reg.B = cpu.Reg.H; }
        ),
        new Instruction(
            "LD B,L", // 0x45
            static cpu => { cpu.Reg.B = cpu.Reg.L; }
        ),
        new Instruction(
            "LD B,(HL)", // 0x46
            static cpu => { cpu.Reg.B = cpu.Read8(cpu.Reg.HL); }
        ),
        new Instruction(
            "LD B,A", // 0x47
            static cpu => { cpu.Reg.B = cpu.Reg.A; }
        ),
        new Instruction(
            "LD C,B", // 0x48
            static cpu => { cpu.Reg.C = cpu.Reg.B; }
        ),
        new Instruction(
            "LD C,C", // 0x49
            static _ => { }),
        new Instruction(
            "LD C,D", // 0x4A
            static cpu => { cpu.Reg.C = cpu.Reg.D; }
        ),
        new Instruction(
            "LD C,E", // 0x4B
            static cpu => { cpu.Reg.C = cpu.Reg.E; }
        ),
        new Instruction(
            "LD C,H", // 0x4C
            static cpu => { cpu.Reg.C = cpu.Reg.H; }
        ),
        new Instruction(
            "LD C,L", // 0x4D
            static cpu => { cpu.Reg.C = cpu.Reg.L; }
        ),
        new Instruction(
            "LD C,(HL)", // 0x4E
            static cpu => { cpu.Reg.C = cpu.Read8(cpu.Reg.HL); }
        ),
        new Instruction(
            "LD C,A", // 0x4F
            static cpu => { cpu.Reg.C = cpu.Reg.A; }
        ),
        new Instruction(
            "LD D,B", // 0x50
            static cpu => { cpu.Reg.D = cpu.Reg.B; }
        ),
        new Instruction(
            "LD D,C", // 0x51
            static cpu => { cpu.Reg.D = cpu.Reg.C; }
        ),
        new Instruction(
            "LD D,D", // 0x52
            static _ => { }),
        new Instruction(
            "LD D,E", // 0x53
            static cpu => { cpu.Reg.D = cpu.Reg.E; }
        ),
        new Instruction(
            "LD D,H", // 0x54
            static cpu => { cpu.Reg.D = cpu.Reg.H; }
        ),
        new Instruction(
            "LD D,L", // 0x55
            static cpu => { cpu.Reg.D = cpu.Reg.L; }
        ),
        new Instruction(
            "LD D,(HL)", // 0x56
            static cpu => { cpu.Reg.D = cpu.Read8(cpu.Reg.HL); }
        ),
        new Instruction(
            "LD D,A", // 0x57
            static cpu => { cpu.Reg.D = cpu.Reg.A; }
        ),
        new Instruction(
            "LD E,B", // 0x58
            static cpu => { cpu.Reg.E = cpu.Reg.B; }
        ),
        new Instruction(
            "LD E,C", // 0x59
            static cpu => { cpu.Reg.E = cpu.Reg.C; }
        ),
        new Instruction(
            "LD E,D", // 0x5A
            static cpu => { cpu.Reg.E = cpu.Reg.D; }
        ),
        new Instruction(
            "LD E,E", // 0x5B
            static _ => { }),
        new Instruction(
            "LD E,H", // 0x5C
            static cpu => { cpu.Reg.E = cpu.Reg.H; }
        ),
        new Instruction(
            "LD E,L", // 0x5D
            static cpu => { cpu.Reg.E = cpu.Reg.L; }
        ),
        new Instruction(
            "LD E,(HL)", // 0x5E
            static cpu => { cpu.Reg.E = cpu.Read8(cpu.Reg.HL); }
        ),
        new Instruction(
            "LD E,A", // 0x5F
            static cpu => { cpu.Reg.E = cpu.Reg.A; }
        ),
        new Instruction(
            "LD H,B", // 0x60
            static cpu => { cpu.Reg.H = cpu.Reg.B; }
        ),
        new Instruction(
            "LD H,C", // 0x61
            static cpu => { cpu.Reg.H = cpu.Reg.C; }
        ),
        new Instruction(
            "LD H,D", // 0x62
            static cpu => { cpu.Reg.H = cpu.Reg.D; }
        ),
        new Instruction(
            "LD H,E", // 0x63
            static cpu => { cpu.Reg.H = cpu.Reg.E; }
        ),
        new Instruction(
            "LD H,H", // 0x64
            static _ => { }),
        new Instruction(
            "LD H,L", // 0x65
            static cpu => { cpu.Reg.H = cpu.Reg.L; }
        ),
        new Instruction(
            "LD H,(HL)", // 0x66
            static cpu => { cpu.Reg.H = cpu.Read8(cpu.Reg.HL); }
        ),
        new Instruction(
            "LD H,A", // 0x67
            static cpu => { cpu.Reg.H = cpu.Reg.A; }
        ),
        new Instruction(
            "LD L,B", // 0x68
            static cpu => { cpu.Reg.L = cpu.Reg.B; }
        ),
        new Instruction(
            "LD L,C", // 0x69
            static cpu => { cpu.Reg.L = cpu.Reg.C; }
        ),
        new Instruction(
            "LD L,D", // 0x6A
            static cpu => { cpu.Reg.L = cpu.Reg.D; }
        ),
        new Instruction(
            "LD L,E", // 0x6B
            static cpu => { cpu.Reg.L = cpu.Reg.E; }
        ),
        new Instruction(
            "LD L,H", // 0x6C
            static cpu => { cpu.Reg.L = cpu.Reg.H; }
        ),
        new Instruction(
            "LD L,L", // 0x6D
            static _ => { }),
        new Instruction(
            "LD L,(HL)", // 0x6E
            static cpu => { cpu.Reg.L = cpu.Read8(cpu.Reg.HL); }
        ),
        new Instruction(
            "LD L,A", // 0x6F
            static cpu => { cpu.Reg.L = cpu.Reg.A; }
        ),
        new Instruction(
            "LD (HL),B", // 0x70
            static cpu => { cpu.Write8(cpu.Reg.HL, cpu.Reg.B); }
        ),
        new Instruction(
            "LD (HL),C", // 0x71
            static cpu => { cpu.Write8(cpu.Reg.HL, cpu.Reg.C); }
        ),
        new Instruction(
            "LD (HL),D", // 0x72
            static cpu => { cpu.Write8(cpu.Reg.HL, cpu.Reg.D); }
        ),
        new Instruction(
            "LD (HL),E", // 0x73
            static cpu => { cpu.Write8(cpu.Reg.HL, cpu.Reg.E); }
        ),
        new Instruction(
            "LD (HL),H", // 0x74
            static cpu => { cpu.Write8(cpu.Reg.HL, cpu.Reg.H); }
        ),
        new Instruction(
            "LD (HL),L", // 0x75
            static cpu => { cpu.Write8(cpu.Reg.HL, cpu.Reg.L); }
        ),
        new Instruction(
            "HALT", // 0x76
            static cpu =>
            {
                if (cpu.IME)
                {
                    cpu.IsHalted = true; // Sleep until an interrupt is about to be serviced.
                }
                else
                {
                    var ieif = (byte)(cpu.IE & cpu.IF);
                    if (ieif == 0)
                        cpu.IsHalted = true; // IME=0, no pending -> wake when any pending appears (but don't service)
                    else
                        cpu.HaltBug = true; // IME=0, pending!=0 -> HALT bug (don't halt; next opcode byte read twice)
                }
            }
        ),
        new Instruction(
            "LD (HL),A", // 0x77
            static cpu => { cpu.Write8(cpu.Reg.HL, cpu.Reg.A); }
        ),
        new Instruction(
            "LD A,B", // 0x78
            static cpu => { cpu.Reg.A = cpu.Reg.B; }
        ),
        new Instruction(
            "LD A,C", // 0x79
            static cpu => { cpu.Reg.A = cpu.Reg.C; }
        ),
        new Instruction(
            "LD A,D", // 0x7A
            static cpu => { cpu.Reg.A = cpu.Reg.D; }
        ),
        new Instruction(
            "LD A,E", // 0x7B
            static cpu => { cpu.Reg.A = cpu.Reg.E; }
        ),
        new Instruction(
            "LD A,H", // 0x7C
            static cpu => { cpu.Reg.A = cpu.Reg.H; }
        ),
        new Instruction(
            "LD A,L", // 0x7D
            static cpu => { cpu.Reg.A = cpu.Reg.L; }
        ),
        new Instruction(
            "LD A,(HL)", // 0x7E
            static cpu => { cpu.Reg.A = cpu.Read8(cpu.Reg.HL); }
        ),
        new Instruction(
            "LD A,A", // 0x7F
            static _ => { }),
        new Instruction(
            "ADD A,B", // 0x80
            static cpu => { DoADD(cpu, cpu.Reg.B); }
        ),
        new Instruction(
            "ADD A,C", // 0x81
            static cpu => { DoADD(cpu, cpu.Reg.C); }
        ),
        new Instruction(
            "ADD A,D", // 0x82
            static cpu => { DoADD(cpu, cpu.Reg.D); }
        ),
        new Instruction(
            "ADD A,E", // 0x83
            static cpu => { DoADD(cpu, cpu.Reg.E); }
        ),
        new Instruction(
            "ADD A,H", // 0x84
            static cpu => { DoADD(cpu, cpu.Reg.H); }
        ),
        new Instruction(
            "ADD A,L", // 0x85
            static cpu => { DoADD(cpu, cpu.Reg.L); }
        ),
        new Instruction(
            "ADD A,(HL)", // 0x86
            static cpu => { DoADD(cpu, cpu.Read8(cpu.Reg.HL)); }
        ),
        new Instruction(
            "ADD A,A", // 0x87
            static cpu => { DoADD(cpu, cpu.Reg.A); }
        ),
        new Instruction(
            "ADC A,B", // 0x88
            static cpu => { DoADC(cpu, cpu.Reg.B); }
        ),
        new Instruction(
            "ADC A,C", // 0x89
            static cpu => { DoADC(cpu, cpu.Reg.C); }
        ),
        new Instruction(
            "ADC A,D", // 0x8A
            static cpu => { DoADC(cpu, cpu.Reg.D); }
        ),
        new Instruction(
            "ADC A,E", // 0x8B
            static cpu => { DoADC(cpu, cpu.Reg.E); }
        ),
        new Instruction(
            "ADC A,H", // 0x8C
            static cpu => { DoADC(cpu, cpu.Reg.H); }
        ),
        new Instruction(
            "ADC A,L", // 0x8D
            static cpu => { DoADC(cpu, cpu.Reg.L); }
        ),
        new Instruction(
            "ADC A,(HL)", // 0x8E
            static cpu => { DoADC(cpu, cpu.Read8(cpu.Reg.HL)); }
        ),
        new Instruction(
            "ADC A,A", // 0x8F
            static cpu => { DoADC(cpu, cpu.Reg.A); }
        ),
        new Instruction(
            "SUB A,B", // 0x90
            static cpu => { DoSUB(cpu, cpu.Reg.B); }
        ),
        new Instruction(
            "SUB A,C", // 0x91
            static cpu => { DoSUB(cpu, cpu.Reg.C); }
        ),
        new Instruction(
            "SUB A,D", // 0x92
            static cpu => { DoSUB(cpu, cpu.Reg.D); }
        ),
        new Instruction(
            "SUB A,E", // 0x93
            static cpu => { DoSUB(cpu, cpu.Reg.E); }
        ),
        new Instruction(
            "SUB A,H", // 0x94
            static cpu => { DoSUB(cpu, cpu.Reg.H); }
        ),
        new Instruction(
            "SUB A,L", // 0x95
            static cpu => { DoSUB(cpu, cpu.Reg.L); }
        ),
        new Instruction(
            "SUB A,(HL)", // 0x96
            static cpu => { DoSUB(cpu, cpu.Read8(cpu.Reg.HL)); }
        ),
        new Instruction(
            "SUB A,A", // 0x97
            static cpu =>
            {
                cpu.Reg.A = 0;
                cpu.Reg.Zf = true;
                cpu.Reg.Nf = true;
                cpu.Reg.Hf = false;
                cpu.Reg.Cf = false;
            }
        ),
        new Instruction(
            "SBC A,B", // 0x98
            static cpu => { DoSBC(cpu, cpu.Reg.B); }
        ),
        new Instruction(
            "SBC A,C", // 0x99
            static cpu => { DoSBC(cpu, cpu.Reg.C); }
        ),
        new Instruction(
            "SBC A,D", // 0x9A
            static cpu => { DoSBC(cpu, cpu.Reg.D); }
        ),
        new Instruction(
            "SBC A,E", // 0x9B
            static cpu => { DoSBC(cpu, cpu.Reg.E); }
        ),
        new Instruction(
            "SBC A,H", // 0x9C
            static cpu => { DoSBC(cpu, cpu.Reg.H); }
        ),
        new Instruction(
            "SBC A,L", // 0x9D
            static cpu => { DoSBC(cpu, cpu.Reg.L); }
        ),
        new Instruction(
            "SBC A,(HL)", // 0x9E
            static cpu => { DoSBC(cpu, cpu.Read8(cpu.Reg.HL)); }
        ),
        new Instruction(
            "SBC A,A", // 0x9F
            static cpu => { DoSBC(cpu, cpu.Reg.A); }
        ),
        new Instruction(
            "AND A,B", // 0xA0
            static cpu => { DoAND(cpu, cpu.Reg.B); }
        ),
        new Instruction(
            "AND A,C", // 0xA1
            static cpu => { DoAND(cpu, cpu.Reg.C); }
        ),
        new Instruction(
            "AND A,D", // 0xA2
            static cpu => { DoAND(cpu, cpu.Reg.D); }
        ),
        new Instruction(
            "AND A,E", // 0xA3
            static cpu => { DoAND(cpu, cpu.Reg.E); }
        ),
        new Instruction(
            "AND A,H", // 0xA4
            static cpu => { DoAND(cpu, cpu.Reg.H); }
        ),
        new Instruction(
            "AND A,L", // 0xA5
            static cpu => { DoAND(cpu, cpu.Reg.L); }
        ),
        new Instruction(
            "AND A,(HL)", // 0xA6
            static cpu => { DoAND(cpu, cpu.Read8(cpu.Reg.HL)); }
        ),
        new Instruction(
            "AND A,A", // 0xA7
            static cpu =>
            {
                cpu.Reg.Zf = cpu.Reg.A == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
                cpu.Reg.Cf = false;
            }
        ),
        new Instruction(
            "XOR A,B", // 0xA8
            static cpu => { DoXOR(cpu, cpu.Reg.B); }
        ),
        new Instruction(
            "XOR A,C", // 0xA9
            static cpu => { DoXOR(cpu, cpu.Reg.C); }
        ),
        new Instruction(
            "XOR A,D", // 0xAA
            static cpu => { DoXOR(cpu, cpu.Reg.D); }
        ),
        new Instruction(
            "XOR A,E", // 0xAB
            static cpu => { DoXOR(cpu, cpu.Reg.E); }
        ),
        new Instruction(
            "XOR A,H", // 0xAC
            static cpu => { DoXOR(cpu, cpu.Reg.H); }
        ),
        new Instruction(
            "XOR A,L", // 0xAD
            static cpu => { DoXOR(cpu, cpu.Reg.L); }
        ),
        new Instruction(
            "XOR A,(HL)", // 0xAE
            static cpu => { DoXOR(cpu, cpu.Read8(cpu.Reg.HL)); }
        ),
        new Instruction(
            "XOR A,A", // 0xAF
            static cpu =>
            {
                cpu.Reg.A = 0;
                cpu.Reg.Zf = true;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = false;
                cpu.Reg.Cf = false;
            }
        ),
        new Instruction(
            "OR A,B", // 0xB0
            static cpu => { DoOR(cpu, cpu.Reg.B); }
        ),
        new Instruction(
            "OR A,C", // 0xB1
            static cpu => { DoOR(cpu, cpu.Reg.C); }
        ),
        new Instruction(
            "OR A,D", // 0xB2
            static cpu => { DoOR(cpu, cpu.Reg.D); }
        ),
        new Instruction(
            "OR A,E", // 0xB3
            static cpu => { DoOR(cpu, cpu.Reg.E); }
        ),
        new Instruction(
            "OR A,H", // 0xB4
            static cpu => { DoOR(cpu, cpu.Reg.H); }
        ),
        new Instruction(
            "OR A,L", // 0xB5
            static cpu => { DoOR(cpu, cpu.Reg.L); }
        ),
        new Instruction(
            "OR A,(HL)", // 0xB6
            static cpu => { DoOR(cpu, cpu.Read8(cpu.Reg.HL)); }
        ),
        new Instruction(
            "OR A,A", // 0xB7
            static cpu =>
            {
                cpu.Reg.Zf = cpu.Reg.A == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = false;
                cpu.Reg.Cf = false;
            }
        ),
        new Instruction(
            "CP A,B", // 0xB8
            static cpu =>
            {
                cpu.Reg.Zf = cpu.Reg.A == cpu.Reg.B;
                cpu.Reg.Nf = true;
                cpu.Reg.SetHfForDec(cpu.Reg.A, cpu.Reg.B);
                cpu.Reg.Cf = cpu.Reg.B > cpu.Reg.A;
            }
        ),
        new Instruction(
            "CP A,C", // 0xB9
            static cpu =>
            {
                cpu.Reg.Zf = cpu.Reg.A == cpu.Reg.C;
                cpu.Reg.Nf = true;
                cpu.Reg.SetHfForDec(cpu.Reg.A, cpu.Reg.C);
                cpu.Reg.Cf = cpu.Reg.C > cpu.Reg.A;
            }
        ),
        new Instruction(
            "CP A,D", // 0xBA
            static cpu =>
            {
                cpu.Reg.Zf = cpu.Reg.A == cpu.Reg.D;
                cpu.Reg.Nf = true;
                cpu.Reg.SetHfForDec(cpu.Reg.A, cpu.Reg.D);
                cpu.Reg.Cf = cpu.Reg.D > cpu.Reg.A;
            }
        ),
        new Instruction(
            "CP A,E", // 0xBB
            static cpu =>
            {
                cpu.Reg.Zf = cpu.Reg.A == cpu.Reg.E;
                cpu.Reg.Nf = true;
                cpu.Reg.SetHfForDec(cpu.Reg.A, cpu.Reg.E);
                cpu.Reg.Cf = cpu.Reg.E > cpu.Reg.A;
            }
        ),
        new Instruction(
            "CP A,H", // 0xBC
            static cpu =>
            {
                cpu.Reg.Zf = cpu.Reg.A == cpu.Reg.H;
                cpu.Reg.Nf = true;
                cpu.Reg.SetHfForDec(cpu.Reg.A, cpu.Reg.H);
                cpu.Reg.Cf = cpu.Reg.H > cpu.Reg.A;
            }
        ),
        new Instruction(
            "CP A,L", // 0xBD
            static cpu =>
            {
                cpu.Reg.Zf = cpu.Reg.A == cpu.Reg.L;
                cpu.Reg.Nf = true;
                cpu.Reg.SetHfForDec(cpu.Reg.A, cpu.Reg.L);
                cpu.Reg.Cf = cpu.Reg.L > cpu.Reg.A;
            }
        ),
        new Instruction(
            "CP A,(HL)", // 0xBE
            static cpu =>
            {
                var value = cpu.Read8(cpu.Reg.HL);
                cpu.Reg.Zf = cpu.Reg.A == value;
                cpu.Reg.Nf = true;
                cpu.Reg.SetHfForDec(cpu.Reg.A, value);
                cpu.Reg.Cf = value > cpu.Reg.A;
            }
        ),
        new Instruction(
            "CP A,A", // 0xBF
            static cpu =>
            {
                cpu.Reg.Zf = true;
                cpu.Reg.Nf = true;
                cpu.Reg.SetHfForDec(cpu.Reg.A, cpu.Reg.A);
                cpu.Reg.Cf = false;
            }
        ),
        new Instruction(
            "RET NZ", // 0xC0
            static cpu =>
            {
                cpu.InternalWaitM();
                if (cpu.Reg.Zf)
                    return;
                DoRET(cpu);
            }
        ),
        new Instruction(
            "POP BC", // 0xC1
            static cpu =>
            {
                cpu.Reg.C = cpu.Read8(cpu.Reg.SP++);
                cpu.Reg.B = cpu.Read8(cpu.Reg.SP++);
            }
        ),
        new Instruction(
            "JP NZ,a16", // 0xC2 nn nn
            static cpu =>
            {
                var target = cpu.Fetch16();
                if (cpu.Reg.Zf)
                    return;
                cpu.Reg.PC = target;
                cpu.InternalWaitM();
            }
        ),
        new Instruction(
            "JP a16", // 0xC3 nn nn
            static cpu =>
            {
                cpu.Reg.PC = cpu.Fetch16();
                cpu.InternalWaitM();
            }
        ),
        new Instruction(
            "CALL NZ,a16", // 0xC4 nn nn
            static cpu =>
            {
                var addr = cpu.Fetch16();
                if (cpu.Reg.Zf)
                    return; // No jump.
                cpu.PushPC();
                cpu.Reg.PC = addr;
                cpu.InternalWaitM();
            }
        ),
        new Instruction(
            "PUSH BC", // 0xC5
            static cpu =>
            {
                cpu.InternalWaitM();
                cpu.Write8(--cpu.Reg.SP, cpu.Reg.B);
                cpu.Write8(--cpu.Reg.SP, cpu.Reg.C);
            }
        ),
        new Instruction(
            "ADD A,nn", // 0xC6 nn
            static cpu => { DoADD(cpu, cpu.Fetch8()); }
        ),
        new Instruction(
            "RST $00", // 0xC7
            static cpu =>
            {
                cpu.InternalWaitM();
                cpu.PushPC();
                cpu.Reg.PC = 0x00;
            }
        ),
        new Instruction(
            "RET Z", // 0xC8
            static cpu =>
            {
                cpu.InternalWaitM();
                if (!cpu.Reg.Zf)
                    return;
                DoRET(cpu);
            }
        ),
        new Instruction(
            "RET", // 0xC9
            DoRET
        ),
        new Instruction(
            "JP Z,a16", // 0xCA nn nn
            static cpu =>
            {
                var target = cpu.Fetch16();
                if (!cpu.Reg.Zf)
                    return;
                cpu.Reg.PC = target;
                cpu.InternalWaitM();
            }
        ),
        null, // 0xCB
        new Instruction(
            "CALL Z,a16", // 0xCC nn nn
            static cpu =>
            {
                var addr = cpu.Fetch16();
                if (!cpu.Reg.Zf)
                    return; // No jump.
                cpu.PushPC();
                cpu.Reg.PC = addr;
                cpu.InternalWaitM();
            }
        ),
        new Instruction(
            "CALL a16", // 0xCD nn nn
            static cpu =>
            {
                var addr = cpu.Fetch16();
                cpu.PushPC();
                cpu.Reg.PC = addr;
                cpu.InternalWaitM();
            }
        ),
        new Instruction(
            "ADC A,nn", // 0xCE nn
            static cpu => { DoADC(cpu, cpu.Fetch8()); }
        ),
        new Instruction(
            "RST $08", // 0xCF
            static cpu =>
            {
                cpu.InternalWaitM();
                cpu.PushPC();
                cpu.Reg.PC = 0x08;
            }
        ),
        new Instruction(
            "RET NC", // 0xD0
            static cpu =>
            {
                cpu.InternalWaitM();
                if (cpu.Reg.Cf)
                    return;
                DoRET(cpu);
            }
        ),
        new Instruction(
            "POP DE", // 0xD1
            static cpu =>
            {
                cpu.Reg.E = cpu.Read8(cpu.Reg.SP++);
                cpu.Reg.D = cpu.Read8(cpu.Reg.SP++);
            }
        ),
        new Instruction(
            "JP NC,a16", // 0xD2 nn nn
            static cpu =>
            {
                var target = cpu.Fetch16();
                if (cpu.Reg.Cf)
                    return;
                cpu.Reg.PC = target;
                cpu.InternalWaitM();
            }
        ),
        new Instruction(
            "#INV_D3",
            _ => throw new InvalidOperationException("Invalid instruction.")),
        new Instruction(
            "CALL NC,a16", // 0xD4 nn nn
            static cpu =>
            {
                var addr = cpu.Fetch16();
                if (cpu.Reg.Cf)
                    return; // No jump.
                cpu.PushPC();
                cpu.Reg.PC = addr;
                cpu.InternalWaitM();
            }
        ),
        new Instruction(
            "PUSH DE", // 0xD5
            static cpu =>
            {
                cpu.InternalWaitM();
                cpu.Write8(--cpu.Reg.SP, cpu.Reg.D);
                cpu.Write8(--cpu.Reg.SP, cpu.Reg.E);
            }
        ),
        new Instruction(
            "SUB A,nn", // 0xD6 nn
            static cpu => { DoSUB(cpu, cpu.Fetch8()); }
        ),
        new Instruction(
            "RST $10", // 0xD7
            static cpu =>
            {
                cpu.InternalWaitM();
                cpu.PushPC();
                cpu.Reg.PC = 0x10;
            }
        ),
        new Instruction(
            "RET C", // 0xD8
            static cpu =>
            {
                cpu.InternalWaitM();
                if (!cpu.Reg.Cf)
                    return;
                DoRET(cpu);
            }
        ),
        new Instruction(
            "RETI", // 0xD9
            static cpu =>
            {
                DoRET(cpu);
                cpu.IME = true;
            }
        ),
        new Instruction(
            "JP C,a16", // 0xDA nn nn
            static cpu =>
            {
                var target = cpu.Fetch16();
                if (!cpu.Reg.Cf)
                    return;
                cpu.Reg.PC = target;
                cpu.InternalWaitM();
            }
        ),
        new Instruction(
            "#INV_DB",
            _ => throw new InvalidOperationException("Invalid instruction.")),
        new Instruction(
            "CALL C,a16", // 0xDC nn nn
            static cpu =>
            {
                var addr = cpu.Fetch16();
                if (!cpu.Reg.Cf)
                    return; // No jump.
                cpu.PushPC();
                cpu.Reg.PC = addr;
                cpu.InternalWaitM();
            }
        ),
        new Instruction(
            "#INV_DD",
            _ => throw new InvalidOperationException("Invalid instruction.")),
        new Instruction(
            "SBC A,nn", // 0xDE nn
            static cpu => { DoSBC(cpu, cpu.Fetch8()); }
        ),
        new Instruction(
            "RST $18", // 0xDF
            static cpu =>
            {
                cpu.InternalWaitM();
                cpu.PushPC();
                cpu.Reg.PC = 0x18;
            }
        ),
        new Instruction(
            "LDH (a8),A", // 0xE0 nn
            static cpu => { cpu.Write8((ushort)(0xFF00 + cpu.Fetch8()), cpu.Reg.A); }
        ),
        new Instruction(
            "POP HL", // 0xE1
            static cpu =>
            {
                cpu.Reg.L = cpu.Read8(cpu.Reg.SP++);
                cpu.Reg.H = cpu.Read8(cpu.Reg.SP++);
            }
        ),
        new Instruction(
            "LDH (C),A", // 0xE2
            static cpu => { cpu.Write8((ushort)(0xFF00 + cpu.Reg.C), cpu.Reg.A); }
        ),
        new Instruction(
            "#INV_E3",
            _ => throw new InvalidOperationException("Invalid instruction.")),
        new Instruction(
            "#INV_E4",
            _ => throw new InvalidOperationException("Invalid instruction.")),
        new Instruction(
            "PUSH HL", // 0xE5
            static cpu =>
            {
                cpu.InternalWaitM();
                cpu.Write8(--cpu.Reg.SP, cpu.Reg.H);
                cpu.Write8(--cpu.Reg.SP, cpu.Reg.L);
            }
        ),
        new Instruction(
            "AND A,nn", // 0xE6 nn
            static cpu => { DoAND(cpu, cpu.Fetch8()); }
        ),
        new Instruction(
            "RST $20", // 0xE7
            static cpu =>
            {
                cpu.InternalWaitM();
                cpu.PushPC();
                cpu.Reg.PC = 0x20;
            }
        ),
        new Instruction(
            "ADD SP,e8", // 0xE8 nn
            static cpu =>
            {
                var imm = cpu.Fetch8();
                var off = (sbyte)imm;
                var sp = cpu.Reg.SP;
                cpu.Reg.SP = (ushort)(sp + off);
                cpu.Reg.Zf = false;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = (sp & 0x000F) + (imm & 0x0F) > 0x0F;
                cpu.Reg.Cf = (sp & 0x00FF) + imm > 0xFF;
                cpu.InternalWaitM();
                cpu.InternalWaitM();
            }
        ),
        new Instruction(
            "JP HL", // 0xE9
            static cpu => { cpu.Reg.PC = cpu.Reg.HL; }
        ),
        new Instruction(
            "LD (a16),A", // 0xEA nn nn
            static cpu => { cpu.Write8(cpu.Fetch16(), cpu.Reg.A); }
        ),
        new Instruction(
            "#INV_EB",
            _ => throw new InvalidOperationException("Invalid instruction.")),
        new Instruction(
            "#INV_EC",
            _ => throw new InvalidOperationException("Invalid instruction.")),
        new Instruction(
            "#INV_ED",
            _ => throw new InvalidOperationException("Invalid instruction.")),
        new Instruction(
            "XOR A,nn", // 0xEE nn
            static cpu => { DoXOR(cpu, cpu.Fetch8()); }
        ),
        new Instruction(
            "RST $28", // 0xEF
            static cpu =>
            {
                cpu.InternalWaitM();
                cpu.PushPC();
                cpu.Reg.PC = 0x28;
            }
        ),
        new Instruction(
            "LDH A,(a8)", // 0xF0 nn
            static cpu => { cpu.Reg.A = cpu.Read8((ushort)(0xFF00 + cpu.Fetch8())); }
        ),
        new Instruction(
            "POP AF", // 0xF1
            static cpu =>
            {
                var restoredF = cpu.Read8(cpu.Reg.SP++);
                cpu.Reg.A = cpu.Read8(cpu.Reg.SP++);
                cpu.Reg.Zf = (restoredF & 0x80) != 0;
                cpu.Reg.Nf = (restoredF & 0x40) != 0;
                cpu.Reg.Hf = (restoredF & 0x20) != 0;
                cpu.Reg.Cf = (restoredF & 0x10) != 0;
            }
        ),
        new Instruction(
            "LDH A,(C)", // 0xF2
            static cpu => { cpu.Reg.A = cpu.Read8((ushort)(0xFF00 + cpu.Reg.C)); }
        ),
        new Instruction(
            "DI", // 0xF3
            static cpu =>
            {
                cpu.IME = false;
                cpu.PendingIME = false;
            }
        ),
        new Instruction(
            "#INV_F4",
            _ => throw new InvalidOperationException("Invalid instruction.")),
        new Instruction(
            "PUSH AF", // 0xF5
            static cpu =>
            {
                cpu.InternalWaitM();
                cpu.Write8(--cpu.Reg.SP, cpu.Reg.A);
                cpu.Write8(--cpu.Reg.SP, cpu.Reg.F);
            }
        ),
        new Instruction(
            "OR A,nn", // 0xF6 nn
            static cpu => { DoOR(cpu, cpu.Fetch8()); }
        ),
        new Instruction(
            "RST $30", // 0xF7
            static cpu =>
            {
                cpu.InternalWaitM();
                cpu.PushPC();
                cpu.Reg.PC = 0x30;
            }
        ),
        new Instruction(
            "LD HL,SP+e8", // 0xF8 nn
            static cpu =>
            {
                var imm = cpu.Fetch8();
                var sp = cpu.Reg.SP;
                var sum = sp + (sbyte)imm;
                cpu.Reg.HL = (ushort)sum;

                cpu.Reg.Zf = false;
                cpu.Reg.Nf = false;
                cpu.Reg.SetHfForInc((byte)sp, imm);
                cpu.Reg.Cf = (sp & 0xFF) + imm > 0xFF;

                cpu.InternalWaitM();
            }
        ),
        new Instruction(
            "LD SP,HL", // 0xF9
            static cpu =>
            {
                cpu.Reg.SP = cpu.Reg.HL;
                cpu.InternalWaitM();
            }
        ),
        new Instruction(
            "LD A,(a16)", // 0xFA nn nn
            static cpu => { cpu.Reg.A = cpu.Read8(cpu.Fetch16()); }
        ),
        new Instruction(
            "EI", // 0xFB
            static cpu => cpu.PendingIME = true),
        new Instruction(
            "#INV_FC",
            _ => throw new InvalidOperationException("Invalid instruction.")),
        new Instruction(
            "#INV_FD",
            _ => throw new InvalidOperationException("Invalid instruction.")),
        new Instruction(
            "CP A,nn", // 0xFE nn
            static cpu =>
            {
                var imm = cpu.Fetch8();
                cpu.Reg.Zf = cpu.Reg.A == imm;
                cpu.Reg.Nf = true;
                cpu.Reg.SetHfForDec(cpu.Reg.A, imm);
                cpu.Reg.Cf = imm > cpu.Reg.A;
            }
        ),
        new Instruction(
            "RST $38", // 0xFF
            static cpu =>
            {
                cpu.InternalWaitM();
                cpu.PushPC();
                cpu.Reg.PC = 0x38;
            }
        )
    ];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void DoRET(Cpu cpu)
    {
        var lo = cpu.Read8(cpu.Reg.SP++);
        var hi = cpu.Read8(cpu.Reg.SP++);
        cpu.Reg.PC = (ushort)(hi << 8 | lo);
        cpu.InternalWaitM();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void DoSBC(Cpu cpu, byte value)
    {
        var c = cpu.Reg.Cf ? 1 : 0;
        cpu.Reg.Nf = true;
        cpu.Reg.Hf = (cpu.Reg.A & 0xF) < (value & 0xF) + c;
        cpu.Reg.Cf = cpu.Reg.A < value + c;
        cpu.Reg.A = (byte)(cpu.Reg.A - value - c);
        cpu.Reg.Zf = cpu.Reg.A == 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void DoADC(Cpu cpu, byte value)
    {
        cpu.Reg.Hf = (cpu.Reg.A & 0xF) + (value & 0xF) + (cpu.Reg.Cf ? 1 : 0) > 0xF;
        var sum = cpu.Reg.A + value + (cpu.Reg.Cf ? 1 : 0);
        cpu.Reg.A = (byte)sum;
        cpu.Reg.Zf = cpu.Reg.A == 0;
        cpu.Reg.Nf = false;
        cpu.Reg.Cf = sum > 0xFF;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void DoADD(Cpu cpu, byte value)
    {
        cpu.Reg.SetHfForInc(cpu.Reg.A, value);
        var sum = cpu.Reg.A + value;
        cpu.Reg.A = (byte)sum;
        cpu.Reg.Zf = cpu.Reg.A == 0;
        cpu.Reg.Nf = false;
        cpu.Reg.Cf = sum > 0xFF;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void DoSUB(Cpu cpu, byte value)
    {
        cpu.Reg.SetHfForDec(cpu.Reg.A, value);
        cpu.Reg.Cf = cpu.Reg.A < value;
        cpu.Reg.A -= value;
        cpu.Reg.Zf = cpu.Reg.A == 0;
        cpu.Reg.Nf = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void DoINC(Cpu cpu, ref byte reg)
    {
        cpu.Reg.SetHfForInc(reg);
        reg++;
        cpu.Reg.Zf = reg == 0;
        cpu.Reg.Nf = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void DoDEC(Cpu cpu, ref byte reg)
    {
        cpu.Reg.SetHfForDec(reg);
        reg--;
        cpu.Reg.Zf = reg == 0;
        cpu.Reg.Nf = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void DoAND(Cpu cpu, byte value)
    {
        cpu.Reg.A &= value;
        cpu.Reg.Zf = cpu.Reg.A == 0;
        cpu.Reg.Nf = false;
        cpu.Reg.Hf = true;
        cpu.Reg.Cf = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void DoXOR(Cpu cpu, byte value)
    {
        cpu.Reg.A ^= value;
        cpu.Reg.Zf = cpu.Reg.A == 0;
        cpu.Reg.Nf = false;
        cpu.Reg.Hf = false;
        cpu.Reg.Cf = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void DoOR(Cpu cpu, byte value)
    {
        cpu.Reg.A |= value;
        cpu.Reg.Zf = cpu.Reg.A == 0;
        cpu.Reg.Nf = false;
        cpu.Reg.Hf = false;
        cpu.Reg.Cf = false;
    }
}
