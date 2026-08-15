using BepInEx.Configuration;
using System.Collections.Generic;
using UnityEngine;

namespace SephiriaQoL;

internal sealed partial class QoLControlCenter
{
    private float DrawHotkeyRow(
        float y,
        string title,
        ConfigEntry<KeyboardShortcut> entry)
    {
        Rect row = new Rect(16f, y, 488f, 42f);
        bool capturing = _capturingHotkey == entry;
        OverlayGui.DrawPanel(row, capturing ? OverlayGui.Header : OverlayGui.PanelRaised,
            capturing ? OverlayGui.Accent : OverlayGui.Border);
        GUI.Label(new Rect(30f, y + 10f, 222f, 22f), title, OverlayGui.LabelStyle);

        if (GUI.Button(new Rect(258f, y + 7f, 182f, 28f),
                capturing ? "Press a key…" : FormatShortcut(entry.Value),
                capturing ? OverlayGui.SelectedButtonStyle : OverlayGui.ButtonStyle))
            _capturingHotkey = capturing ? null : entry;
        if (GUI.Button(new Rect(446f, y + 7f, 42f, 28f), "×", OverlayGui.ButtonStyle))
        {
            entry.Value = new KeyboardShortcut(KeyCode.None);
            if (_capturingHotkey == entry)
                _capturingHotkey = null;
        }

        return y + 48f;
    }

    private void HandleHotkeyCapture()
    {
        if (_capturingHotkey == null || Event.current == null || Event.current.type != EventType.KeyDown)
            return;

        KeyCode mainKey = Event.current.keyCode;
        if (mainKey == KeyCode.Escape)
        {
            _capturingHotkey = null;
            Event.current.Use();
            return;
        }
        if (mainKey == KeyCode.None || IsModifier(mainKey))
            return;

        var modifiers = new List<KeyCode>();
        AddHeldModifier(modifiers, KeyCode.LeftControl, KeyCode.RightControl);
        AddHeldModifier(modifiers, KeyCode.LeftShift, KeyCode.RightShift);
        AddHeldModifier(modifiers, KeyCode.LeftAlt, KeyCode.RightAlt);
        AddHeldModifier(modifiers, KeyCode.LeftCommand, KeyCode.RightCommand);
        modifiers.Remove(mainKey);
        _capturingHotkey.Value = new KeyboardShortcut(mainKey, modifiers.ToArray());
        _capturingHotkey = null;
        Event.current.Use();
    }

    private static void AddHeldModifier(ICollection<KeyCode> modifiers, KeyCode left, KeyCode right)
    {
        if (Input.GetKey(left))
            modifiers.Add(left);
        else if (Input.GetKey(right))
            modifiers.Add(right);
    }

    private static bool IsModifier(KeyCode key) =>
        key == KeyCode.LeftControl || key == KeyCode.RightControl ||
        key == KeyCode.LeftShift || key == KeyCode.RightShift ||
        key == KeyCode.LeftAlt || key == KeyCode.RightAlt ||
        key == KeyCode.LeftCommand || key == KeyCode.RightCommand;

    private static string FormatShortcut(KeyboardShortcut shortcut) =>
        shortcut.MainKey == KeyCode.None ? "Unbound" : shortcut.ToString().ToUpperInvariant();
}
