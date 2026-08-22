using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace SephiriaQoL;

internal static class PartyScalingFeature
{
    private const int AutomaticScalingMinimumPlayers = 5;
    private const float AutomaticHealthGrowthPerPlayer = 0.05f;
    private const float AutomaticSpawnGrowthPerPlayer = 0.15f;
    private const int MaximumNormalEnemiesPerPhase = 96;

    private sealed class ScaledMarker
    {
    }

    private static readonly ConditionalWeakTable<UnitAvatar, ScaledMarker> ScaledEnemies = new();
    private static readonly Dictionary<Type, FieldInfo> OwnerFields = new();
    private static readonly Dictionary<Type, FieldInfo> SpawnedFields = new();

    private static ConfigEntry<bool> _enabled;
    private static ConfigEntry<bool> _autoScaleLargeParties;
    private static ConfigEntry<float> _healthMultiplier;
    private static ConfigEntry<float> _spawnMultiplier;
    private static ManualLogSource _logger;

    internal static bool IsHostActive => NetworkServer.active;

    internal static void Configure(
        ConfigEntry<bool> enabled,
        ConfigEntry<bool> autoScaleLargeParties,
        ConfigEntry<float> healthMultiplier,
        ConfigEntry<float> spawnMultiplier,
        ManualLogSource logger)
    {
        _enabled = enabled;
        _autoScaleLargeParties = autoScaleLargeParties;
        _healthMultiplier = healthMultiplier;
        _spawnMultiplier = spawnMultiplier;
        _logger = logger;

        _healthMultiplier.Value = Mathf.Clamp(_healthMultiplier.Value, 1f, 10f);
        _spawnMultiplier.Value = Mathf.Clamp(_spawnMultiplier.Value, 1f, 4f);
    }

    internal static float BaselineSpawnMultiplier
    {
        get
        {
            float configured = _enabled?.Value == true
                ? Mathf.Clamp(_spawnMultiplier.Value, 1f, 4f)
                : 1f;
            return Mathf.Max(configured, CalculateAutomaticSpawnMultiplier());
        }
    }

    internal static float BaselineHealthMultiplier
    {
        get
        {
            float configured = _enabled?.Value == true
                ? Mathf.Clamp(_healthMultiplier.Value, 1f, 10f)
                : 1f;
            return Mathf.Max(configured, CalculateAutomaticHealthMultiplier());
        }
    }

    private static bool ShouldScale => NetworkServer.active &&
        (EndlessExpeditionFeature.IsHostActive || BaselineHealthMultiplier > 1.001f ||
         BaselineSpawnMultiplier > 1.001f);

    private static int ConnectedPlayerCount => NetworkServer.connections.Values.Count(connection =>
        connection?.identity != null && connection.identity.GetComponent<PlayerAvatar>() != null);

    private static float CalculateAutomaticHealthMultiplier()
    {
        if (_autoScaleLargeParties?.Value != true)
            return 1f;

        int extraPlayers = Mathf.Max(0, ConnectedPlayerCount - (AutomaticScalingMinimumPlayers - 1));
        return Mathf.Clamp(1f + extraPlayers * AutomaticHealthGrowthPerPlayer, 1f, 2f);
    }

    private static float CalculateAutomaticSpawnMultiplier()
    {
        if (_autoScaleLargeParties?.Value != true)
            return 1f;

        int extraPlayers = Mathf.Max(0, ConnectedPlayerCount - (AutomaticScalingMinimumPlayers - 1));
        return Mathf.Clamp(1f + extraPlayers * AutomaticSpawnGrowthPerPlayer, 1f, 4f);
    }

    private static void ScaleSpawnPlan(MonsterSpawnPhase phase)
    {
        if (!ShouldScale || phase?.spawnDatas == null)
            return;

        float multiplier = EndlessExpeditionFeature.IsHostActive
            ? EndlessExpeditionFeature.CurrentSpawnMultiplier
            : BaselineSpawnMultiplier;
        if (multiplier <= 1.001f)
            return;

        List<MonsterSpawnData> normalGroups = phase.spawnDatas
            .Where(data => data != null && data.monsterType == EMonsterType.Normal && data.count > 0)
            .ToList();
        int baselineCount = normalGroups.Sum(data => data.count);
        int remainingExtra = Mathf.Max(0, MaximumNormalEnemiesPerPhase - baselineCount);

        foreach (MonsterSpawnData data in normalGroups)
        {
            int desiredExtra = Mathf.Max(0, Mathf.CeilToInt(data.count * multiplier) - data.count);
            int extra = Mathf.Min(desiredExtra, remainingExtra);
            data.count += extra;
            remainingExtra -= extra;

            if (remainingExtra <= 0)
                break;
        }
    }

    private static void ScaleNewEnemies(object stateMachine)
    {
        if (!ShouldScale || stateMachine == null)
            return;

        try
        {
            Type stateMachineType = stateMachine.GetType();
            if (!OwnerFields.TryGetValue(stateMachineType, out FieldInfo ownerField))
            {
                ownerField = AccessTools.Field(stateMachineType, "<>4__this");
                OwnerFields[stateMachineType] = ownerField;
            }

            object owner = ownerField?.GetValue(stateMachine);
            if (owner == null)
                return;

            Type ownerType = owner.GetType();
            if (!SpawnedFields.TryGetValue(ownerType, out FieldInfo spawnedField))
            {
                spawnedField = AccessTools.Field(ownerType, "spawned");
                SpawnedFields[ownerType] = spawnedField;
            }

            if (spawnedField?.GetValue(owner) is not IEnumerable<UnitAvatar> spawned)
                return;

            foreach (UnitAvatar enemy in spawned)
                ScaleEnemyHealth(enemy);
        }
        catch (Exception exception)
        {
            _logger?.LogWarning($"Party Scaling skipped an enemy update: {exception.Message}");
        }
    }

    private static void ScaleEnemyHealth(UnitAvatar enemy)
    {
        if (enemy == null || enemy is PlayerAvatar || enemy.monsterType == EMonsterType.Dummy)
            return;
        if (ScaledEnemies.TryGetValue(enemy, out _))
            return;

        ScaledEnemies.Add(enemy, new ScaledMarker());
        float multiplier = EndlessExpeditionFeature.IsHostActive
            ? EndlessExpeditionFeature.CurrentHealthMultiplier
            : BaselineHealthMultiplier;
        if (multiplier <= 1.001f)
            return;

        float originalMaximum = enemy.MaxHp;
        float healthRatio = originalMaximum > 0f ? Mathf.Clamp01(enemy.Networkhp / originalMaximum) : 1f;
        enemy.NetworkmaxHp = Mathf.Min(enemy.NetworkmaxHp * multiplier, 2_000_000_000f);
        enemy.SetHp(enemy.MaxHp * healthRatio);
    }

    [HarmonyPatch(typeof(MonsterSpawnPhase), nameof(MonsterSpawnPhase.GenerateSpawnData))]
    private static class SpawnPlanPatch
    {
        private static void Postfix(MonsterSpawnPhase __result) => ScaleSpawnPlan(__result);
    }

    [HarmonyPatch]
    private static class SpawnedEnemyPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            return new[] { typeof(EnemySpawner), typeof(CommonEnemySpawner), typeof(RandomEnemyPhaseSpawner) }
                .SelectMany(type => type.GetNestedTypes(BindingFlags.NonPublic))
                .Where(type => type.Name.StartsWith("<SpawnCoroutine>", StringComparison.Ordinal) ||
                               type.Name.StartsWith("<SpawnEnemy>", StringComparison.Ordinal))
                .Select(type => AccessTools.Method(type, "MoveNext"))
                .Where(method => method != null);
        }

        private static void Postfix(object __instance) => ScaleNewEnemies(__instance);
    }

    [HarmonyPatch(typeof(BossSpawner), nameof(BossSpawner.NetworkbossObject), MethodType.Setter)]
    private static class BossSpawnPatch
    {
        private static void Postfix(BossSpawner __instance) => ScaleEnemyHealth(__instance.NetworkbossObject);
    }

    [HarmonyPatch(typeof(SeedBossSpawner), nameof(SeedBossSpawner.NetworkbossObject), MethodType.Setter)]
    private static class SeedBossSpawnPatch
    {
        private static void Postfix(SeedBossSpawner __instance) => ScaleEnemyHealth(__instance.NetworkbossObject);
    }
}
