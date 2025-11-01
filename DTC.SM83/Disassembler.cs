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
/// Provides helpers for turning opcode data back into human-readable mnemonics.
/// </summary>
public static class Disassembler
{
    public static string GetMnemonic(Memory memory, ushort address)
    {
        if (memory == null)
            throw new ArgumentNullException(nameof(memory));

        var opcode = memory[address];

        if (opcode == 0xCB)
        {
            var cbOpcodeAddress = (ushort)(address + 1);
            var cbOpcode = memory[cbOpcodeAddress];
            var cbInstruction = Instructions.CbPrefixed[cbOpcode];
            return cbInstruction?.Mnemonic ?? $"CB ${cbOpcode:X2}";
        }

        var instruction = Instructions.Unprefixed[opcode];
        return instruction?.Mnemonic ?? $"DB ${opcode:X2}";
    }
}
