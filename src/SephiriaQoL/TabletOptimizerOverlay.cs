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
    private readonly ManualLogSource _logger;

    private PlayerAvatar _player;
    private Rect _windowRect = new Rect(20f, 90f, 330f, 238f);
    private float _nextPlayerLookup;
    private float _nextAllowedRun;
    private float _statusUntil;
    private string _status = "Open a run to optimize your inventory.";
    private bool _guiLogged;

    internal TabletOptimizerOverlay(
        ConfigEntry<bool> showPanel,
        ConfigEntry<KeyboardShortcut> toggleHotkey,
        ConfigEntry<int> passes,
        ConfigEntry<bool> allowRotation,
        ManualLogSource logger)
    {
        _showPanel = showPanel;
        _toggleHotkey = toggleHotkey;
        _passes = passes;
        _allowRotation = allowRotation;
        _logger = logger;
        _passes.Value = Mathf.Clamp(_passes.Value, 1, 4);
    }

    internal void Update()
    {
        if (_toggleHotkey.Value.IsDown())
            _showPanel.Value = !_showPanel.Value;

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

        _windowRect.x = Mathf.Clamp(_windowRect.x, 0f, Math.Max(0f, Screen.width - _windowRect.width));
        _windowRect.y = Mathf.Clamp(_windowRect.y, 0f, Math.Max(0f, Screen.height - 30f));
        _windowRect = GUI.Window(43131, _windowRect, DrawWindow, "Tablet optimizer  [F10]");
    }

    private void DrawWindow(int id)
    {
        GridInventory inventory = _player != null ? _player.Inventory : null;
        int tabletCount = inventory?.CurrentStoneTabletsCount ?? 0;
        int charmCount = inventory?.charms?.Count ?? 0;

        GUI.Label(new Rect(12f, 28f, 306f, 40f),
            "Rearranges your whole inventory to maximize active charm levels and tablet bonuses.");
        GUI.Label(new Rect(12f, 70f, 190f, 22f),
            inventory == null ? "No active player inventory" : $"Tablets: {tabletCount}   Charms: {charmCount}");

        GUI.Label(new Rect(12f, 100f, 80f, 22f), "Passes");
        if (GUI.Button(new Rect(90f, 98f, 30f, 24f), "−"))
            _passes.Value = Mathf.Max(1, _passes.Value - 1);
        GUI.Label(new Rect(126f, 100f, 28f, 22f), _passes.Value.ToString());
        if (GUI.Button(new Rect(156f, 98f, 30f, 24f), "+"))
            _passes.Value = Mathf.Min(4, _passes.Value + 1);

        bool allowRotation = GUI.Toggle(new Rect(204f, 100f, 114f, 22f),
            _allowRotation.Value, "Rotate tablets");
        if (allowRotation != _allowRotation.Value)
            _allowRotation.Value = allowRotation;

        bool canRun = inventory != null && charmCount > 0 && Time.unscaledTime >= _nextAllowedRun;
        bool previousEnabled = GUI.enabled;
        GUI.enabled = canRun;
        if (GUI.Button(new Rect(12f, 132f, 306f, 34f), $"Optimize layout ({_passes.Value} pass{(_passes.Value == 1 ? "" : "es")})"))
            Optimize(inventory);
        GUI.enabled = previousEnabled;

        string status = Time.unscaledTime <= _statusUntil || inventory == null
            ? _status
            : "Ready. Higher pass counts may briefly pause the game.";
        GUI.Label(new Rect(12f, 174f, 306f, 42f), status);
        GUI.Label(new Rect(12f, 214f, 306f, 20f), "Changes only your own inventory; click once and wait.");

        GUI.DragWindow(new Rect(0f, 0f, _windowRect.width, 25f));
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
