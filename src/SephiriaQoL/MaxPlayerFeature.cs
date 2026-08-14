using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace SephiriaQoL;

internal static class MaxPlayerFeature
{
    private const int VanillaMaxPlayers = 4;
    private const int SupportedMaxPlayers = 16;

    private static ConfigEntry<bool> _enabled;
    private static ConfigEntry<int> _maxPlayers;
    private static ConfigEntry<bool> _compactHud;
    private static ManualLogSource _log;

    internal static void Configure(
        ConfigEntry<bool> enabled,
        ConfigEntry<int> maxPlayers,
        ConfigEntry<bool> compactHud,
        ManualLogSource log)
    {
        _enabled = enabled;
        _maxPlayers = maxPlayers;
        _compactHud = compactHud;
        _log = log;
    }

    private static bool Enabled => _enabled?.Value == true;

    private static int PlayerLimit => Mathf.Clamp(
        _maxPlayers?.Value ?? SupportedMaxPlayers,
        2,
        SupportedMaxPlayers);

    [HarmonyPatch(typeof(HorayNetworkManager), nameof(HorayNetworkManager.Awake))]
    private static class NetworkCapacityPatch
    {
        private static void Postfix(HorayNetworkManager __instance)
        {
            if (Enabled)
            {
                __instance.maxConnections = PlayerLimit;
                _log?.LogInfo($"Host network capacity set to {PlayerLimit} players.");
            }
        }
    }

    [HarmonyPatch(typeof(UI_HorizontalSelectionBox_MultiplayerNumber), "OnEnable")]
    private static class LobbyPlayerSelectorPatch
    {
        private static void Postfix(UI_HorizontalSelectionBox_MultiplayerNumber __instance)
        {
            if (!Enabled || __instance?.box == null)
                return;

            // The game displays selection index + 2, so 15 choices represents 2–16 players.
            int choices = PlayerLimit - 1;
            __instance.box.numberOfElements = choices;
            __instance.box.ChangeValue(Mathf.Clamp(__instance.box.CurrentSelection, 0, choices - 1));
        }
    }

    [HarmonyPatch(typeof(EOSLobbyManager), nameof(EOSLobbyManager.Create))]
    private static class EosLobbyCapacityPatch
    {
        private static void Prefix(ref int maxMembers)
        {
            if (Enabled)
                maxMembers = Mathf.Clamp(maxMembers, 2, PlayerLimit);
        }
    }

    [HarmonyPatch(typeof(UI_MultiplayerHUD), "Update")]
    private static class MultiplayerHudScalePatch
    {
        private static void Postfix(UI_MultiplayerHUD __instance)
        {
            if (__instance?.contentsZone == null)
                return;

            if (!Enabled || _compactHud?.Value != true)
            {
                __instance.contentsZone.localScale = Vector3.one;
                return;
            }

            int entries = __instance.contentsZone.childCount;
            float scale = entries switch
            {
                <= VanillaMaxPlayers => 1f,
                <= 8 => 0.82f,
                <= 12 => 0.68f,
                _ => 0.56f
            };

            __instance.contentsZone.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
