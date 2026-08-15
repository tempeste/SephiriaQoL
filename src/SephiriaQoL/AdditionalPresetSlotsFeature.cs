using BepInEx.Configuration;
using HarmonyLib;
using System;

namespace SephiriaQoL;

internal static class AdditionalPresetSlotsFeature
{
    internal const int NativeSlotLimit = 15;
    internal const int MaximumSupportedSlots = 50;

    private static ConfigEntry<bool> _enabled;
    private static ConfigEntry<int> _slotLimit;

    internal static void Configure(ConfigEntry<bool> enabled, ConfigEntry<int> slotLimit)
    {
        _enabled = enabled;
        _slotLimit = slotLimit;
    }

    [HarmonyPatch(typeof(UI_PresetPanel), nameof(UI_PresetPanel.GetSlotLimitCount))]
    private static class SlotLimitPatch
    {
        private static void Postfix(ref int __result)
        {
            if (_enabled?.Value == true)
                __result = Math.Max(__result, Math.Min(MaximumSupportedSlots, _slotLimit.Value));
        }
    }
}
