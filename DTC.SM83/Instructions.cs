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

namespace DTC.SM83;

/// <summary>
/// Initially auto-generated from https://gbdev.io/gb-opcodes/Opcodes.json
/// </summary>
public static class Instructions
{
    public static readonly Instruction[] Table =
    [
        new Instruction(
            "NOP", // 0x00
            static _ => 4),
        new Instruction(
            "LD BC,nn", // 0x01 nn nn
            static cpu => {
                cpu.Reg.C = cpu.Fetch8();
                cpu.Reg.B = cpu.Fetch8();
                return 12;
            }
        ),
        new Instruction(
            "LD (BC),A", // 0x02
            static cpu => {
                cpu.Ram.Write8(cpu.Reg.BC, cpu.Reg.A);
                return 8;
            }
        ),
        new Instruction(
            "INC BC", // 0x03
            static cpu => {
                cpu.Reg.BC++;
                cpu.InternalWaitM();
                return 8;
            }
        ),
        new Instruction(
            "INC B", // 0x04
            static cpu => {
                cpu.Reg.SetHfForInc(cpu.Reg.B);
                cpu.Reg.B++;
                cpu.Reg.Zf = cpu.Reg.B == 0;
                cpu.Reg.Nf = false;
                return 4;
            }
        ),
        new Instruction(
            "DEC B", // 0x05
            static cpu => {
                cpu.Reg.SetHfForDec(cpu.Reg.B);
                cpu.Reg.B--;
                cpu.Reg.Zf = cpu.Reg.B == 0;
                cpu.Reg.Nf = true;
                return 4;
            }
        ),
        new Instruction(
            "LD B,nn", // 0x06 nn
            static cpu => {
                cpu.Reg.B = cpu.Fetch8();
                return 8;
            }
        ),
        new Instruction(
            "RLCA", // 0x07
            static cpu => {
                cpu.Reg.Cf = (cpu.Reg.A & 0x80) != 0;
                cpu.Reg.A = (byte)((cpu.Reg.A << 1) + (cpu.Reg.Cf ? 1 : 0));
                cpu.Reg.Zf = false;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = false;
                return 4;
            }
        ),
        new Instruction(
            "LD (a16),SP", // 0x08 nn nn
            static cpu => {
                cpu.Ram.Write16(cpu.Fetch16(), cpu.Reg.SP);
                return 20;
            }
        ),
        new Instruction(
            "ADD HL,BC", // 0x09
            static cpu => {
                cpu.Reg.Hf = (cpu.Reg.HL & 0x0FFF) + (cpu.Reg.BC & 0x0FFF) > 0x0FFF;
                var sum = (uint)cpu.Reg.HL + cpu.Reg.BC;
                cpu.Reg.Cf = sum > 0xFFFF;
                cpu.Reg.HL = (ushort)sum;
                cpu.Reg.Nf = false;
                cpu.InternalWaitM();
                return 8;
            }
        ),
        new Instruction(
            "LD A,(BC)", // 0x0A
            static cpu => {
                cpu.Reg.A = cpu.Ram.Read8(cpu.Reg.BC);
                return 8;
            }
        ),
        new Instruction(
            "DEC BC", // 0x0B
            static cpu => {
                cpu.Reg.BC--;
                cpu.InternalWaitM();
                return 8;
            }
        ),
        new Instruction(
            "INC C", // 0x0C
            static cpu => {
                cpu.Reg.SetHfForInc(cpu.Reg.C);
                cpu.Reg.C++;
                cpu.Reg.Zf = cpu.Reg.C == 0;
                cpu.Reg.Nf = false;
                return 4;
            }
        ),
        new Instruction(
            "DEC C", // 0x0D
            static cpu => {
                cpu.Reg.SetHfForDec(cpu.Reg.C);
                cpu.Reg.C--;
                cpu.Reg.Zf = cpu.Reg.C == 0;
                cpu.Reg.Nf = true;
                return 4;
            }
        ),
        new Instruction(
            "LD C,nn", // 0x0E nn
            static cpu => {
                cpu.Reg.C = cpu.Fetch8();
                return 8;
            }
        ),
        new Instruction(
            "RRCA", // 0x0F
            static cpu => {
                // todo

                cpu.Reg.Zf = false;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = false;
                cpu.Reg.Cf = false; // todo - Calculate
                return 4;
            }
        ),
        new Instruction(
            "STOP nn", // 0x10 nn
            static cpu => {
                // todo

                return 4;
            }
        ),
        new Instruction(
            "LD DE,nn", // 0x11 nn nn
            static cpu => {
                cpu.Reg.E = cpu.Fetch8();
                cpu.Reg.D = cpu.Fetch8();
                return 12;
            }
        ),
        new Instruction(
            "LD (DE),A", // 0x12
            static cpu => {
                cpu.Ram.Write8(cpu.Reg.DE, cpu.Reg.A);
                return 8;
            }
        ),
        new Instruction(
            "INC DE", // 0x13
            static cpu => {
                cpu.Reg.DE++;
                cpu.InternalWaitM();
                return 8;
            }
        ),
        new Instruction(
            "INC D", // 0x14
            static cpu => {
                cpu.Reg.SetHfForInc(cpu.Reg.D);
                cpu.Reg.D++;
                cpu.Reg.Zf = cpu.Reg.D == 0;
                cpu.Reg.Nf = false;
                return 4;
            }
        ),
        new Instruction(
            "DEC D", // 0x15
            static cpu => {
                cpu.Reg.SetHfForDec(cpu.Reg.D);
                cpu.Reg.D--;
                cpu.Reg.Zf = cpu.Reg.D == 0;
                cpu.Reg.Nf = true;
                return 4;
            }
        ),
        new Instruction(
            "LD D,nn", // 0x16 nn
            static cpu => {
                cpu.Reg.D = cpu.Fetch8();
                return 8;
            }
        ),
        new Instruction(
            "RLA", // 0x17
            static cpu => {
                // todo

                cpu.Reg.Zf = false;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = false;
                cpu.Reg.Cf = false; // todo - Calculate
                return 4;
            }
        ),
        new Instruction(
            "JR e8", // 0x18 nn
            static cpu =>
            {
                var diff = (sbyte)cpu.Fetch8();
                cpu.Reg.PC = (ushort) (cpu.Reg.PC + diff);
                cpu.InternalWaitM();
                return 12;
            }
        ),
        new Instruction(
            "ADD HL,DE", // 0x19
            static cpu => {
                cpu.Reg.Hf = (cpu.Reg.HL & 0x0FFF) + (cpu.Reg.DE & 0x0FFF) > 0x0FFF;
                var sum = (uint)cpu.Reg.HL + cpu.Reg.DE;
                cpu.Reg.Cf = sum > 0xFFFF;
                cpu.Reg.HL = (ushort)sum;
                cpu.Reg.Nf = false;
                cpu.InternalWaitM();
                return 8;
            }
        ),
        new Instruction(
            "LD A,(DE)", // 0x1A
            static cpu => {
                cpu.Reg.A = cpu.Ram.Read8(cpu.Reg.DE);
                return 8;
            }
        ),
        new Instruction(
            "DEC DE", // 0x1B
            static cpu => {
                cpu.Reg.DE--;
                cpu.InternalWaitM();
                return 8;
            }
        ),
        new Instruction(
            "INC E", // 0x1C
            static cpu => {
                cpu.Reg.SetHfForInc(cpu.Reg.E);
                cpu.Reg.E++;
                cpu.Reg.Zf = cpu.Reg.E == 0;
                cpu.Reg.Nf = false;
                return 4;
            }
        ),
        new Instruction(
            "DEC E", // 0x1D
            static cpu => {
                cpu.Reg.SetHfForDec(cpu.Reg.E);
                cpu.Reg.E--;
                cpu.Reg.Zf = cpu.Reg.E == 0;
                cpu.Reg.Nf = true;
                return 4;
            }
        ),
        new Instruction(
            "LD E,nn", // 0x1E nn
            static cpu => {
                cpu.Reg.E = cpu.Fetch8();
                return 8;
            }
        ),
        new Instruction(
            "RRA", // 0x1F
            static cpu => {
                // todo

                cpu.Reg.Zf = false;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = false;
                cpu.Reg.Cf = false; // todo - Calculate
                return 4;
            }
        ),
        new Instruction(
            "JR NZ,e8", // 0x20 nn
            static cpu => {
                var diff = (sbyte)cpu.Fetch8();
                if (cpu.Reg.Zf)
                    return 8;
                cpu.Reg.PC = (ushort) (cpu.Reg.PC + diff);
                cpu.InternalWaitM();
                return 12;
            }
        ),
        new Instruction(
            "LD HL,nn", // 0x21 nn nn
            static cpu => {
                cpu.Reg.L = cpu.Fetch8();
                cpu.Reg.H = cpu.Fetch8();
                return 12;
            }
        ),
        new Instruction(
            "LD (HL+),A", // 0x22
            static cpu => {
                cpu.Ram.Write8(cpu.Reg.HL, cpu.Reg.A);
                cpu.Reg.HL++;
                return 8;
            }
        ),
        new Instruction(
            "INC HL", // 0x23
            static cpu => {
                cpu.Reg.HL++;
                cpu.InternalWaitM();
                return 8;
            }
        ),
        new Instruction(
            "INC H", // 0x24
            static cpu => {
                cpu.Reg.SetHfForInc(cpu.Reg.H);
                cpu.Reg.H++;
                cpu.Reg.Zf = cpu.Reg.H == 0;
                cpu.Reg.Nf = false;
                return 4;
            }
        ),
        new Instruction(
            "DEC H", // 0x25
            static cpu => {
                cpu.Reg.SetHfForDec(cpu.Reg.H);
                cpu.Reg.H--;
                cpu.Reg.Zf = cpu.Reg.H == 0;
                cpu.Reg.Nf = true;
                return 4;
            }
        ),
        new Instruction(
            "LD H,nn", // 0x26 nn
            static cpu => {
                cpu.Reg.H = cpu.Fetch8();
                return 8;
            }
        ),
        new Instruction(
            "DAA", // 0x27
            static cpu => {
                // todo

                cpu.Reg.Zf = false; // todo - Calculate
                cpu.Reg.Hf = false;
                cpu.Reg.Cf = false; // todo - Calculate
                return 4;
            }
        ),
        new Instruction(
            "JR Z,e8", // 0x28 nn
            static cpu => {
                var diff = (sbyte)cpu.Fetch8();
                if (!cpu.Reg.Zf)
                    return 8;
                cpu.Reg.PC = (ushort) (cpu.Reg.PC + diff);
                cpu.InternalWaitM();
                return 12;
            }
        ),
        new Instruction(
            "ADD HL,HL", // 0x29
            static cpu => {
                cpu.Reg.Hf = (cpu.Reg.HL & 0x0FFF) + (cpu.Reg.HL & 0x0FFF) > 0x0FFF;
                var sum = (uint)cpu.Reg.HL + cpu.Reg.HL;
                cpu.Reg.Cf = sum > 0xFFFF;
                cpu.Reg.HL = (ushort)sum;
                cpu.Reg.Nf = false;
                cpu.InternalWaitM();
                return 8;
            }
        ),
        new Instruction(
            "LD A,(HL+)", // 0x2A
            static cpu => {
                cpu.Reg.A = cpu.Ram.Read8(cpu.Reg.HL);
                cpu.Reg.HL++;
                return 8;
            }
        ),
        new Instruction(
            "DEC HL", // 0x2B
            static cpu => {
                cpu.Reg.HL--;
                cpu.InternalWaitM();
                return 8;
            }
        ),
        new Instruction(
            "INC L", // 0x2C
            static cpu => {
                cpu.Reg.SetHfForInc(cpu.Reg.L);
                cpu.Reg.L++;
                cpu.Reg.Zf = cpu.Reg.L == 0;
                cpu.Reg.Nf = false;
                return 4;
            }
        ),
        new Instruction(
            "DEC L", // 0x2D
            static cpu => {
                cpu.Reg.SetHfForDec(cpu.Reg.L);
                cpu.Reg.L--;
                cpu.Reg.Zf = cpu.Reg.L == 0;
                cpu.Reg.Nf = true;
                return 4;
            }
        ),
        new Instruction(
            "LD L,nn", // 0x2E nn
            static cpu => {
                cpu.Reg.L = cpu.Fetch8();
                return 8;
            }
        ),
        new Instruction(
            "CPL", // 0x2F
            static cpu => {
                // todo

                cpu.Reg.Nf = true;
                cpu.Reg.Hf = true;
                return 4;
            }
        ),
        new Instruction(
            "JR NC,e8", // 0x30 nn
            static cpu => {
                var diff = (sbyte)cpu.Fetch8();
                if (cpu.Reg.Cf)
                    return 8;
                cpu.Reg.PC = (ushort) (cpu.Reg.PC + diff);
                cpu.InternalWaitM();
                return 12;
            }
        ),
        new Instruction(
            "LD SP,nn", // 0x31 nn nn
            static cpu => {
                cpu.Reg.SP = cpu.Fetch16();
                return 12;
            }
        ),
        new Instruction(
            "LD (HL-),A", // 0x32
            static cpu => {
                cpu.Ram.Write8(cpu.Reg.HL, cpu.Reg.A);
                cpu.Reg.HL--;
                return 8;
            }
        ),
        new Instruction(
            "INC SP", // 0x33
            static cpu => {
                cpu.Reg.SP++;
                cpu.InternalWaitM();
                return 8;
            }
        ),
        new Instruction(
            "INC (HL)", // 0x34
            static cpu => {
                var value = cpu.Ram.Read8(cpu.Reg.HL);
                cpu.Reg.SetHfForInc(value);
                cpu.Ram.Write8(cpu.Reg.HL, (byte)(value + 1));
                cpu.Reg.Zf = value == 0;
                cpu.Reg.Nf = false;
                return 12;
            }
        ),
        new Instruction(
            "DEC (HL)", // 0x35
            static cpu => {
                var value = cpu.Ram.Read8(cpu.Reg.HL);
                cpu.Reg.SetHfForDec(value);
                cpu.Ram.Write8(cpu.Reg.HL, (byte)(value - 1));
                cpu.Reg.Zf = value == 0;
                cpu.Reg.Nf = true;
                return 12;
            }
        ),
        new Instruction(
            "LD (HL),nn", // 0x36 nn
            static cpu => {
                cpu.Ram.Write8(cpu.Reg.HL, cpu.Fetch8());
                return 12;
            }
        ),
        new Instruction(
            "SCF", // 0x37
            static cpu => {
                // todo

                cpu.Reg.Nf = false;
                cpu.Reg.Hf = false;
                cpu.Reg.Cf = true;
                return 4;
            }
        ),
        new Instruction(
            "JR C,e8", // 0x38 nn
            static cpu => {
                var diff = (sbyte)cpu.Fetch8();
                if (!cpu.Reg.Cf)
                    return 8;
                cpu.Reg.PC = (ushort) (cpu.Reg.PC + diff);
                cpu.InternalWaitM();
                return 12;
            }
        ),
        new Instruction(
            "ADD HL,SP", // 0x39
            static cpu => {
                cpu.Reg.Hf = (cpu.Reg.HL & 0x0FFF) + (cpu.Reg.SP & 0x0FFF) > 0x0FFF;
                var sum = (uint)cpu.Reg.HL + cpu.Reg.SP;
                cpu.Reg.Cf = sum > 0xFFFF;
                cpu.Reg.HL = (ushort)sum;
                cpu.Reg.Nf = false;
                cpu.InternalWaitM();
                return 8;
            }
        ),
        new Instruction(
            "LD A,(HL-)", // 0x3A
            static cpu => {
                cpu.Reg.A = cpu.Ram.Read8(cpu.Reg.HL);
                cpu.Reg.HL--;
                return 8;
            }
        ),
        new Instruction(
            "DEC SP", // 0x3B
            static cpu => {
                cpu.Reg.SP--;
                cpu.InternalWaitM();
                return 8;
            }
        ),
        new Instruction(
            "INC A", // 0x3C
            static cpu => {
                cpu.Reg.SetHfForInc(cpu.Reg.A);
                cpu.Reg.A++;
                cpu.Reg.Zf = cpu.Reg.A == 0;
                cpu.Reg.Nf = false;
                return 4;
            }
        ),
        new Instruction(
            "DEC A", // 0x3D
            static cpu => {
                cpu.Reg.SetHfForDec(cpu.Reg.A);
                cpu.Reg.A--;
                cpu.Reg.Zf = cpu.Reg.A == 0;
                cpu.Reg.Nf = true;
                return 4;
            }
        ),
        new Instruction(
            "LD A,nn", // 0x3E nn
            static cpu => {
                cpu.Reg.A = cpu.Fetch8();
                return 8;
            }
        ),
        new Instruction(
            "CCF", // 0x3F
            static cpu => {
                // todo

                cpu.Reg.Nf = false;
                cpu.Reg.Hf = false;
                cpu.Reg.Cf = false; // todo - Calculate
                return 4;
            }
        ),
        new Instruction(
            "LD B,B", // 0x40
            static _ => 4),
        new Instruction(
            "LD B,C", // 0x41
            static cpu => {
                cpu.Reg.B = cpu.Reg.C;
                return 4;
            }
        ),
        new Instruction(
            "LD B,D", // 0x42
            static cpu => {
                cpu.Reg.B = cpu.Reg.D;
                return 4;
            }
        ),
        new Instruction(
            "LD B,E", // 0x43
            static cpu => {
                cpu.Reg.B = cpu.Reg.E;
                return 4;
            }
        ),
        new Instruction(
            "LD B,H", // 0x44
            static cpu => {
                cpu.Reg.B = cpu.Reg.H;
                return 4;
            }
        ),
        new Instruction(
            "LD B,L", // 0x45
            static cpu => {
                cpu.Reg.B = cpu.Reg.L;
                return 4;
            }
        ),
        new Instruction(
            "LD B,(HL)", // 0x46
            static cpu => {
                cpu.Reg.B = cpu.Ram.Read8(cpu.Reg.HL);
                return 8;
            }
        ),
        new Instruction(
            "LD B,A", // 0x47
            static cpu => {
                cpu.Reg.B = cpu.Reg.A;
                return 4;
            }
        ),
        new Instruction(
            "LD C,B", // 0x48
            static cpu => {
                cpu.Reg.C = cpu.Reg.B;
                return 4;
            }
        ),
        new Instruction(
            "LD C,C", // 0x49
            static _ => 4),
        new Instruction(
            "LD C,D", // 0x4A
            static cpu => {
                cpu.Reg.C = cpu.Reg.D;
                return 4;
            }
        ),
        new Instruction(
            "LD C,E", // 0x4B
            static cpu => {
                cpu.Reg.C = cpu.Reg.E;
                return 4;
            }
        ),
        new Instruction(
            "LD C,H", // 0x4C
            static cpu => {
                cpu.Reg.C = cpu.Reg.H;
                return 4;
            }
        ),
        new Instruction(
            "LD C,L", // 0x4D
            static cpu => {
                cpu.Reg.C = cpu.Reg.L;
                return 4;
            }
        ),
        new Instruction(
            "LD C,(HL)", // 0x4E
            static cpu => {
                cpu.Reg.C = cpu.Ram.Read8(cpu.Reg.HL);
                return 8;
            }
        ),
        new Instruction(
            "LD C,A", // 0x4F
            static cpu => {
                cpu.Reg.C = cpu.Reg.A;
                return 4;
            }
        ),
        new Instruction(
            "LD D,B", // 0x50
            static cpu => {
                cpu.Reg.D = cpu.Reg.B;
                return 4;
            }
        ),
        new Instruction(
            "LD D,C", // 0x51
            static cpu => {
                cpu.Reg.D = cpu.Reg.C;
                return 4;
            }
        ),
        new Instruction(
            "LD D,D", // 0x52
            static _ => 4),
        new Instruction(
            "LD D,E", // 0x53
            static cpu => {
                cpu.Reg.D = cpu.Reg.E;
                return 4;
            }
        ),
        new Instruction(
            "LD D,H", // 0x54
            static cpu => {
                cpu.Reg.D = cpu.Reg.H;
                return 4;
            }
        ),
        new Instruction(
            "LD D,L", // 0x55
            static cpu => {
                cpu.Reg.D = cpu.Reg.L;
                return 4;
            }
        ),
        new Instruction(
            "LD D,(HL)", // 0x56
            static cpu => {
                cpu.Reg.D = cpu.Ram.Read8(cpu.Reg.HL);
                return 8;
            }
        ),
        new Instruction(
            "LD D,A", // 0x57
            static cpu => {
                cpu.Reg.D = cpu.Reg.A;
                return 4;
            }
        ),
        new Instruction(
            "LD E,B", // 0x58
            static cpu => {
                cpu.Reg.E = cpu.Reg.B;
                return 4;
            }
        ),
        new Instruction(
            "LD E,C", // 0x59
            static cpu => {
                cpu.Reg.E = cpu.Reg.C;
                return 4;
            }
        ),
        new Instruction(
            "LD E,D", // 0x5A
            static cpu => {
                cpu.Reg.E = cpu.Reg.D;
                return 4;
            }
        ),
        new Instruction(
            "LD E,E", // 0x5B
            static _ => 4),
        new Instruction(
            "LD E,H", // 0x5C
            static cpu => {
                cpu.Reg.E = cpu.Reg.H;
                return 4;
            }
        ),
        new Instruction(
            "LD E,L", // 0x5D
            static cpu => {
                cpu.Reg.E = cpu.Reg.L;
                return 4;
            }
        ),
        new Instruction(
            "LD E,(HL)", // 0x5E
            static cpu => {
                cpu.Reg.E = cpu.Ram.Read8(cpu.Reg.HL);
                return 8;
            }
        ),
        new Instruction(
            "LD E,A", // 0x5F
            static cpu => {
                cpu.Reg.E = cpu.Reg.A;
                return 4;
            }
        ),
        new Instruction(
            "LD H,B", // 0x60
            static cpu => {
                cpu.Reg.H = cpu.Reg.B;
                return 4;
            }
        ),
        new Instruction(
            "LD H,C", // 0x61
            static cpu => {
                cpu.Reg.H = cpu.Reg.C;
                return 4;
            }
        ),
        new Instruction(
            "LD H,D", // 0x62
            static cpu => {
                cpu.Reg.H = cpu.Reg.D;
                return 4;
            }
        ),
        new Instruction(
            "LD H,E", // 0x63
            static cpu => {
                cpu.Reg.H = cpu.Reg.E;
                return 4;
            }
        ),
        new Instruction(
            "LD H,H", // 0x64
            static _ => 4),
        new Instruction(
            "LD H,L", // 0x65
            static cpu => {
                cpu.Reg.H = cpu.Reg.L;
                return 4;
            }
        ),
        new Instruction(
            "LD H,(HL)", // 0x66
            static cpu => {
                cpu.Reg.H = cpu.Ram.Read8(cpu.Reg.HL);
                return 8;
            }
        ),
        new Instruction(
            "LD H,A", // 0x67
            static cpu => {
                cpu.Reg.H = cpu.Reg.A;
                return 4;
            }
        ),
        new Instruction(
            "LD L,B", // 0x68
            static cpu => {
                cpu.Reg.L = cpu.Reg.B;
                return 4;
            }
        ),
        new Instruction(
            "LD L,C", // 0x69
            static cpu => {
                cpu.Reg.L = cpu.Reg.C;
                return 4;
            }
        ),
        new Instruction(
            "LD L,D", // 0x6A
            static cpu => {
                cpu.Reg.L = cpu.Reg.D;
                return 4;
            }
        ),
        new Instruction(
            "LD L,E", // 0x6B
            static cpu => {
                cpu.Reg.L = cpu.Reg.E;
                return 4;
            }
        ),
        new Instruction(
            "LD L,H", // 0x6C
            static cpu => {
                cpu.Reg.L = cpu.Reg.H;
                return 4;
            }
        ),
        new Instruction(
            "LD L,L", // 0x6D
            static _ => 4),
        new Instruction(
            "LD L,(HL)", // 0x6E
            static cpu => {
                cpu.Reg.L = cpu.Ram.Read8(cpu.Reg.HL);
                return 8;
            }
        ),
        new Instruction(
            "LD L,A", // 0x6F
            static cpu => {
                cpu.Reg.L = cpu.Reg.A;
                return 4;
            }
        ),
        new Instruction(
            "LD (HL),B", // 0x70
            static cpu => {
                cpu.Ram.Write8(cpu.Reg.HL, cpu.Reg.B);
                return 8;
            }
        ),
        new Instruction(
            "LD (HL),C", // 0x71
            static cpu => {
                cpu.Ram.Write8(cpu.Reg.HL, cpu.Reg.C);
                return 8;
            }
        ),
        new Instruction(
            "LD (HL),D", // 0x72
            static cpu => {
                cpu.Ram.Write8(cpu.Reg.HL, cpu.Reg.D);
                return 8;
            }
        ),
        new Instruction(
            "LD (HL),E", // 0x73
            static cpu => {
                cpu.Ram.Write8(cpu.Reg.HL, cpu.Reg.E);
                return 8;
            }
        ),
        new Instruction(
            "LD (HL),H", // 0x74
            static cpu => {
                cpu.Ram.Write8(cpu.Reg.HL, cpu.Reg.H);
                return 8;
            }
        ),
        new Instruction(
            "LD (HL),L", // 0x75
            static cpu => {
                cpu.Ram.Write8(cpu.Reg.HL, cpu.Reg.L);
                return 8;
            }
        ),
        new Instruction(
            "HALT", // 0x76
            static cpu => {
                // todo

                return 4;
            }
        ),
        new Instruction(
            "LD (HL),A", // 0x77
            static cpu => {
                cpu.Ram.Write8(cpu.Reg.HL, cpu.Reg.A);
                return 8;
            }
        ),
        new Instruction(
            "LD A,B", // 0x78
            static cpu => {
                cpu.Reg.A = cpu.Reg.B;
                return 4;
            }
        ),
        new Instruction(
            "LD A,C", // 0x79
            static cpu => {
                cpu.Reg.A = cpu.Reg.C;
                return 4;
            }
        ),
        new Instruction(
            "LD A,D", // 0x7A
            static cpu => {
                cpu.Reg.A = cpu.Reg.D;
                return 4;
            }
        ),
        new Instruction(
            "LD A,E", // 0x7B
            static cpu => {
                cpu.Reg.A = cpu.Reg.E;
                return 4;
            }
        ),
        new Instruction(
            "LD A,H", // 0x7C
            static cpu => {
                cpu.Reg.A = cpu.Reg.H;
                return 4;
            }
        ),
        new Instruction(
            "LD A,L", // 0x7D
            static cpu => {
                cpu.Reg.A = cpu.Reg.L;
                return 4;
            }
        ),
        new Instruction(
            "LD A,(HL)", // 0x7E
            static cpu => {
                cpu.Reg.A = cpu.Ram.Read8(cpu.Reg.HL);
                return 8;
            }
        ),
        new Instruction(
            "LD A,A", // 0x7F
            static _ => 4),
        new Instruction(
            "ADD A,B", // 0x80
            static cpu => {
                cpu.Reg.SetHfForInc(cpu.Reg.A, cpu.Reg.B);
                var sum = cpu.Reg.A + cpu.Reg.B;
                cpu.Reg.A = (byte)sum;
                
                cpu.Reg.Zf = cpu.Reg.A == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Cf = sum > 0xFF;
                return 4;
            }
        ),
        new Instruction(
            "ADD A,C", // 0x81
            static cpu => {
                cpu.Reg.SetHfForInc(cpu.Reg.A, cpu.Reg.C);
                var sum = cpu.Reg.A + cpu.Reg.C;
                cpu.Reg.A = (byte)sum;

                cpu.Reg.Zf = cpu.Reg.A == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Cf = sum > 0xFF;
                return 4;
            }
        ),
        new Instruction(
            "ADD A,D", // 0x82
            static cpu => {
                cpu.Reg.SetHfForInc(cpu.Reg.A, cpu.Reg.D);
                var sum = cpu.Reg.A + cpu.Reg.D;
                cpu.Reg.A = (byte)sum;

                cpu.Reg.Zf = cpu.Reg.A == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Cf = sum > 0xFF;
                return 4;
            }
        ),
        new Instruction(
            "ADD A,E", // 0x83
            static cpu => {
                cpu.Reg.SetHfForInc(cpu.Reg.A, cpu.Reg.E);
                var sum = cpu.Reg.A + cpu.Reg.E;
                cpu.Reg.A = (byte)sum;

                cpu.Reg.Zf = cpu.Reg.A == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Cf = sum > 0xFF;
                return 4;
            }
        ),
        new Instruction(
            "ADD A,H", // 0x84
            static cpu => {
                cpu.Reg.SetHfForInc(cpu.Reg.A, cpu.Reg.H);
                var sum = cpu.Reg.A + cpu.Reg.H;
                cpu.Reg.A = (byte)sum;

                cpu.Reg.Zf = cpu.Reg.A == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Cf = sum > 0xFF;
                return 4;
            }
        ),
        new Instruction(
            "ADD A,L", // 0x85
            static cpu => {
                cpu.Reg.SetHfForInc(cpu.Reg.A, cpu.Reg.L);
                var sum = cpu.Reg.A + cpu.Reg.L;
                cpu.Reg.A = (byte)sum;

                cpu.Reg.Zf = cpu.Reg.A == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Cf = sum > 0xFF;
                return 4;
            }
        ),
        new Instruction(
            "ADD A,(HL)", // 0x86
            static cpu => {
                var inc = cpu.Ram.Read8(cpu.Reg.HL);
                cpu.Reg.SetHfForInc(cpu.Reg.A, inc);
                var sum = cpu.Reg.A + inc;
                cpu.Reg.A = (byte)sum;

                cpu.Reg.Zf = cpu.Reg.A == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Cf = sum > 0xFF;
                return 8;
            }
        ),
        new Instruction(
            "ADD A,A", // 0x87
            static cpu => {
                cpu.Reg.SetHfForInc(cpu.Reg.A, cpu.Reg.A);
                var sum = cpu.Reg.A + cpu.Reg.A;
                cpu.Reg.A = (byte)sum;

                cpu.Reg.Zf = cpu.Reg.A == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Cf = sum > 0xFF;
                return 4;
            }
        ),
        new Instruction(
            "ADC A,B", // 0x88
            static cpu => {
                // todo

                cpu.Reg.Zf = false; // todo - Calculate
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = false; // todo - Calculate
                cpu.Reg.Cf = false; // todo - Calculate
                return 4;
            }
        ),
        new Instruction(
            "ADC A,C", // 0x89
            static cpu => {
                // todo

                cpu.Reg.Zf = false; // todo - Calculate
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = false; // todo - Calculate
                cpu.Reg.Cf = false; // todo - Calculate
                return 4;
            }
        ),
        new Instruction(
            "ADC A,D", // 0x8A
            static cpu => {
                // todo

                cpu.Reg.Zf = false; // todo - Calculate
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = false; // todo - Calculate
                cpu.Reg.Cf = false; // todo - Calculate
                return 4;
            }
        ),
        new Instruction(
            "ADC A,E", // 0x8B
            static cpu => {
                // todo

                cpu.Reg.Zf = false; // todo - Calculate
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = false; // todo - Calculate
                cpu.Reg.Cf = false; // todo - Calculate
                return 4;
            }
        ),
        new Instruction(
            "ADC A,H", // 0x8C
            static cpu => {
                // todo

                cpu.Reg.Zf = false; // todo - Calculate
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = false; // todo - Calculate
                cpu.Reg.Cf = false; // todo - Calculate
                return 4;
            }
        ),
        new Instruction(
            "ADC A,L", // 0x8D
            static cpu => {
                // todo

                cpu.Reg.Zf = false; // todo - Calculate
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = false; // todo - Calculate
                cpu.Reg.Cf = false; // todo - Calculate
                return 4;
            }
        ),
        new Instruction(
            "ADC A,(HL)", // 0x8E
            static cpu => {
                // todo

                cpu.Reg.Zf = false; // todo - Calculate
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = false; // todo - Calculate
                cpu.Reg.Cf = false; // todo - Calculate
                return 8;
            }
        ),
        new Instruction(
            "ADC A,A", // 0x8F
            static cpu => {
                // todo

                cpu.Reg.Zf = false; // todo - Calculate
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = false; // todo - Calculate
                cpu.Reg.Cf = false; // todo - Calculate
                return 4;
            }
        ),
        new Instruction(
            "SUB A,B", // 0x90
            static cpu => {
                cpu.Reg.SetHfForDec(cpu.Reg.A, cpu.Reg.B);
                cpu.Reg.Cf = cpu.Reg.A < cpu.Reg.B;
                cpu.Reg.A -= cpu.Reg.B;
                cpu.Reg.Zf = cpu.Reg.A == 0;
                cpu.Reg.Nf = true;
                return 4;
            }
        ),
        new Instruction(
            "SUB A,C", // 0x91
            static cpu => {
                cpu.Reg.SetHfForDec(cpu.Reg.A, cpu.Reg.C);
                cpu.Reg.Cf = cpu.Reg.A < cpu.Reg.C;
                cpu.Reg.A -= cpu.Reg.C;
                cpu.Reg.Zf = cpu.Reg.A == 0;
                cpu.Reg.Nf = true;
                return 4;
            }
        ),
        new Instruction(
            "SUB A,D", // 0x92
            static cpu => {
                cpu.Reg.SetHfForDec(cpu.Reg.A, cpu.Reg.D);
                cpu.Reg.Cf = cpu.Reg.A < cpu.Reg.D;
                cpu.Reg.A -= cpu.Reg.D;
                cpu.Reg.Zf = cpu.Reg.A == 0;
                cpu.Reg.Nf = true;
                return 4;
            }
        ),
        new Instruction(
            "SUB A,E", // 0x93
            static cpu => {
                cpu.Reg.SetHfForDec(cpu.Reg.A, cpu.Reg.E);
                cpu.Reg.Cf = cpu.Reg.A < cpu.Reg.E;
                cpu.Reg.A -= cpu.Reg.E;
                cpu.Reg.Zf = cpu.Reg.A == 0;
                cpu.Reg.Nf = true;
                return 4;
            }
        ),
        new Instruction(
            "SUB A,H", // 0x94
            static cpu => {
                cpu.Reg.SetHfForDec(cpu.Reg.A, cpu.Reg.H);
                cpu.Reg.Cf = cpu.Reg.A < cpu.Reg.H;
                cpu.Reg.A -= cpu.Reg.H;
                cpu.Reg.Zf = cpu.Reg.A == 0;
                cpu.Reg.Nf = true;
                return 4;
            }
        ),
        new Instruction(
            "SUB A,L", // 0x95
            static cpu => {
                cpu.Reg.SetHfForDec(cpu.Reg.A, cpu.Reg.L);
                cpu.Reg.Cf = cpu.Reg.A < cpu.Reg.L;
                cpu.Reg.A -= cpu.Reg.L;
                cpu.Reg.Zf = cpu.Reg.A == 0;
                cpu.Reg.Nf = true;
                return 4;
            }
        ),
        new Instruction(
            "SUB A,(HL)", // 0x96
            static cpu => {
                var value = cpu.Ram.Read8(cpu.Reg.HL);
                cpu.Reg.SetHfForDec(cpu.Reg.A, value);
                cpu.Reg.Cf = cpu.Reg.A < value;
                cpu.Reg.A -= value;
                cpu.Reg.Zf = cpu.Reg.A == 0;
                cpu.Reg.Nf = true;
                return 8;
            }
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
                return 4;
            }
        ),
        new Instruction(
            "SBC A,B", // 0x98
            static cpu => {
                // todo

                cpu.Reg.Zf = false; // todo - Calculate
                cpu.Reg.Nf = true;
                cpu.Reg.Hf = false; // todo - Calculate
                cpu.Reg.Cf = false; // todo - Calculate
                return 4;
            }
        ),
        new Instruction(
            "SBC A,C", // 0x99
            static cpu => {
                // todo

                cpu.Reg.Zf = false; // todo - Calculate
                cpu.Reg.Nf = true;
                cpu.Reg.Hf = false; // todo - Calculate
                cpu.Reg.Cf = false; // todo - Calculate
                return 4;
            }
        ),
        new Instruction(
            "SBC A,D", // 0x9A
            static cpu => {
                // todo

                cpu.Reg.Zf = false; // todo - Calculate
                cpu.Reg.Nf = true;
                cpu.Reg.Hf = false; // todo - Calculate
                cpu.Reg.Cf = false; // todo - Calculate
                return 4;
            }
        ),
        new Instruction(
            "SBC A,E", // 0x9B
            static cpu => {
                // todo

                cpu.Reg.Zf = false; // todo - Calculate
                cpu.Reg.Nf = true;
                cpu.Reg.Hf = false; // todo - Calculate
                cpu.Reg.Cf = false; // todo - Calculate
                return 4;
            }
        ),
        new Instruction(
            "SBC A,H", // 0x9C
            static cpu => {
                // todo

                cpu.Reg.Zf = false; // todo - Calculate
                cpu.Reg.Nf = true;
                cpu.Reg.Hf = false; // todo - Calculate
                cpu.Reg.Cf = false; // todo - Calculate
                return 4;
            }
        ),
        new Instruction(
            "SBC A,L", // 0x9D
            static cpu => {
                // todo

                cpu.Reg.Zf = false; // todo - Calculate
                cpu.Reg.Nf = true;
                cpu.Reg.Hf = false; // todo - Calculate
                cpu.Reg.Cf = false; // todo - Calculate
                return 4;
            }
        ),
        new Instruction(
            "SBC A,(HL)", // 0x9E
            static cpu => {
                // todo

                cpu.Reg.Zf = false; // todo - Calculate
                cpu.Reg.Nf = true;
                cpu.Reg.Hf = false; // todo - Calculate
                cpu.Reg.Cf = false; // todo - Calculate
                return 8;
            }
        ),
        new Instruction(
            "SBC A,A", // 0x9F
            static cpu => {
                // todo

                cpu.Reg.Zf = false; // todo - Calculate
                cpu.Reg.Nf = true;
                cpu.Reg.Hf = false; // todo - Calculate
                return 4;
            }
        ),
        new Instruction(
            "AND A,B", // 0xA0
            static cpu =>
            {
                cpu.Reg.A &= cpu.Reg.B;
                cpu.Reg.Zf = cpu.Reg.A == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
                cpu.Reg.Cf = false;
                return 4;
            }
        ),
        new Instruction(
            "AND A,C", // 0xA1
            static cpu => {
                cpu.Reg.A &= cpu.Reg.C;
                cpu.Reg.Zf = cpu.Reg.A == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
                cpu.Reg.Cf = false;
                return 4;
            }
        ),
        new Instruction(
            "AND A,D", // 0xA2
            static cpu => {
                cpu.Reg.A &= cpu.Reg.D;
                cpu.Reg.Zf = cpu.Reg.A == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
                cpu.Reg.Cf = false;
                return 4;
            }
        ),
        new Instruction(
            "AND A,E", // 0xA3
            static cpu => {
                cpu.Reg.A &= cpu.Reg.E;
                cpu.Reg.Zf = cpu.Reg.A == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
                cpu.Reg.Cf = false;
                return 4;
            }
        ),
        new Instruction(
            "AND A,H", // 0xA4
            static cpu => {
                cpu.Reg.A &= cpu.Reg.H;
                cpu.Reg.Zf = cpu.Reg.A == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
                cpu.Reg.Cf = false;
                return 4;
            }
        ),
        new Instruction(
            "AND A,L", // 0xA5
            static cpu => {
                cpu.Reg.A &= cpu.Reg.L;
                cpu.Reg.Zf = cpu.Reg.A == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
                cpu.Reg.Cf = false;
                return 4;
            }
        ),
        new Instruction(
            "AND A,(HL)", // 0xA6
            static cpu => {
                cpu.Reg.A &= cpu.Ram.Read8(cpu.Reg.HL);
                cpu.Reg.Zf = cpu.Reg.A == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
                cpu.Reg.Cf = false;
                return 8;
            }
        ),
        new Instruction(
            "AND A,A", // 0xA7
            static cpu =>
            {
                cpu.Reg.Zf = cpu.Reg.A == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
                cpu.Reg.Cf = false;
                return 4;
            }
        ),
        new Instruction(
            "XOR A,B", // 0xA8
            static cpu => {
                cpu.Reg.A ^= cpu.Reg.B;
                cpu.Reg.Zf = cpu.Reg.A == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = false;
                cpu.Reg.Cf = false;
                return 4;
            }
        ),
        new Instruction(
            "XOR A,C", // 0xA9
            static cpu => {
                cpu.Reg.A ^= cpu.Reg.C;
                cpu.Reg.Zf = cpu.Reg.A == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = false;
                cpu.Reg.Cf = false;
                return 4;
            }
        ),
        new Instruction(
            "XOR A,D", // 0xAA
            static cpu => {
                cpu.Reg.A ^= cpu.Reg.D;
                cpu.Reg.Zf = cpu.Reg.A == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = false;
                cpu.Reg.Cf = false;
                return 4;
            }
        ),
        new Instruction(
            "XOR A,E", // 0xAB
            static cpu => {
                cpu.Reg.A ^= cpu.Reg.E;
                cpu.Reg.Zf = cpu.Reg.A == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = false;
                cpu.Reg.Cf = false;
                return 4;
            }
        ),
        new Instruction(
            "XOR A,H", // 0xAC
            static cpu => {
                cpu.Reg.A ^= cpu.Reg.H;
                cpu.Reg.Zf = cpu.Reg.A == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = false;
                cpu.Reg.Cf = false;
                return 4;
            }
        ),
        new Instruction(
            "XOR A,L", // 0xAD
            static cpu => {
                cpu.Reg.A ^= cpu.Reg.L;
                cpu.Reg.Zf = cpu.Reg.A == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = false;
                cpu.Reg.Cf = false;
                return 4;
            }
        ),
        new Instruction(
            "XOR A,(HL)", // 0xAE
            static cpu => {
                cpu.Reg.A ^= cpu.Ram.Read8(cpu.Reg.HL);
                cpu.Reg.Zf = cpu.Reg.A == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = false;
                cpu.Reg.Cf = false;
                return 8;
            }
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
                return 4;
            }
        ),
        new Instruction(
            "OR A,B", // 0xB0
            static cpu => {
                cpu.Reg.A |= cpu.Reg.B;
                cpu.Reg.Zf = cpu.Reg.A == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = false;
                cpu.Reg.Cf = false;
                return 4;
            }
        ),
        new Instruction(
            "OR A,C", // 0xB1
            static cpu => {
                cpu.Reg.A |= cpu.Reg.C;
                cpu.Reg.Zf = cpu.Reg.A == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = false;
                cpu.Reg.Cf = false;
                return 4;
            }
        ),
        new Instruction(
            "OR A,D", // 0xB2
            static cpu => {
                cpu.Reg.A |= cpu.Reg.D;
                cpu.Reg.Zf = cpu.Reg.A == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = false;
                cpu.Reg.Cf = false;
                return 4;
            }
        ),
        new Instruction(
            "OR A,E", // 0xB3
            static cpu => {
                cpu.Reg.A |= cpu.Reg.E;
                cpu.Reg.Zf = cpu.Reg.A == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = false;
                cpu.Reg.Cf = false;
                return 4;
            }
        ),
        new Instruction(
            "OR A,H", // 0xB4
            static cpu => {
                cpu.Reg.A |= cpu.Reg.H;
                cpu.Reg.Zf = cpu.Reg.A == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = false;
                cpu.Reg.Cf = false;
                return 4;
            }
        ),
        new Instruction(
            "OR A,L", // 0xB5
            static cpu => {
                cpu.Reg.A |= cpu.Reg.L;
                cpu.Reg.Zf = cpu.Reg.A == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = false;
                cpu.Reg.Cf = false;
                return 4;
            }
        ),
        new Instruction(
            "OR A,(HL)", // 0xB6
            static cpu => {
                cpu.Reg.A |= cpu.Ram.Read8(cpu.Reg.HL);
                cpu.Reg.Zf = cpu.Reg.A == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = false;
                cpu.Reg.Cf = false;
                return 8;
            }
        ),
        new Instruction(
            "OR A,A", // 0xB7
            static cpu => {
                cpu.Reg.Zf = cpu.Reg.A == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = false;
                cpu.Reg.Cf = false;
                return 4;
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
                return 4;
            }
        ),
        new Instruction(
            "CP A,C", // 0xB9
            static cpu => {
                cpu.Reg.Zf = cpu.Reg.A == cpu.Reg.C;
                cpu.Reg.Nf = true;
                cpu.Reg.SetHfForDec(cpu.Reg.A, cpu.Reg.C);
                cpu.Reg.Cf = cpu.Reg.C > cpu.Reg.A;
                return 4;
            }
        ),
        new Instruction(
            "CP A,D", // 0xBA
            static cpu => {
                cpu.Reg.Zf = cpu.Reg.A == cpu.Reg.D;
                cpu.Reg.Nf = true;
                cpu.Reg.SetHfForDec(cpu.Reg.A, cpu.Reg.D);
                cpu.Reg.Cf = cpu.Reg.D > cpu.Reg.A;
                return 4;
            }
        ),
        new Instruction(
            "CP A,E", // 0xBB
            static cpu => {
                cpu.Reg.Zf = cpu.Reg.A == cpu.Reg.E;
                cpu.Reg.Nf = true;
                cpu.Reg.SetHfForDec(cpu.Reg.A, cpu.Reg.E);
                cpu.Reg.Cf = cpu.Reg.E > cpu.Reg.A;
                return 4;
            }
        ),
        new Instruction(
            "CP A,H", // 0xBC
            static cpu => {
                cpu.Reg.Zf = cpu.Reg.A == cpu.Reg.H;
                cpu.Reg.Nf = true;
                cpu.Reg.SetHfForDec(cpu.Reg.A, cpu.Reg.H);
                cpu.Reg.Cf = cpu.Reg.H > cpu.Reg.A;
                return 4;
            }
        ),
        new Instruction(
            "CP A,L", // 0xBD
            static cpu => {
                cpu.Reg.Zf = cpu.Reg.A == cpu.Reg.L;
                cpu.Reg.Nf = true;
                cpu.Reg.SetHfForDec(cpu.Reg.A, cpu.Reg.L);
                cpu.Reg.Cf = cpu.Reg.L > cpu.Reg.A;
                return 4;
            }
        ),
        new Instruction(
            "CP A,(HL)", // 0xBE
            static cpu => {
                var value = cpu.Ram.Read8(cpu.Reg.HL);
                cpu.Reg.Zf = cpu.Reg.A == value;
                cpu.Reg.Nf = true;
                cpu.Reg.SetHfForDec(cpu.Reg.A, value);
                cpu.Reg.Cf = value > cpu.Reg.A;
                return 8;
            }
        ),
        new Instruction(
            "CP A,A", // 0xBF
            static cpu => {
                cpu.Reg.Zf = true;
                cpu.Reg.Nf = true;
                cpu.Reg.SetHfForDec(cpu.Reg.A, cpu.Reg.B);
                cpu.Reg.Cf = false;
                return 4;
            }
        ),
        new Instruction(
            "RET NZ", // 0xC0
            static cpu => {
                cpu.InternalWaitM();
                if (cpu.Reg.Zf)
                    return 8;
                cpu.Reg.PC = cpu.Ram.Read16(cpu.Reg.SP);
                cpu.Reg.SP += 2;
                cpu.InternalWaitM();
                return 20;
            }
        ),
        new Instruction(
            "POP BC", // 0xC1
            static cpu => {
                cpu.Reg.BC = cpu.Ram.Read16(cpu.Reg.SP);
                cpu.Reg.SP += 2;
                return 12;
            }
        ),
        new Instruction(
            "JP NZ,a16", // 0xC2 nn nn
            static cpu =>
            {
                var target = cpu.Fetch16();
                if (cpu.Reg.Zf)
                    return 12;
                cpu.Reg.PC = target;
                cpu.InternalWaitM();
                return 16;
            }
        ),
        new Instruction(
            "JP a16", // 0xC3 nn nn
            static cpu => {
                cpu.Reg.PC = cpu.Ram.Read16(cpu.Reg.PC);
                cpu.InternalWaitM();
                return 16;
            }
        ),
        new Instruction(
            "CALL NZ,a16", // 0xC4 nn nn
            static cpu => {
                var addr = cpu.Fetch16();
                if (cpu.Reg.Zf)
                    return 12; // No jump.
                cpu.Reg.SP -= 2;
                cpu.Ram.Write16(cpu.Reg.SP, cpu.Reg.PC);
                cpu.Reg.PC = addr;
                cpu.InternalWaitM();
                return 24;
            }
        ),
        new Instruction(
            "PUSH BC", // 0xC5
            static cpu =>
            {
                cpu.Reg.SP -= 2;
                cpu.Ram.Write16(cpu.Reg.SP, cpu.Reg.BC);
                cpu.InternalWaitM();
                return 16;
            }
        ),
        new Instruction(
            "ADD A,nn", // 0xC6 nn
            static cpu =>
            {
                var imm = cpu.Fetch8();
                cpu.Reg.SetHfForInc(cpu.Reg.A, imm);
                var sum = cpu.Reg.A + imm;
                cpu.Reg.A = (byte)sum;
                
                cpu.Reg.Zf = cpu.Reg.A == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Cf = sum > 0xFF;
                return 8;
            }
        ),
        new Instruction(
            "RST $00", // 0xC7
            static cpu => {
                cpu.InternalWaitM();
                cpu.Reg.SP -= 2;
                cpu.Ram.Write16(cpu.Reg.SP, cpu.Reg.PC);
                cpu.Reg.PC = 0x00;
                return 16;
            }
        ),
        new Instruction(
            "RET Z", // 0xC8
            static cpu => {
                cpu.InternalWaitM();
                if (!cpu.Reg.Zf)
                    return 8;
                cpu.Reg.PC = cpu.Ram.Read16(cpu.Reg.SP);
                cpu.Reg.SP += 2;
                cpu.InternalWaitM();
                return 20;
            }
        ),
        new Instruction(
            "RET", // 0xC9
            static cpu =>
            {
                cpu.Reg.PC = cpu.Ram.Read16(cpu.Reg.SP);
                cpu.Reg.SP += 2;
                cpu.InternalWaitM();
                return 16;
            }
        ),
        new Instruction(
            "JP Z,a16", // 0xCA nn nn
            static cpu => {
                var target = cpu.Fetch16();
                if (!cpu.Reg.Zf)
                    return 12;
                cpu.Reg.PC = target;
                cpu.InternalWaitM();
                return 16;
            }
        ),
        null, // 0xCB
        new Instruction(
            "CALL Z,a16", // 0xCC nn nn
            static cpu => {
                var addr = cpu.Fetch16();
                if (!cpu.Reg.Zf)
                    return 12; // No jump.
                cpu.Reg.SP -= 2;
                cpu.Ram.Write16(cpu.Reg.SP, cpu.Reg.PC);
                cpu.Reg.PC = addr;
                cpu.InternalWaitM();
                return 24;            }
        ),
        new Instruction(
            "CALL a16", // 0xCD nn nn
            static cpu =>
            {
                var addr = cpu.Fetch16();
                cpu.Reg.SP -= 2;
                cpu.Ram.Write16(cpu.Reg.SP, cpu.Reg.PC);
                cpu.Reg.PC = addr;
                cpu.InternalWaitM();
                return 24;
            }
        ),
        new Instruction(
            "ADC A,nn", // 0xCE nn
            static cpu => {
                // todo

                cpu.Reg.Zf = false; // todo - Calculate
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = false; // todo - Calculate
                cpu.Reg.Cf = false; // todo - Calculate
                return 8;
            }
        ),
        new Instruction(
            "RST $08", // 0xCF
            static cpu => {
                cpu.InternalWaitM();
                cpu.Reg.SP -= 2;
                cpu.Ram.Write16(cpu.Reg.SP, cpu.Reg.PC);
                cpu.Reg.PC = 0x08;
                return 16;
            }
        ),
        new Instruction(
            "RET NC", // 0xD0
            static cpu => {
                cpu.InternalWaitM();
                if (cpu.Reg.Cf)
                    return 8;
                cpu.Reg.PC = cpu.Ram.Read16(cpu.Reg.SP);
                cpu.Reg.SP += 2;
                cpu.InternalWaitM();
                return 20;
            }
        ),
        new Instruction(
            "POP DE", // 0xD1
            static cpu => {
                cpu.Reg.DE = cpu.Ram.Read16(cpu.Reg.SP);
                cpu.Reg.SP += 2;
                return 12;
            }
        ),
        new Instruction(
            "JP NC,a16", // 0xD2 nn nn
            static cpu => {
                var target = cpu.Fetch16();
                if (cpu.Reg.Cf)
                    return 12;
                cpu.Reg.PC = target;
                cpu.InternalWaitM();
                return 16;
            }
        ),
        null, // 0xD3
        new Instruction(
            "CALL NC,a16", // 0xD4 nn nn
            static cpu => {
                var addr = cpu.Fetch16();
                if (cpu.Reg.Cf)
                    return 12; // No jump.
                cpu.Reg.SP -= 2;
                cpu.Ram.Write16(cpu.Reg.SP, cpu.Reg.PC);
                cpu.Reg.PC = addr;
                cpu.InternalWaitM();
                return 24;            }
        ),
        new Instruction(
            "PUSH DE", // 0xD5
            static cpu => {
                cpu.Reg.SP -= 2;
                cpu.Ram.Write16(cpu.Reg.SP, cpu.Reg.DE);
                cpu.InternalWaitM();
                return 16;
            }
        ),
        new Instruction(
            "SUB A,nn", // 0xD6 nn
            static cpu => {
                var value = cpu.Fetch8();
                cpu.Reg.SetHfForDec(cpu.Reg.A, value);
                cpu.Reg.Cf = cpu.Reg.A < value;
                cpu.Reg.A -= value;
                cpu.Reg.Zf = cpu.Reg.A == 0;
                cpu.Reg.Nf = true;
                return 8;
            }
        ),
        new Instruction(
            "RST $10", // 0xD7
            static cpu => {
                cpu.InternalWaitM();
                cpu.Reg.SP -= 2;
                cpu.Ram.Write16(cpu.Reg.SP, cpu.Reg.PC);
                cpu.Reg.PC = 0x10;
                return 16;
            }
        ),
        new Instruction(
            "RET C", // 0xD8
            static cpu => {
                cpu.InternalWaitM();
                if (!cpu.Reg.Cf)
                    return 8;
                cpu.Reg.PC = cpu.Ram.Read16(cpu.Reg.SP);
                cpu.Reg.SP += 2;
                cpu.InternalWaitM();
                return 20;
            }
        ),
        new Instruction(
            "RETI", // 0xD9
            static cpu => {
                // todo

                return 16;
            }
        ),
        new Instruction(
            "JP C,a16", // 0xDA nn nn
            static cpu => {
                var target = cpu.Fetch16();
                if (!cpu.Reg.Cf)
                    return 12;
                cpu.Reg.PC = target;
                cpu.InternalWaitM();
                return 16;
            }
        ),
        null, // 0xDB
        new Instruction(
            "CALL C,a16", // 0xDC nn nn
            static cpu => {
                var addr = cpu.Fetch16();
                if (!cpu.Reg.Cf)
                    return 12; // No jump.
                cpu.Reg.SP -= 2;
                cpu.Ram.Write16(cpu.Reg.SP, cpu.Reg.PC);
                cpu.Reg.PC = addr;
                cpu.InternalWaitM();
                return 24;
            }
        ),
        null, // 0xDD
        new Instruction(
            "SBC A,nn", // 0xDE nn
            static cpu => {
                // todo

                cpu.Reg.Zf = false; // todo - Calculate
                cpu.Reg.Nf = true;
                cpu.Reg.Hf = false; // todo - Calculate
                cpu.Reg.Cf = false; // todo - Calculate
                return 8;
            }
        ),
        new Instruction(
            "RST $18", // 0xDF
            static cpu => {
                cpu.InternalWaitM();
                cpu.Reg.SP -= 2;
                cpu.Ram.Write16(cpu.Reg.SP, cpu.Reg.PC);
                cpu.Reg.PC = 0x18;
                return 16;
            }
        ),
        new Instruction(
            "LDH (a8),A", // 0xE0 nn
            static cpu => {
                cpu.Ram.Write8((ushort)(0xFF00 + cpu.Fetch8()), cpu.Reg.A);;
                return 12;
            }
        ),
        new Instruction(
            "POP HL", // 0xE1
            static cpu => {
                cpu.Reg.HL = cpu.Ram.Read16(cpu.Reg.SP);
                cpu.Reg.SP += 2;
                return 12;
            }
        ),
        new Instruction(
            "LDH (C),A", // 0xE2
            static cpu => {
                cpu.Ram.Write8((ushort)(0xFF00 + cpu.Reg.C), cpu.Reg.A);
                return 8;
            }
        ),
        null, // 0xE3
        null, // 0xE4
        new Instruction(
            "PUSH HL", // 0xE5
            static cpu => {
                cpu.Reg.SP -= 2;
                cpu.Ram.Write16(cpu.Reg.SP, cpu.Reg.HL);
                cpu.InternalWaitM();
                return 16;
            }
        ),
        new Instruction(
            "AND A,nn", // 0xE6 nn
            static cpu => {
                cpu.Reg.A &= cpu.Fetch8();
                cpu.Reg.Zf = cpu.Reg.A == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
                cpu.Reg.Cf = false;
                return 8;
            }
        ),
        new Instruction(
            "RST $20", // 0xE7
            static cpu => {
                cpu.InternalWaitM();
                cpu.Reg.SP -= 2;
                cpu.Ram.Write16(cpu.Reg.SP, cpu.Reg.PC);
                cpu.Reg.PC = 0x20;
                return 16;
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
                cpu.InternalWaitM(2);
                return 16;
            }
        ),
        new Instruction(
            "JP HL", // 0xE9
            static cpu => {
                cpu.Reg.PC = cpu.Reg.HL;
                return 4;
            }
        ),
        new Instruction(
            "LD (a16),A", // 0xEA nn nn
            static cpu => {
                cpu.Ram.Write8(cpu.Fetch16(), cpu.Reg.A);
                return 16;
            }
        ),
        null, // 0xEB
        null, // 0xEC
        null, // 0xED
        new Instruction(
            "XOR A,nn", // 0xEE nn
            static cpu => {
                cpu.Reg.A ^= cpu.Fetch8();
                cpu.Reg.Zf = cpu.Reg.A == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = false;
                cpu.Reg.Cf = false;
                return 8;
            }
        ),
        new Instruction(
            "RST $28", // 0xEF
            static cpu => {
                cpu.InternalWaitM();
                cpu.Reg.SP -= 2;
                cpu.Ram.Write16(cpu.Reg.SP, cpu.Reg.PC);
                cpu.Reg.PC = 0x28;
                return 16;
            }
        ),
        new Instruction(
            "LDH A,(a8)", // 0xF0 nn
            static cpu => {
                cpu.Reg.A = cpu.Ram.Read8((ushort)(0xFF00 + cpu.Fetch8()));
                return 12;
            }
        ),
        new Instruction(
            "POP AF", // 0xF1
            static cpu => {
                var restoredF = cpu.Ram.Read8(cpu.Reg.SP++);
                cpu.Reg.A = cpu.Ram.Read8(cpu.Reg.SP++);
                cpu.Reg.Zf = (restoredF & 0x80) != 0;
                cpu.Reg.Nf = (restoredF & 0x40) != 0;
                cpu.Reg.Hf = (restoredF & 0x20) != 0;
                cpu.Reg.Cf = (restoredF & 0x10) != 0;
                return 12;
            }
        ),
        new Instruction(
            "LDH A,(C)", // 0xF2
            static cpu => {
                cpu.Reg.A = cpu.Ram.Read8((ushort)(0xFF00 + cpu.Reg.C));
                return 8;
            }
        ),
        new Instruction(
            "DI", // 0xF3
            static cpu => {
                // todo

                return 4;
            }
        ),
        null, // 0xF4
        new Instruction(
            "PUSH AF", // 0xF5
            static cpu => {
                cpu.Ram.Write8(--cpu.Reg.SP, cpu.Reg.A);
                cpu.Ram.Write8(--cpu.Reg.SP, cpu.Reg.F);
                cpu.InternalWaitM();
                return 16;
            }
        ),
        new Instruction(
            "OR A,nn", // 0xF6 nn
            static cpu => {
                cpu.Reg.A |= cpu.Fetch8();
                cpu.Reg.Zf = cpu.Reg.A == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = false;
                cpu.Reg.Cf = false;
                return 8;
            }
        ),
        new Instruction(
            "RST $30", // 0xF7
            static cpu => {
                cpu.InternalWaitM();
                cpu.Reg.SP -= 2;
                cpu.Ram.Write16(cpu.Reg.SP, cpu.Reg.PC);
                cpu.Reg.PC = 0x30;
                return 16;
            }
        ),
        new Instruction(
            "LD HL,SP+e8", // 0xF8 nn
            static cpu => {
                var imm = cpu.Fetch8();
                var sp = cpu.Reg.SP;
                var sum = sp + (sbyte)imm;
                cpu.Reg.HL = (ushort)sum;

                cpu.Reg.Zf = false;
                cpu.Reg.Nf = false;
                cpu.Reg.SetHfForInc((byte)sp, imm);
                cpu.Reg.Cf = (sp & 0xFF) + imm > 0xFF;
                
                cpu.InternalWaitM();
                return 12;
            }
        ),
        new Instruction(
            "LD SP,HL", // 0xF9
            static cpu => {
                cpu.Reg.SP = cpu.Reg.HL;
                cpu.InternalWaitM();
                return 8;
            }
        ),
        new Instruction(
            "LD A,(a16)", // 0xFA nn nn
            static cpu => {
                cpu.Reg.A = cpu.Ram.Read8(cpu.Fetch16());
                return 16;
            }
        ),
        new Instruction(
            "EI", // 0xFB
            static cpu => {
                // todo

                return 4;
            }
        ),
        null, // 0xFC
        null, // 0xFD
        new Instruction(
            "CP A,nn", // 0xFE nn
            static cpu => {
                var imm = cpu.Fetch8();
                cpu.Reg.Zf = cpu.Reg.A == imm;
                cpu.Reg.Nf = true;
                cpu.Reg.SetHfForDec(cpu.Reg.A, imm);
                cpu.Reg.Cf = imm > cpu.Reg.A;
                return 8;
            }
        ),
        new Instruction(
            "RST $38", // 0xFF
            static cpu => {
                cpu.InternalWaitM();
                cpu.Reg.SP -= 2;
                cpu.Ram.Write16(cpu.Reg.SP, cpu.Reg.PC);
                cpu.Reg.PC = 0x38;
                return 16;
            }
        )
    ];
}