using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace SephiriaQoL;

internal static class ConditionalSynergyScoring
{
    // A capped charm level contributes 10,000 points to the game's native score.
    // Valuing each real damage-percent point at 20,000 makes a productive Needle
    // link more important than gaining a few irrelevant levels, while still
    // letting tablet levels distinguish layouts with equivalent damage links.
    private const float ScorePerDamagePercent = 20_000f;
    private const float ScorePerSupportPercent = 10_000f;
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
            switch (charm)
            {
                case Charm_UpCharmDamage needle
                    when TryResolveDamageTarget(__instance, needle, out Charm_Basic target):
                    int damagePercent = GetDamagePercent(needle, target);
                    __result += ScoreLink(needle.IsEffectEnabled, damagePercent, ScorePerDamagePercent);
                    break;

                case Charm_RightSpellCooldownHelper hourglass
                    when TryGetMagic(__instance, hourglass.xIdx + 1, hourglass.yIdx, out _):
                    int hastePercent = GetLevelValue(hourglass, hourglass.cooldownRecoveryByLevel);
                    __result += ScoreLink(hourglass.IsEffectEnabled, hastePercent, ScorePerSupportPercent);
                    break;

                case Charm_ReduceMPCost starFragment
                    when TryGetMagic(__instance, starFragment.xIdx - 1, starFragment.yIdx, out _):
                    int reductionPercent = GetLevelValue(starFragment, starFragment.reducePercentByLevel);
                    __result += ScoreLink(starFragment.IsEffectEnabled, reductionPercent, ScorePerSupportPercent);
                    break;
            }
        }
    }

    private static float ScoreLink(bool enabled, int percent, float scorePerPercent)
    {
        return enabled
            ? ValidLinkBonus + Mathf.Max(0, percent) * scorePerPercent
            : DormantLinkBonus;
    }

    private static bool TryGetMagic(
        GridInventory inventory,
        int x,
        int y,
        out Charm_Magic magic)
    {
        magic = null;
        if (x < sbyte.MinValue || x > sbyte.MaxValue || y < sbyte.MinValue || y > sbyte.MaxValue)
            return false;

        NewItemOwnInstance item = inventory.FindItem(new ItemPosition((sbyte)x, (sbyte)y));
        magic = item?.Charm as Charm_Magic;
        return magic != null;
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
        int result = GetLevelValue(needle, needle.damageBonusByLevel);

        if (!needle.hasDependencyCondition || target?.Item == null ||
            needle.dependencyDamageBonusByLevel == null || needle.dependencyDamageBonusByLevel.Length == 0)
            return result;

        ItemEntity targetEntity = ItemDatabase.FindItemById(target.Item.EntityID);
        if (targetEntity != null && targetEntity.rarity <= needle.maxRarity)
        {
            result += GetLevelValue(needle, needle.dependencyDamageBonusByLevel);
        }

        return result;
    }

    private static int GetLevelValue(Charm_Basic charm, int[] values)
    {
        if (values == null || values.Length == 0)
            return 0;

        int index = Mathf.Clamp(charm.CurrentLevelToIdx(), 0, values.Length - 1);
        return values[index];
    }
}
