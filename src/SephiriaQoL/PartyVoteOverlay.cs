using BepInEx.Configuration;
using BepInEx.Logging;
using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SephiriaQoL;

internal struct PartyVoteMessage : NetworkMessage
{
    internal byte Category;
    internal byte Choice;
}

internal sealed class PartyVoteOverlay
{
    private sealed class VoteRecord
    {
        internal string Name;
        internal byte RoomChoice;
        internal byte LootChoice;
    }

    private static PartyVoteOverlay _instance;
    private static bool _serializersRegistered;

    private readonly ConfigEntry<bool> _enabled;
    private readonly ConfigEntry<KeyboardShortcut> _hotkey;
    private readonly ConfigEntry<float> _panelScale;
    private readonly ManualLogSource _log;
    private readonly Dictionary<int, VoteRecord> _votes = new Dictionary<int, VoteRecord>();
    private Rect _windowRect = new Rect(420f, 290f, 420f, 330f);
    private bool _serverWasActive;
    private bool _visible;

    internal PartyVoteOverlay(
        ConfigEntry<bool> enabled,
        ConfigEntry<KeyboardShortcut> hotkey,
        ConfigEntry<float> panelScale,
        ManualLogSource log)
    {
        _enabled = enabled;
        _hotkey = hotkey;
        _panelScale = panelScale;
        _log = log;
        _instance = this;
        RegisterSerializers();
    }

    internal void Update()
    {
        if (_hotkey.Value.IsDown())
            _visible = !_visible;

        if (NetworkServer.active && !_serverWasActive)
        {
            try
            {
                NetworkServer.RegisterHandler<PartyVoteMessage>(OnServerVote, true);
            }
            catch (Exception exception)
            {
                _log?.LogWarning($"Party voting could not register on the host: {exception.Message}");
            }
        }

        _serverWasActive = NetworkServer.active;
        if (!NetworkServer.active)
        {
            _votes.Clear();
            return;
        }

        foreach (int connectionId in _votes.Keys
                     .Where(id => !NetworkServer.connections.ContainsKey(id)).ToArray())
            _votes.Remove(connectionId);
    }

    internal void OnGUI()
    {
        if (_enabled.Value != true || !_visible || !NetworkClient.active)
            return;

        float scale = OverlayGui.ResolveScale(_panelScale);
        _windowRect = OverlayGui.BeginScaledWindow(
            43154, _windowRect, 420f, 330f, scale, DrawWindow, out _);
    }

    internal void Dispose()
    {
        if (_instance == this)
            _instance = null;
    }

    private void DrawWindow(int id)
    {
        OverlayGui.Fill(new Rect(0f, 0f, 420f, 40f), OverlayGui.PanelRaised);
        OverlayGui.Fill(new Rect(0f, 39f, 420f, 1f), OverlayGui.Border);
        OverlayGui.Fill(new Rect(0f, 0f, 5f, 40f), OverlayGui.Accent);
        GUI.Label(new Rect(16f, 7f, 190f, 24f), "PARTY VOTE", OverlayGui.TitleStyle);
        OverlayGui.DrawScaleControls(_panelScale, 248f, 9f);
        if (GUI.Button(new Rect(386f, 9f, 23f, 22f), "×", OverlayGui.ButtonStyle))
            _visible = false;

        GUI.Label(new Rect(16f, 50f, 388f, 36f),
            "Choose the numbered room or loot option. Votes never select anything automatically.",
            OverlayGui.MutedStyle);

        DrawCategory(94f, "ROOM / PATH", category: 0);
        DrawCategory(164f, "LOOT / REWARD", category: 1);

        if (NetworkServer.active)
        {
            GUI.Label(new Rect(16f, 236f, 388f, 20f), "HOST TALLY", OverlayGui.TitleStyle);
            GUI.Label(new Rect(16f, 260f, 388f, 20f),
                $"ROOM   {Tally(0, 1)} / {Tally(0, 2)} / {Tally(0, 3)}     •     LOOT   {Tally(1, 1)} / {Tally(1, 2)} / {Tally(1, 3)}",
                OverlayGui.LabelStyle);
            GUI.Label(new Rect(16f, 285f, 290f, 20f), $"{_votes.Count} PLAYER VOTE RECORDS", OverlayGui.MutedStyle);
            if (GUI.Button(new Rect(318f, 282f, 86f, 24f), "CLEAR", OverlayGui.ButtonStyle))
                _votes.Clear();
        }
        else
        {
            GUI.Label(new Rect(16f, 248f, 388f, 40f),
                "Your host sees the tally. Every voter and the host need this QoL version.",
                OverlayGui.MutedStyle);
        }

        GUI.DragWindow(new Rect(0f, 0f, 220f, 40f));
    }

    private void DrawCategory(float y, string label, byte category)
    {
        OverlayGui.Fill(new Rect(16f, y, 388f, 56f), OverlayGui.PanelRaised);
        GUI.Label(new Rect(28f, y + 6f, 150f, 20f), label, OverlayGui.LabelStyle);
        for (byte choice = 1; choice <= 3; choice++)
        {
            byte selectedChoice = choice;
            if (GUI.Button(new Rect(190f + (choice - 1) * 50f, y + 14f, 40f, 28f),
                    choice.ToString(), OverlayGui.ButtonStyle))
                SendVote(category, selectedChoice);
        }
        if (GUI.Button(new Rect(344f, y + 14f, 48f, 28f), "—", OverlayGui.ButtonStyle))
            SendVote(category, 0);
    }

    private void SendVote(byte category, byte choice)
    {
        if (_enabled.Value != true || !NetworkClient.active)
            return;

        try
        {
            NetworkClient.Send(new PartyVoteMessage { Category = category, Choice = choice });
        }
        catch (Exception exception)
        {
            _log?.LogWarning($"Party vote could not be sent: {exception.Message}");
        }
    }

    private static void OnServerVote(NetworkConnectionToClient connection, PartyVoteMessage message)
    {
        if (_instance?._enabled.Value != true || connection?.identity == null ||
            message.Category > 1 || message.Choice > 3)
            return;

        PlayerAvatar player = connection.identity.GetComponent<PlayerAvatar>();
        if (player == null)
            return;

        if (!_instance._votes.TryGetValue(connection.connectionId, out VoteRecord record))
        {
            record = new VoteRecord { Name = player.Name };
            _instance._votes[connection.connectionId] = record;
        }

        if (message.Category == 0)
            record.RoomChoice = message.Choice;
        else
            record.LootChoice = message.Choice;
    }

    private int Tally(byte category, byte choice) => _votes.Values.Count(record =>
        (category == 0 ? record.RoomChoice : record.LootChoice) == choice);

    private static void RegisterSerializers()
    {
        if (_serializersRegistered)
            return;

        Writer<PartyVoteMessage>.write = (writer, message) =>
        {
            writer.WriteByte(message.Category);
            writer.WriteByte(message.Choice);
        };
        Reader<PartyVoteMessage>.read = reader => new PartyVoteMessage
        {
            Category = reader.ReadByte(),
            Choice = reader.ReadByte()
        };
        _serializersRegistered = true;
    }
}
