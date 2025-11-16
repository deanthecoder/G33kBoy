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

using Avalonia.Controls;
using SharpHook;
using SharpHook.Native;

namespace DTC.SM83;

/// <summary>Captures global keyboard input and tracks Game Boy button state.</summary>
public sealed class Joypad : IDisposable
{
    private readonly SimpleGlobalHook m_keyboardHook;
    private readonly Lock m_stateLock = new();
    private JoypadButtons m_pressedButtons;
    private bool m_handlePressEvents = true;

    public Joypad()
    {
        m_keyboardHook = new SimpleGlobalHook();
        m_keyboardHook.KeyPressed += (_, args) => HandleKey(args.Data.KeyCode, true);
        m_keyboardHook.KeyReleased += (_, args) => HandleKey(args.Data.KeyCode, false);

        if (!Design.IsDesignMode)
            m_keyboardHook.RunAsync();
    }

    /// <summary>
    /// Gets or sets whether key press events should update the button state.
    /// </summary>
    public bool HandlePressEvents
    {
        get => m_handlePressEvents;
        set
        {
            if (m_handlePressEvents == value)
                return;

            m_handlePressEvents = value;
            if (!value)
                ClearState();
        }
    }

    public JoypadButtons GetPressedButtons()
    {
        lock (m_stateLock)
            return m_pressedButtons;
    }

    public IDisposable CreatePressBlocker() =>
        new PressBlocker(this);

    private void HandleKey(KeyCode keyCode, bool isPressed)
    {
        if (!m_handlePressEvents)
            return;

        if (!TryMapButton(keyCode, out var button))
            return;

        lock (m_stateLock)
        {
            if (isPressed)
                m_pressedButtons |= button;
            else
                m_pressedButtons &= ~button;
        }
    }

    private static bool TryMapButton(KeyCode keyCode, out JoypadButtons button)
    {
        switch (keyCode)
        {
            case KeyCode.VcUp:
                button = JoypadButtons.Up;
                return true;
            case KeyCode.VcDown:
                button = JoypadButtons.Down;
                return true;
            case KeyCode.VcLeft:
                button = JoypadButtons.Left;
                return true;
            case KeyCode.VcRight:
                button = JoypadButtons.Right;
                return true;
            case KeyCode.VcZ:
                button = JoypadButtons.B;
                return true;
            case KeyCode.VcX:
                button = JoypadButtons.A;
                return true;
            case KeyCode.VcSpace:
                button = JoypadButtons.Select;
                return true;
            case KeyCode.VcEnter:
                button = JoypadButtons.Start;
                return true;
            default:
                button = JoypadButtons.None;
                return false;
        }
    }

    private void ClearState()
    {
        lock (m_stateLock)
            m_pressedButtons = JoypadButtons.None;
    }

    public void Dispose() =>
        m_keyboardHook.Dispose();

    [Flags]
    public enum JoypadButtons
    {
        None = 0,
        Right = 1 << 0,
        Left = 1 << 1,
        Up = 1 << 2,
        Down = 1 << 3,
        A = 1 << 4,
        B = 1 << 5,
        Select = 1 << 6,
        Start = 1 << 7
    }

    private sealed class PressBlocker : IDisposable
    {
        private readonly Joypad m_joypad;
        private readonly bool m_oldHandlePressEvents;

        internal PressBlocker(Joypad joypad)
        {
            m_joypad = joypad;
            m_oldHandlePressEvents = joypad.HandlePressEvents;
            joypad.HandlePressEvents = false;
        }

        public void Dispose() =>
            m_joypad.HandlePressEvents = m_oldHandlePressEvents;
    }
}
