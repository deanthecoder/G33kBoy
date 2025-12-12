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

    /// <summary>
    /// Gets the mnemonic for the instruction at the supplied address, with any immediate operands resolved.
    /// </summary>
    /// <remarks>
    /// Useful for logging and debugging live CPU execution.
    /// </remarks>
    public static string GetInstructionWithOperands(Bus memory, ushort address)
    {
        if (memory == null)
            throw new ArgumentNullException(nameof(memory));

        var opcode = memory.Read8(address);
        if (opcode == 0xCB)
        {
            var cbOpcode = memory.Read8((ushort)(address + 1));
            var mnemonic = PrefixedInstructions.Table[cbOpcode]?.Mnemonic ?? $"CB ${cbOpcode:X2}";
            var length = Math.Max(InstructionLengths.GetLength(cbOpcode, isCbPrefixed: true), 2);
            Span<byte> instructionBytes = stackalloc byte[2];
            instructionBytes[0] = opcode;
            instructionBytes[1] = cbOpcode;
            return ResolveImmediateOperands(mnemonic, instructionBytes, length, isCbPrefixed: true, address);
        }

        var unprefixedInstruction = Instructions.Instructions.Table[opcode];
        var unprefixedMnemonic = unprefixedInstruction?.Mnemonic ?? $"DB ${opcode:X2}";
        var unprefixedLength = Math.Max(InstructionLengths.GetLength(opcode, isCbPrefixed: false), 1);

        Span<byte> bytes = stackalloc byte[3];
        bytes[0] = opcode;
        if (unprefixedLength > 1)
            bytes[1] = memory.Read8((ushort)(address + 1));
        if (unprefixedLength > 2)
            bytes[2] = memory.Read8((ushort)(address + 2));

        return ResolveImmediateOperands(unprefixedMnemonic, bytes[..Math.Min(unprefixedLength, bytes.Length)], unprefixedLength, isCbPrefixed: false, address);
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
        var resolved = ReplaceImmediatePlaceholders(mnemonic, hex);

        if (!mnemonic.StartsWith("JR", StringComparison.Ordinal))
            return resolved;
        
        var displacement = unchecked((sbyte)romData[immediateStart]);
        var currentPc = GetProgramCounter(offset);
        var targetPc = (ushort)(currentPc + actualLength + displacement);
        var bank = offset / 0x4000;
        var targetOffset = targetPc < 0x4000 ? targetPc : bank * 0x4000 + (targetPc - 0x4000);
        var targetDisplay = FormatAddress(targetOffset);
        return $"{resolved} -> {(targetDisplay.Contains(':') ? targetDisplay : $"${targetDisplay}")}";
    }

    private static string ResolveImmediateOperands(string mnemonic, ReadOnlySpan<byte> instructionData, int declaredLength, bool isCbPrefixed, ushort instructionAddress)
    {
        var opcodeBytes = isCbPrefixed ? 2 : 1;
        var immediateLength = Math.Max(0, declaredLength - opcodeBytes);
        if (immediateLength == 0)
            return mnemonic;

        var available = Math.Max(0, Math.Min(immediateLength, instructionData.Length - opcodeBytes));
        if (available == 0)
            return mnemonic;

        var value = available switch
        {
            1 => instructionData[opcodeBytes],
            _ => instructionData[opcodeBytes] | (instructionData[Math.Min(opcodeBytes + 1, instructionData.Length - 1)] << 8)
        };

        var hex = available == 1 ? $"${value:X2}" : $"${value:X4}";
        var resolved = ReplaceImmediatePlaceholders(mnemonic, hex);

        if (!mnemonic.StartsWith("JR", StringComparison.Ordinal))
            return resolved;

        var displacement = unchecked((sbyte)instructionData[opcodeBytes]);
        var targetPc = (ushort)(instructionAddress + declaredLength + displacement);
        return $"{resolved} -> ${targetPc:X4}";
    }

    private static string FormatAddress(int offset)
    {
        var bank = offset / 0x4000;
        var pc = offset < 0x4000 ? offset : 0x4000 + offset % 0x4000;
        return bank == 0 ? $"{pc:X4}" : $"{bank:X2}:{pc:X4}";
    }

    private static int GetProgramCounter(int offset) =>
        offset < 0x4000 ? offset & 0xFFFF : (0x4000 + offset % 0x4000) & 0xFFFF;

    private static string ReplaceImmediatePlaceholders(string mnemonic, string hex) =>
        mnemonic
            .Replace("a16", hex, StringComparison.Ordinal)
            .Replace("a8", hex, StringComparison.Ordinal)
            .Replace("nn", hex, StringComparison.Ordinal)
            .Replace("e8", hex, StringComparison.Ordinal)
            .Replace("n", hex, StringComparison.Ordinal);
}
