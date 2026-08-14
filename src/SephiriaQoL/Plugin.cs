using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace SephiriaQoL;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "dev.tempeste.sephiria.qol";
    public const string PluginName = "Sephiria QoL";
    public const string PluginVersion = "0.7.0";

    private Harmony _harmony;
    private UtilityOverlay _utility;
    private TabletOptimizerOverlay _tabletOptimizer;
    private SpectatorFeature _spectator;
    private QoLControlCenter _controlCenter;
    private bool _nativeAddOnsChecked;

    private void Awake()
    {
        ConfigEntry<bool> showTimer = Config.Bind("Utility", "ShowRunTimer", true,
            "Displays elapsed run time at the top of the screen.");
        ConfigEntry<bool> showDamage = Config.Bind("Utility", "ShowDamageContribution", true,
            "Displays each active player's damage and contribution percentage.");
        ConfigEntry<float> damagePanelScale = Config.Bind("Utility", "DamagePanelScale", 0f,
            "Damage panel scale from 0.75 to 2.0. Use 0 for automatic Retina-aware sizing.");
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
            "Rewards satisfied positional relic conditions and damage-producing artifact links.");
        ConfigEntry<float> tabletOptimizerScale = Config.Bind("TabletOptimizer", "PanelScale", 0f,
            "Tablet optimizer panel scale from 0.75 to 2.0. Use 0 for automatic Retina-aware sizing.");
        ConfigEntry<bool> spectatorEnabled = Config.Bind("Spectator", "Enabled", true,
            "When you die in multiplayer, follows a living teammate until you are revived.");
        ConfigEntry<KeyboardShortcut> spectatorPreviousHotkey = Config.Bind("Spectator", "PreviousPlayerHotkey",
            new KeyboardShortcut(UnityEngine.KeyCode.LeftArrow), "Selects the previous living player while spectating.");
        ConfigEntry<KeyboardShortcut> spectatorNextHotkey = Config.Bind("Spectator", "NextPlayerHotkey",
            new KeyboardShortcut(UnityEngine.KeyCode.RightArrow), "Selects the next living player while spectating.");
        ConfigEntry<float> spectatorPanelScale = Config.Bind("Spectator", "PanelScale", 0f,
            "Spectator panel scale from 0.75 to 2.0. Use 0 for automatic Retina-aware sizing.");
        ConfigEntry<bool> maxPlayerEnabled = Config.Bind("MaxPlayer", "Enabled", true,
            "Allows hosts to create rooms above Sephiria's vanilla four-player limit.");
        ConfigEntry<int> maxPlayers = Config.Bind("MaxPlayer", "MaximumPlayers", 16,
            new ConfigDescription("Maximum host/lobby capacity from 2 to 16 players.",
                new AcceptableValueRange<int>(2, 16)));
        ConfigEntry<bool> compactMultiplayerHud = Config.Bind("MaxPlayer", "CompactMultiplayerHud", true,
            "Scales down Sephiria's multiplayer roster as more players join.");
        ConfigEntry<bool> partyScalingEnabled = Config.Bind("PartyScaling", "Enabled", false,
            "Allows this host to scale newly spawned enemy health and normal-enemy counts.");
        ConfigEntry<float> enemyHealthMultiplier = Config.Bind("PartyScaling", "EnemyHealthMultiplier", 1f,
            new ConfigDescription("Host-side enemy health multiplier from 1.0 to 10.0.",
                new AcceptableValueRange<float>(1f, 10f)));
        ConfigEntry<float> enemySpawnMultiplier = Config.Bind("PartyScaling", "EnemySpawnMultiplier", 1f,
            new ConfigDescription("Host-side normal-enemy count multiplier from 1.0 to 4.0.",
                new AcceptableValueRange<float>(1f, 4f)));
        ConfigEntry<bool> extendedLevelingEnabled = Config.Bind("ExtendedLeveling", "Enabled", true,
            "Allows run levels to continue beyond Sephiria's standard maximum level.");
        ConfigEntry<int> maximumLevel = Config.Bind("ExtendedLeveling", "MaximumLevel",
            ExtendedLevelingFeature.DefaultMaximumLevel,
            new ConfigDescription("Maximum run level from 30 to 200.",
                new AcceptableValueRange<int>(ExtendedLevelingFeature.VanillaMaximumLevel,
                    ExtendedLevelingFeature.MaximumSupportedLevel)));
        ConfigEntry<bool> showControlCenter = Config.Bind("Interface", "ShowControlCenter", false,
            "Shows the QoL Control Center. The compact QOL button remains available while hidden.");
        ConfigEntry<KeyboardShortcut> controlCenterHotkey = Config.Bind("Interface", "ToggleControlCenterHotkey",
            new KeyboardShortcut(UnityEngine.KeyCode.F11), "Shows or hides the QoL Control Center.");
        ConfigEntry<float> controlCenterScale = Config.Bind("Interface", "ControlCenterScale", 0f,
            "Control Center scale from 0.75 to 2.0. Use 0 for automatic Retina-aware sizing.");

        UtilityOverlay.Configure(showTimer, showDamage, damagePanelScale);
        JournalSearch.Configure(journalSearch);
        JustAnvilFeature.Configure(guaranteedAnvil, Logger);
        _utility = new UtilityOverlay();
        _tabletOptimizer = new TabletOptimizerOverlay(
            showTabletOptimizer, tabletOptimizerHotkey, tabletOptimizerPasses, allowTabletRotation,
            preferConditionalSynergies, tabletOptimizerScale, Logger);
        _spectator = new SpectatorFeature(
            spectatorEnabled, spectatorPreviousHotkey, spectatorNextHotkey, spectatorPanelScale);
        ConditionalSynergyScoring.Configure(preferConditionalSynergies);
        MaxPlayerFeature.Configure(maxPlayerEnabled, maxPlayers, compactMultiplayerHud, Logger);
        PartyScalingFeature.Configure(partyScalingEnabled, enemyHealthMultiplier, enemySpawnMultiplier, Logger);
        ExtendedLevelingFeature.Configure(extendedLevelingEnabled, maximumLevel, Logger);
        _controlCenter = new QoLControlCenter(
            showControlCenter, controlCenterHotkey, controlCenterScale,
            showTimer, showDamage, damagePanelScale, journalSearch, guaranteedAnvil,
            showTabletOptimizer, allowTabletRotation, preferConditionalSynergies, tabletOptimizerScale,
            spectatorEnabled, spectatorPanelScale, maxPlayerEnabled, maxPlayers, compactMultiplayerHud,
            partyScalingEnabled, enemyHealthMultiplier, enemySpawnMultiplier,
            extendedLevelingEnabled, maximumLevel);

        _harmony = new Harmony(PluginGuid);
        _harmony.PatchAll();
        Logger.LogInfo("Loaded independent QoL features: Control Center, run timer, detailed damage contribution, journal search, JustAnvil, tablet optimizer, spectator mode, configurable 16-player rooms, optional host Party Scaling, and extended run leveling.");
    }

    private void Update()
    {
        _utility?.Update();
        _tabletOptimizer?.Update();
        _spectator?.Update();
        _controlCenter?.Update();
        ExtendedLevelingFeature.Refresh();

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
        _spectator?.OnGUI();
        _controlCenter?.OnGUI();
    }

    private void OnDestroy()
    {
        ExtendedLevelingFeature.Restore();
        _harmony?.UnpatchSelf();
        _spectator?.Dispose();
        _utility = null;
        _tabletOptimizer = null;
        _spectator = null;
        _controlCenter = null;
    }
}
