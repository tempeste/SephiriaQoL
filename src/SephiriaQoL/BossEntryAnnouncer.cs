using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Mirror;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace SephiriaQoL;

internal static class BossEntryAnnouncer
{
    private static ConfigEntry<bool> _enabled;
    private static ManualLogSource _log;
    private static readonly FieldInfo EnemyBattlePhaseField =
        AccessTools.Field(typeof(EnemySpawner), "battlePhase");

    internal static void Configure(ConfigEntry<bool> enabled, ManualLogSource log)
    {
        _enabled = enabled;
        _log = log;
    }

    [HarmonyPatch(typeof(EnemySpawner), nameof(EnemySpawner.ServerBattleBegin))]
    private static class MinibossBattlePatch
    {
        private static void Prefix(EnemySpawner __instance)
        {
            try
            {
                if (_enabled?.Value != true || !NetworkServer.active || __instance == null ||
                    !IsPending(__instance) || !ContainsMiniboss(__instance))
                    return;

                PlayerAvatar triggeringPlayer = FindTriggeringPlayer(__instance);
                if (triggeringPlayer != null)
                    Broadcast($"{ReadPlayerName(triggeringPlayer)} triggered the miniboss encounter.");
            }
            catch (Exception exception)
            {
                _log?.LogWarning($"Miniboss-entry announcement failed: {exception.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(BossSpawner), nameof(BossSpawner.StartBattle))]
    private static class StartBattlePatch
    {
        private static void Prefix(BossSpawner __instance, PlayerAvatar player)
        {
            if (_enabled?.Value != true || !NetworkServer.active || __instance == null ||
                __instance.IsBossBattleInProgress || player == null || PlayerSpawner.MultiplayerList == null)
                return;

            try
            {
                Broadcast($"{ReadPlayerName(player)} triggered the boss encounter.");
            }
            catch (Exception exception)
            {
                _log?.LogWarning($"Boss-entry announcement failed: {exception.Message}");
            }
        }
    }

    private static bool ContainsMiniboss(EnemySpawner spawner)
    {
        return spawner.spawnDatasByPhase != null && spawner.spawnDatasByPhase
            .Where(phase => phase?.spawnDatas != null)
            .SelectMany(phase => phase.spawnDatas)
            .Any(spawn => spawn != null && spawn.monsterType == EMonsterType.Miniboss);
    }

    private static bool IsPending(EnemySpawner spawner)
    {
        object phase = EnemyBattlePhaseField?.GetValue(spawner);
        return phase != null && Convert.ToInt32(phase) == 0;
    }

    private static PlayerAvatar FindTriggeringPlayer(EnemySpawner spawner)
    {
        if (PlayerSpawner.MultiplayerList == null)
            return null;

        Vector2 origin = spawner.transform.position;
        Vector2 lower = origin + spawner.detectArea_lb;
        Vector2 upper = origin + spawner.detectArea_rt;
        return PlayerSpawner.MultiplayerList
            .Select(entry => entry?.PlayerAvatar)
            .Where(player => player != null && !player.IsDead)
            .Where(player =>
            {
                Vector2 position = player.transform.position;
                return position.x >= lower.x && position.y >= lower.y &&
                       position.x <= upper.x && position.y <= upper.y;
            })
            .OrderBy(player => Vector2.SqrMagnitude((Vector2)player.transform.position - origin))
            .FirstOrDefault();
    }

    private static void Broadcast(string message)
    {
        if (PlayerSpawner.MultiplayerList == null)
            return;

        foreach (PlayerSpawner spawner in PlayerSpawner.MultiplayerList)
        {
            PlayerAvatar recipient = spawner?.PlayerAvatar;
            if (recipient?.connectionToClient != null)
                recipient.TargetSendCustomMessage(recipient.connectionToClient, message);
        }
    }

    private static string ReadPlayerName(PlayerAvatar player)
    {
        try
        {
            object value = AccessTools.Property(typeof(PlayerAvatar), "Name")?.GetValue(player);
            if (value is string name && !string.IsNullOrWhiteSpace(name))
                return name;
        }
        catch
        {
        }

        return player.name.Replace("(Clone)", "");
    }
}
