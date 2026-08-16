using BepInEx.Configuration;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SephiriaQoL;

internal static class ShortcutInput
{
    private static readonly KeyCode[] ModifierKeys =
    {
        KeyCode.LeftControl,
        KeyCode.RightControl,
        KeyCode.LeftShift,
        KeyCode.RightShift,
        KeyCode.LeftAlt,
        KeyCode.RightAlt,
        KeyCode.LeftCommand,
        KeyCode.RightCommand
    };

    internal static bool IsDown(KeyboardShortcut shortcut) =>
        shortcut.MainKey != KeyCode.None &&
        WasPressedThisFrame(shortcut.MainKey) &&
        ModifiersMatch(shortcut);

    internal static bool IsPressed(KeyboardShortcut shortcut) =>
        shortcut.MainKey != KeyCode.None &&
        IsKeyPressed(shortcut.MainKey) &&
        ModifiersMatch(shortcut);

    internal static bool IsKeyPressed(KeyCode keyCode) =>
        TryGetKey(keyCode, out Key key) && Keyboard.current != null && Keyboard.current[key].isPressed;

    private static bool ModifiersMatch(KeyboardShortcut shortcut)
    {
        foreach (KeyCode required in shortcut.Modifiers)
        {
            if (!IsKeyPressed(required))
                return false;
        }

        foreach (KeyCode modifier in ModifierKeys)
        {
            if (modifier != shortcut.MainKey && IsKeyPressed(modifier) && !HasModifier(shortcut, modifier))
                return false;
        }

        return true;
    }

    private static bool HasModifier(KeyboardShortcut shortcut, KeyCode modifier)
    {
        foreach (KeyCode required in shortcut.Modifiers)
        {
            if (required == modifier)
                return true;
        }

        return false;
    }

    private static bool WasPressedThisFrame(KeyCode keyCode) =>
        TryGetKey(keyCode, out Key key) && Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame;

    private static bool TryGetKey(KeyCode keyCode, out Key key)
    {
        key = keyCode switch
        {
            KeyCode.Alpha0 => Key.Digit0,
            KeyCode.Alpha1 => Key.Digit1,
            KeyCode.Alpha2 => Key.Digit2,
            KeyCode.Alpha3 => Key.Digit3,
            KeyCode.Alpha4 => Key.Digit4,
            KeyCode.Alpha5 => Key.Digit5,
            KeyCode.Alpha6 => Key.Digit6,
            KeyCode.Alpha7 => Key.Digit7,
            KeyCode.Alpha8 => Key.Digit8,
            KeyCode.Alpha9 => Key.Digit9,
            KeyCode.Keypad0 => Key.Numpad0,
            KeyCode.Keypad1 => Key.Numpad1,
            KeyCode.Keypad2 => Key.Numpad2,
            KeyCode.Keypad3 => Key.Numpad3,
            KeyCode.Keypad4 => Key.Numpad4,
            KeyCode.Keypad5 => Key.Numpad5,
            KeyCode.Keypad6 => Key.Numpad6,
            KeyCode.Keypad7 => Key.Numpad7,
            KeyCode.Keypad8 => Key.Numpad8,
            KeyCode.Keypad9 => Key.Numpad9,
            KeyCode.KeypadPeriod => Key.NumpadPeriod,
            KeyCode.KeypadDivide => Key.NumpadDivide,
            KeyCode.KeypadMultiply => Key.NumpadMultiply,
            KeyCode.KeypadMinus => Key.NumpadMinus,
            KeyCode.KeypadPlus => Key.NumpadPlus,
            KeyCode.KeypadEnter => Key.NumpadEnter,
            KeyCode.KeypadEquals => Key.NumpadEquals,
            KeyCode.Return => Key.Enter,
            KeyCode.BackQuote => Key.Backquote,
            KeyCode.LeftControl => Key.LeftCtrl,
            KeyCode.RightControl => Key.RightCtrl,
            KeyCode.LeftCommand => Key.LeftMeta,
            KeyCode.RightCommand => Key.RightMeta,
            KeyCode.Menu => Key.ContextMenu,
            KeyCode.Print => Key.PrintScreen,
            KeyCode.Break => Key.Pause,
            _ => Key.None
        };

        if (key != Key.None)
            return true;

        return Enum.TryParse(keyCode.ToString(), ignoreCase: false, out key) && key != Key.None;
    }
}
