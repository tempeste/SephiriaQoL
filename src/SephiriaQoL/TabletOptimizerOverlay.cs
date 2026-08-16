using BepInEx.Configuration;
using BepInEx.Logging;
using Mirror;
using System;
using UnityEngine;

namespace SephiriaQoL;

internal sealed class TabletOptimizerOverlay
{
    private readonly ConfigEntry<bool> _showPanel;
    private readonly ConfigEntry<KeyboardShortcut> _toggleHotkey;
    private readonly ConfigEntry<int> _passes;
    private readonly ConfigEntry<bool> _allowRotation;
    private readonly ConfigEntry<bool> _preferConditionalSynergies;
    private readonly ConfigEntry<float> _panelScale;
    private readonly ManualLogSource _logger;

    private PlayerAvatar _player;
    private Rect _windowRect = new Rect(20f, 90f, 330f, 266f);
    private float _nextPlayerLookup;
    private float _nextAllowedRun;
    private float _statusUntil;
    private string _status = "Open a run to optimize your inventory.";
    private bool _guiLogged;
    private bool _toggleHotkeyHeld;

    internal TabletOptimizerOverlay(
        ConfigEntry<bool> showPanel,
        ConfigEntry<KeyboardShortcut> toggleHotkey,
        ConfigEntry<int> passes,
        ConfigEntry<bool> allowRotation,
        ConfigEntry<bool> preferConditionalSynergies,
        ConfigEntry<float> panelScale,
        ManualLogSource logger)
    {
        _showPanel = showPanel;
        _toggleHotkey = toggleHotkey;
        _passes = passes;
        _allowRotation = allowRotation;
        _preferConditionalSynergies = preferConditionalSynergies;
        _panelScale = panelScale;
        _logger = logger;
        _passes.Value = Mathf.Clamp(_passes.Value, 1, 4);
    }

    internal void Update()
    {
        bool toggleHotkeyPressed = ShortcutInput.IsPressed(_toggleHotkey.Value);
        if (toggleHotkeyPressed && !_toggleHotkeyHeld)
            _showPanel.Value = !_showPanel.Value;
        _toggleHotkeyHeld = toggleHotkeyPressed;

        if (Time.unscaledTime < _nextPlayerLookup)
            return;

        _nextPlayerLookup = Time.unscaledTime + 0.5f;
        PlayerAvatar candidate = null;
        if (NetworkClient.localPlayer != null)
            candidate = NetworkClient.localPlayer.GetComponent<PlayerAvatar>();
        else if (!NetworkClient.active)
            candidate = UnityEngine.Object.FindFirstObjectByType<PlayerAvatar>(FindObjectsInactive.Exclude);

        _player = candidate;
    }

    internal void OnGUI()
    {
        if (!_guiLogged)
        {
            _guiLogged = true;
            _logger.LogDebug($"Tablet optimizer GUI initialized; visible={_showPanel.Value}.");
        }

        if (!_showPanel.Value)
            return;

        float scale = OverlayGui.ResolveScale(_panelScale);
        _windowRect = OverlayGui.BeginScaledWindow(
            43131,
            _windowRect,
            360f,
            294f,
            scale,
            DrawWindow,
            out _);
    }

    private void DrawWindow(int id)
    {
        GridInventory inventory = _player != null ? _player.Inventory : null;
        int tabletCount = inventory?.CurrentStoneTabletsCount ?? 0;
        int charmCount = inventory?.charms?.Count ?? 0;

        OverlayGui.DrawHeader(new Rect(4f, 4f, 352f, 32f));
        GUI.Label(new Rect(12f, 7f, 165f, 24f), "Tablet Optimizer", OverlayGui.TitleStyle);
        OverlayGui.DrawScaleControls(_panelScale, 190f, 6f);
        if (GUI.Button(new Rect(326f, 6f, 22f, 22f), "×", OverlayGui.ButtonStyle))
            _showPanel.Value = false;

        GUI.Label(new Rect(12f, 42f, 336f, 40f),
            "Rearranges your whole inventory to maximize useful damage links, charm levels, and tablet bonuses.",
            OverlayGui.LabelStyle);
        GUI.Label(new Rect(12f, 84f, 220f, 22f),
            inventory == null ? "No active player inventory" : $"Tablets: {tabletCount}   Charms: {charmCount}",
            OverlayGui.MutedStyle);

        GUI.Label(new Rect(12f, 114f, 80f, 22f), "Passes", OverlayGui.LabelStyle);
        if (GUI.Button(new Rect(90f, 112f, 30f, 24f), "−", OverlayGui.ButtonStyle))
            _passes.Value = Mathf.Max(1, _passes.Value - 1);
        GUI.Label(new Rect(126f, 114f, 28f, 22f), _passes.Value.ToString(), OverlayGui.LabelStyle);
        if (GUI.Button(new Rect(156f, 112f, 30f, 24f), "+", OverlayGui.ButtonStyle))
            _passes.Value = Mathf.Min(4, _passes.Value + 1);

        bool allowRotation = GUI.Toggle(new Rect(226f, 114f, 122f, 22f),
            _allowRotation.Value, "Rotate tablets", OverlayGui.ToggleStyle);
        if (allowRotation != _allowRotation.Value)
            _allowRotation.Value = allowRotation;

        bool preferConditionals = GUI.Toggle(new Rect(12f, 142f, 336f, 22f),
            _preferConditionalSynergies.Value, "Prefer positional relic synergies", OverlayGui.ToggleStyle);
        if (preferConditionals != _preferConditionalSynergies.Value)
            _preferConditionalSynergies.Value = preferConditionals;

        bool canRun = inventory != null && charmCount > 0 && Time.unscaledTime >= _nextAllowedRun;
        bool previousEnabled = GUI.enabled;
        GUI.enabled = canRun;
        if (GUI.Button(new Rect(12f, 172f, 336f, 36f), $"Optimize layout ({_passes.Value} pass{(_passes.Value == 1 ? "" : "es")})",
                OverlayGui.SelectedButtonStyle))
            Optimize(inventory);
        GUI.enabled = previousEnabled;

        string status = Time.unscaledTime <= _statusUntil || inventory == null
            ? _status
            : "Ready. Higher pass counts may briefly pause the game.";
        GUI.Label(new Rect(12f, 216f, 336f, 42f), status, OverlayGui.LabelStyle);
        GUI.Label(new Rect(12f, 264f, 336f, 20f), "Changes only your own inventory; click once and wait.", OverlayGui.MutedStyle);

        GUI.DragWindow(new Rect(0f, 0f, 210f, 34f));
    }

    private void Optimize(GridInventory inventory)
    {
        int passes = Mathf.Clamp(_passes.Value, 1, 4);
        _nextAllowedRun = Time.unscaledTime + 3f;
        _status = "Optimization requested. Please wait for the inventory to settle…";
        _statusUntil = Time.unscaledTime + 8f;

        try
        {
            inventory.RequestAutoArrangeInventoryForBestCharmLevels(passes, _allowRotation.Value);
            _logger.LogInfo($"Tablet optimization requested: {passes} pass(es), rotation={_allowRotation.Value}.");
        }
        catch (Exception exception)
        {
            _status = "Could not optimize; see the BepInEx log for details.";
            _statusUntil = Time.unscaledTime + 8f;
            _logger.LogError($"Tablet optimization failed: {exception}");
        }
    }
}
