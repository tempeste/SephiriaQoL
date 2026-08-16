using BepInEx.Configuration;
using BepInEx.Logging;
using System;
using UnityEngine;

namespace SephiriaQoL;

internal sealed class FastShopRerollFeature
{
    private readonly ConfigEntry<bool> _enabled;
    private readonly ConfigEntry<KeyboardShortcut> _hotkey;
    private readonly ConfigEntry<float> _repeatInterval;
    private readonly ManualLogSource _log;
    private float _nextRerollTime;

    internal FastShopRerollFeature(
        ConfigEntry<bool> enabled,
        ConfigEntry<KeyboardShortcut> hotkey,
        ConfigEntry<float> repeatInterval,
        ManualLogSource log)
    {
        _enabled = enabled;
        _hotkey = hotkey;
        _repeatInterval = repeatInterval;
        _log = log;
    }

    internal void Update()
    {
        if (_enabled.Value != true || !ShortcutInput.IsPressed(_hotkey.Value))
        {
            _nextRerollTime = 0f;
            return;
        }

        if (Time.unscaledTime < _nextRerollTime)
            return;

        try
        {
            UI_ShopPanel panel = UIManager.Instance?.GetElement<UI_ShopPanel>();
            if (panel == null || !panel.IsOpened)
                return;

            panel.DoReplenishment();
            _nextRerollTime = Time.unscaledTime + Mathf.Clamp(_repeatInterval.Value, 0.15f, 1f);
        }
        catch (Exception exception)
        {
            _nextRerollTime = Time.unscaledTime + 1f;
            _log?.LogWarning($"Fast shop reroll failed: {exception.Message}");
        }
    }
}
