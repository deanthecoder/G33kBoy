// Code authored by Dean Edis (DeanTheCoder).
// Anyone is free to copy, modify, use, compile, or distribute this software,
// either in source code form or as a compiled binary, for any purpose.
// 
// If you modify the code, please retain this copyright header,
// and consider contributing back to the repository or letting us know
// about your modifications. Your contributions are valued!
// 
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.
namespace DTC.SM83.HostDevices;

public interface IAudioSink : IDisposable
{
    /// <summary>
    /// Adds a single stereo sample. Values are expected in the range -1.0 to +1.0.
    /// </summary>
    void AddSample(double left, double right);
}