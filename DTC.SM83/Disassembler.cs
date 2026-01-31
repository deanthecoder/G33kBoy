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

    private static string ReplaceImmediatePlaceholders(string mnemonic, string hex) =>
        mnemonic
            .Replace("a16", hex, StringComparison.Ordinal)
            .Replace("a8", hex, StringComparison.Ordinal)
            .Replace("nn", hex, StringComparison.Ordinal)
            .Replace("e8", hex, StringComparison.Ordinal)
            .Replace("n", hex, StringComparison.Ordinal);
}
