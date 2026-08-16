using BepInEx.Configuration;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SephiriaQoL;

internal sealed class SpectatorFeature
{
    private readonly ConfigEntry<bool> _enabled;
    private readonly ConfigEntry<KeyboardShortcut> _previousHotkey;
    private readonly ConfigEntry<KeyboardShortcut> _nextHotkey;
    private readonly ConfigEntry<float> _panelScale;
    private readonly List<PlayerAvatar> _targets = new List<PlayerAvatar>();

    private PlayerAvatar _localPlayer;
    private PlayerAvatar _currentTarget;
    private Rect _windowRect = new Rect(0f, 70f, 360f, 70f);
    private float _nextRefresh;
    private int _targetIndex;
    private bool _wasSpectating;

    internal SpectatorFeature(
        ConfigEntry<bool> enabled,
        ConfigEntry<KeyboardShortcut> previousHotkey,
        ConfigEntry<KeyboardShortcut> nextHotkey,
        ConfigEntry<float> panelScale)
    {
        _enabled = enabled;
        _previousHotkey = previousHotkey;
        _nextHotkey = nextHotkey;
        _panelScale = panelScale;
    }

    internal void Update()
    {
        if (_enabled.Value != true)
        {
            ExitSpectating();
            return;
        }

        if (Time.unscaledTime >= _nextRefresh)
        {
            _nextRefresh = Time.unscaledTime + 0.35f;
            RefreshPlayers();
        }

        bool shouldSpectate = _localPlayer != null && _localPlayer.IsDead && PlayerSpawner.MultiplayerList?.Count > 1;
        if (!shouldSpectate)
        {
            ExitSpectating();
            return;
        }

        _wasSpectating = true;
        if (ShortcutInput.IsDown(_previousHotkey.Value))
            ChangeTarget(-1);
        if (ShortcutInput.IsDown(_nextHotkey.Value))
            ChangeTarget(1);

        ApplyTarget();
    }

    internal void OnGUI()
    {
        if (!_wasSpectating || _localPlayer == null || !_localPlayer.IsDead)
            return;

        float scale = OverlayGui.ResolveScale(_panelScale);
        float width = 360f;
        _windowRect.x = (Screen.width - width * scale) * 0.5f;
        _windowRect = OverlayGui.BeginScaledWindow(
            43133,
            _windowRect,
            width,
            68f,
            scale,
            DrawWindow,
            out _);
    }

    internal void Dispose()
    {
        ExitSpectating();
    }

    private void DrawWindow(int id)
    {
        OverlayGui.DrawHeader(new Rect(4f, 4f, 352f, 60f));
        GUI.Label(new Rect(16f, 9f, 100f, 20f), "Spectating", OverlayGui.MutedStyle);

        string targetName = _currentTarget != null ? _currentTarget.Name : "Waiting for a living teammate…";
        GUI.Label(new Rect(16f, 28f, 230f, 26f), targetName, OverlayGui.TitleStyle);

        bool previousEnabled = GUI.enabled;
        GUI.enabled = _targets.Count > 1;
        if (GUI.Button(new Rect(254f, 21f, 40f, 30f), "‹", OverlayGui.ButtonStyle))
            ChangeTarget(-1);
        if (GUI.Button(new Rect(302f, 21f, 40f, 30f), "›", OverlayGui.ButtonStyle))
            ChangeTarget(1);
        GUI.enabled = previousEnabled;
    }

    private void RefreshPlayers()
    {
        List<PlayerSpawner> players = PlayerSpawner.MultiplayerList;
        _localPlayer = players?
            .Select(spawner => spawner?.PlayerAvatar)
            .FirstOrDefault(player => player != null && player.isLocalPlayer);

        PlayerAvatar previousTarget = _currentTarget;
        _targets.Clear();
        if (players != null)
        {
            foreach (PlayerAvatar player in players.Select(spawner => spawner?.PlayerAvatar))
            {
                if (player != null && player != _localPlayer && !player.IsDead && player.gameObject.activeInHierarchy)
                    _targets.Add(player);
            }
        }

        if (previousTarget != null && _targets.Contains(previousTarget))
        {
            _targetIndex = _targets.IndexOf(previousTarget);
            _currentTarget = previousTarget;
        }
        else if (_targets.Count > 0)
        {
            _targetIndex = Mathf.Clamp(_targetIndex, 0, _targets.Count - 1);
            _currentTarget = _targets[_targetIndex];
        }
        else
        {
            _targetIndex = 0;
            _currentTarget = null;
        }
    }

    private void ChangeTarget(int direction)
    {
        if (_targets.Count == 0)
            return;

        _targetIndex = (_targetIndex + direction + _targets.Count) % _targets.Count;
        _currentTarget = _targets[_targetIndex];
        ApplyTarget();
    }

    private void ApplyTarget()
    {
        if (_currentTarget != null && GameCamera.Instance != null && GameCamera.Instance.Observer != _currentTarget)
            GameCamera.Instance.SetObserver(_currentTarget, syncPosition: false);
    }

    private void ExitSpectating()
    {
        if (_wasSpectating && _localPlayer != null && GameCamera.Instance != null && GameCamera.Instance.Observer != _localPlayer)
            GameCamera.Instance.SetObserver(_localPlayer, syncPosition: true);

        _wasSpectating = false;
        _currentTarget = null;
        _targets.Clear();
    }
}
