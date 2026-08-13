using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace SephiriaQoL;

internal sealed class UtilityOverlay
{
    private sealed class DamageEntry
    {
        internal string Name;
        internal float Damage;
    }

    private static readonly FieldInfo PlayedRealtimeField =
        AccessTools.Field(typeof(DungeonManager), "playedRealtimeClientside");
    private static readonly PropertyInfo PlayerNameProperty =
        AccessTools.Property(typeof(PlayerAvatar), "Name");

    private static ConfigEntry<bool> _showTimer;
    private static ConfigEntry<bool> _showDamage;

    private readonly List<DamageEntry> _damage = new List<DamageEntry>();
    private float _nextRefresh;
    private Rect _damageRect = new Rect(20f, 450f, 330f, 80f);
    private bool _collapsed;
    private GUIStyle _timerStyle;
    private GUIStyle _rightStyle;

    internal static void Configure(ConfigEntry<bool> showTimer, ConfigEntry<bool> showDamage)
    {
        _showTimer = showTimer;
        _showDamage = showDamage;
    }

    internal void Update()
    {
        if (Time.unscaledTime < _nextRefresh)
            return;
        _nextRefresh = Time.unscaledTime + 0.15f;

        _damage.Clear();
        if (_showDamage?.Value != true || PlayerSpawner.MultiplayerList == null)
            return;

        foreach (PlayerSpawner spawner in PlayerSpawner.MultiplayerList)
        {
            PlayerAvatar player = spawner?.PlayerAvatar;
            if (player == null || !player.gameObject.activeInHierarchy || player.dealsStatistics == null)
                continue;

            float damage = player.dealsStatistics.Values.Sum();
            if (damage <= 0f)
                continue;

            string name = PlayerNameProperty?.GetValue(player) as string;
            _damage.Add(new DamageEntry
            {
                Name = string.IsNullOrEmpty(name) ? player.name : name,
                Damage = damage
            });
        }

        _damage.Sort((a, b) => b.Damage.CompareTo(a.Damage));
    }

    internal void OnGUI()
    {
        EnsureStyles();

        if (_showTimer?.Value == true && DungeonManager.Instance != null)
        {
            float seconds = PlayedRealtimeField?.GetValue(DungeonManager.Instance) is float value ? value : 0f;
            TimeSpan elapsed = TimeSpan.FromSeconds(Math.Max(0f, seconds));
            string timer = $"{(int)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";
            GUI.Label(new Rect((Screen.width - 180f) * 0.5f, 16f, 180f, 32f), timer, _timerStyle);
        }

        if (_showDamage?.Value == true && _damage.Count > 0)
        {
            float height = _collapsed ? 50f : 54f + _damage.Count * 42f;
            _damageRect.height = height;
            _damageRect = GUI.Window(43128, _damageRect, DrawDamageWindow, "Damage contribution");
        }
    }

    private void DrawDamageWindow(int id)
    {
        if (GUI.Button(new Rect(_damageRect.width - 38f, 2f, 32f, 22f), _collapsed ? "+" : "−"))
            _collapsed = !_collapsed;

        if (!_collapsed)
        {
            float total = _damage.Sum(entry => entry.Damage);
            float y = 29f;
            foreach (DamageEntry entry in _damage)
            {
                float ratio = total > 0f ? entry.Damage / total : 0f;
                GUI.Label(new Rect(12f, y, 205f, 20f), $"{entry.Name}  ({entry.Damage:N0})");
                GUI.Label(new Rect(220f, y, 94f, 20f), $"{ratio:P0}", _rightStyle);
                y += 19f;

                Rect track = new Rect(12f, y, 302f, 8f);
                GUI.Box(track, GUIContent.none);
                Color previous = GUI.color;
                GUI.color = new Color(0.46f, 0.9f, 0.15f, 1f);
                GUI.Box(new Rect(track.x, track.y, track.width * Mathf.Clamp01(ratio), track.height), GUIContent.none);
                GUI.color = previous;
                y += 23f;
            }
        }

        GUI.DragWindow(new Rect(0f, 0f, _damageRect.width - 45f, 25f));
    }

    private void EnsureStyles()
    {
        if (_timerStyle != null)
            return;

        _timerStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 18,
            fontStyle = FontStyle.Bold
        };
        _rightStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleRight
        };
    }
}

