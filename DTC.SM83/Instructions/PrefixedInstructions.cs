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
public static class PrefixedInstructions
{
    public static readonly Instruction[] Table =
    [
        new Instruction(
            "RLC B", // 0x00
            static cpu => { DoRLC(cpu, ref cpu.Reg.B); }
        ),
        new Instruction(
            "RLC C", // 0x01
            static cpu => { DoRLC(cpu, ref cpu.Reg.C); }
        ),
        new Instruction(
            "RLC D", // 0x02
            static cpu => { DoRLC(cpu, ref cpu.Reg.D); }
        ),
        new Instruction(
            "RLC E", // 0x03
            static cpu => { DoRLC(cpu, ref cpu.Reg.E); }
        ),
        new Instruction(
            "RLC H", // 0x04
            static cpu => { DoRLC(cpu, ref cpu.Reg.H); }
        ),
        new Instruction(
            "RLC L", // 0x05
            static cpu => { DoRLC(cpu, ref cpu.Reg.L); }
        ),
        new Instruction(
            "RLC (HL)", // 0x06
            static cpu =>
            {
                var value = cpu.Read8(cpu.Reg.HL);
                DoRLC(cpu, ref value);
                cpu.Write8(cpu.Reg.HL, value);
            }
        ),
        new Instruction(
            "RLC A", // 0x07
            static cpu => { DoRLC(cpu, ref cpu.Reg.A); }
        ),
        new Instruction(
            "RRC B", // 0x08
            static cpu => { DoRRC(cpu, ref cpu.Reg.B); }
        ),
        new Instruction(
            "RRC C", // 0x09
            static cpu => { DoRRC(cpu, ref cpu.Reg.C); }
        ),
        new Instruction(
            "RRC D", // 0x0A
            static cpu => { DoRRC(cpu, ref cpu.Reg.D); }
        ),
        new Instruction(
            "RRC E", // 0x0B
            static cpu => { DoRRC(cpu, ref cpu.Reg.E); }
        ),
        new Instruction(
            "RRC H", // 0x0C
            static cpu => { DoRRC(cpu, ref cpu.Reg.H); }
        ),
        new Instruction(
            "RRC L", // 0x0D
            static cpu => { DoRRC(cpu, ref cpu.Reg.L); }
        ),
        new Instruction(
            "RRC (HL)", // 0x0E
            static cpu =>
            {
                var value = cpu.Read8(cpu.Reg.HL);
                DoRRC(cpu, ref value);
                cpu.Write8(cpu.Reg.HL, value);
            }
        ),
        new Instruction(
            "RRC A", // 0x0F
            static cpu => { DoRRC(cpu, ref cpu.Reg.A); }
        ),
        new Instruction(
            "RL B", // 0x10
            static cpu => { DoRL(cpu, ref cpu.Reg.B); }
        ),
        new Instruction(
            "RL C", // 0x11
            static cpu => { DoRL(cpu, ref cpu.Reg.C); }
        ),
        new Instruction(
            "RL D", // 0x12
            static cpu => { DoRL(cpu, ref cpu.Reg.D); }
        ),
        new Instruction(
            "RL E", // 0x13
            static cpu => { DoRL(cpu, ref cpu.Reg.E); }
        ),
        new Instruction(
            "RL H", // 0x14
            static cpu => { DoRL(cpu, ref cpu.Reg.H); }
        ),
        new Instruction(
            "RL L", // 0x15
            static cpu => { DoRL(cpu, ref cpu.Reg.L); }
        ),
        new Instruction(
            "RL (HL)", // 0x16
            static cpu =>
            {
                var value = cpu.Read8(cpu.Reg.HL);
                DoRL(cpu, ref value);
                cpu.Write8(cpu.Reg.HL, value);
            }
        ),
        new Instruction(
            "RL A", // 0x17
            static cpu => { DoRL(cpu, ref cpu.Reg.A); }
        ),
        new Instruction(
            "RR B", // 0x18
            static cpu => { DoRR(cpu, ref cpu.Reg.B); }
        ),
        new Instruction(
            "RR C", // 0x19
            static cpu => { DoRR(cpu, ref cpu.Reg.C); }
        ),
        new Instruction(
            "RR D", // 0x1A
            static cpu => { DoRR(cpu, ref cpu.Reg.D); }
        ),
        new Instruction(
            "RR E", // 0x1B
            static cpu => { DoRR(cpu, ref cpu.Reg.E); }
        ),
        new Instruction(
            "RR H", // 0x1C
            static cpu => { DoRR(cpu, ref cpu.Reg.H); }
        ),
        new Instruction(
            "RR L", // 0x1D
            static cpu => { DoRR(cpu, ref cpu.Reg.L); }
        ),
        new Instruction(
            "RR (HL)", // 0x1E
            static cpu =>
            {
                var value = cpu.Read8(cpu.Reg.HL);
                DoRR(cpu, ref value);
                cpu.Write8(cpu.Reg.HL, value);
            }
        ),
        new Instruction(
            "RR A", // 0x1F
            static cpu => { DoRR(cpu, ref cpu.Reg.A); }
        ),
        new Instruction(
            "SLA B", // 0x20
            static cpu => { DoSLA(cpu, ref cpu.Reg.B); }
        ),
        new Instruction(
            "SLA C", // 0x21
            static cpu => { DoSLA(cpu, ref cpu.Reg.C); }
        ),
        new Instruction(
            "SLA D", // 0x22
            static cpu => { DoSLA(cpu, ref cpu.Reg.D); }
        ),
        new Instruction(
            "SLA E", // 0x23
            static cpu => { DoSLA(cpu, ref cpu.Reg.E); }
        ),
        new Instruction(
            "SLA H", // 0x24
            static cpu => { DoSLA(cpu, ref cpu.Reg.H); }
        ),
        new Instruction(
            "SLA L", // 0x25
            static cpu => { DoSLA(cpu, ref cpu.Reg.L); }
        ),
        new Instruction(
            "SLA (HL)", // 0x26
            static cpu =>
            {
                var value = cpu.Read8(cpu.Reg.HL);
                DoSLA(cpu, ref value);
                cpu.Write8(cpu.Reg.HL, value);
            }
        ),
        new Instruction(
            "SLA A", // 0x27
            static cpu => { DoSLA(cpu, ref cpu.Reg.A); }
        ),
        new Instruction(
            "SRA B", // 0x28
            static cpu => { DoSRA(cpu, ref cpu.Reg.B); }
        ),
        new Instruction(
            "SRA C", // 0x29
            static cpu => { DoSRA(cpu, ref cpu.Reg.C); }
        ),
        new Instruction(
            "SRA D", // 0x2A
            static cpu => { DoSRA(cpu, ref cpu.Reg.D); }
        ),
        new Instruction(
            "SRA E", // 0x2B
            static cpu => { DoSRA(cpu, ref cpu.Reg.E); }
        ),
        new Instruction(
            "SRA H", // 0x2C
            static cpu => { DoSRA(cpu, ref cpu.Reg.H); }
        ),
        new Instruction(
            "SRA L", // 0x2D
            static cpu => { DoSRA(cpu, ref cpu.Reg.L); }
        ),
        new Instruction(
            "SRA (HL)", // 0x2E
            static cpu =>
            {
                var value = cpu.Read8(cpu.Reg.HL);
                DoSRA(cpu, ref value);
                cpu.Write8(cpu.Reg.HL, value);
            }
        ),
        new Instruction(
            "SRA A", // 0x2F
            static cpu => { DoSRA(cpu, ref cpu.Reg.A); }
        ),
        new Instruction(
            "SWAP B", // 0x30
            static cpu => { DoSWAP(cpu, ref cpu.Reg.B); }
        ),
        new Instruction(
            "SWAP C", // 0x31
            static cpu => { DoSWAP(cpu, ref cpu.Reg.C); }
        ),
        new Instruction(
            "SWAP D", // 0x32
            static cpu => { DoSWAP(cpu, ref cpu.Reg.D); }
        ),
        new Instruction(
            "SWAP E", // 0x33
            static cpu => { DoSWAP(cpu, ref cpu.Reg.E); }
        ),
        new Instruction(
            "SWAP H", // 0x34
            static cpu => { DoSWAP(cpu, ref cpu.Reg.H); }
        ),
        new Instruction(
            "SWAP L", // 0x35
            static cpu => { DoSWAP(cpu, ref cpu.Reg.L); }
        ),
        new Instruction(
            "SWAP (HL)", // 0x36
            static cpu =>
            {
                var value = cpu.Read8(cpu.Reg.HL);
                DoSWAP(cpu, ref value);
                cpu.Write8(cpu.Reg.HL, value);
            }
        ),
        new Instruction(
            "SWAP A", // 0x37
            static cpu => { DoSWAP(cpu, ref cpu.Reg.A); }
        ),
        new Instruction(
            "SRL B", // 0x38
            static cpu => { DoSRL(cpu, ref cpu.Reg.B); }
        ),
        new Instruction(
            "SRL C", // 0x39
            static cpu => { DoSRL(cpu, ref cpu.Reg.C); }
        ),
        new Instruction(
            "SRL D", // 0x3A
            static cpu => { DoSRL(cpu, ref cpu.Reg.D); }
        ),
        new Instruction(
            "SRL E", // 0x3B
            static cpu => { DoSRL(cpu, ref cpu.Reg.E); }
        ),
        new Instruction(
            "SRL H", // 0x3C
            static cpu => { DoSRL(cpu, ref cpu.Reg.H); }
        ),
        new Instruction(
            "SRL L", // 0x3D
            static cpu => { DoSRL(cpu, ref cpu.Reg.L); }
        ),
        new Instruction(
            "SRL (HL)", // 0x3E
            static cpu =>
            {
                var value = cpu.Read8(cpu.Reg.HL);
                DoSRL(cpu, ref value);
                cpu.Write8(cpu.Reg.HL, value);
            }
        ),
        new Instruction(
            "SRL A", // 0x3F
            static cpu => { DoSRL(cpu, ref cpu.Reg.A); }
        ),
        new Instruction(
            "BIT 0,B", // 0x40
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 0) & cpu.Reg.B) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 0,C", // 0x41
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 0) & cpu.Reg.C) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 0,D", // 0x42
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 0) & cpu.Reg.D) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 0,E", // 0x43
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 0) & cpu.Reg.E) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 0,H", // 0x44
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 0) & cpu.Reg.H) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 0,L", // 0x45
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 0) & cpu.Reg.L) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 0,(HL)", // 0x46
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 0) & cpu.Read8(cpu.Reg.HL)) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 0,A", // 0x47
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 0) & cpu.Reg.A) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 1,B", // 0x48
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 1) & cpu.Reg.B) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 1,C", // 0x49
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 1) & cpu.Reg.C) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 1,D", // 0x4A
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 1) & cpu.Reg.D) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 1,E", // 0x4B
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 1) & cpu.Reg.E) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 1,H", // 0x4C
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 1) & cpu.Reg.H) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 1,L", // 0x4D
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 1) & cpu.Reg.L) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 1,(HL)", // 0x4E
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 1) & cpu.Read8(cpu.Reg.HL)) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 1,A", // 0x4F
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 1) & cpu.Reg.A) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 2,B", // 0x50
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 2) & cpu.Reg.B) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 2,C", // 0x51
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 2) & cpu.Reg.C) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 2,D", // 0x52
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 2) & cpu.Reg.D) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 2,E", // 0x53
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 2) & cpu.Reg.E) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 2,H", // 0x54
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 2) & cpu.Reg.H) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 2,L", // 0x55
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 2) & cpu.Reg.L) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 2,(HL)", // 0x56
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 2) & cpu.Read8(cpu.Reg.HL)) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 2,A", // 0x57
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 2) & cpu.Reg.A) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 3,B", // 0x58
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 3) & cpu.Reg.B) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 3,C", // 0x59
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 3) & cpu.Reg.C) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 3,D", // 0x5A
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 3) & cpu.Reg.D) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 3,E", // 0x5B
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 3) & cpu.Reg.E) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 3,H", // 0x5C
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 3) & cpu.Reg.H) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 3,L", // 0x5D
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 3) & cpu.Reg.L) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 3,(HL)", // 0x5E
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 3) & cpu.Read8(cpu.Reg.HL)) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 3,A", // 0x5F
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 3) & cpu.Reg.A) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 4,B", // 0x60
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 4) & cpu.Reg.B) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 4,C", // 0x61
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 4) & cpu.Reg.C) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 4,D", // 0x62
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 4) & cpu.Reg.D) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 4,E", // 0x63
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 4) & cpu.Reg.E) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 4,H", // 0x64
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 4) & cpu.Reg.H) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 4,L", // 0x65
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 4) & cpu.Reg.L) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 4,(HL)", // 0x66
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 4) & cpu.Read8(cpu.Reg.HL)) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 4,A", // 0x67
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 4) & cpu.Reg.A) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 5,B", // 0x68
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 5) & cpu.Reg.B) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 5,C", // 0x69
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 5) & cpu.Reg.C) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 5,D", // 0x6A
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 5) & cpu.Reg.D) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 5,E", // 0x6B
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 5) & cpu.Reg.E) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 5,H", // 0x6C
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 5) & cpu.Reg.H) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 5,L", // 0x6D
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 5) & cpu.Reg.L) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 5,(HL)", // 0x6E
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 5) & cpu.Read8(cpu.Reg.HL)) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 5,A", // 0x6F
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 5) & cpu.Reg.A) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 6,B", // 0x70
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 6) & cpu.Reg.B) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 6,C", // 0x71
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 6) & cpu.Reg.C) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 6,D", // 0x72
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 6) & cpu.Reg.D) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 6,E", // 0x73
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 6) & cpu.Reg.E) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 6,H", // 0x74
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 6) & cpu.Reg.H) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 6,L", // 0x75
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 6) & cpu.Reg.L) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 6,(HL)", // 0x76
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 6) & cpu.Read8(cpu.Reg.HL)) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 6,A", // 0x77
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 6) & cpu.Reg.A) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 7,B", // 0x78
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 7) & cpu.Reg.B) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 7,C", // 0x79
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 7) & cpu.Reg.C) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 7,D", // 0x7A
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 7) & cpu.Reg.D) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 7,E", // 0x7B
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 7) & cpu.Reg.E) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 7,H", // 0x7C
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 7) & cpu.Reg.H) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 7,L", // 0x7D
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 7) & cpu.Reg.L) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 7,(HL)", // 0x7E
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 7) & cpu.Read8(cpu.Reg.HL)) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "BIT 7,A", // 0x7F
            static cpu =>
            {
                cpu.Reg.Zf = ((1 << 7) & cpu.Reg.A) == 0;
                cpu.Reg.Nf = false;
                cpu.Reg.Hf = true;
            }
        ),
        new Instruction(
            "RES 0,B", // 0x80
            static cpu => { cpu.Reg.B = (byte)(cpu.Reg.B & ~(1 << 0)); }
        ),
        new Instruction(
            "RES 0,C", // 0x81
            static cpu => { cpu.Reg.C = (byte)(cpu.Reg.C & ~(1 << 0)); }
        ),
        new Instruction(
            "RES 0,D", // 0x82
            static cpu => { cpu.Reg.D = (byte)(cpu.Reg.D & ~(1 << 0)); }
        ),
        new Instruction(
            "RES 0,E", // 0x83
            static cpu => { cpu.Reg.E = (byte)(cpu.Reg.E & ~(1 << 0)); }
        ),
        new Instruction(
            "RES 0,H", // 0x84
            static cpu => { cpu.Reg.H = (byte)(cpu.Reg.H & ~(1 << 0)); }
        ),
        new Instruction(
            "RES 0,L", // 0x85
            static cpu => { cpu.Reg.L = (byte)(cpu.Reg.L & ~(1 << 0)); }
        ),
        new Instruction(
            "RES 0,(HL)", // 0x86
            static cpu => { cpu.Write8(cpu.Reg.HL, (byte)(cpu.Read8(cpu.Reg.HL) & ~(1 << 0))); }
        ),
        new Instruction(
            "RES 0,A", // 0x87
            static cpu => { cpu.Reg.A = (byte)(cpu.Reg.A & ~(1 << 0)); }
        ),
        new Instruction(
            "RES 1,B", // 0x88
            static cpu => { cpu.Reg.B = (byte)(cpu.Reg.B & ~(1 << 1)); }
        ),
        new Instruction(
            "RES 1,C", // 0x89
            static cpu => { cpu.Reg.C = (byte)(cpu.Reg.C & ~(1 << 1)); }
        ),
        new Instruction(
            "RES 1,D", // 0x8A
            static cpu => { cpu.Reg.D = (byte)(cpu.Reg.D & ~(1 << 1)); }
        ),
        new Instruction(
            "RES 1,E", // 0x8B
            static cpu => { cpu.Reg.E = (byte)(cpu.Reg.E & ~(1 << 1)); }
        ),
        new Instruction(
            "RES 1,H", // 0x8C
            static cpu => { cpu.Reg.H = (byte)(cpu.Reg.H & ~(1 << 1)); }
        ),
        new Instruction(
            "RES 1,L", // 0x8D
            static cpu => { cpu.Reg.L = (byte)(cpu.Reg.L & ~(1 << 1)); }
        ),
        new Instruction(
            "RES 1,(HL)", // 0x8E
            static cpu => { cpu.Write8(cpu.Reg.HL, (byte)(cpu.Read8(cpu.Reg.HL) & ~(1 << 1))); }
        ),
        new Instruction(
            "RES 1,A", // 0x8F
            static cpu => { cpu.Reg.A = (byte)(cpu.Reg.A & ~(1 << 1)); }
        ),
        new Instruction(
            "RES 2,B", // 0x90
            static cpu => { cpu.Reg.B = (byte)(cpu.Reg.B & ~(1 << 2)); }
        ),
        new Instruction(
            "RES 2,C", // 0x91
            static cpu => { cpu.Reg.C = (byte)(cpu.Reg.C & ~(1 << 2)); }
        ),
        new Instruction(
            "RES 2,D", // 0x92
            static cpu => { cpu.Reg.D = (byte)(cpu.Reg.D & ~(1 << 2)); }
        ),
        new Instruction(
            "RES 2,E", // 0x93
            static cpu => { cpu.Reg.E = (byte)(cpu.Reg.E & ~(1 << 2)); }
        ),
        new Instruction(
            "RES 2,H", // 0x94
            static cpu => { cpu.Reg.H = (byte)(cpu.Reg.H & ~(1 << 2)); }
        ),
        new Instruction(
            "RES 2,L", // 0x95
            static cpu => { cpu.Reg.L = (byte)(cpu.Reg.L & ~(1 << 2)); }
        ),
        new Instruction(
            "RES 2,(HL)", // 0x96
            static cpu => { cpu.Write8(cpu.Reg.HL, (byte)(cpu.Read8(cpu.Reg.HL) & ~(1 << 2))); }
        ),
        new Instruction(
            "RES 2,A", // 0x97
            static cpu => { cpu.Reg.A = (byte)(cpu.Reg.A & ~(1 << 2)); }
        ),
        new Instruction(
            "RES 3,B", // 0x98
            static cpu => { cpu.Reg.B = (byte)(cpu.Reg.B & ~(1 << 3)); }
        ),
        new Instruction(
            "RES 3,C", // 0x99
            static cpu => { cpu.Reg.C = (byte)(cpu.Reg.C & ~(1 << 3)); }
        ),
        new Instruction(
            "RES 3,D", // 0x9A
            static cpu => { cpu.Reg.D = (byte)(cpu.Reg.D & ~(1 << 3)); }
        ),
        new Instruction(
            "RES 3,E", // 0x9B
            static cpu => { cpu.Reg.E = (byte)(cpu.Reg.E & ~(1 << 3)); }
        ),
        new Instruction(
            "RES 3,H", // 0x9C
            static cpu => { cpu.Reg.H = (byte)(cpu.Reg.H & ~(1 << 3)); }
        ),
        new Instruction(
            "RES 3,L", // 0x9D
            static cpu => { cpu.Reg.L = (byte)(cpu.Reg.L & ~(1 << 3)); }
        ),
        new Instruction(
            "RES 3,(HL)", // 0x9E
            static cpu => { cpu.Write8(cpu.Reg.HL, (byte)(cpu.Read8(cpu.Reg.HL) & ~(1 << 3))); }
        ),
        new Instruction(
            "RES 3,A", // 0x9F
            static cpu => { cpu.Reg.A = (byte)(cpu.Reg.A & ~(1 << 3)); }
        ),
        new Instruction(
            "RES 4,B", // 0xA0
            static cpu => { cpu.Reg.B = (byte)(cpu.Reg.B & ~(1 << 4)); }
        ),
        new Instruction(
            "RES 4,C", // 0xA1
            static cpu => { cpu.Reg.C = (byte)(cpu.Reg.C & ~(1 << 4)); }
        ),
        new Instruction(
            "RES 4,D", // 0xA2
            static cpu => { cpu.Reg.D = (byte)(cpu.Reg.D & ~(1 << 4)); }
        ),
        new Instruction(
            "RES 4,E", // 0xA3
            static cpu => { cpu.Reg.E = (byte)(cpu.Reg.E & ~(1 << 4)); }
        ),
        new Instruction(
            "RES 4,H", // 0xA4
            static cpu => { cpu.Reg.H = (byte)(cpu.Reg.H & ~(1 << 4)); }
        ),
        new Instruction(
            "RES 4,L", // 0xA5
            static cpu => { cpu.Reg.L = (byte)(cpu.Reg.L & ~(1 << 4)); }
        ),
        new Instruction(
            "RES 4,(HL)", // 0xA6
            static cpu => { cpu.Write8(cpu.Reg.HL, (byte)(cpu.Read8(cpu.Reg.HL) & ~(1 << 4))); }
        ),
        new Instruction(
            "RES 4,A", // 0xA7
            static cpu => { cpu.Reg.A = (byte)(cpu.Reg.A & ~(1 << 4)); }
        ),
        new Instruction(
            "RES 5,B", // 0xA8
            static cpu => { cpu.Reg.B = (byte)(cpu.Reg.B & ~(1 << 5)); }
        ),
        new Instruction(
            "RES 5,C", // 0xA9
            static cpu => { cpu.Reg.C = (byte)(cpu.Reg.C & ~(1 << 5)); }
        ),
        new Instruction(
            "RES 5,D", // 0xAA
            static cpu => { cpu.Reg.D = (byte)(cpu.Reg.D & ~(1 << 5)); }
        ),
        new Instruction(
            "RES 5,E", // 0xAB
            static cpu => { cpu.Reg.E = (byte)(cpu.Reg.E & ~(1 << 5)); }
        ),
        new Instruction(
            "RES 5,H", // 0xAC
            static cpu => { cpu.Reg.H = (byte)(cpu.Reg.H & ~(1 << 5)); }
        ),
        new Instruction(
            "RES 5,L", // 0xAD
            static cpu => { cpu.Reg.L = (byte)(cpu.Reg.L & ~(1 << 5)); }
        ),
        new Instruction(
            "RES 5,(HL)", // 0xAE
            static cpu => { cpu.Write8(cpu.Reg.HL, (byte)(cpu.Read8(cpu.Reg.HL) & ~(1 << 5))); }
        ),
        new Instruction(
            "RES 5,A", // 0xAF
            static cpu => { cpu.Reg.A = (byte)(cpu.Reg.A & ~(1 << 5)); }
        ),
        new Instruction(
            "RES 6,B", // 0xB0
            static cpu => { cpu.Reg.B = (byte)(cpu.Reg.B & ~(1 << 6)); }
        ),
        new Instruction(
            "RES 6,C", // 0xB1
            static cpu => { cpu.Reg.C = (byte)(cpu.Reg.C & ~(1 << 6)); }
        ),
        new Instruction(
            "RES 6,D", // 0xB2
            static cpu => { cpu.Reg.D = (byte)(cpu.Reg.D & ~(1 << 6)); }
        ),
        new Instruction(
            "RES 6,E", // 0xB3
            static cpu => { cpu.Reg.E = (byte)(cpu.Reg.E & ~(1 << 6)); }
        ),
        new Instruction(
            "RES 6,H", // 0xB4
            static cpu => { cpu.Reg.H = (byte)(cpu.Reg.H & ~(1 << 6)); }
        ),
        new Instruction(
            "RES 6,L", // 0xB5
            static cpu => { cpu.Reg.L = (byte)(cpu.Reg.L & ~(1 << 6)); }
        ),
        new Instruction(
            "RES 6,(HL)", // 0xB6
            static cpu => { cpu.Write8(cpu.Reg.HL, (byte)(cpu.Read8(cpu.Reg.HL) & ~(1 << 6))); }
        ),
        new Instruction(
            "RES 6,A", // 0xB7
            static cpu => { cpu.Reg.A = (byte)(cpu.Reg.A & ~(1 << 6)); }
        ),
        new Instruction(
            "RES 7,B", // 0xB8
            static cpu => { cpu.Reg.B = (byte)(cpu.Reg.B & ~(1 << 7)); }
        ),
        new Instruction(
            "RES 7,C", // 0xB9
            static cpu => { cpu.Reg.C = (byte)(cpu.Reg.C & ~(1 << 7)); }
        ),
        new Instruction(
            "RES 7,D", // 0xBA
            static cpu => { cpu.Reg.D = (byte)(cpu.Reg.D & ~(1 << 7)); }
        ),
        new Instruction(
            "RES 7,E", // 0xBB
            static cpu => { cpu.Reg.E = (byte)(cpu.Reg.E & ~(1 << 7)); }
        ),
        new Instruction(
            "RES 7,H", // 0xBC
            static cpu => { cpu.Reg.H = (byte)(cpu.Reg.H & ~(1 << 7)); }
        ),
        new Instruction(
            "RES 7,L", // 0xBD
            static cpu => { cpu.Reg.L = (byte)(cpu.Reg.L & ~(1 << 7)); }
        ),
        new Instruction(
            "RES 7,(HL)", // 0xBE
            static cpu => { cpu.Write8(cpu.Reg.HL, (byte)(cpu.Read8(cpu.Reg.HL) & ~(1 << 7))); }
        ),
        new Instruction(
            "RES 7,A", // 0xBF
            static cpu => { cpu.Reg.A = (byte)(cpu.Reg.A & ~(1 << 7)); }
        ),
        new Instruction(
            "SET 0,B", // 0xC0
            static cpu => { cpu.Reg.B |= 1 << 0; }
        ),
        new Instruction(
            "SET 0,C", // 0xC1
            static cpu => { cpu.Reg.C |= 1 << 0; }
        ),
        new Instruction(
            "SET 0,D", // 0xC2
            static cpu => { cpu.Reg.D |= 1 << 0; }
        ),
        new Instruction(
            "SET 0,E", // 0xC3
            static cpu => { cpu.Reg.E |= 1 << 0; }
        ),
        new Instruction(
            "SET 0,H", // 0xC4
            static cpu => { cpu.Reg.H |= 1 << 0; }
        ),
        new Instruction(
            "SET 0,L", // 0xC5
            static cpu => { cpu.Reg.L |= 1 << 0; }
        ),
        new Instruction(
            "SET 0,(HL)", // 0xC6
            static cpu => { cpu.Write8(cpu.Reg.HL, (byte)(cpu.Read8(cpu.Reg.HL) | (1 << 0))); }
        ),
        new Instruction(
            "SET 0,A", // 0xC7
            static cpu => { cpu.Reg.A |= 1 << 0; }
        ),
        new Instruction(
            "SET 1,B", // 0xC8
            static cpu => { cpu.Reg.B |= 1 << 1; }
        ),
        new Instruction(
            "SET 1,C", // 0xC9
            static cpu => { cpu.Reg.C |= 1 << 1; }
        ),
        new Instruction(
            "SET 1,D", // 0xCA
            static cpu => { cpu.Reg.D |= 1 << 1; }
        ),
        new Instruction(
            "SET 1,E", // 0xCB
            static cpu => { cpu.Reg.E |= 1 << 1; }
        ),
        new Instruction(
            "SET 1,H", // 0xCC
            static cpu => { cpu.Reg.H |= 1 << 1; }
        ),
        new Instruction(
            "SET 1,L", // 0xCD
            static cpu => { cpu.Reg.L |= 1 << 1; }
        ),
        new Instruction(
            "SET 1,(HL)", // 0xCE
            static cpu => { cpu.Write8(cpu.Reg.HL, (byte)(cpu.Read8(cpu.Reg.HL) | (1 << 1))); }
        ),
        new Instruction(
            "SET 1,A", // 0xCF
            static cpu => { cpu.Reg.A |= 1 << 1; }
        ),
        new Instruction(
            "SET 2,B", // 0xD0
            static cpu => { cpu.Reg.B |= 1 << 2; }
        ),
        new Instruction(
            "SET 2,C", // 0xD1
            static cpu => { cpu.Reg.C |= 1 << 2; }
        ),
        new Instruction(
            "SET 2,D", // 0xD2
            static cpu => { cpu.Reg.D |= 1 << 2; }
        ),
        new Instruction(
            "SET 2,E", // 0xD3
            static cpu => { cpu.Reg.E |= 1 << 2; }
        ),
        new Instruction(
            "SET 2,H", // 0xD4
            static cpu => { cpu.Reg.H |= 1 << 2; }
        ),
        new Instruction(
            "SET 2,L", // 0xD5
            static cpu => { cpu.Reg.L |= 1 << 2; }
        ),
        new Instruction(
            "SET 2,(HL)", // 0xD6
            static cpu => { cpu.Write8(cpu.Reg.HL, (byte)(cpu.Read8(cpu.Reg.HL) | (1 << 2))); }
        ),
        new Instruction(
            "SET 2,A", // 0xD7
            static cpu => { cpu.Reg.A |= 1 << 2; }
        ),
        new Instruction(
            "SET 3,B", // 0xD8
            static cpu => { cpu.Reg.B |= 1 << 3; }
        ),
        new Instruction(
            "SET 3,C", // 0xD9
            static cpu => { cpu.Reg.C |= 1 << 3; }
        ),
        new Instruction(
            "SET 3,D", // 0xDA
            static cpu => { cpu.Reg.D |= 1 << 3; }
        ),
        new Instruction(
            "SET 3,E", // 0xDB
            static cpu => { cpu.Reg.E |= 1 << 3; }
        ),
        new Instruction(
            "SET 3,H", // 0xDC
            static cpu => { cpu.Reg.H |= 1 << 3; }
        ),
        new Instruction(
            "SET 3,L", // 0xDD
            static cpu => { cpu.Reg.L |= 1 << 3; }
        ),
        new Instruction(
            "SET 3,(HL)", // 0xDE
            static cpu => { cpu.Write8(cpu.Reg.HL, (byte)(cpu.Read8(cpu.Reg.HL) | (1 << 3))); }
        ),
        new Instruction(
            "SET 3,A", // 0xDF
            static cpu => { cpu.Reg.A |= 1 << 3; }
        ),
        new Instruction(
            "SET 4,B", // 0xE0
            static cpu => { cpu.Reg.B |= 1 << 4; }
        ),
        new Instruction(
            "SET 4,C", // 0xE1
            static cpu => { cpu.Reg.C |= 1 << 4; }
        ),
        new Instruction(
            "SET 4,D", // 0xE2
            static cpu => { cpu.Reg.D |= 1 << 4; }
        ),
        new Instruction(
            "SET 4,E", // 0xE3
            static cpu => { cpu.Reg.E |= 1 << 4; }
        ),
        new Instruction(
            "SET 4,H", // 0xE4
            static cpu => { cpu.Reg.H |= 1 << 4; }
        ),
        new Instruction(
            "SET 4,L", // 0xE5
            static cpu => { cpu.Reg.L |= 1 << 4; }
        ),
        new Instruction(
            "SET 4,(HL)", // 0xE6
            static cpu => { cpu.Write8(cpu.Reg.HL, (byte)(cpu.Read8(cpu.Reg.HL) | (1 << 4))); }
        ),
        new Instruction(
            "SET 4,A", // 0xE7
            static cpu => { cpu.Reg.A |= 1 << 4; }
        ),
        new Instruction(
            "SET 5,B", // 0xE8
            static cpu => { cpu.Reg.B |= 1 << 5; }
        ),
        new Instruction(
            "SET 5,C", // 0xE9
            static cpu => { cpu.Reg.C |= 1 << 5; }
        ),
        new Instruction(
            "SET 5,D", // 0xEA
            static cpu => { cpu.Reg.D |= 1 << 5; }
        ),
        new Instruction(
            "SET 5,E", // 0xEB
            static cpu => { cpu.Reg.E |= 1 << 5; }
        ),
        new Instruction(
            "SET 5,H", // 0xEC
            static cpu => { cpu.Reg.H |= 1 << 5; }
        ),
        new Instruction(
            "SET 5,L", // 0xED
            static cpu => { cpu.Reg.L |= 1 << 5; }
        ),
        new Instruction(
            "SET 5,(HL)", // 0xEE
            static cpu => { cpu.Write8(cpu.Reg.HL, (byte)(cpu.Read8(cpu.Reg.HL) | (1 << 5))); }
        ),
        new Instruction(
            "SET 5,A", // 0xEF
            static cpu => { cpu.Reg.A |= 1 << 5; }
        ),
        new Instruction(
            "SET 6,B", // 0xF0
            static cpu => { cpu.Reg.B |= 1 << 6; }
        ),
        new Instruction(
            "SET 6,C", // 0xF1
            static cpu => { cpu.Reg.C |= 1 << 6; }
        ),
        new Instruction(
            "SET 6,D", // 0xF2
            static cpu => { cpu.Reg.D |= 1 << 6; }
        ),
        new Instruction(
            "SET 6,E", // 0xF3
            static cpu => { cpu.Reg.E |= 1 << 6; }
        ),
        new Instruction(
            "SET 6,H", // 0xF4
            static cpu => { cpu.Reg.H |= 1 << 6; }
        ),
        new Instruction(
            "SET 6,L", // 0xF5
            static cpu => { cpu.Reg.L |= 1 << 6; }
        ),
        new Instruction(
            "SET 6,(HL)", // 0xF6
            static cpu => { cpu.Write8(cpu.Reg.HL, (byte)(cpu.Read8(cpu.Reg.HL) | (1 << 6))); }
        ),
        new Instruction(
            "SET 6,A", // 0xF7
            static cpu => { cpu.Reg.A |= 1 << 6; }
        ),
        new Instruction(
            "SET 7,B", // 0xF8
            static cpu => { cpu.Reg.B |= 1 << 7; }
        ),
        new Instruction(
            "SET 7,C", // 0xF9
            static cpu => { cpu.Reg.C |= 1 << 7; }
        ),
        new Instruction(
            "SET 7,D", // 0xFA
            static cpu => { cpu.Reg.D |= 1 << 7; }
        ),
        new Instruction(
            "SET 7,E", // 0xFB
            static cpu => { cpu.Reg.E |= 1 << 7; }
        ),
        new Instruction(
            "SET 7,H", // 0xFC
            static cpu => { cpu.Reg.H |= 1 << 7; }
        ),
        new Instruction(
            "SET 7,L", // 0xFD
            static cpu => { cpu.Reg.L |= 1 << 7; }
        ),
        new Instruction(
            "SET 7,(HL)", // 0xFE
            static cpu => { cpu.Write8(cpu.Reg.HL, (byte)(cpu.Read8(cpu.Reg.HL) | (1 << 7))); }
        ),
        new Instruction(
            "SET 7,A", // 0xFF
            static cpu => { cpu.Reg.A |= 1 << 7; }
        )
    ];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void DoRR(Cpu cpu, ref byte reg)
    {
        var cf = cpu.Reg.Cf;
        cpu.Reg.Cf = (reg & 0x01) != 0;
        reg = (byte)((reg >> 1) + (cf ? 0x80 : 0x00));
        cpu.Reg.Zf = reg == 0;
        cpu.Reg.Nf = false;
        cpu.Reg.Hf = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void DoRL(Cpu cpu, ref byte reg)
    {
        var cf = cpu.Reg.Cf;
        cpu.Reg.Cf = (reg & 0x80) != 0;
        reg = (byte)((reg << 1) + (cf ? 1 : 0));
        cpu.Reg.Zf = reg == 0;
        cpu.Reg.Nf = false;
        cpu.Reg.Hf = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void DoRLC(Cpu cpu, ref byte reg)
    {
        cpu.Reg.Cf = (reg & 0x80) != 0;
        reg = (byte)((reg << 1) + (cpu.Reg.Cf ? 1 : 0));
        cpu.Reg.Zf = reg == 0;
        cpu.Reg.Nf = false;
        cpu.Reg.Hf = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void DoRRC(Cpu cpu, ref byte reg)
    {
        cpu.Reg.Cf = (reg & 0x01) != 0;
        reg = (byte)((reg >> 1) + (cpu.Reg.Cf ? 0x80 : 0x00));
        cpu.Reg.Zf = reg == 0;
        cpu.Reg.Nf = false;
        cpu.Reg.Hf = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void DoSLA(Cpu cpu, ref byte reg)
    {
        cpu.Reg.Cf = (reg & 0x80) != 0;
        reg <<= 1;
        cpu.Reg.Zf = reg == 0;
        cpu.Reg.Nf = false;
        cpu.Reg.Hf = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void DoSRA(Cpu cpu, ref byte reg)
    {
        cpu.Reg.Cf = (reg & 0x01) != 0;
        reg = (byte)((reg >> 1) | (reg & 0x80));
        cpu.Reg.Zf = reg == 0;
        cpu.Reg.Nf = false;
        cpu.Reg.Hf = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void DoSWAP(Cpu cpu, ref byte reg)
    {
        reg = (byte)((reg >> 4) | (reg << 4));
        cpu.Reg.Zf = reg == 0;
        cpu.Reg.Nf = false;
        cpu.Reg.Hf = false;
        cpu.Reg.Cf = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void DoSRL(Cpu cpu, ref byte reg)
    {
        cpu.Reg.Cf = (reg & 0x01) != 0;
        reg >>= 1;
        cpu.Reg.Zf = reg == 0;
        cpu.Reg.Nf = false;
        cpu.Reg.Hf = false;
    }
}