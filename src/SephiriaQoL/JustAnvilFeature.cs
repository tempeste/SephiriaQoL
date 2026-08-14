using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SephiriaQoL;

[HarmonyPatch(typeof(DungeonManager))]
internal static class JustAnvilFeature
{
    private const uint AnvilPrefabAssetId = 2386519421u;
    private const int AnvilEventType = 11;
    private const int AnvilThreatType = 1;

    private static ConfigEntry<bool> _enabled;
    private static ManualLogSource _log;
    private static bool _placedThisRun;
    private static EFloorMainEventType _replacedEvent;
    private static EFloorThreatType _replacedThreat;
    private static uint _replacedPrefab;

    internal static void Configure(ConfigEntry<bool> enabled, ManualLogSource log)
    {
        _enabled = enabled;
        _log = log;
    }

    [HarmonyPostfix]
    [HarmonyPatch("GetAllFloorInStage")]
    private static void EnsureFirstChoiceContainsAnvil(List<FloorData> __result)
    {
        if (_enabled?.Value != true || _placedThisRun || __result == null || IsRemoteClient())
            return;

        List<FloorData> firstChoices = __result
            .Where(f => f != null && f.nodeProgress == 1 && IsPlayableNode(f.name))
            .ToList();
        if (firstChoices.Count == 0)
            return;

        if (firstChoices.Any(IsAnvilRoom))
        {
            _placedThisRun = true;
            _log?.LogInfo("JustAnvil: the first choice already contains an anvil room.");
            return;
        }

        FloorData replacement = firstChoices[0];
        _replacedEvent = replacement.mainEventType;
        _replacedThreat = replacement.threatType;
        _replacedPrefab = replacement.prefabAssetId;

        SetAsAnvil(replacement);

        FloorData laterAnvil = __result.FirstOrDefault(f =>
            f != null && f.guid != replacement.guid && IsAnvilRoom(f));
        if (laterAnvil != null)
        {
            laterAnvil.mainEventType = _replacedEvent;
            laterAnvil.threatType = _replacedThreat;
            laterAnvil.prefabAssetId = _replacedPrefab;
        }

        _placedThisRun = true;
        _log?.LogInfo("JustAnvil: moved an anvil room to the first playable choice.");
    }

    [HarmonyPrefix]
    [HarmonyPatch("FloorAlloc")]
    private static void KeepAllocatedAnvilPrefab(DungeonManager __instance, string guid)
    {
        if (_enabled?.Value != true || __instance?.generatedFloors == null || IsRemoteClient())
            return;

        if (__instance.generatedFloors.TryGetValue(guid, out FloorData floor) && IsAnvilRoom(floor))
            floor.prefabAssetId = AnvilPrefabAssetId;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameObject), nameof(GameObject.SetActive))]
    private static void ResetAfterGameOver(GameObject __instance, bool value)
    {
        if (value && __instance != null &&
            string.Equals(__instance.name, "GameOverLabel", StringComparison.Ordinal))
            _placedThisRun = false;
    }

    private static bool IsAnvilRoom(FloorData floor) =>
        floor != null && (int)floor.mainEventType == AnvilEventType &&
        (int)floor.threatType == AnvilThreatType;

    private static void SetAsAnvil(FloorData floor)
    {
        floor.mainEventType = (EFloorMainEventType)AnvilEventType;
        floor.threatType = (EFloorThreatType)AnvilThreatType;
        floor.prefabAssetId = AnvilPrefabAssetId;
    }

    private static bool IsPlayableNode(string name)
    {
        string normalized = (name ?? string.Empty).ToLowerInvariant();
        return !normalized.Contains("entrance") &&
               !normalized.Contains("town") &&
               !normalized.Contains("station");
    }

    private static bool IsRemoteClient() => NetworkClient.active && !NetworkServer.active;
}
