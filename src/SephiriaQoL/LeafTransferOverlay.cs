using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Mirror;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SephiriaQoL;

internal sealed class LeafTransferOverlay
{
    private static ConfigEntry<bool> _enabled;
    private static ConfigEntry<int> _maximumTransfer;
    private static ManualLogSource _log;
    private static readonly Dictionary<uint, float> LastTransferAt = new Dictionary<uint, float>();

    private readonly ConfigEntry<KeyboardShortcut> _hotkey;
    private readonly ConfigEntry<float> _panelScale;
    private Rect _windowRect = new Rect(860f, 290f, 390f, 370f);
    private Vector2 _scroll;
    private PlayerAvatar _armedRecipient;
    private float _armedUntil;
    private int _amount = 100;
    private bool _visible;

    internal LeafTransferOverlay(
        ConfigEntry<bool> enabled,
        ConfigEntry<KeyboardShortcut> hotkey,
        ConfigEntry<int> maximumTransfer,
        ConfigEntry<float> panelScale,
        ManualLogSource log)
    {
        _enabled = enabled;
        _hotkey = hotkey;
        _maximumTransfer = maximumTransfer;
        _panelScale = panelScale;
        _log = log;
    }

    internal void Update()
    {
        if (_hotkey.Value.IsDown())
            _visible = !_visible;
        if (_armedRecipient != null && Time.unscaledTime > _armedUntil)
            _armedRecipient = null;
    }

    internal void OnGUI()
    {
        if (_enabled.Value != true || !_visible || !NetworkClient.active)
            return;

        float scale = OverlayGui.ResolveScale(_panelScale);
        _windowRect = OverlayGui.BeginScaledWindow(
            43155, _windowRect, 390f, 370f, scale, DrawWindow, out _);
    }

    private void DrawWindow(int id)
    {
        PlayerAvatar local = NetworkClient.connection?.identity?.GetComponent<PlayerAvatar>();
        List<PlayerAvatar> recipients = PlayerSpawner.MultiplayerList?
            .Select(spawner => spawner?.PlayerAvatar)
            .Where(player => player != null && player != local && !player.IsDead)
            .ToList() ?? new List<PlayerAvatar>();

        OverlayGui.Fill(new Rect(0f, 0f, 390f, 40f), OverlayGui.PanelRaised);
        OverlayGui.Fill(new Rect(0f, 39f, 390f, 1f), OverlayGui.Border);
        OverlayGui.Fill(new Rect(0f, 0f, 5f, 40f), OverlayGui.Accent);
        GUI.Label(new Rect(16f, 7f, 170f, 24f), "LEAF TRANSFER", OverlayGui.TitleStyle);
        OverlayGui.DrawScaleControls(_panelScale, 218f, 9f);
        if (GUI.Button(new Rect(356f, 9f, 23f, 22f), "×", OverlayGui.ButtonStyle))
            _visible = false;

        GUI.Label(new Rect(16f, 49f, 358f, 20f),
            local == null ? "LOCAL PLAYER UNAVAILABLE" : $"BALANCE {local.Money:N0}",
            OverlayGui.LabelStyle);
        GUI.Label(new Rect(16f, 72f, 100f, 22f), "AMOUNT", OverlayGui.MutedStyle);
        if (GUI.Button(new Rect(124f, 70f, 36f, 25f), "−", OverlayGui.ButtonStyle))
            _amount = Mathf.Max(1, _amount - 10);
        GUI.Label(new Rect(164f, 71f, 82f, 23f), _amount.ToString("N0"), OverlayGui.RightStyle);
        if (GUI.Button(new Rect(252f, 70f, 36f, 25f), "+", OverlayGui.ButtonStyle))
            _amount = Mathf.Min(_maximumTransfer.Value, _amount + 10);
        if (GUI.Button(new Rect(294f, 70f, 80f, 25f), "MAX", OverlayGui.ButtonStyle) && local != null)
            _amount = Mathf.Clamp(local.Money, 1, _maximumTransfer.Value);

        GUI.Label(new Rect(16f, 104f, 358f, 36f),
            "Click SEND twice on the same teammate to confirm. Transfers require the host's validation.",
            OverlayGui.MutedStyle);

        float contentHeight = recipients.Count * 48f + 4f;
        _scroll = GUI.BeginScrollView(new Rect(0f, 145f, 390f, 205f), _scroll,
            new Rect(0f, 0f, 390f, Mathf.Max(205f, contentHeight)));
        float y = 2f;
        foreach (PlayerAvatar recipient in recipients)
        {
            OverlayGui.Fill(new Rect(12f, y, 366f, 40f), OverlayGui.PanelRaised);
            GUI.Label(new Rect(24f, y + 10f, 218f, 20f), recipient.Name, OverlayGui.LabelStyle);
            bool armed = _armedRecipient == recipient && Time.unscaledTime <= _armedUntil;
            Color previous = GUI.backgroundColor;
            GUI.backgroundColor = armed ? new Color(0.98f, 0.68f, 0.22f, 1f) : OverlayGui.Accent;
            if (GUI.Button(new Rect(270f, y + 7f, 94f, 26f), armed ? "CONFIRM" : "SEND", OverlayGui.ButtonStyle))
                HandleSend(local, recipient, armed);
            GUI.backgroundColor = previous;
            y += 48f;
        }
        if (recipients.Count == 0)
            GUI.Label(new Rect(16f, 8f, 358f, 22f), "No eligible teammates are connected.", OverlayGui.MutedStyle);
        GUI.EndScrollView();
        GUI.DragWindow(new Rect(0f, 0f, 190f, 40f));
    }

    private void HandleSend(PlayerAvatar local, PlayerAvatar recipient, bool armed)
    {
        if (local == null || recipient == null)
            return;
        if (!armed)
        {
            _armedRecipient = recipient;
            _armedUntil = Time.unscaledTime + 4f;
            return;
        }

        int amount = Mathf.Clamp(_amount, 1, _maximumTransfer.Value);
        local.GiveMoney(recipient, amount);
        _armedRecipient = null;
    }

    private static bool Validate(PlayerAvatar sender, PlayerAvatar recipient, int amount, bool applyCooldown)
    {
        if (_enabled?.Value != true)
            return true;
        if (sender == null || recipient == null || sender == recipient ||
            !IsConnectedPlayer(sender) || !IsConnectedPlayer(recipient) || amount <= 0 ||
            amount > _maximumTransfer.Value || !sender.HasMoney(amount) || sender.IsDead || recipient.IsDead ||
            string.IsNullOrEmpty(sender.currentFloorGuid) || sender.currentFloorGuid != recipient.currentFloorGuid)
            return false;
        if (!applyCooldown)
            return true;

        float now = Time.realtimeSinceStartup;
        if (LastTransferAt.TryGetValue(sender.netId, out float previous) && now - previous < 0.5f)
            return false;
        LastTransferAt[sender.netId] = now;
        return true;
    }

    private static bool IsConnectedPlayer(PlayerAvatar player) =>
        player.gameObject.activeInHierarchy && PlayerSpawner.MultiplayerList != null &&
        PlayerSpawner.MultiplayerList.Any(spawner => spawner?.PlayerAvatar == player);

    [HarmonyPatch(typeof(UnitAvatar), nameof(UnitAvatar.GiveMoney))]
    private static class DirectTransferPatch
    {
        private static bool Prefix(UnitAvatar __instance, UnitAvatar avatar, int m)
        {
            if (_enabled?.Value != true)
                return true;
            if (__instance is not PlayerAvatar sender || avatar is not PlayerAvatar recipient)
                return false;
            if (!NetworkServer.active)
                return Validate(sender, recipient, m, applyCooldown: false);

            bool valid = Validate(sender, recipient, m, applyCooldown: true);
            if (!valid)
                _log?.LogWarning("Rejected an invalid host leaf transfer.");
            return valid;
        }
    }

    [HarmonyPatch(typeof(UnitAvatar), "UserCode_CmdGiveMoney__UnitAvatar__Int32")]
    private static class RemoteTransferPatch
    {
        private static bool Prefix(UnitAvatar __instance, UnitAvatar avatar, int m)
        {
            if (_enabled?.Value != true)
                return true;
            bool valid = __instance is PlayerAvatar sender && avatar is PlayerAvatar recipient &&
                         NetworkServer.active && Validate(sender, recipient, m, applyCooldown: true);
            if (!valid)
                _log?.LogWarning("Rejected an invalid remote leaf transfer.");
            return valid;
        }
    }
}
