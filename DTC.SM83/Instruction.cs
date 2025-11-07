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
namespace DTC.SM83;

/// <summary>
/// Represents a single CPU instruction with its mnemonic and execution logic.
/// </summary>

public sealed class Instruction
{
    /// <summary>
    /// Gets the mnemonic representation of the instruction.
    /// </summary>
    public string Mnemonic { get; }

    /// <summary>
    /// Gets the function that executes this instruction on the CPU and returns the number of cycles taken.
    /// </summary>
    public Action<Cpu> Execute { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Instruction"/> class.
    /// </summary>
    /// <param name="mnemonic">The mnemonic representation of the instruction.</param>
    /// <param name="execute">The function that executes this instruction on the CPU.</param>
    public Instruction(string mnemonic, Action<Cpu> execute)
    {
        Mnemonic = mnemonic;
        Execute = execute;
    }
    
    public override string ToString() => Mnemonic;
}