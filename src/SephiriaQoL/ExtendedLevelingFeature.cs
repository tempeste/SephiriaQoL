using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;

namespace SephiriaQoL;

internal static class ExtendedLevelingFeature
{
    internal const int VanillaMaximumLevel = 30;
    internal const int DefaultMaximumLevel = 100;
    internal const int MaximumSupportedLevel = 200;

    private static readonly AccessTools.FieldRef<int[]> ExperienceTableReference =
        AccessTools.StaticFieldRefAccess<int[]>(
            AccessTools.Field(typeof(LevelController), nameof(LevelController.ExpTableByLevel)));

    private static ConfigEntry<bool> _enabled;
    private static ConfigEntry<int> _maximumLevel;
    private static ManualLogSource _logger;
    private static int[] _baselineExperienceTable;
    private static bool? _lastAppliedState;
    private static int _lastAppliedMaximumLevel = -1;

    internal static void Configure(
        ConfigEntry<bool> enabled,
        ConfigEntry<int> maximumLevel,
        ManualLogSource logger)
    {
        _enabled = enabled;
        _maximumLevel = maximumLevel;
        _logger = logger;

        // Force Sephiria's type initializer to populate its authoritative
        // cumulative-XP table before retaining an untouched baseline copy.
        int[] currentTable = LevelController.ExpTableByLevel;
        if (currentTable == null || currentTable.Length < 2)
        {
            _logger?.LogError("Extended leveling could not read Sephiria's XP table.");
            return;
        }

        _baselineExperienceTable = (int[])currentTable.Clone();
        if (_baselineExperienceTable.Length != VanillaMaximumLevel)
        {
            _logger?.LogWarning(
                $"Expected Sephiria's standard maximum level to be {VanillaMaximumLevel}, but found {_baselineExperienceTable.Length}. " +
                "The installed game's table will be preserved as the progression baseline.");
        }

        Refresh();
    }

    internal static void Refresh()
    {
        if (_baselineExperienceTable == null || _enabled == null || _maximumLevel == null)
            return;

        bool shouldApply = _enabled.Value;
        int minimumLevel = Math.Max(VanillaMaximumLevel, _baselineExperienceTable.Length);
        int requestedMaximum = Math.Max(minimumLevel,
            Math.Min(MaximumSupportedLevel, _maximumLevel.Value));

        if (_lastAppliedState == shouldApply &&
            (!shouldApply || _lastAppliedMaximumLevel == requestedMaximum))
            return;

        ref int[] liveTable = ref ExperienceTableReference();
        if (shouldApply)
        {
            liveTable = BuildExtendedTable(_baselineExperienceTable, requestedMaximum);
            _lastAppliedMaximumLevel = requestedMaximum;
            _logger?.LogInfo(
                $"Extended run leveling enabled: level {minimumLevel} -> {requestedMaximum}.");
        }
        else
        {
            liveTable = (int[])_baselineExperienceTable.Clone();
            _lastAppliedMaximumLevel = _baselineExperienceTable.Length;
            if (_lastAppliedState == true)
                _logger?.LogInfo("Extended run leveling disabled; restored Sephiria's original XP table.");
        }

        _lastAppliedState = shouldApply;
    }

    internal static void Restore()
    {
        if (_baselineExperienceTable == null)
            return;

        ref int[] liveTable = ref ExperienceTableReference();
        liveTable = (int[])_baselineExperienceTable.Clone();
        _lastAppliedState = false;
        _lastAppliedMaximumLevel = _baselineExperienceTable.Length;
    }

    private static int[] BuildExtendedTable(int[] baseline, int maximumLevel)
    {
        if (maximumLevel <= baseline.Length)
            return (int[])baseline.Clone();

        int[] extended = new int[maximumLevel];
        Array.Copy(baseline, extended, baseline.Length);

        int incrementalCost = Math.Max(1,
            baseline[baseline.Length - 1] - baseline[baseline.Length - 2]);
        for (int levelIndex = baseline.Length; levelIndex < extended.Length; levelIndex++)
        {
            // Continue the late-game curve smoothly: the XP required for the next
            // level increases by 200 every three levels beyond the standard cap.
            if ((levelIndex - baseline.Length) % 3 == 0)
                incrementalCost += 200;

            long nextThreshold = (long)extended[levelIndex - 1] + incrementalCost;
            extended[levelIndex] = (int)Math.Min(int.MaxValue - 1L, nextThreshold);
        }

        return extended;
    }
}
