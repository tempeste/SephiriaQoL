using BepInEx.Logging;
using System;
using System.IO;

namespace SephiriaQoL;

internal static class NativeAddOnBootstrap
{
    internal static void EnsureLoaded(ManualLogSource log)
    {
        string addOnsPath = AddOnLoader.AddOnsPath;
        if (!Directory.Exists(addOnsPath) || Directory.GetDirectories(addOnsPath).Length == 0)
            return;

        if (AddOnLoader.LoadedMods != null && AddOnLoader.LoadedMods.Count > 0)
        {
            log.LogInfo($"Native AddOns already loaded: {AddOnLoader.LoadedMods.Count}.");
            return;
        }

        try
        {
            AddOnLoader.LoadAll();
            int count = AddOnLoader.LoadedMods?.Count ?? 0;
            log.LogInfo($"Loaded {count} native AddOn(s) from {addOnsPath}.");
        }
        catch (Exception exception)
        {
            log.LogError($"Native AddOn loading failed: {exception}");
        }
    }
}
