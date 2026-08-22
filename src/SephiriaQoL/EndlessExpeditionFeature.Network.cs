using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SephiriaQoL;

internal struct EndlessExpeditionCapabilityMessage : NetworkMessage
{
    internal byte Protocol;
    internal byte Enabled;
}

internal struct EndlessExpeditionStateMessage : NetworkMessage
{
    internal byte Mode;
    internal int Stage;
    internal int MinibossInterval;
    internal float HealthMultiplier;
    internal float SpawnMultiplier;
    internal int VictoryType;
}

internal struct EndlessExpeditionHostStatusMessage : NetworkMessage
{
    internal byte Protocol;
    internal byte Enabled;
}

internal sealed partial class EndlessExpeditionFeature
{
    private const byte ProtocolVersion = 2;

    private static bool _serializersRegistered;

    private readonly HashSet<int> _capableConnections = new HashSet<int>();
    private bool _serverWasActive;
    private bool _clientWasActive;
    private bool _capabilitySent;
    private bool _lastCapabilityEnabled;
    private float _nextCapabilityReportTime;
    private bool _hostAvailable;
    private bool _hostStatusSent;
    private bool _lastHostStatusEnabled;
    private float _nextHostStatusTime;

    private void UpdateNetworking()
    {
        if (NetworkServer.active && !_serverWasActive)
        {
            try
            {
                NetworkServer.RegisterHandler<EndlessExpeditionCapabilityMessage>(HandleCapabilityMessage, true);
            }
            catch (Exception exception)
            {
                _log?.LogWarning($"Endless Expedition could not register its host capability handler: {exception.Message}");
            }
        }

        _serverWasActive = NetworkServer.active;
        if (!NetworkServer.active)
        {
            _capableConnections.Clear();
            _hostStatusSent = false;
            _nextHostStatusTime = 0f;
        }
        else
            _capableConnections.RemoveWhere(id => !NetworkServer.connections.ContainsKey(id));

        if (NetworkClient.active && !_clientWasActive)
        {
            try
            {
                NetworkClient.RegisterHandler<EndlessExpeditionStateMessage>(HandleStateMessage, true);
                NetworkClient.RegisterHandler<EndlessExpeditionHostStatusMessage>(HandleHostStatusMessage, true);
            }
            catch (Exception exception)
            {
                _log?.LogWarning($"Endless Expedition could not register its client state handler: {exception.Message}");
            }
        }

        bool enabled = _enabled?.Value == true;
        if (NetworkServer.active &&
            (!_hostStatusSent || enabled != _lastHostStatusEnabled || Time.unscaledTime >= _nextHostStatusTime))
        {
            BroadcastHostStatus(enabled);
            _hostStatusSent = true;
            _lastHostStatusEnabled = enabled;
            _nextHostStatusTime = Time.unscaledTime + 2f;
        }

        if (NetworkClient.active &&
            (!_capabilitySent || enabled != _lastCapabilityEnabled || Time.unscaledTime >= _nextCapabilityReportTime))
        {
            try
            {
                NetworkClient.Send(new EndlessExpeditionCapabilityMessage
                {
                    Protocol = ProtocolVersion,
                    Enabled = enabled ? (byte)1 : (byte)0
                });
                _capabilitySent = true;
                _lastCapabilityEnabled = enabled;
                _nextCapabilityReportTime = Time.unscaledTime + 2f;
            }
            catch (Exception exception)
            {
                _capabilitySent = false;
                _nextCapabilityReportTime = Time.unscaledTime + 2f;
                _log?.LogWarning($"Endless Expedition could not report client readiness: {exception.Message}");
            }
        }

        _clientWasActive = NetworkClient.active;
        if (!NetworkClient.active)
        {
            _capabilitySent = false;
            _nextCapabilityReportTime = 0f;
            _hostAvailable = false;
        }
    }

    private bool CanHostContinue(out int missingPlayers)
    {
        if (!NetworkServer.active)
        {
            missingPlayers = 0;
            return false;
        }

        int[] playerConnections = NetworkServer.connections.Values
            .Where(connection => connection?.identity != null &&
                                 connection.identity.GetComponent<PlayerAvatar>() != null)
            .Select(connection => connection.connectionId)
            .ToArray();
        missingPlayers = playerConnections.Count(id => !_capableConnections.Contains(id));
        return _enabled?.Value == true && playerConnections.Length > 0 && missingPlayers == 0;
    }

    private static void HandleCapabilityMessage(
        NetworkConnectionToClient connection,
        EndlessExpeditionCapabilityMessage message)
    {
        if (_instance == null || connection == null)
            return;

        if (message.Protocol == ProtocolVersion && message.Enabled == 1)
            _instance._capableConnections.Add(connection.connectionId);
        else
            _instance._capableConnections.Remove(connection.connectionId);

        connection.Send(new EndlessExpeditionHostStatusMessage
        {
            Protocol = ProtocolVersion,
            Enabled = _instance._enabled?.Value == true ? (byte)1 : (byte)0
        });
    }

    private static void BroadcastHostStatus(bool enabled)
    {
        NetworkServer.SendToAll(new EndlessExpeditionHostStatusMessage
        {
            Protocol = ProtocolVersion,
            Enabled = enabled ? (byte)1 : (byte)0
        });
    }

    private static void HandleHostStatusMessage(EndlessExpeditionHostStatusMessage message)
    {
        if (_instance != null)
            _instance._hostAvailable = message.Protocol == ProtocolVersion && message.Enabled == 1;
    }

    private void BroadcastState(byte mode, int victoryType = 0)
    {
        EndlessExpeditionStateMessage message = new EndlessExpeditionStateMessage
        {
            Mode = mode,
            Stage = _currentStage,
            MinibossInterval = Mathf.Clamp(_minibossInterval.Value, 2, 10),
            HealthMultiplier = CalculateHealthMultiplier(_currentStage),
            SpawnMultiplier = CalculateSpawnMultiplier(_currentStage),
            VictoryType = Math.Max(0, victoryType)
        };
        NetworkServer.SendToAll(message);
    }

    private static void HandleStateMessage(EndlessExpeditionStateMessage message)
    {
        if (_instance == null)
            return;

        if (message.Mode == FinishMode)
        {
            if (DungeonManager.Instance != null)
                DungeonManager.Instance.victoryType = Math.Max(0, message.VictoryType);
            _instance.ResetLocal();
            if (message.VictoryType > 0)
            {
                _instance._finishPending = true;
                _instance.TryCompleteLocalGameOver();
            }
            return;
        }

        if (message.Mode != ActiveMode)
            return;

        _instance._pendingChoice = false;
        _instance._active = true;
        _instance._currentStage = Math.Max(1, message.Stage);
        _instance._displayMinibossInterval = Mathf.Clamp(message.MinibossInterval, 2, 10);
        _instance._displayHealthMultiplier = Math.Max(1f, message.HealthMultiplier);
        _instance._displaySpawnMultiplier = Math.Max(1f, message.SpawnMultiplier);
        _instance._confirmFinish = false;
    }

    private static void RegisterSerializers()
    {
        if (_serializersRegistered)
            return;

        Writer<EndlessExpeditionCapabilityMessage>.write = (writer, message) =>
        {
            writer.WriteByte(message.Protocol);
            writer.WriteByte(message.Enabled);
        };
        Reader<EndlessExpeditionCapabilityMessage>.read = reader => new EndlessExpeditionCapabilityMessage
        {
            Protocol = reader.ReadByte(),
            Enabled = reader.ReadByte()
        };

        Writer<EndlessExpeditionStateMessage>.write = (writer, message) =>
        {
            writer.WriteByte(message.Mode);
            writer.WriteVarInt(message.Stage);
            writer.WriteVarInt(message.MinibossInterval);
            writer.WriteFloat(message.HealthMultiplier);
            writer.WriteFloat(message.SpawnMultiplier);
            writer.WriteVarInt(message.VictoryType);
        };
        Reader<EndlessExpeditionStateMessage>.read = reader => new EndlessExpeditionStateMessage
        {
            Mode = reader.ReadByte(),
            Stage = reader.ReadVarInt(),
            MinibossInterval = reader.ReadVarInt(),
            HealthMultiplier = reader.ReadFloat(),
            SpawnMultiplier = reader.ReadFloat(),
            VictoryType = reader.ReadVarInt()
        };

        Writer<EndlessExpeditionHostStatusMessage>.write = (writer, message) =>
        {
            writer.WriteByte(message.Protocol);
            writer.WriteByte(message.Enabled);
        };
        Reader<EndlessExpeditionHostStatusMessage>.read = reader => new EndlessExpeditionHostStatusMessage
        {
            Protocol = reader.ReadByte(),
            Enabled = reader.ReadByte()
        };
        _serializersRegistered = true;
    }
}
