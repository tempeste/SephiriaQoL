using BepInEx.Configuration;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SephiriaQoL;

internal sealed class PartyReadinessOverlay
{
    private sealed class PlayerState
    {
        internal string Name;
        internal string Status;
        internal string Floor;
        internal Color Color;
    }

    private readonly ConfigEntry<bool> _enabled;
    private readonly ConfigEntry<KeyboardShortcut> _hotkey;
    private readonly ConfigEntry<float> _panelScale;
    private readonly List<PlayerState> _players = new List<PlayerState>();
    private readonly Dictionary<string, GUIStyle> _statusStyles = new Dictionary<string, GUIStyle>();
    private Rect _windowRect = new Rect(420f, 90f, 360f, 180f);
    private Vector2 _scroll;
    private float _nextRefresh;
    private bool _visible;

    internal PartyReadinessOverlay(
        ConfigEntry<bool> enabled,
        ConfigEntry<KeyboardShortcut> hotkey,
        ConfigEntry<float> panelScale)
    {
        _enabled = enabled;
        _hotkey = hotkey;
        _panelScale = panelScale;
    }

    internal void Update()
    {
        if (_hotkey.Value.IsDown())
            _visible = !_visible;

        if (Time.unscaledTime < _nextRefresh)
            return;

        _nextRefresh = Time.unscaledTime + 0.5f;
        RefreshPlayers();
    }

    internal void OnGUI()
    {
        if (_enabled.Value != true || !_visible || _players.Count == 0)
            return;

        float scale = OverlayGui.ResolveScale(_panelScale);
        float desiredHeight = 70f + _players.Count * 34f;
        float height = Mathf.Min(desiredHeight, Mathf.Max(180f, Screen.height / scale - 36f));
        _windowRect = OverlayGui.BeginScaledWindow(
            43153, _windowRect, 360f, height, scale, id => DrawWindow(id, height), out _);
    }

    private void RefreshPlayers()
    {
        _players.Clear();
        if (_enabled.Value != true || PlayerSpawner.MultiplayerList == null)
            return;

        PlayerAvatar observer = GameCamera.Instance?.Observer;
        string localFloor = observer?.currentFloorGuid;
        int colorIndex = 0;
        foreach (PlayerSpawner spawner in PlayerSpawner.MultiplayerList)
        {
            PlayerAvatar player = spawner?.PlayerAvatar;
            if (player == null)
                continue;

            PlayerLocalDataStorage localData = spawner.LocalDataStorage;
            _players.Add(new PlayerState
            {
                Name = player.Name,
                Status = ResolveStatus(player, localData, localFloor),
                Floor = ShortFloor(player.currentFloorGuid, localFloor),
                Color = OverlayGui.PlayerColors[colorIndex++ % OverlayGui.PlayerColors.Length]
            });
        }
    }

    private static string ResolveStatus(
        PlayerAvatar player,
        PlayerLocalDataStorage localData,
        string localFloor)
    {
        if (player.loadingScreenType >= 0)
            return "LOADING";
        if (player.IsDead)
            return "DOWN";
        if (localData?.NetworkreadyToLeave == true)
            return "READY";
        if (localData?.NetworkpreparingUIThings == true)
            return "PREPARING";
        if (localData?.NetworkdoingSomeUIThings == true)
            return "IN MENU";
        if (player.IsInBattle)
            return "FIGHTING";
        if (!string.IsNullOrEmpty(localFloor) && player.currentFloorGuid != localFloor)
            return "OTHER ROOM";
        return "AVAILABLE";
    }

    private static string ShortFloor(string floor, string localFloor)
    {
        if (string.IsNullOrWhiteSpace(floor))
            return "NO FLOOR";
        if (floor == localFloor)
            return "HERE";
        return floor.Length <= 12 ? floor.ToUpperInvariant() : floor.Substring(0, 12).ToUpperInvariant();
    }

    private void DrawWindow(int id, float height)
    {
        OverlayGui.Fill(new Rect(0f, 0f, 360f, 40f), OverlayGui.PanelRaised);
        OverlayGui.Fill(new Rect(0f, 39f, 360f, 1f), OverlayGui.Border);
        OverlayGui.Fill(new Rect(0f, 0f, 5f, 40f), OverlayGui.Accent);
        GUI.Label(new Rect(16f, 7f, 165f, 24f), "PARTY READINESS", OverlayGui.TitleStyle);
        OverlayGui.DrawScaleControls(_panelScale, 190f, 9f);
        if (GUI.Button(new Rect(326f, 9f, 23f, 22f), "×", OverlayGui.ButtonStyle))
            _visible = false;

        int ready = _players.Count(player => player.Status == "READY" || player.Status == "AVAILABLE");
        GUI.Label(new Rect(16f, 44f, 328f, 18f), $"{ready}/{_players.Count} AVAILABLE OR READY", OverlayGui.MutedStyle);

        float contentHeight = _players.Count * 34f + 4f;
        Rect viewport = new Rect(0f, 65f, 360f, Mathf.Max(1f, height - 65f));
        _scroll = GUI.BeginScrollView(viewport, _scroll, new Rect(0f, 0f, 360f, contentHeight));
        float y = 2f;
        foreach (PlayerState player in _players)
        {
            Rect row = new Rect(10f, y, 340f, 28f);
            OverlayGui.Fill(row, OverlayGui.PanelRaised);
            OverlayGui.Fill(new Rect(row.x, row.y, 3f, row.height), player.Color);
            GUI.Label(new Rect(20f, y + 4f, 156f, 20f), player.Name, OverlayGui.LabelStyle);
            GUI.Label(new Rect(176f, y + 4f, 88f, 20f), player.Status, StatusStyle(player.Status));
            GUI.Label(new Rect(264f, y + 4f, 76f, 20f), player.Floor, OverlayGui.SmallRightStyle);
            y += 34f;
        }
        GUI.EndScrollView();
        GUI.DragWindow(new Rect(0f, 0f, 180f, 40f));
    }

    private GUIStyle StatusStyle(string status)
    {
        if (_statusStyles.TryGetValue(status, out GUIStyle cached))
            return cached;

        GUIStyle style = new GUIStyle(OverlayGui.SmallRightStyle);
        style.normal.textColor = status switch
        {
            "READY" or "AVAILABLE" => OverlayGui.Accent,
            "DOWN" => OverlayGui.Danger,
            "FIGHTING" => new Color(0.98f, 0.68f, 0.22f, 1f),
            _ => OverlayGui.Text
        };
        _statusStyles[status] = style;
        return style;
    }
}
