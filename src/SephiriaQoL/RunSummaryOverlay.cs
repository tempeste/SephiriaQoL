using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SephiriaQoL;

internal sealed class RunSummaryOverlay
{
    private static RunSummaryOverlay _instance;

    private readonly ConfigEntry<bool> _enabled;
    private readonly ConfigEntry<float> _panelScale;
    private readonly ConfigEntry<KeyboardShortcut> _historyHotkey;
    private readonly UtilityOverlay _utility;
    private readonly RunSummaryHistoryStore _historyStore;
    private readonly List<RunSummaryRecord> _history;
    private readonly int _historyLimit;
    private List<UtilityOverlay.DamageEntry> _entries = new List<UtilityOverlay.DamageEntry>();
    private Rect _windowRect = new Rect(470f, 110f, 560f, 460f);
    private Vector2 _scroll;
    private UtilityOverlay.DamageEntry _selected;
    private float _playedSeconds;
    private int _historyIndex = -1;
    private bool _visible;

    internal RunSummaryOverlay(
        ConfigEntry<bool> enabled,
        ConfigEntry<float> panelScale,
        ConfigEntry<KeyboardShortcut> historyHotkey,
        ConfigEntry<int> historyLimit,
        UtilityOverlay utility)
    {
        _enabled = enabled;
        _panelScale = panelScale;
        _historyHotkey = historyHotkey;
        _utility = utility;
        _historyLimit = historyLimit.Value;
        _historyStore = new RunSummaryHistoryStore(_historyLimit);
        _history = _historyStore.Load();
        _instance = this;
    }

    internal void Update()
    {
        if (_enabled.Value != true || !_historyHotkey.Value.IsDown() || _history.Count == 0)
            return;

        if (_visible)
        {
            _visible = false;
            return;
        }

        ShowRecord(_history.Count - 1);
    }

    internal void OnGUI()
    {
        if (_enabled.Value != true || !_visible || _entries.Count == 0)
            return;

        float scale = OverlayGui.ResolveScale(_panelScale);
        float height = Mathf.Min(560f, Mathf.Max(250f, Screen.height / scale - 36f));
        _windowRect = OverlayGui.BeginScaledWindow(
            43149,
            _windowRect,
            560f,
            height,
            scale,
            id => DrawWindow(id, height),
            out _);
    }

    internal void Dispose()
    {
        if (_instance == this)
            _instance = null;
    }

    private void Capture()
    {
        if (_enabled.Value != true)
            return;

        _entries = _utility.CaptureDamageSnapshot();
        _playedSeconds = UtilityOverlay.GetPlayedSeconds();
        _selected = null;
        _scroll = Vector2.zero;
        _visible = _entries.Count > 0;
        _historyIndex = -1;
        if (!_visible)
            return;

        _history.Add(new RunSummaryRecord
        {
            TimestampUtc = DateTime.UtcNow,
            PlayedSeconds = _playedSeconds,
            Entries = _entries
        });
        if (_history.Count > _historyLimit)
            _history.RemoveRange(0, _history.Count - _historyLimit);
        _historyIndex = _history.Count - 1;
        try
        {
            _historyStore.Save(_history);
        }
        catch
        {
            // History is optional; the live summary should still open if local persistence fails.
        }
    }

    private void DrawWindow(int id, float height)
    {
        const float width = 560f;
        float partyDamage = _entries.Sum(entry => entry.Damage);
        float partyTaken = _entries.Sum(entry => entry.DamageTaken);

        OverlayGui.Fill(new Rect(0f, 0f, width, 42f), OverlayGui.PanelRaised);
        OverlayGui.Fill(new Rect(0f, 41f, width, 1f), OverlayGui.Border);
        OverlayGui.Fill(new Rect(0f, 0f, 5f, 42f), OverlayGui.Accent);
        GUI.Label(new Rect(16f, 7f, 174f, 26f), "TEAM RUN SUMMARY", OverlayGui.TitleStyle);
        if (_historyIndex > 0 && GUI.Button(new Rect(194f, 10f, 31f, 22f), "‹", OverlayGui.ButtonStyle))
            ShowRecord(_historyIndex - 1);
        if (_historyIndex >= 0 && _historyIndex < _history.Count - 1 &&
            GUI.Button(new Rect(229f, 10f, 31f, 22f), "›", OverlayGui.ButtonStyle))
            ShowRecord(_historyIndex + 1);
        if (_historyIndex >= 0)
            GUI.Label(new Rect(267f, 11f, 98f, 20f), $"RUN {_historyIndex + 1}/{_history.Count}", OverlayGui.MutedStyle);
        OverlayGui.DrawScaleControls(_panelScale, 372f, 10f);
        if (GUI.Button(new Rect(522f, 10f, 25f, 22f), "×", OverlayGui.ButtonStyle))
            _visible = false;

        TimeSpan elapsed = TimeSpan.FromSeconds(Math.Max(0f, _playedSeconds));
        string totals = $"{_entries.Count} PLAYERS   •   {elapsed.Hours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}   •   OUT {partyDamage:N0}   •   IN {partyTaken:N0}";
        GUI.Label(new Rect(16f, 49f, 528f, 22f), totals, OverlayGui.LabelStyle);
        string recordedAt = _historyIndex >= 0 && _historyIndex < _history.Count
            ? $"   •   {_history[_historyIndex].TimestampUtc.ToLocalTime():yyyy-MM-dd HH:mm}"
            : string.Empty;
        GUI.Label(new Rect(16f, 71f, 528f, 18f),
            $"CLICK A PLAYER FOR THEIR TOP DAMAGE SOURCES{recordedAt}", OverlayGui.MutedStyle);

        float contentHeight = 8f + _entries.Count * 58f + (_selected == null ? 0f : 126f);
        Rect viewport = new Rect(0f, 94f, width, Mathf.Max(1f, height - 94f));
        _scroll = GUI.BeginScrollView(
            viewport,
            _scroll,
            new Rect(0f, 0f, width, contentHeight),
            false,
            contentHeight > viewport.height);

        float y = 4f;
        for (int i = 0; i < _entries.Count; i++)
        {
            UtilityOverlay.DamageEntry entry = _entries[i];
            DrawPlayerRow(entry, i + 1, partyDamage, y);
            y += 58f;
        }

        if (_selected != null)
            DrawSources(_selected, y + 2f);

        GUI.EndScrollView();
        GUI.DragWindow(new Rect(0f, 0f, 350f, 42f));
    }

    private void DrawPlayerRow(UtilityOverlay.DamageEntry entry, int rank, float partyDamage, float y)
    {
        Rect row = new Rect(12f, y, 536f, 50f);
        bool selected = _selected == entry;
        OverlayGui.Fill(row, selected
            ? new Color(entry.Color.r, entry.Color.g, entry.Color.b, 0.17f)
            : OverlayGui.PanelRaised);
        OverlayGui.Fill(new Rect(row.x, row.y, 4f, row.height), entry.Color);
        OverlayGui.Outline(row, selected ? entry.Color : OverlayGui.Border);

        if (GUI.Button(row, GUIContent.none, GUIStyle.none))
            _selected = selected ? null : entry;

        float share = partyDamage > 0f ? entry.Damage / partyDamage : 0f;
        float dps = entry.Damage / Mathf.Max(1f, _playedSeconds);
        float? previousDamage = PreviousDamage(entry.Name);
        string delta = previousDamage.HasValue ? $"   •   Δ {entry.Damage - previousDamage.Value:+0;-0;0}" : string.Empty;
        GUI.Label(new Rect(24f, y + 4f, 28f, 20f), $"{rank:00}", OverlayGui.MutedStyle);
        GUI.Label(new Rect(54f, y + 4f, 205f, 20f), entry.Name, OverlayGui.LabelStyle);
        GUI.Label(new Rect(264f, y + 4f, 272f, 20f),
            $"OUT {entry.Damage:N0}   •   {share:P1}", OverlayGui.RightStyle);
        GUI.Label(new Rect(54f, y + 25f, 482f, 18f),
            $"IN {entry.DamageTaken:N0}   •   AVG DPS {dps:N1}   •   AREA {entry.AreaDamage:N0}{delta}",
            OverlayGui.MutedStyle);
    }

    private void ShowRecord(int index)
    {
        if (index < 0 || index >= _history.Count)
            return;

        RunSummaryRecord record = _history[index];
        _historyIndex = index;
        _entries = record.Entries;
        _playedSeconds = record.PlayedSeconds;
        _selected = null;
        _scroll = Vector2.zero;
        _visible = _entries.Count > 0;
    }

    private float? PreviousDamage(string playerName)
    {
        if (_historyIndex <= 0)
            return null;

        return _history[_historyIndex - 1].Entries
            .FirstOrDefault(entry => string.Equals(entry.Name, playerName, StringComparison.OrdinalIgnoreCase))?.Damage;
    }

    private static void DrawSources(UtilityOverlay.DamageEntry entry, float y)
    {
        Rect panel = new Rect(12f, y, 536f, 116f);
        OverlayGui.Fill(panel, new Color(0.03f, 0.043f, 0.05f, 0.98f));
        OverlayGui.Outline(panel, entry.Color);
        GUI.Label(new Rect(24f, y + 8f, 330f, 20f), $"{entry.Name.ToUpperInvariant()} • TOP SOURCES", OverlayGui.TitleStyle);

        int count = Math.Min(4, entry.Sources.Count);
        float sourceY = y + 32f;
        for (int i = 0; i < count; i++)
        {
            UtilityOverlay.DamageSourceEntry source = entry.Sources[i];
            OverlayGui.Fill(new Rect(24f, sourceY + 6f, 7f, 7f), OverlayGui.ElementColor(source.Element));
            GUI.Label(new Rect(36f, sourceY, 340f, 19f), source.Name, OverlayGui.LabelStyle);
            float share = entry.Damage > 0f ? source.Damage / entry.Damage : 0f;
            GUI.Label(new Rect(380f, sourceY, 156f, 19f), $"{source.Damage:N0}  •  {share:P0}", OverlayGui.SmallRightStyle);
            sourceY += 20f;
        }

        if (count == 0)
            GUI.Label(new Rect(24f, sourceY, 500f, 19f), "No damage sources were recorded.", OverlayGui.MutedStyle);
    }

    [HarmonyPatch(typeof(UI_GameOverLabel), nameof(UI_GameOverLabel.OnOpened))]
    private static class GameOverOpenedPatch
    {
        private static void Postfix() => _instance?.Capture();
    }

    [HarmonyPatch(typeof(UI_GameOverLabel), nameof(UI_GameOverLabel.OnClosed))]
    private static class GameOverClosedPatch
    {
        private static void Postfix()
        {
            if (_instance != null)
                _instance._visible = false;
        }
    }
}
