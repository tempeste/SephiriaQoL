using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace SephiriaQoL;

internal static class DamageSynergyScoring
{
    // A capped charm level contributes 10,000 points to the game's native score.
    // Valuing each real damage-percent point at 20,000 makes a productive Needle
    // link more important than gaining a few irrelevant levels, while still
    // letting tablet levels distinguish layouts with equivalent damage links.
    private const float ScorePerDamagePercent = 20_000f;
    private const float ValidLinkBonus = 100_000f;
    private const float DormantLinkBonus = 10_000f;

    private static ConfigEntry<bool> _enabled;

    internal static void Configure(ConfigEntry<bool> enabled) => _enabled = enabled;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GridInventory), nameof(GridInventory.EvaluateCurrentAutoArrangeScore))]
    private static void IncludeDamageDependencies(GridInventory __instance, ref float __result)
    {
        if (_enabled?.Value != true || __instance == null || __instance.charms == null)
            return;

        foreach (Charm_Basic charm in __instance.charms.Values)
        {
            if (charm is Charm_UpCharmDamage needle &&
                TryResolveDamageTarget(__instance, needle, out Charm_Basic target))
            {
                if (!needle.IsEffectEnabled)
                {
                    __result += DormantLinkBonus;
                    continue;
                }

                int damagePercent = GetDamagePercent(needle, target);
                __result += ValidLinkBonus + damagePercent * ScorePerDamagePercent;
            }
        }
    }

    private static bool TryResolveDamageTarget(
        GridInventory inventory,
        Charm_UpCharmDamage origin,
        out Charm_Basic target)
    {
        target = null;
        Charm_UpCharmDamage current = origin;
        var visited = new HashSet<int>();

        while (current != null)
        {
            if (!visited.Add(current.GetInstanceID()))
                return false;

            var position = new ItemPosition(
                (sbyte)(current.xIdx + current.xOffset),
                (sbyte)(current.yIdx + current.yOffset));
            NewItemOwnInstance item = inventory.FindItem(position);
            Charm_Basic next = item?.Charm;
            if (next == null || next == origin)
                return false;

            if (next is Charm_UpCharmDamage nextNeedle)
            {
                current = nextNeedle;
                continue;
            }

            if (next is IAttackableCharm attackable && attackable.IsAttackableCharm())
            {
                target = next;
                return true;
            }

            return false;
        }

        return false;
    }

    private static int GetDamagePercent(Charm_UpCharmDamage needle, Charm_Basic target)
    {
        int result = 0;
        if (needle.damageBonusByLevel != null && needle.damageBonusByLevel.Length > 0)
        {
            int levelIndex = Mathf.Clamp(needle.CurrentLevelToIdx(), 0, needle.damageBonusByLevel.Length - 1);
            result = needle.damageBonusByLevel[levelIndex];
        }

        if (!needle.hasDependencyCondition || target?.Item == null ||
            needle.dependencyDamageBonusByLevel == null || needle.dependencyDamageBonusByLevel.Length == 0)
            return result;

        ItemEntity targetEntity = ItemDatabase.FindItemById(target.Item.EntityID);
        if (targetEntity != null && targetEntity.rarity <= needle.maxRarity)
        {
            int dependencyIndex = Mathf.Clamp(
                needle.CurrentLevelToIdx(), 0, needle.dependencyDamageBonusByLevel.Length - 1);
            result += needle.dependencyDamageBonusByLevel[dependencyIndex];
        }

        return result;
    }
}
