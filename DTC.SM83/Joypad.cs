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

/// <summary>
/// Captures global keyboard input and tracks Game Boy button state.
/// </summary>
public sealed class Joypad : IDisposable
{
    private const int AutoFireIntervalMs = 60; // ~8 presses per second (50% duty cycle).
    private readonly SimpleGlobalHook m_keyboardHook;
    private readonly Lock m_stateLock = new();
    private readonly Timer m_autoFireTimer;
    private JoypadButtons m_physicalButtons;
    private JoypadButtons m_pressedButtons;
    private bool m_handlePressEvents = true;
    private JoypadButtons m_autoFireHeldButtons;
    private bool m_autoFirePulseOn;

    public Joypad()
    {
        m_keyboardHook = new SimpleGlobalHook();
        m_keyboardHook.KeyPressed += (_, args) => HandleKey(args.Data.KeyCode, true);
        m_keyboardHook.KeyReleased += (_, args) => HandleKey(args.Data.KeyCode, false);
        m_autoFireTimer = new Timer(_ => AutoFireTick(), null, Timeout.Infinite, Timeout.Infinite);

        if (!Design.IsDesignMode)
            m_keyboardHook.RunAsync();
    }

    /// <summary>
    /// Gets or sets whether key press events should update the button state.
    /// </summary>
    private bool HandlePressEvents
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

        if (keyCode == KeyCode.VcC)
        {
            SetAutoFireHeld(JoypadButtons.B, isPressed);
            return;
        }

        if (keyCode == KeyCode.VcV)
        {
            SetAutoFireHeld(JoypadButtons.A, isPressed);
            return;
        }

        if (!TryMapButton(keyCode, out var button))
            return;

        lock (m_stateLock)
        {
            if (isPressed)
                m_physicalButtons |= button;
            else
                m_physicalButtons &= ~button;

            RecomputeButtons();
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
        {
            m_physicalButtons = JoypadButtons.None;
            ResetAutoFireStateInternal();
            RecomputeButtons();
        }
    }
    
    public void Dispose()
    {
        m_autoFireTimer?.Dispose();
        m_keyboardHook.Dispose();
    }

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

    private void SetAutoFireHeld(JoypadButtons button, bool isPressed)
    {
        lock (m_stateLock)
        {
            var wasHeld = (m_autoFireHeldButtons & button) != 0;
            if (wasHeld == isPressed)
                return;

            var hadAnyHeld = m_autoFireHeldButtons != JoypadButtons.None;
            if (isPressed)
                m_autoFireHeldButtons |= button;
            else
                m_autoFireHeldButtons &= ~button;

            if (m_autoFireHeldButtons == JoypadButtons.None)
            {
                ResetAutoFireStateInternal();
                RecomputeButtons();
                return;
            }

            if (!hadAnyHeld)
            {
                m_autoFirePulseOn = true; // Press immediately on engage.
                RecomputeButtons();
                m_autoFireTimer.Change(AutoFireIntervalMs, AutoFireIntervalMs);
                return;
            }

            RecomputeButtons();
        }
    }

    private void AutoFireTick()
    {
        lock (m_stateLock)
        {
            if (m_autoFireHeldButtons == JoypadButtons.None)
            {
                ResetAutoFireStateInternal();
                RecomputeButtons();
                return;
            }

            m_autoFirePulseOn = !m_autoFirePulseOn;
            RecomputeButtons();
        }
    }

    private void RecomputeButtons()
    {
        var combined = m_physicalButtons;
        if (m_autoFireHeldButtons != JoypadButtons.None && m_autoFirePulseOn)
            combined |= m_autoFireHeldButtons;

        m_pressedButtons = combined;
    }

    private void ResetAutoFireStateInternal()
    {
        m_autoFireHeldButtons = JoypadButtons.None;
        m_autoFirePulseOn = false;
        m_autoFireTimer?.Change(Timeout.Infinite, Timeout.Infinite);
    }
}
