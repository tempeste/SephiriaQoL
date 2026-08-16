using BepInEx.Configuration;
using UnityEngine;

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
        Input.GetKeyDown(shortcut.MainKey) &&
        ModifiersMatch(shortcut);

    internal static bool IsPressed(KeyboardShortcut shortcut) =>
        shortcut.MainKey != KeyCode.None &&
        Input.GetKey(shortcut.MainKey) &&
        ModifiersMatch(shortcut);

    private static bool ModifiersMatch(KeyboardShortcut shortcut)
    {
        foreach (KeyCode required in shortcut.Modifiers)
        {
            if (!Input.GetKey(required))
                return false;
        }

        foreach (KeyCode modifier in ModifierKeys)
        {
            if (modifier != shortcut.MainKey && Input.GetKey(modifier) && !HasModifier(shortcut, modifier))
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
}
