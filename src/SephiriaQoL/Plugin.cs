using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace SephiriaQoL;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "dev.tempeste.sephiria.qol";
    public const string PluginName = "Sephiria QoL";
    public const string PluginVersion = "0.9.1";

    private Harmony _harmony;
    private UtilityOverlay _utility;
    private RunSummaryOverlay _runSummary;
    private FastShopRerollFeature _fastShopReroll;
    private PartyReadinessOverlay _partyReadiness;
    private HiddenRoomGuidanceOverlay _hiddenRoomGuidance;
    private PartyVoteOverlay _partyVote;
    private LeafTransferOverlay _leafTransfer;
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
        ConfigEntry<bool> showDamageTaken = Config.Bind("Utility", "ShowDamageTaken", true,
            "Displays each active player's run-total damage taken in the combat tracker.");
        ConfigEntry<float> damagePanelScale = Config.Bind("Utility", "DamagePanelScale", 0f,
            "Damage panel scale from 0.75 to 2.0. Use 0 for automatic Retina-aware sizing.");
        ConfigEntry<bool> journalSearch = Config.Bind("Utility", "JournalSearch", true,
            "Adds a text filter while the artifact journal is open.");
        ConfigEntry<bool> runSummaryEnabled = Config.Bind("RunSummary", "Enabled", true,
            "Shows a team combat summary when the game-over screen opens.");
        ConfigEntry<float> runSummaryScale = Config.Bind("RunSummary", "PanelScale", 0f,
            "Run summary scale from 0.75 to 2.0. Use 0 for automatic Retina-aware sizing.");
        ConfigEntry<KeyboardShortcut> runSummaryHistoryHotkey = Config.Bind(
            "RunSummary", "HistoryHotkey", new KeyboardShortcut(UnityEngine.KeyCode.F8),
            "Shows or hides the most recent locally saved run summary.");
        ConfigEntry<int> runSummaryHistoryLimit = Config.Bind("RunSummary", "HistoryLimit", 20,
            new ConfigDescription("Number of recent run summaries retained locally from 1 to 50.",
                new AcceptableValueRange<int>(1, 50)));
        ConfigEntry<bool> bossAnnouncerEnabled = Config.Bind("BossAnnouncer", "Enabled", true,
            "When hosting, announces the first player who triggers each miniboss or boss encounter.");
        ConfigEntry<bool> fastShopRerollEnabled = Config.Bind("FastShopReroll", "Enabled", true,
            "Allows the shop's normal replenishment action to repeat while its hotkey is held.");
        ConfigEntry<KeyboardShortcut> fastShopRerollHotkey = Config.Bind(
            "FastShopReroll", "Hotkey", new KeyboardShortcut(UnityEngine.KeyCode.R),
            "Hold this key while the shop is open to repeat Sephiria's normal reroll action.");
        ConfigEntry<float> fastShopRerollInterval = Config.Bind("FastShopReroll", "RepeatInterval", 0.35f,
            new ConfigDescription("Seconds between held shop rerolls from 0.15 to 1.0.",
                new AcceptableValueRange<float>(0.15f, 1f)));
        ConfigEntry<bool> holdToCastEnabled = Config.Bind("HoldToCast", "Enabled", true,
            "Repeats held spell and active-artifact inputs as soon as their cooldown finishes.");
        ConfigEntry<bool> partyReadinessEnabled = Config.Bind("PartyReadiness", "Enabled", true,
            "Shows synchronized large-party loading, menu, combat, and ready states.");
        ConfigEntry<KeyboardShortcut> partyReadinessHotkey = Config.Bind(
            "PartyReadiness", "TogglePanelHotkey", new KeyboardShortcut(UnityEngine.KeyCode.F5),
            "Shows or hides the party readiness panel.");
        ConfigEntry<float> partyReadinessScale = Config.Bind("PartyReadiness", "PanelScale", 0f,
            "Party readiness panel scale from 0.75 to 2.0. Use 0 for automatic sizing.");
        ConfigEntry<bool> hiddenRoomGuidanceEnabled = Config.Bind("HiddenRoomGuidance", "Enabled", true,
            "Shows a client-side direction marker for nearby undiscovered hidden-room entrances.");
        ConfigEntry<float> hiddenRoomGuidanceScale = Config.Bind("HiddenRoomGuidance", "MarkerScale", 0f,
            "Hidden-room marker scale from 0.75 to 2.0. Use 0 for automatic sizing.");
        ConfigEntry<bool> partyVotingEnabled = Config.Bind("PartyVoting", "Enabled", true,
            "Allows manual room and loot votes; only the host sees the tally and nothing is auto-selected.");
        ConfigEntry<KeyboardShortcut> partyVotingHotkey = Config.Bind(
            "PartyVoting", "TogglePanelHotkey", new KeyboardShortcut(UnityEngine.KeyCode.F7),
            "Shows or hides the party voting panel.");
        ConfigEntry<float> partyVotingScale = Config.Bind("PartyVoting", "PanelScale", 0f,
            "Party voting panel scale from 0.75 to 2.0. Use 0 for automatic sizing.");
        ConfigEntry<bool> leafTransferEnabled = Config.Bind("LeafTransfer", "Enabled", false,
            "Allows validated same-floor leaf transfers when the host also enables this feature.");
        ConfigEntry<KeyboardShortcut> leafTransferHotkey = Config.Bind(
            "LeafTransfer", "TogglePanelHotkey", new KeyboardShortcut(UnityEngine.KeyCode.F6),
            "Shows or hides the leaf transfer panel.");
        ConfigEntry<int> maximumLeafTransfer = Config.Bind("LeafTransfer", "MaximumPerTransfer", 9999,
            new ConfigDescription("Maximum leaves allowed in one validated transfer from 1 to 999999.",
                new AcceptableValueRange<int>(1, 999999)));
        ConfigEntry<float> leafTransferScale = Config.Bind("LeafTransfer", "PanelScale", 0f,
            "Leaf transfer panel scale from 0.75 to 2.0. Use 0 for automatic sizing.");
        ConfigEntry<bool> additionalPresetSlotsEnabled = Config.Bind("PresetSlots", "Enabled", true,
            "Expands Sephiria's native preset list without changing existing preset data.");
        ConfigEntry<int> presetSlotLimit = Config.Bind("PresetSlots", "MaximumSlots", 30,
            new ConfigDescription("Native preset slot limit from 15 to 50.",
                new AcceptableValueRange<int>(AdditionalPresetSlotsFeature.NativeSlotLimit,
                    AdditionalPresetSlotsFeature.MaximumSupportedSlots)));
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

        UtilityOverlay.Configure(showTimer, showDamage, showDamageTaken, damagePanelScale);
        JournalSearch.Configure(journalSearch);
        JustAnvilFeature.Configure(guaranteedAnvil, Logger);
        _utility = new UtilityOverlay();
        _runSummary = new RunSummaryOverlay(
            runSummaryEnabled, runSummaryScale, runSummaryHistoryHotkey, runSummaryHistoryLimit, _utility);
        _fastShopReroll = new FastShopRerollFeature(
            fastShopRerollEnabled, fastShopRerollHotkey, fastShopRerollInterval, Logger);
        _partyReadiness = new PartyReadinessOverlay(
            partyReadinessEnabled, partyReadinessHotkey, partyReadinessScale);
        _hiddenRoomGuidance = new HiddenRoomGuidanceOverlay(
            hiddenRoomGuidanceEnabled, hiddenRoomGuidanceScale);
        _partyVote = new PartyVoteOverlay(
            partyVotingEnabled, partyVotingHotkey, partyVotingScale, Logger);
        _leafTransfer = new LeafTransferOverlay(
            leafTransferEnabled, leafTransferHotkey, maximumLeafTransfer, leafTransferScale, Logger);
        _tabletOptimizer = new TabletOptimizerOverlay(
            showTabletOptimizer, tabletOptimizerHotkey, tabletOptimizerPasses, allowTabletRotation,
            preferConditionalSynergies, tabletOptimizerScale, Logger);
        _spectator = new SpectatorFeature(
            spectatorEnabled, spectatorPreviousHotkey, spectatorNextHotkey, spectatorPanelScale);
        ConditionalSynergyScoring.Configure(preferConditionalSynergies);
        MaxPlayerFeature.Configure(maxPlayerEnabled, maxPlayers, compactMultiplayerHud, Logger);
        PartyScalingFeature.Configure(partyScalingEnabled, enemyHealthMultiplier, enemySpawnMultiplier, Logger);
        ExtendedLevelingFeature.Configure(extendedLevelingEnabled, maximumLevel, Logger);
        BossEntryAnnouncer.Configure(bossAnnouncerEnabled, Logger);
        AdditionalPresetSlotsFeature.Configure(additionalPresetSlotsEnabled, presetSlotLimit);
        HoldToCastFeature.Configure(holdToCastEnabled, Logger);
        _controlCenter = new QoLControlCenter(
            showControlCenter, controlCenterHotkey, controlCenterScale,
            showTimer, showDamage, showDamageTaken, damagePanelScale, journalSearch, guaranteedAnvil,
            showTabletOptimizer, tabletOptimizerHotkey, allowTabletRotation, preferConditionalSynergies, tabletOptimizerScale,
            spectatorEnabled, spectatorPreviousHotkey, spectatorNextHotkey, spectatorPanelScale,
            maxPlayerEnabled, maxPlayers, compactMultiplayerHud,
            partyScalingEnabled, enemyHealthMultiplier, enemySpawnMultiplier,
            extendedLevelingEnabled, maximumLevel, runSummaryEnabled, runSummaryScale, runSummaryHistoryHotkey,
            bossAnnouncerEnabled, fastShopRerollEnabled, fastShopRerollInterval, fastShopRerollHotkey,
            holdToCastEnabled,
            partyReadinessEnabled, partyReadinessHotkey, partyReadinessScale,
            hiddenRoomGuidanceEnabled, hiddenRoomGuidanceScale,
            partyVotingEnabled, partyVotingHotkey, partyVotingScale,
            leafTransferEnabled, leafTransferHotkey, leafTransferScale,
            additionalPresetSlotsEnabled, presetSlotLimit);

        _harmony = new Harmony(PluginGuid);
        _harmony.PatchAll();
        Logger.LogInfo($"Loaded {PluginName} {PluginVersion} with the current feature catalog, including hold-to-cast and configurable shortcuts.");
    }

    private void Update()
    {
        _utility?.Update();
        _runSummary?.Update();
        _fastShopReroll?.Update();
        _partyReadiness?.Update();
        _hiddenRoomGuidance?.Update();
        _partyVote?.Update();
        _leafTransfer?.Update();
        _tabletOptimizer?.Update();
        _spectator?.Update();
        _controlCenter?.Update();
        HoldToCastFeature.Update();
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
        _runSummary?.OnGUI();
        _partyReadiness?.OnGUI();
        _hiddenRoomGuidance?.OnGUI();
        _partyVote?.OnGUI();
        _leafTransfer?.OnGUI();
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
        _runSummary?.Dispose();
        _hiddenRoomGuidance?.Dispose();
        _partyVote?.Dispose();
        _utility = null;
        _runSummary = null;
        _fastShopReroll = null;
        _partyReadiness = null;
        _hiddenRoomGuidance = null;
        _partyVote = null;
        _leafTransfer = null;
        _tabletOptimizer = null;
        _spectator = null;
        _controlCenter = null;
    }
}
