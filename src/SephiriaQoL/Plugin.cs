using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace SephiriaQoL;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "dev.tempeste.sephiria.qol";
    public const string PluginName = "Sephiria QoL";
    public const string PluginVersion = "0.2.2";

    private Harmony _harmony;
    private UtilityOverlay _utility;
    private TabletOptimizerOverlay _tabletOptimizer;
    private bool _nativeAddOnsChecked;

    private void Awake()
    {
        ConfigEntry<bool> showTimer = Config.Bind("Utility", "ShowRunTimer", true,
            "Displays elapsed run time at the top of the screen.");
        ConfigEntry<bool> showDamage = Config.Bind("Utility", "ShowDamageContribution", true,
            "Displays each active player's damage and contribution percentage.");
        ConfigEntry<bool> journalSearch = Config.Bind("Utility", "JournalSearch", true,
            "Adds a text filter while the artifact journal is open.");
        ConfigEntry<bool> guaranteedAnvil = Config.Bind("JustAnvil", "Enabled", true,
            "Places an anvil room in the first playable choice once per run.");
        ConfigEntry<bool> showTabletOptimizer = Config.Bind("TabletOptimizer", "ShowPanel", true,
            "Shows the tablet optimizer control panel. Press F10 to toggle it.");
        ConfigEntry<KeyboardShortcut> tabletOptimizerHotkey = Config.Bind("TabletOptimizer", "TogglePanelHotkey",
            new KeyboardShortcut(UnityEngine.KeyCode.F10), "Shows or hides the tablet optimizer panel.");
        ConfigEntry<int> tabletOptimizerPasses = Config.Bind("TabletOptimizer", "OptimizationPasses", 1,
            "Number of improvement passes per click (1-4). More passes can pause the game for longer.");
        ConfigEntry<bool> allowTabletRotation = Config.Bind("TabletOptimizer", "AllowTabletRotation", true,
            "Allows the optimizer to rotate tablets when that improves the layout.");
        ConfigEntry<bool> preferConditionalSynergies = Config.Bind("TabletOptimizer", "PreferConditionalSynergies", true,
            "Rewards satisfied positional relic conditions, including Needles and left/right Grimoire supports.");

        UtilityOverlay.Configure(showTimer, showDamage);
        JournalSearch.Configure(journalSearch);
        JustAnvilFeature.Configure(guaranteedAnvil, Logger);
        _utility = new UtilityOverlay();
        _tabletOptimizer = new TabletOptimizerOverlay(
            showTabletOptimizer, tabletOptimizerHotkey, tabletOptimizerPasses, allowTabletRotation,
            preferConditionalSynergies, Logger);
        ConditionalSynergyScoring.Configure(preferConditionalSynergies);

        _harmony = new Harmony(PluginGuid);
        _harmony.PatchAll();
        Logger.LogInfo("Loaded independent QoL features: run timer, damage contribution, journal search, JustAnvil, and tablet optimizer.");
    }

    private void Update()
    {
        _utility?.Update();
        _tabletOptimizer?.Update();

        if (!_nativeAddOnsChecked && UnityEngine.Time.realtimeSinceStartup >= 4f)
        {
            _nativeAddOnsChecked = true;
            NativeAddOnBootstrap.EnsureLoaded(Logger);
        }
    }

    private void OnGUI()
    {
        _utility?.OnGUI();
        JournalSearch.OnGUI();
        _tabletOptimizer?.OnGUI();
    }

    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
        _utility = null;
        _tabletOptimizer = null;
    }
}
