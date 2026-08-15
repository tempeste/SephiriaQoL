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
    internal sealed class DamageSourceEntry
    {
        internal string Name;
        internal EDamageElementalType Element;
        internal float Damage;
    }

    internal sealed class DamageEntry
    {
        internal PlayerAvatar Player;
        internal string Name;
        internal float Damage;
        internal float AreaDamage;
        internal float DamageTaken;
        internal float Hp;
        internal float MaxHp;
        internal bool IsDead;
        internal Color Color;
        internal readonly List<DamageSourceEntry> Sources = new List<DamageSourceEntry>();
        internal readonly Dictionary<EDamageElementalType, float> ElementDamage = new Dictionary<EDamageElementalType, float>();
    }

    private static readonly FieldInfo PlayedRealtimeField =
        AccessTools.Field(typeof(DungeonManager), "playedRealtimeClientside");
    private static readonly PropertyInfo PlayerNameProperty =
        AccessTools.Property(typeof(PlayerAvatar), "Name");

    private static ConfigEntry<bool> _showTimer;
    private static ConfigEntry<bool> _showDamage;
    private static ConfigEntry<bool> _showDamageTaken;
    private static ConfigEntry<float> _damageScale;

    private readonly List<DamageEntry> _damage = new List<DamageEntry>();
    private readonly Dictionary<PlayerAvatar, int> _playerColorSlots = new Dictionary<PlayerAvatar, int>();
    private readonly Dictionary<string, string> _damageSourceNames = new Dictionary<string, string>();
    private float _nextRefresh;
    private Rect _damageRect = new Rect(20f, 450f, 380f, 80f);
    private Vector2 _damageScroll;
    private float _logicalDamageHeight;
    private bool _collapsed;
    private PlayerAvatar _selectedPlayer;
    private int _nextColorSlot;
    private GUIStyle _timerStyle;

    internal static void Configure(
        ConfigEntry<bool> showTimer,
        ConfigEntry<bool> showDamage,
        ConfigEntry<bool> showDamageTaken,
        ConfigEntry<float> damageScale)
    {
        _showTimer = showTimer;
        _showDamage = showDamage;
        _showDamageTaken = showDamageTaken;
        _damageScale = damageScale;
    }

    internal void Update()
    {
        if (Time.unscaledTime < _nextRefresh)
            return;
        _nextRefresh = Time.unscaledTime + 0.5f;

        RefreshDamageEntries(force: false);
    }

    internal List<DamageEntry> CaptureDamageSnapshot()
    {
        RefreshDamageEntries(force: true);
        return _damage.Select(CloneEntry).ToList();
    }

    internal static float GetPlayedSeconds()
    {
        if (DungeonManager.Instance == null)
            return 0f;

        return PlayedRealtimeField?.GetValue(DungeonManager.Instance) is float value ? Mathf.Max(0f, value) : 0f;
    }

    private void RefreshDamageEntries(bool force)
    {

        _damage.Clear();
        if ((!force && _showDamage?.Value != true) || PlayerSpawner.MultiplayerList == null)
            return;

        foreach (PlayerSpawner spawner in PlayerSpawner.MultiplayerList)
        {
            PlayerAvatar player = spawner?.PlayerAvatar;
            if (player == null || !player.gameObject.activeInHierarchy || player.dealsStatistics == null)
                continue;

            if (!_playerColorSlots.TryGetValue(player, out int colorSlot))
            {
                colorSlot = _nextColorSlot++ % OverlayGui.PlayerColors.Length;
                _playerColorSlots[player] = colorSlot;
            }

            DamageEntry entry = new DamageEntry
            {
                Player = player,
                Name = ReadPlayerName(player),
                Damage = player.dealsStatistics.Values.Sum(),
                AreaDamage = player.dealsStatistics_LastLocation?.Values.Sum() ?? 0f,
                DamageTaken = player.receivedDamage,
                Hp = Mathf.Max(0f, player.hp),
                MaxHp = Mathf.Max(0f, player.MaxHp),
                IsDead = player.IsDead,
                Color = OverlayGui.PlayerColors[colorSlot]
            };

            foreach (KeyValuePair<DamageKey, float> source in player.dealsStatistics)
            {
                entry.Sources.Add(new DamageSourceEntry
                {
                    Name = ResolveDamageSourceName(source.Key),
                    Element = source.Key.ElementalType,
                    Damage = source.Value
                });

                if (entry.ElementDamage.ContainsKey(source.Key.ElementalType))
                    entry.ElementDamage[source.Key.ElementalType] += source.Value;
                else
                    entry.ElementDamage[source.Key.ElementalType] = source.Value;
            }

            entry.Sources.Sort((a, b) => b.Damage.CompareTo(a.Damage));
            _damage.Add(entry);
        }

        _damage.Sort((a, b) => b.Damage.CompareTo(a.Damage));
        if (_selectedPlayer != null && _damage.All(entry => entry.Player != _selectedPlayer))
            _selectedPlayer = null;
    }

    private static DamageEntry CloneEntry(DamageEntry source)
    {
        DamageEntry copy = new DamageEntry
        {
            Name = source.Name,
            Damage = source.Damage,
            AreaDamage = source.AreaDamage,
            DamageTaken = source.DamageTaken,
            Hp = source.Hp,
            MaxHp = source.MaxHp,
            IsDead = source.IsDead,
            Color = source.Color
        };

        copy.Sources.AddRange(source.Sources.Select(entry => new DamageSourceEntry
        {
            Name = entry.Name,
            Element = entry.Element,
            Damage = entry.Damage
        }));
        foreach (KeyValuePair<EDamageElementalType, float> pair in source.ElementDamage)
            copy.ElementDamage[pair.Key] = pair.Value;

        return copy;
    }

    internal void OnGUI()
    {
        EnsureTimerStyle();

        if (_showTimer?.Value == true && DungeonManager.Instance != null)
        {
            float seconds = GetPlayedSeconds();
            TimeSpan elapsed = TimeSpan.FromSeconds(Math.Max(0f, seconds));
            string timer = $"{(int)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";
            GUI.Label(new Rect((Screen.width - 180f) * 0.5f, 16f, 180f, 32f), timer, _timerStyle);
        }

        if (_showDamage?.Value != true || _damage.Count == 0)
            return;

        float scale = OverlayGui.ResolveScale(_damageScale);
        _logicalDamageHeight = CalculateWindowHeight(scale);
        _damageRect = OverlayGui.BeginScaledWindow(
            43128,
            _damageRect,
            380f,
            _logicalDamageHeight,
            scale,
            DrawDamageWindow,
            out _);
    }

    private void DrawDamageWindow(int id)
    {
        const float width = 380f;
        OverlayGui.Fill(new Rect(0f, 0f, width, 34f), OverlayGui.PanelRaised);
        OverlayGui.Fill(new Rect(0f, 33f, width, 1f), OverlayGui.Border);
        GUI.Label(new Rect(12f, 5f, 170f, 24f), "COMBAT CONTRIBUTION", OverlayGui.TitleStyle);
        OverlayGui.DrawScaleControls(_damageScale, 205f, 6f);
        if (GUI.Button(new Rect(340f, 6f, 28f, 22f), _collapsed ? "+" : "−", OverlayGui.ButtonStyle))
            _collapsed = !_collapsed;

        if (!_collapsed)
        {
            float total = _damage.Sum(entry => entry.Damage);
            float totalTaken = _damage.Sum(entry => entry.DamageTaken);
            GUI.Label(new Rect(12f, 38f, 205f, 18f), "RUN TOTALS • CLICK FOR DETAILS", OverlayGui.MutedStyle);
            string totalLabel = _showDamageTaken?.Value == true
                ? $"OUT {total:N0}  •  IN {totalTaken:N0}"
                : $"OUT {total:N0}";
            GUI.Label(new Rect(210f, 38f, 158f, 18f), totalLabel, OverlayGui.SmallRightStyle);

            float contentHeight = CalculateDamageContentHeight();
            Rect viewport = new Rect(0f, 58f, 380f, Mathf.Max(1f, _logicalDamageHeight - 58f));
            _damageScroll = GUI.BeginScrollView(
                viewport,
                _damageScroll,
                new Rect(0f, 0f, 380f, contentHeight),
                false,
                contentHeight > viewport.height);

            float y = 2f;
            int rank = 1;
            foreach (DamageEntry entry in _damage)
            {
                float ratio = total > 0f ? entry.Damage / total : 0f;
                DrawDamageRow(entry, rank, ratio, y);
                y += DamageRowHeight;
                rank++;
            }

            DamageEntry selected = _damage.FirstOrDefault(entry => entry.Player == _selectedPlayer);
            if (selected != null)
                DrawDetails(selected, total, y + 2f);

            GUI.EndScrollView();
        }

        GUI.DragWindow(new Rect(0f, 0f, 190f, 34f));
    }

    private void DrawDamageRow(DamageEntry entry, int rank, float ratio, float y)
    {
        bool showTaken = _showDamageTaken?.Value == true;
        Rect row = new Rect(10f, y, 360f, showTaken ? 52f : 44f);
        bool selected = entry.Player == _selectedPlayer;
        OverlayGui.Fill(row, selected ? new Color(entry.Color.r, entry.Color.g, entry.Color.b, 0.17f) : OverlayGui.PanelRaised);
        OverlayGui.Fill(new Rect(row.x, row.y, 3f, row.height), entry.Color);
        OverlayGui.Outline(row, selected ? entry.Color : OverlayGui.Border);

        if (GUI.Button(row, GUIContent.none, GUIStyle.none))
            _selectedPlayer = selected ? null : entry.Player;

        GUI.Label(new Rect(20f, y + 3f, 25f, 20f), $"{rank:00}", OverlayGui.MutedStyle);
        GUI.Label(new Rect(48f, y + 3f, 198f, 20f), entry.Name, OverlayGui.LabelStyle);
        if (entry.IsDead)
            GUI.Label(new Rect(246f, y + 3f, 45f, 20f), "DOWN", OverlayGui.MutedStyle);
        GUI.Label(new Rect(260f, y + 3f, 100f, 20f), $"OUT {entry.Damage:N0}", OverlayGui.RightStyle);

        if (showTaken)
            GUI.Label(new Rect(48f, y + 24f, 180f, 18f), $"IN {entry.DamageTaken:N0}", OverlayGui.MutedStyle);

        Rect track = new Rect(48f, y + (showTaken ? 43f : 28f), 260f, showTaken ? 4f : 6f);
        OverlayGui.Fill(track, OverlayGui.Track);
        OverlayGui.Fill(new Rect(track.x, track.y, track.width * Mathf.Clamp01(ratio), track.height), entry.Color);
        GUI.Label(new Rect(312f, y + (showTaken ? 24f : 20f), 48f, 20f), $"{ratio:P0}", OverlayGui.SmallRightStyle);
    }

    private void DrawDetails(DamageEntry entry, float partyTotal, float y)
    {
        int sourceCount = Math.Min(4, entry.Sources.Count);
        float panelHeight = 140f + sourceCount * 20f;
        Rect panel = new Rect(10f, y, 360f, panelHeight);
        OverlayGui.Fill(panel, new Color(0.03f, 0.043f, 0.05f, 0.98f));
        OverlayGui.Outline(panel, entry.Color);

        GUI.Label(new Rect(20f, y + 8f, 220f, 22f), entry.Name.ToUpperInvariant(), OverlayGui.TitleStyle);
        GUI.Label(new Rect(242f, y + 8f, 118f, 22f), entry.IsDead ? "DOWN" : "ACTIVE", OverlayGui.RightStyle);

        float playedSeconds = Mathf.Max(1f, GetPlayedSeconds());
        float hpRatio = entry.MaxHp > 0f ? Mathf.Clamp01(entry.Hp / entry.MaxHp) : 0f;
        string firstLine = $"Run {entry.Damage:N0}   •   Area {entry.AreaDamage:N0}   •   Avg DPS {entry.Damage / playedSeconds:N1}";
        GUI.Label(new Rect(20f, y + 34f, 340f, 19f), firstLine, OverlayGui.LabelStyle);
        string secondLine = $"Taken {entry.DamageTaken:N0}   •   HP {entry.Hp:N0}/{entry.MaxHp:N0}   •   Share {(partyTotal > 0f ? entry.Damage / partyTotal : 0f):P1}";
        GUI.Label(new Rect(20f, y + 54f, 340f, 19f), secondLine, OverlayGui.LabelStyle);

        Rect hpTrack = new Rect(20f, y + 77f, 340f, 5f);
        OverlayGui.Fill(hpTrack, OverlayGui.Track);
        OverlayGui.Fill(new Rect(hpTrack.x, hpTrack.y, hpTrack.width * hpRatio, hpTrack.height),
            entry.IsDead ? OverlayGui.Danger : new Color(0.33f, 0.83f, 0.45f, 1f));

        DrawElementMix(entry, new Rect(20f, y + 90f, 340f, 7f));
        GUI.Label(new Rect(20f, y + 102f, 170f, 18f), "TOP DAMAGE SOURCES", OverlayGui.MutedStyle);
        GUI.Label(new Rect(230f, y + 102f, 130f, 18f), "DAMAGE  •  MIX", OverlayGui.SmallRightStyle);

        float sourceY = y + 121f;
        for (int i = 0; i < sourceCount; i++)
        {
            DamageSourceEntry source = entry.Sources[i];
            OverlayGui.Fill(new Rect(20f, sourceY + 7f, 7f, 7f), OverlayGui.ElementColor(source.Element));
            GUI.Label(new Rect(32f, sourceY, 218f, 19f), source.Name, OverlayGui.LabelStyle);
            float sourceRatio = entry.Damage > 0f ? source.Damage / entry.Damage : 0f;
            GUI.Label(new Rect(250f, sourceY, 110f, 19f), $"{source.Damage:N0}  •  {sourceRatio:P0}", OverlayGui.SmallRightStyle);
            sourceY += 20f;
        }

        if (sourceCount == 0)
            GUI.Label(new Rect(20f, sourceY, 340f, 19f), "No damage sources recorded yet.", OverlayGui.MutedStyle);
    }

    private static void DrawElementMix(DamageEntry entry, Rect rect)
    {
        OverlayGui.Fill(rect, OverlayGui.Track);
        if (entry.Damage <= 0f)
            return;

        float x = rect.x;
        foreach (KeyValuePair<EDamageElementalType, float> element in entry.ElementDamage.OrderByDescending(pair => pair.Value))
        {
            float width = rect.width * Mathf.Clamp01(element.Value / entry.Damage);
            OverlayGui.Fill(new Rect(x, rect.y, width, rect.height), OverlayGui.ElementColor(element.Key));
            x += width;
        }
    }

    private float CalculateWindowHeight(float scale)
    {
        if (_collapsed)
            return 34f;

        float desired = 58f + CalculateDamageContentHeight();
        float available = Mathf.Max(220f, Screen.height / scale - 24f);
        return Mathf.Min(desired, available);
    }

    private float CalculateDamageContentHeight()
    {
        float height = 6f + _damage.Count * DamageRowHeight;
        DamageEntry selected = _damage.FirstOrDefault(entry => entry.Player == _selectedPlayer);
        if (selected != null)
            height += 148f + Math.Min(4, selected.Sources.Count) * 20f;
        return height;
    }

    private static float DamageRowHeight => _showDamageTaken?.Value == true ? 58f : 50f;

    private static string ReadPlayerName(PlayerAvatar player)
    {
        string name = PlayerNameProperty?.GetValue(player) as string;
        return string.IsNullOrWhiteSpace(name) ? player.name.Replace("(Clone)", "") : name;
    }

    private string ResolveDamageSourceName(DamageKey key)
    {
        string id = string.IsNullOrWhiteSpace(key.Id) ? "Unknown" : key.Id;
        if (_damageSourceNames.TryGetValue(id, out string cached))
            return cached;

        try
        {
            DamageIdEntity entity = KeywordDatabase.GetDamageIdEntity(key.Id);
            if (entity != null)
            {
                string localized = KeywordDatabase.Convert(entity.aName.ToString(), useColor: false, useSprite: false);
                _damageSourceNames[id] = localized;
                return localized;
            }
        }
        catch
        {
            // Fall back to the synchronized damage id when localization is unavailable.
        }

        _damageSourceNames[id] = id;
        return id;
    }

    private void EnsureTimerStyle()
    {
        if (_timerStyle != null)
            return;

        _timerStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 18,
            fontStyle = FontStyle.Bold
        };
    }
}
