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

using System.Collections.Generic;
using DTC.SM83.Instructions;

namespace DTC.SM83;

/// <summary>
/// Provides helpers for turning opcode data back into human-readable mnemonics.
/// </summary>
public static class Disassembler
{
    public static string GetMnemonic(Bus memory, ushort address)
    {
        if (memory == null)
            throw new ArgumentNullException(nameof(memory));

        var opcode = memory.Read8(address);
        if (opcode == 0xCB)
        {
            var cbOpcodeAddress = (ushort)(address + 1);
            var cbOpcode = memory.Read8(cbOpcodeAddress);
            var cbInstruction = PrefixedInstructions.Table[cbOpcode];
            return cbInstruction?.Mnemonic ?? $"CB ${cbOpcode:X2}";
        }

        var instruction = Instructions.Instructions.Table[opcode];
        return instruction?.Mnemonic ?? $"DB ${opcode:X2}";
    }

    public static IReadOnlyList<string> DisassembleRom(byte[] romData)
    {
        if (romData == null)
            throw new ArgumentNullException(nameof(romData));

        var lines = new List<string>(romData.Length / 2);
        var offset = 0;

        while (offset < romData.Length)
        {
            var (mnemonic, length, isCbPrefixed) = DecodeInstruction(romData, offset);
            var actualLength = Math.Max(1, Math.Min(length, romData.Length - offset));
            var prefix = FormatAddress(offset);
            var resolvedMnemonic = ResolveImmediateOperands(mnemonic, romData, offset, actualLength, isCbPrefixed);
            lines.Add($"{prefix}:  {resolvedMnemonic}");
            offset += actualLength;
        }

        return lines;
    }

    private static (string mnemonic, int length, bool isCbPrefixed) DecodeInstruction(byte[] romData, int offset)
    {
        var opcode = romData[offset];
        if (opcode == 0xCB)
        {
            var cbOpcode = offset + 1 < romData.Length ? romData[offset + 1] : (byte)0x00;
            var length = InstructionLengths.GetLength(cbOpcode, isCbPrefixed: true);
            return (PrefixedInstructions.Table[cbOpcode]?.Mnemonic ?? $"CB ${cbOpcode:X2}", Math.Max(length, 2), true);
        }

        var unprefixedLength = InstructionLengths.GetLength(opcode, isCbPrefixed: false);
        return (Instructions.Instructions.Table[opcode]?.Mnemonic ?? $"DB ${opcode:X2}", Math.Max(unprefixedLength, 1), false);
    }

    private static string ResolveImmediateOperands(string mnemonic, byte[] romData, int offset, int actualLength, bool isCbPrefixed)
    {
        var opcodeBytes = isCbPrefixed ? 2 : 1;
        var immediateLength = Math.Max(0, actualLength - opcodeBytes);
        if (immediateLength == 0)
            return mnemonic;

        var immediateStart = offset + opcodeBytes;
        var available = Math.Max(0, Math.Min(immediateLength, romData.Length - immediateStart));
        if (available == 0)
            return mnemonic;

        var value = available switch
        {
            1 => romData[immediateStart],
            _ => romData[immediateStart] + (romData[Math.Min(immediateStart + 1, romData.Length - 1)] << 8)
        };

        var hex = available == 1 ? $"${value:X2}" : $"${value:X4}";
        var resolved = mnemonic
            .Replace("a16", hex, StringComparison.Ordinal)
            .Replace("a8", hex, StringComparison.Ordinal)
            .Replace("nn", hex, StringComparison.Ordinal)
            .Replace("e8", hex, StringComparison.Ordinal)
            .Replace("n", hex, StringComparison.Ordinal);

        if (mnemonic.StartsWith("JR", StringComparison.Ordinal) && available >= 1)
        {
            var displacement = unchecked((sbyte)romData[immediateStart]);
            var currentPc = GetProgramCounter(offset);
            var targetPc = (ushort)(currentPc + actualLength + displacement);
            var bank = offset / 0x4000;
            var targetOffset = targetPc < 0x4000 ? targetPc : bank * 0x4000 + (targetPc - 0x4000);
            var targetDisplay = FormatAddress(targetOffset);
            resolved = $"{resolved} -> {(targetDisplay.Contains(':') ? targetDisplay : $"${targetDisplay}")}";
        }

        return resolved;
    }

    private static string FormatAddress(int offset)
    {
        var bank = offset / 0x4000;
        var pc = offset < 0x4000 ? offset : 0x4000 + offset % 0x4000;
        return bank == 0 ? $"{pc:X4}" : $"{bank:X2}:{pc:X4}";
    }

    private static int GetProgramCounter(int offset) =>
        offset < 0x4000 ? offset & 0xFFFF : (0x4000 + offset % 0x4000) & 0xFFFF;
}
