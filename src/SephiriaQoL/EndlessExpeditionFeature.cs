using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SephiriaQoL;

internal sealed partial class EndlessExpeditionFeature
{
    private const byte ActiveMode = 1;
    private const byte FinishMode = 2;
    private const int MaximumGeneratedSegmentsAhead = 1;

    private sealed class Segment
    {
        internal int GlobalY;
        internal string HeadGuid;
        internal string[] LeafGuids;
    }

    private static EndlessExpeditionFeature _instance;
    private static bool _allowGameOverOnce;

    private readonly ConfigEntry<bool> _enabled;
    private readonly ConfigEntry<float> _healthGrowth;
    private readonly ConfigEntry<float> _spawnGrowth;
    private readonly ConfigEntry<int> _minibossInterval;
    private readonly ConfigEntry<float> _panelScale;
    private readonly ManualLogSource _log;
    private readonly Dictionary<int, Segment> _segments = new Dictionary<int, Segment>();
    private readonly Dictionary<int, int> _stageByGlobalY = new Dictionary<int, int>();

    private Rect _choiceWindow = new Rect(0f, 0f, 430f, 280f);
    private Rect _statusWindow = new Rect(0f, 82f, 330f, 168f);
    private StageEntity_Infinity _sourceStage;
    private bool _pendingChoice;
    private bool _active;
    private bool _finishPending;
    private bool _confirmFinish;
    private float _nextGameOverAttemptTime;
    private int _currentStage;
    private int _nextGlobalY;
    private int _settlementVictoryType;
    private int _displayMinibossInterval = 5;
    private float _displayHealthMultiplier = 1f;
    private float _displaySpawnMultiplier = 1f;

    internal EndlessExpeditionFeature(
        ConfigEntry<bool> enabled,
        ConfigEntry<float> healthGrowth,
        ConfigEntry<float> spawnGrowth,
        ConfigEntry<int> minibossInterval,
        ConfigEntry<float> panelScale,
        ManualLogSource log)
    {
        _enabled = enabled;
        _healthGrowth = healthGrowth;
        _spawnGrowth = spawnGrowth;
        _minibossInterval = minibossInterval;
        _panelScale = panelScale;
        _log = log;
        _instance = this;
        RegisterSerializers();
    }

    internal static bool IsHostActive =>
        _instance?._active == true && NetworkServer.active;

    internal static float CurrentSpawnMultiplier =>
        IsHostActive ? _instance.CalculateSpawnMultiplier(_instance._currentStage) : 1f;

    internal static float CurrentHealthMultiplier =>
        IsHostActive ? _instance.CalculateHealthMultiplier(_instance._currentStage) : 1f;

    internal void Update()
    {
        UpdateNetworking();
        if (!NetworkClient.active)
        {
            ResetLocal();
            return;
        }

        if (_finishPending)
            TryCompleteLocalGameOver();

        if (_active && NetworkServer.active)
            UpdateHostProgress();
    }

    internal void Dispose()
    {
        if (_instance == this)
            _instance = null;
        ResetLocal();
    }

    private void StartOnHost()
    {
        if (!NetworkServer.active || _active)
            return;
        if (!CanHostContinue(out int missingPlayers))
        {
            _log?.LogWarning($"Endless Expedition cannot start while {missingPlayers} player(s) are not ready.");
            return;
        }

        DungeonManager dungeon = DungeonManager.Instance;
        _sourceStage = FindSourceStage(dungeon);
        if (dungeon == null || _sourceStage == null)
        {
            _log?.LogError("Endless Expedition could not find a native procedural stage for this run.");
            FinishOnHost(victory: true);
            return;
        }

        try
        {
            _segments.Clear();
            _stageByGlobalY.Clear();
            _settlementVictoryType = Math.Max(1, dungeon.victoryType);
            dungeon.NetworkvictoryType = 0;
            _nextGlobalY = dungeon.generatedFloors.Count == 0
                ? 1
                : dungeon.generatedFloors.Values.Max(floor => floor.globalY) + 1;
            _currentStage = 1;
            _active = true;
            _pendingChoice = false;
            EnsureGeneratedThrough(_currentStage + MaximumGeneratedSegmentsAhead);
            MovePartyUnsaved(_segments[1].HeadGuid, _sourceStage.stageLoadingScreenIdx);
            BroadcastState(ActiveMode);
            BroadcastGameMessage("Endless Expedition stage 1 has begun.");
            _log?.LogInfo($"Endless Expedition started with native stage template '{_sourceStage.name}'.");
        }
        catch (Exception exception)
        {
            _log?.LogError($"Endless Expedition could not start: {exception}");
            _active = false;
            FinishOnHost(victory: true);
        }
    }

    private void UpdateHostProgress()
    {
        DungeonManager dungeon = DungeonManager.Instance;
        if (dungeon == null || PlayerSpawner.MultiplayerList == null)
            return;

        int furthestStage = _currentStage;
        foreach (PlayerSpawner spawner in PlayerSpawner.MultiplayerList)
        {
            PlayerAvatar player = spawner?.PlayerAvatar;
            if (player == null || player.IsDead ||
                !dungeon.generatedFloors.TryGetValue(player.currentFloorGuid, out FloorData floor) ||
                !_stageByGlobalY.TryGetValue(floor.globalY, out int stage))
                continue;

            furthestStage = Math.Max(furthestStage, stage);
        }

        if (furthestStage <= _currentStage)
            return;

        _currentStage = furthestStage;
        EnsureGeneratedThrough(_currentStage + MaximumGeneratedSegmentsAhead);
        BroadcastState(ActiveMode);
        BroadcastGameMessage($"Endless Expedition stage {_currentStage} reached.");
    }

    private void EnsureGeneratedThrough(int targetStage)
    {
        for (int stage = 1; stage <= targetStage; stage++)
        {
            if (_segments.ContainsKey(stage))
                continue;

            Segment segment = GenerateSegment(stage);
            _segments[stage] = segment;
            _stageByGlobalY[segment.GlobalY] = stage;
            if (stage > 1)
                LinkSegments(_segments[stage - 1], segment);
        }
    }

    private Segment GenerateSegment(int stage)
    {
        DungeonManager dungeon = DungeonManager.Instance;
        int globalY = _nextGlobalY++;
        int seed = unchecked(dungeon.DestinySeed * 486187739 + stage * 16777619);
        int hardModeAllBoss = 0;
        if (dungeon.hardModeEnvironment != null)
            dungeon.hardModeEnvironment.TryGetValue("ALLBOSS", out hardModeAllBoss);
        bool originalMinibossSetting = _sourceStage.createMiniBossNode;
        FloorData[] floors;
        try
        {
            _sourceStage.createMiniBossNode = false;
            floors = _sourceStage.GenerateStage(
                dungeon, seed, globalY, createMiracle: true, createAnvil: true,
                dungeon.Race?.enhancedDisturbance == true, dungeon.BossGenerateParameter, hardModeAllBoss);
        }
        finally
        {
            _sourceStage.createMiniBossNode = originalMinibossSetting;
        }

        if (floors == null || floors.Length == 0)
            throw new InvalidOperationException("The native stage generator returned no floors.");

        int difficultyBonus = Math.Min(20, (stage - 1) / 2);
        foreach (FloorData floor in floors)
            floor.difficulty = Mathf.Clamp(floor.difficulty + difficultyBonus, 0, 20);

        if (stage % Mathf.Clamp(_minibossInterval.Value, 2, 10) == 0)
            PromoteMilestoneRoom(floors, stage);

        string[] leaves = floors
            .Where(floor => !floor.isHidden &&
                            (floor.connectionToOtherFloors == null || floor.connectionToOtherFloors.Length == 0))
            .Select(floor => floor.guid)
            .ToArray();
        if (leaves.Length == 0)
            throw new InvalidOperationException("The generated stage segment has no terminal rooms.");

        FloorData head = floors
            .Where(floor => !floor.isHidden)
            .OrderBy(floor => floor.nodeProgress)
            .FirstOrDefault();
        if (head == null)
            throw new InvalidOperationException("The generated stage segment has no playable entrance.");

        foreach (FloorData floor in floors)
            dungeon.generatedFloors.Add(floor.guid, floor);

        return new Segment
        {
            GlobalY = globalY,
            HeadGuid = head.guid,
            LeafGuids = leaves
        };
    }

    private void PromoteMilestoneRoom(FloorData[] floors, int stage)
    {
        if (_sourceStage.minibossFloorPrefabs == null || _sourceStage.minibossFloorPrefabs.Length == 0)
        {
            _log?.LogWarning($"Endless stage {stage} has no native miniboss prefab available.");
            return;
        }

        FloorData candidate = floors
            .Where(floor => !floor.isHidden && floor.nodeProgress > 0 &&
                            (floor.threatType == EFloorThreatType.Battle ||
                             floor.threatType == EFloorThreatType.HardBattle))
            .OrderByDescending(floor => floor.nodeProgress)
            .FirstOrDefault();
        int milestone = Math.Max(0, stage / Mathf.Clamp(_minibossInterval.Value, 2, 10) - 1);
        GameObject prefab = _sourceStage.minibossFloorPrefabs[milestone % _sourceStage.minibossFloorPrefabs.Length];
        NetworkIdentity identity = prefab != null ? prefab.GetComponent<NetworkIdentity>() : null;
        if (candidate == null || identity == null)
        {
            _log?.LogWarning($"Endless stage {stage} could not promote a miniboss room.");
            return;
        }

        candidate.threatType = EFloorThreatType.MiniBoss;
        candidate.disturbance = "";
        candidate.prefabAssetId = identity.assetId;
    }

    private void LinkSegments(Segment previous, Segment next)
    {
        DungeonManager dungeon = DungeonManager.Instance;
        foreach (string leafGuid in previous.LeafGuids)
        {
            if (!dungeon.generatedFloors.TryGetValue(leafGuid, out FloorData leaf))
                continue;
            leaf.connectionToOtherFloors = new[] { next.HeadGuid };
            dungeon.generatedFloors[leafGuid] = leaf;
        }
    }

    private static StageEntity_Infinity FindSourceStage(DungeonManager dungeon)
    {
        if (dungeon?.Race?.stages == null)
            return null;

        for (int index = (dungeon.sortedStages?.Count ?? 0) - 1; index >= 0; index--)
        {
            string stageName = dungeon.sortedStages[index];
            StageEntity_Infinity stage = dungeon.Race.stages
                .OfType<StageEntity_Infinity>()
                .FirstOrDefault(candidate => candidate.name == stageName);
            if (stage != null)
                return stage;
        }

        return dungeon.Race.stages.OfType<StageEntity_Infinity>().LastOrDefault();
    }

    private static void MovePartyUnsaved(string floorGuid, int loadingScreen)
    {
        DungeonManager dungeon = DungeonManager.Instance;
        if (PlayerSpawner.MultiplayerList == null)
            return;

        foreach (PlayerSpawner spawner in PlayerSpawner.MultiplayerList)
        {
            PlayerAvatar player = spawner?.PlayerAvatar;
            if (player != null)
                dungeon.MoveFloor(player, floorGuid, "FLOORSTARTING", loadingScreen,
                    recordHistory: true, allowSave: false, keepPrevFloor: false, randomPosition: true);
        }
    }

    private void FinishOnHost(bool victory)
    {
        if (!NetworkServer.active)
            return;

        DungeonManager dungeon = DungeonManager.Instance;
        int settlementType = 0;
        if (victory)
        {
            settlementType = _settlementVictoryType > 0
                ? _settlementVictoryType
                : Math.Max(1, dungeon?.victoryType ?? 1);
        }

        if (dungeon != null)
            dungeon.NetworkvictoryType = settlementType;
        _active = false;
        _pendingChoice = false;
        _confirmFinish = false;
        BroadcastState(FinishMode, settlementType);
    }

    private void TryCompleteLocalGameOver()
    {
        if (Time.unscaledTime < _nextGameOverAttemptTime)
            return;

        PlayerAvatar observer = GameCamera.Instance?.Observer;
        if (observer?.spawner == null)
        {
            _nextGameOverAttemptTime = Time.unscaledTime + 0.25f;
            return;
        }

        try
        {
            _allowGameOverOnce = true;
            observer.spawner.ClientGameOver();
            _allowGameOverOnce = false;
            _finishPending = false;
            _nextGameOverAttemptTime = 0f;
        }
        catch (Exception exception)
        {
            _allowGameOverOnce = false;
            _nextGameOverAttemptTime = Time.unscaledTime + 0.5f;
            _log?.LogWarning($"Endless Expedition is waiting to open the game-over screen: {exception.Message}");
        }
    }

    private float CalculateHealthMultiplier(int stage) =>
        Mathf.Clamp(
            PartyScalingFeature.BaselineHealthMultiplier +
            Math.Max(0, stage - 1) * Mathf.Clamp(_healthGrowth.Value, 0.05f, 0.5f),
            1f,
            25f);

    private float CalculateSpawnMultiplier(int stage) =>
        Mathf.Clamp(
            PartyScalingFeature.BaselineSpawnMultiplier +
            Math.Max(0, stage - 1) * Mathf.Clamp(_spawnGrowth.Value, 0f, 0.25f),
            1f,
            4f);

    private static bool AllPlayersDead()
    {
        if (PlayerSpawner.MultiplayerList == null)
            return false;

        PlayerAvatar[] players = PlayerSpawner.MultiplayerList
            .Select(spawner => spawner?.PlayerAvatar)
            .Where(player => player != null)
            .ToArray();
        return players.Length > 0 && players.All(player => player.IsDead);
    }

    private bool ShouldOfferContinuation()
    {
        if (_enabled?.Value != true || !NetworkClient.active || AllPlayersDead() ||
            (!NetworkServer.active && !_hostAvailable))
            return false;

        DungeonManager dungeon = DungeonManager.Instance;
        if (dungeon == null || dungeon.victoryType <= 0)
            return false;

        return dungeon.dungeonEnvironment.TryGetValue("ChapterNum", out int chapter) && chapter >= 6;
    }

    private void ResetLocal()
    {
        _pendingChoice = false;
        _active = false;
        _finishPending = false;
        _confirmFinish = false;
        _nextGameOverAttemptTime = 0f;
        _currentStage = 0;
        _settlementVictoryType = 0;
        _displayMinibossInterval = 5;
        _displayHealthMultiplier = 1f;
        _displaySpawnMultiplier = 1f;
        _sourceStage = null;
        _segments.Clear();
        _stageByGlobalY.Clear();
    }

    private static void BroadcastGameMessage(string message)
    {
        if (PlayerSpawner.MultiplayerList == null)
            return;
        foreach (PlayerSpawner spawner in PlayerSpawner.MultiplayerList)
        {
            PlayerAvatar player = spawner?.PlayerAvatar;
            if (player?.connectionToClient != null)
                player.TargetSendCustomMessage(player.connectionToClient, message);
        }
    }

    [HarmonyPatch(typeof(PlayerSpawner), nameof(PlayerSpawner.ClientGameOver))]
    private static class GameOverPatch
    {
        private static bool Prefix()
        {
            if (_allowGameOverOnce || _instance?._finishPending == true)
            {
                _allowGameOverOnce = false;
                if (_instance != null)
                    _instance._finishPending = false;
                return true;
            }

            if (_instance == null)
                return true;

            if (_instance._active && AllPlayersDead())
            {
                if (DungeonManager.Instance != null)
                    DungeonManager.Instance.victoryType = 0;
                _instance.ResetLocal();
                return true;
            }

            if (_instance._active || _instance._pendingChoice || _instance.ShouldOfferContinuation())
            {
                _instance._pendingChoice = !_instance._active;
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(PlayerSpawner), nameof(PlayerSpawner.RpcGameOver))]
    private static class ServerGameOverPatch
    {
        private static void Prefix()
        {
            if (_instance?._active == true && NetworkServer.active)
                _instance.FinishOnHost(victory: false);
        }
    }
}
