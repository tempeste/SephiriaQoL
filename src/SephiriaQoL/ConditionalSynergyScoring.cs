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
    private const float UtilityLinkBonus = 60_000f;

    private static ConfigEntry<bool> _enabled;

    internal static void Configure(ConfigEntry<bool> enabled) => _enabled = enabled;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GridInventory), nameof(GridInventory.EvaluateCurrentAutoArrangeScore))]
    private static void IncludeDamageDependencies(GridInventory __instance, ref float __result)
    {
        if (_enabled?.Value != true || __instance == null || __instance.charms == null)
            return;

        var scoredPlanets = new HashSet<int>();
        var scoredChaoticCompanions = new HashSet<int>();

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

                case Charm_AutoMagic autoMagic when HasBoltBelow(__instance, autoMagic):
                    float cooldown = GetLevelValue(autoMagic, autoMagic.cooldownByLevel);
                    float castsPerTenSeconds = cooldown > 0f ? 10f / cooldown : 0f;
                    __result += ScoreLink(autoMagic.IsEffectEnabled,
                        Mathf.RoundToInt(castsPerTenSeconds * 10f), ScorePerSupportPercent);
                    break;

                case Charm_NearLevelDamage nearLevel:
                    int adjacentDamage = GetAdjacentLevelDamage(__instance, nearLevel);
                    if (adjacentDamage > 0)
                        __result += ScoreLink(nearLevel.IsEffectEnabled, adjacentDamage, ScorePerDamagePercent);
                    break;

                case Charm_NearMagicBullet fireworks:
                    int magicCount = CountMagicInColumn(__instance, fireworks.xIdx);
                    if (magicCount > 0)
                    {
                        int fireworkDamage = GetLevelValue(fireworks, fireworks.damagePercentByLevel);
                        // Every connected Grimoire can trigger the effect and each hit
                        // emits one firework per connected Grimoire.
                        int effectiveDamage = fireworkDamage * magicCount * magicCount;
                        __result += ScoreLink(fireworks.IsEffectEnabled, effectiveDamage, ScorePerDamagePercent);
                    }
                    break;

                case Charm_PlanetModule planetModule when planetModule.IsEffectEnabled:
                    int enhancedPlanetDamage = GetAdjacentPlanetDamage(
                        __instance, planetModule, scoredPlanets);
                    if (enhancedPlanetDamage > 0)
                        __result += ScoreLink(true,
                            Mathf.RoundToInt(enhancedPlanetDamage * 0.5f), ScorePerDamagePercent);
                    break;

                case Charm_CompanionChaos chaos when chaos.IsEffectEnabled:
                    int companionCount = CountCompanionsInRow(
                        __instance, chaos.yIdx, scoredChaoticCompanions);
                    if (companionCount > 0)
                        __result += UtilityLinkBonus * companionCount;
                    break;

                case Charm_WhitePaper whitePaper when HasMatchingAdjacentCategories(__instance, whitePaper):
                    if (whitePaper.IsEffectEnabled)
                        __result += ValidLinkBonus;
                    break;

                case Charm_WoodenBox woodenBox:
                    int topRowCharms = CountTopRowCharms(__instance);
                    if (topRowCharms > 0)
                    {
                        int elementalDamage = GetLevelValue(woodenBox, woodenBox.apPerQuickSlotCharmByLevel) *
                                              topRowCharms * 3;
                        __result += ScoreLink(woodenBox.IsEffectEnabled, elementalDamage, ScorePerDamagePercent);
                    }
                    break;
            }
        }
    }

    private static float ScoreLink(bool enabled, int percent, float scorePerPercent)
    {
        return enabled
            ? ValidLinkBonus + Mathf.Max(0, percent) * scorePerPercent
            : 0f;
    }

    private static bool HasBoltBelow(GridInventory inventory, Charm_AutoMagic autoMagic)
    {
        if (!TryGetMagic(inventory, autoMagic.xIdx, autoMagic.yIdx + 1, out Charm_Magic magic) ||
            magic.ContainedMagic == null || magic.ContainedMagic.magicPrefab == null)
            return false;

        return magic.ContainedMagic.magicPrefab.GetComponent<ActiveSkill>() is ActiveSkill_Bolt;
    }

    private static int GetAdjacentLevelDamage(GridInventory inventory, Charm_NearLevelDamage origin)
    {
        int level = Mathf.Clamp(origin.CurrentLevelToIdx(), 0,
            Mathf.Max(0, origin.allDamageBonusByLevel.Length - 1));
        float damagePerLevel = origin.allDamageBonusByLevel.Length == 0
            ? 0f
            : origin.allDamageBonusByLevel[level];
        int adjacentLevels = 0;

        for (int x = -1; x <= 1; x++)
        for (int y = -1; y <= 1; y++)
        {
            if (x == 0 && y == 0)
                continue;

            Charm_Basic neighbor = FindCharm(inventory, origin.xIdx + x, origin.yIdx + y);
            if (neighbor != null)
                adjacentLevels += Mathf.Min(neighbor.DisplayedLevel, neighbor.maxLevel);
        }

        return Mathf.FloorToInt(Mathf.Max(0f, damagePerLevel * adjacentLevels));
    }

    private static int CountMagicInColumn(GridInventory inventory, int x)
    {
        int count = 0;
        for (int y = 0; y < inventory.Height; y++)
        {
            if (FindCharm(inventory, x, y) is Charm_Magic)
                count++;
        }
        return count;
    }

    private static int GetAdjacentPlanetDamage(
        GridInventory inventory,
        Charm_PlanetModule origin,
        HashSet<int> scoredPlanets)
    {
        int damage = 0;
        for (int x = -1; x <= 1; x++)
        for (int y = -1; y <= 1; y++)
        {
            if (x == 0 && y == 0)
                continue;

            if (FindCharm(inventory, origin.xIdx + x, origin.yIdx + y) is Charm_SummonGreenBat planet &&
                scoredPlanets.Add(planet.GetInstanceID()))
                damage += GetLevelValue(planet, planet.damageByLevel);
        }
        return damage;
    }

    private static int CountCompanionsInRow(
        GridInventory inventory,
        int y,
        HashSet<int> scoredCompanions)
    {
        int count = 0;
        for (int x = 0; x < inventory.Width; x++)
        {
            if (FindCharm(inventory, x, y) is Charm_Basic companion &&
                companion is ICompanionCharm && scoredCompanions.Add(companion.GetInstanceID()))
                count++;
        }
        return count;
    }

    private static int CountTopRowCharms(GridInventory inventory)
    {
        int count = 0;
        int slots = Mathf.Min(6, inventory.CurrentInventoryStorage);
        for (int index = 0; index < slots; index++)
        {
            NewItemOwnInstance item = inventory.FindItem(inventory.IdxToPos(index));
            if (item?.Entity != null && item.Entity.type == EItemType.Charm)
                count++;
        }
        return count;
    }

    private static bool HasMatchingAdjacentCategories(GridInventory inventory, Charm_WhitePaper paper)
    {
        Charm_Basic left = FindCharm(inventory, paper.xIdx - 1, paper.yIdx);
        Charm_Basic right = FindCharm(inventory, paper.xIdx + 1, paper.yIdx);
        if (left == null || right == null)
            return false;

        var leftCategories = new HashSet<string>(left.GetItemCategory());
        foreach (string category in right.GetItemCategory())
        {
            if (!string.IsNullOrEmpty(category) && leftCategories.Contains(category))
                return true;
        }
        return false;
    }

    private static Charm_Basic FindCharm(GridInventory inventory, int x, int y)
    {
        if (x < 0 || x >= inventory.Width || y < 0 || y >= inventory.Height)
            return null;
        return inventory.FindItem(new ItemPosition((sbyte)x, (sbyte)y))?.Charm;
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

    private static float GetLevelValue(Charm_Basic charm, float[] values)
    {
        if (values == null || values.Length == 0)
            return 0f;

        int index = Mathf.Clamp(charm.CurrentLevelToIdx(), 0, values.Length - 1);
        return values[index];
    }
}
