using BepInEx.Configuration;
using UnityEngine;

namespace SephiriaQoL;

internal sealed partial class QoLControlCenter
{
    private enum Page
    {
        General,
        Multiplayer,
        RunTools,
        Interface
    }

    private readonly ConfigEntry<bool> _visible;
    private readonly ConfigEntry<KeyboardShortcut> _hotkey;
    private readonly ConfigEntry<float> _panelScale;
    private readonly ConfigEntry<bool> _showTimer;
    private readonly ConfigEntry<bool> _showDamage;
    private readonly ConfigEntry<bool> _showDamageTaken;
    private readonly ConfigEntry<float> _damageScale;
    private readonly ConfigEntry<bool> _journalSearch;
    private readonly ConfigEntry<bool> _guaranteedAnvil;
    private readonly ConfigEntry<bool> _showTabletOptimizer;
    private readonly ConfigEntry<KeyboardShortcut> _tabletOptimizerHotkey;
    private readonly ConfigEntry<bool> _allowTabletRotation;
    private readonly ConfigEntry<bool> _preferConditionalSynergies;
    private readonly ConfigEntry<float> _tabletScale;
    private readonly ConfigEntry<bool> _spectatorEnabled;
    private readonly ConfigEntry<KeyboardShortcut> _spectatorPreviousHotkey;
    private readonly ConfigEntry<KeyboardShortcut> _spectatorNextHotkey;
    private readonly ConfigEntry<float> _spectatorScale;
    private readonly ConfigEntry<bool> _maxPlayerEnabled;
    private readonly ConfigEntry<int> _maxPlayers;
    private readonly ConfigEntry<bool> _compactMultiplayerHud;
    private readonly ConfigEntry<bool> _partyScalingEnabled;
    private readonly ConfigEntry<float> _enemyHealthMultiplier;
    private readonly ConfigEntry<float> _enemySpawnMultiplier;
    private readonly ConfigEntry<bool> _extendedLevelingEnabled;
    private readonly ConfigEntry<int> _maximumLevel;
    private readonly ConfigEntry<bool> _runSummaryEnabled;
    private readonly ConfigEntry<float> _runSummaryScale;
    private readonly ConfigEntry<KeyboardShortcut> _runSummaryHistoryHotkey;
    private readonly ConfigEntry<bool> _bossAnnouncerEnabled;
    private readonly ConfigEntry<bool> _fastShopRerollEnabled;
    private readonly ConfigEntry<float> _fastShopRerollInterval;
    private readonly ConfigEntry<KeyboardShortcut> _fastShopRerollHotkey;
    private readonly ConfigEntry<bool> _holdToCastEnabled;
    private readonly ConfigEntry<bool> _partyReadinessEnabled;
    private readonly ConfigEntry<KeyboardShortcut> _partyReadinessHotkey;
    private readonly ConfigEntry<float> _partyReadinessScale;
    private readonly ConfigEntry<bool> _hiddenRoomGuidanceEnabled;
    private readonly ConfigEntry<float> _hiddenRoomGuidanceScale;
    private readonly ConfigEntry<bool> _partyVotingEnabled;
    private readonly ConfigEntry<KeyboardShortcut> _partyVotingHotkey;
    private readonly ConfigEntry<float> _partyVotingScale;
    private readonly ConfigEntry<bool> _leafTransferEnabled;
    private readonly ConfigEntry<KeyboardShortcut> _leafTransferHotkey;
    private readonly ConfigEntry<float> _leafTransferScale;
    private readonly ConfigEntry<bool> _additionalPresetSlotsEnabled;
    private readonly ConfigEntry<int> _presetSlotLimit;

    private Rect _windowRect = new Rect(90f, 76f, 520f, 650f);
    private Vector2 _runToolsScroll;
    private Vector2 _interfaceScroll;
    private ConfigEntry<KeyboardShortcut> _capturingHotkey;
    private Page _page;

    internal QoLControlCenter(
        ConfigEntry<bool> visible,
        ConfigEntry<KeyboardShortcut> hotkey,
        ConfigEntry<float> panelScale,
        ConfigEntry<bool> showTimer,
        ConfigEntry<bool> showDamage,
        ConfigEntry<bool> showDamageTaken,
        ConfigEntry<float> damageScale,
        ConfigEntry<bool> journalSearch,
        ConfigEntry<bool> guaranteedAnvil,
        ConfigEntry<bool> showTabletOptimizer,
        ConfigEntry<KeyboardShortcut> tabletOptimizerHotkey,
        ConfigEntry<bool> allowTabletRotation,
        ConfigEntry<bool> preferConditionalSynergies,
        ConfigEntry<float> tabletScale,
        ConfigEntry<bool> spectatorEnabled,
        ConfigEntry<KeyboardShortcut> spectatorPreviousHotkey,
        ConfigEntry<KeyboardShortcut> spectatorNextHotkey,
        ConfigEntry<float> spectatorScale,
        ConfigEntry<bool> maxPlayerEnabled,
        ConfigEntry<int> maxPlayers,
        ConfigEntry<bool> compactMultiplayerHud,
        ConfigEntry<bool> partyScalingEnabled,
        ConfigEntry<float> enemyHealthMultiplier,
        ConfigEntry<float> enemySpawnMultiplier,
        ConfigEntry<bool> extendedLevelingEnabled,
        ConfigEntry<int> maximumLevel,
        ConfigEntry<bool> runSummaryEnabled,
        ConfigEntry<float> runSummaryScale,
        ConfigEntry<KeyboardShortcut> runSummaryHistoryHotkey,
        ConfigEntry<bool> bossAnnouncerEnabled,
        ConfigEntry<bool> fastShopRerollEnabled,
        ConfigEntry<float> fastShopRerollInterval,
        ConfigEntry<KeyboardShortcut> fastShopRerollHotkey,
        ConfigEntry<bool> holdToCastEnabled,
        ConfigEntry<bool> partyReadinessEnabled,
        ConfigEntry<KeyboardShortcut> partyReadinessHotkey,
        ConfigEntry<float> partyReadinessScale,
        ConfigEntry<bool> hiddenRoomGuidanceEnabled,
        ConfigEntry<float> hiddenRoomGuidanceScale,
        ConfigEntry<bool> partyVotingEnabled,
        ConfigEntry<KeyboardShortcut> partyVotingHotkey,
        ConfigEntry<float> partyVotingScale,
        ConfigEntry<bool> leafTransferEnabled,
        ConfigEntry<KeyboardShortcut> leafTransferHotkey,
        ConfigEntry<float> leafTransferScale,
        ConfigEntry<bool> additionalPresetSlotsEnabled,
        ConfigEntry<int> presetSlotLimit)
    {
        _visible = visible;
        _hotkey = hotkey;
        _panelScale = panelScale;
        _showTimer = showTimer;
        _showDamage = showDamage;
        _showDamageTaken = showDamageTaken;
        _damageScale = damageScale;
        _journalSearch = journalSearch;
        _guaranteedAnvil = guaranteedAnvil;
        _showTabletOptimizer = showTabletOptimizer;
        _tabletOptimizerHotkey = tabletOptimizerHotkey;
        _allowTabletRotation = allowTabletRotation;
        _preferConditionalSynergies = preferConditionalSynergies;
        _tabletScale = tabletScale;
        _spectatorEnabled = spectatorEnabled;
        _spectatorPreviousHotkey = spectatorPreviousHotkey;
        _spectatorNextHotkey = spectatorNextHotkey;
        _spectatorScale = spectatorScale;
        _maxPlayerEnabled = maxPlayerEnabled;
        _maxPlayers = maxPlayers;
        _compactMultiplayerHud = compactMultiplayerHud;
        _partyScalingEnabled = partyScalingEnabled;
        _enemyHealthMultiplier = enemyHealthMultiplier;
        _enemySpawnMultiplier = enemySpawnMultiplier;
        _extendedLevelingEnabled = extendedLevelingEnabled;
        _maximumLevel = maximumLevel;
        _runSummaryEnabled = runSummaryEnabled;
        _runSummaryScale = runSummaryScale;
        _runSummaryHistoryHotkey = runSummaryHistoryHotkey;
        _bossAnnouncerEnabled = bossAnnouncerEnabled;
        _fastShopRerollEnabled = fastShopRerollEnabled;
        _fastShopRerollInterval = fastShopRerollInterval;
        _fastShopRerollHotkey = fastShopRerollHotkey;
        _holdToCastEnabled = holdToCastEnabled;
        _partyReadinessEnabled = partyReadinessEnabled;
        _partyReadinessHotkey = partyReadinessHotkey;
        _partyReadinessScale = partyReadinessScale;
        _hiddenRoomGuidanceEnabled = hiddenRoomGuidanceEnabled;
        _hiddenRoomGuidanceScale = hiddenRoomGuidanceScale;
        _partyVotingEnabled = partyVotingEnabled;
        _partyVotingHotkey = partyVotingHotkey;
        _partyVotingScale = partyVotingScale;
        _leafTransferEnabled = leafTransferEnabled;
        _leafTransferHotkey = leafTransferHotkey;
        _leafTransferScale = leafTransferScale;
        _additionalPresetSlotsEnabled = additionalPresetSlotsEnabled;
        _presetSlotLimit = presetSlotLimit;
    }

    internal void Update()
    {
        if (_hotkey.Value.IsDown())
            _visible.Value = !_visible.Value;
    }

    internal void OnGUI()
    {
        HandleHotkeyCapture();
        float scale = OverlayGui.ResolveScale(_panelScale);
        DrawLauncher(scale);
        if (!_visible.Value)
            return;

        _windowRect = OverlayGui.BeginScaledWindow(
            43141,
            _windowRect,
            520f,
            650f,
            scale,
            DrawWindow,
            out _);
    }

    private void DrawLauncher(float scale)
    {
        Matrix4x4 previousMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        Color previousColor = GUI.backgroundColor;
        GUI.backgroundColor = _visible.Value ? OverlayGui.Danger : OverlayGui.Accent;
        if (GUI.Button(new Rect(100f, 10f, 78f, 32f), _visible.Value ? "CLOSE" : "QOL", OverlayGui.ButtonStyle))
            _visible.Value = !_visible.Value;
        GUI.backgroundColor = previousColor;
        GUI.matrix = previousMatrix;
    }

    private void DrawWindow(int id)
    {
        DrawHeader();
        DrawNavigation();

        switch (_page)
        {
            case Page.Multiplayer:
                DrawMultiplayerPage();
                break;
            case Page.Interface:
                DrawInterfacePage();
                break;
            case Page.RunTools:
                DrawRunToolsPage();
                break;
            default:
                DrawGeneralPage();
                break;
        }

        GUI.DragWindow(new Rect(0f, 0f, 330f, 40f));
    }

    private void DrawHeader()
    {
        OverlayGui.Fill(new Rect(0f, 0f, 520f, 40f), OverlayGui.PanelRaised);
        OverlayGui.Fill(new Rect(0f, 39f, 520f, 1f), OverlayGui.Border);
        OverlayGui.Fill(new Rect(0f, 0f, 5f, 40f), OverlayGui.Accent);
        GUI.Label(new Rect(16f, 6f, 210f, 26f), "QOL CONTROL CENTER", OverlayGui.TitleStyle);
        OverlayGui.DrawScaleControls(_panelScale, 344f, 9f);
        if (GUI.Button(new Rect(482f, 9f, 25f, 22f), "×", OverlayGui.ButtonStyle))
            _visible.Value = false;
    }

    private void DrawNavigation()
    {
        DrawTab(new Rect(14f, 52f, 114f, 30f), Page.General, "GENERAL");
        DrawTab(new Rect(136f, 52f, 114f, 30f), Page.Multiplayer, "MULTIPLAYER");
        DrawTab(new Rect(258f, 52f, 114f, 30f), Page.RunTools, "RUN TOOLS");
        DrawTab(new Rect(380f, 52f, 126f, 30f), Page.Interface, "INTERFACE");
    }

    private void DrawTab(Rect rect, Page page, string label)
    {
        if (_page == page)
        {
            OverlayGui.Fill(rect, new Color(OverlayGui.Accent.r, OverlayGui.Accent.g, OverlayGui.Accent.b, 0.18f));
            OverlayGui.Fill(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), OverlayGui.Accent);
        }

        if (GUI.Button(rect, label, OverlayGui.ButtonStyle))
            _page = page;
    }

    private void DrawGeneralPage()
    {
        GUI.Label(new Rect(16f, 94f, 488f, 24f), "EVERYDAY FEATURES", OverlayGui.TitleStyle);
        float y = 124f;
        y = DrawToggle(y, "Run timer", "Keep elapsed run time visible.", _showTimer);
        y = DrawToggle(y, "Damage contribution", "Color chart with per-player breakdowns.", _showDamage);
        y = DrawToggle(y, "Damage taken tracker", "Show each player's run-total incoming damage.", _showDamageTaken);
        y = DrawToggle(y, "Journal search", "Filter the artifact journal by keyword.", _journalSearch);
        y = DrawToggle(y, "First-choice anvil", "Guarantee one early anvil room on the host.", _guaranteedAnvil);
        y = DrawToggle(y, "Tablet optimizer panel", "Show the inventory layout assistant.", _showTabletOptimizer);
        y = DrawToggle(y, "Allow tablet rotation", "Try rotated tablets while optimizing.", _allowTabletRotation);
        DrawToggle(y, "Prefer positional synergies", "Reward productive relic and tablet links.", _preferConditionalSynergies);

        GUI.Label(new Rect(16f, 615f, 488f, 22f),
            $"Press {FormatShortcut(_hotkey.Value)} anywhere to show or hide this panel.", OverlayGui.MutedStyle);
    }

    private void DrawMultiplayerPage()
    {
        bool hostActive = PartyScalingFeature.IsHostActive;
        Color statusColor = hostActive ? OverlayGui.Accent : new Color(0.98f, 0.68f, 0.22f, 1f);
        OverlayGui.Fill(new Rect(16f, 96f, 488f, 48f), OverlayGui.PanelRaised);
        OverlayGui.Fill(new Rect(16f, 96f, 5f, 48f), statusColor);
        GUI.Label(new Rect(30f, 101f, 460f, 20f),
            hostActive ? "HOST AUTHORITY ACTIVE" : "HOST SETTINGS READY", OverlayGui.TitleStyle);
        GUI.Label(new Rect(30f, 121f, 460f, 18f),
            hostActive ? "Server-side settings apply to newly spawned encounters." : "Settings take effect when this machine hosts a run.",
            OverlayGui.MutedStyle);

        float y = 154f;
        y = DrawToggle(y, "Spectator camera", "Follow living teammates after you are defeated.", _spectatorEnabled);
        y = DrawToggle(y, "Large-party rooms", "Allow this host to create rooms above four players.", _maxPlayerEnabled);

        DrawStepperLabel(y + 2f, "Maximum players", _maxPlayers.Value.ToString());
        if (GUI.Button(new Rect(400f, y + 4f, 28f, 24f), "−", OverlayGui.ButtonStyle))
            _maxPlayers.Value = Mathf.Max(2, _maxPlayers.Value - 1);
        if (GUI.Button(new Rect(462f, y + 4f, 28f, 24f), "+", OverlayGui.ButtonStyle))
            _maxPlayers.Value = Mathf.Min(16, _maxPlayers.Value + 1);
        y += 38f;
        y = DrawToggle(y, "Compact party roster", "Keep Sephiria's player list readable in large rooms.", _compactMultiplayerHud);

        GUI.Label(new Rect(16f, y + 5f, 488f, 24f), "PARTY SCALING", OverlayGui.TitleStyle);
        y += 34f;
        y = DrawToggle(y, "Enable Party Scaling", "Host-only challenge tuning; disabled by default.", _partyScalingEnabled);
        DrawMultiplier(y + 2f, "Enemy health", _enemyHealthMultiplier, 0.25f, 1f, 10f);
        y += 40f;
        DrawMultiplier(y + 2f, "Normal enemy count", _enemySpawnMultiplier, 0.25f, 1f, 4f);
        y += 40f;

        y = DrawToggle(y, "Extended leveling", "Keep earning levels in high-density runs.", _extendedLevelingEnabled);
        DrawStepperLabel(y + 2f, "Maximum run level", _maximumLevel.Value.ToString());
        if (GUI.Button(new Rect(400f, y + 2f, 28f, 24f), "−", OverlayGui.ButtonStyle))
            _maximumLevel.Value = Mathf.Max(
                ExtendedLevelingFeature.VanillaMaximumLevel, _maximumLevel.Value - 10);
        if (GUI.Button(new Rect(462f, y + 2f, 28f, 24f), "+", OverlayGui.ButtonStyle))
            _maximumLevel.Value = Mathf.Min(
                ExtendedLevelingFeature.MaximumSupportedLevel, _maximumLevel.Value + 10);

        GUI.Label(new Rect(16f, 615f, 488f, 22f),
            "Count scaling excludes minibosses, bosses, and training targets.", OverlayGui.MutedStyle);
    }

    private void DrawInterfacePage()
    {
        _interfaceScroll = GUI.BeginScrollView(
            new Rect(0f, 94f, 520f, 536f), _interfaceScroll, new Rect(0f, 0f, 520f, 1240f));
        GUI.Label(new Rect(16f, 0f, 488f, 24f), "PANEL SIZING", OverlayGui.TitleStyle);
        GUI.Label(new Rect(16f, 25f, 488f, 38f),
            "Each overlay keeps its own scale. AUTO detects high-resolution macOS displays; manual values work on every platform.",
            OverlayGui.MutedStyle);

        DrawScaleRow(80f, "Damage chart", _damageScale);
        DrawScaleRow(144f, "Tablet optimizer", _tabletScale);
        DrawScaleRow(208f, "Spectator panel", _spectatorScale);
        DrawScaleRow(272f, "Run summary", _runSummaryScale);
        DrawScaleRow(336f, "Party readiness", _partyReadinessScale);
        DrawScaleRow(400f, "Hidden-room marker", _hiddenRoomGuidanceScale);
        DrawScaleRow(464f, "Party voting", _partyVotingScale);
        DrawScaleRow(528f, "Leaf transfer", _leafTransferScale);
        DrawScaleRow(592f, "Control center", _panelScale);

        OverlayGui.Fill(new Rect(16f, 656f, 488f, 44f), OverlayGui.PanelRaised);
        GUI.Label(new Rect(30f, 662f, 460f, 18f), "Sizing is saved immediately", OverlayGui.LabelStyle);
        GUI.Label(new Rect(30f, 681f, 460f, 16f),
            "Click the percentage to return that panel to automatic sizing.", OverlayGui.MutedStyle);

        GUI.Label(new Rect(16f, 716f, 488f, 24f), "HOTKEYS", OverlayGui.TitleStyle);
        GUI.Label(new Rect(16f, 741f, 488f, 36f),
            "Click a binding, then press a key or modifier combination. Escape cancels; × unbinds it.",
            OverlayGui.MutedStyle);
        float hotkeyY = 784f;
        hotkeyY = DrawHotkeyRow(hotkeyY, "Control Center", _hotkey);
        hotkeyY = DrawHotkeyRow(hotkeyY, "Run summary history", _runSummaryHistoryHotkey);
        hotkeyY = DrawHotkeyRow(hotkeyY, "Fast shop reroll", _fastShopRerollHotkey);
        hotkeyY = DrawHotkeyRow(hotkeyY, "Party readiness", _partyReadinessHotkey);
        hotkeyY = DrawHotkeyRow(hotkeyY, "Party voting", _partyVotingHotkey);
        hotkeyY = DrawHotkeyRow(hotkeyY, "Leaf transfer", _leafTransferHotkey);
        hotkeyY = DrawHotkeyRow(hotkeyY, "Tablet optimizer", _tabletOptimizerHotkey);
        hotkeyY = DrawHotkeyRow(hotkeyY, "Spectator previous", _spectatorPreviousHotkey);
        DrawHotkeyRow(hotkeyY, "Spectator next", _spectatorNextHotkey);
        GUI.EndScrollView();
    }

    private void DrawRunToolsPage()
    {
        _runToolsScroll = GUI.BeginScrollView(
            new Rect(0f, 94f, 520f, 536f), _runToolsScroll, new Rect(0f, 0f, 520f, 750f));
        GUI.Label(new Rect(16f, 0f, 488f, 24f), "RUN TOOLS", OverlayGui.TitleStyle);
        GUI.Label(new Rect(16f, 25f, 488f, 38f),
            "Local conveniences stay local. Shared announcements are sent only by the host.",
            OverlayGui.MutedStyle);

        float y = 70f;
        y = DrawToggle(y, "Run summary and history", "Show at game over; F8 reopens recent local runs.", _runSummaryEnabled);
        y = DrawToggle(y, "Encounter announcer", "Host announces miniboss and boss triggers.", _bossAnnouncerEnabled);
        y = DrawToggle(y, "Fast shop reroll", $"Hold {FormatShortcut(_fastShopRerollHotkey.Value)} in a shop to repeat rerolls.", _fastShopRerollEnabled);
        y = DrawToggle(y, "Hold to cast", "Hold spell keys 1–8 or Cast Mode left-click through cooldowns.", _holdToCastEnabled);
        y = DrawToggle(y, "Party readiness", $"Press {FormatShortcut(_partyReadinessHotkey.Value)} for synchronized player states.", _partyReadinessEnabled);
        y = DrawToggle(y, "Hidden-room guidance", "Client-side marker for registered hidden entrances.", _hiddenRoomGuidanceEnabled);
        y = DrawToggle(y, "Party voting", $"Press {FormatShortcut(_partyVotingHotkey.Value)}; the host sees manual tallies.", _partyVotingEnabled);
        y = DrawToggle(y, "Leaf transfer", $"Press {FormatShortcut(_leafTransferHotkey.Value)}; disabled by default.", _leafTransferEnabled);
        y = DrawToggle(y, "Additional preset slots", "Use Sephiria's native indexed preset storage.", _additionalPresetSlotsEnabled);

        OverlayGui.Fill(new Rect(16f, y, 488f, 52f), OverlayGui.PanelRaised);
        DrawStepperLabel(y + 14f, "Maximum preset slots", _presetSlotLimit.Value.ToString());
        if (GUI.Button(new Rect(400f, y + 14f, 28f, 24f), "−", OverlayGui.ButtonStyle))
            _presetSlotLimit.Value = Mathf.Max(AdditionalPresetSlotsFeature.NativeSlotLimit, _presetSlotLimit.Value - 5);
        if (GUI.Button(new Rect(462f, y + 14f, 28f, 24f), "+", OverlayGui.ButtonStyle))
            _presetSlotLimit.Value = Mathf.Min(AdditionalPresetSlotsFeature.MaximumSupportedSlots, _presetSlotLimit.Value + 5);
        y += 62f;

        OverlayGui.Fill(new Rect(16f, y, 488f, 52f), OverlayGui.PanelRaised);
        DrawStepperLabel(y + 14f, "Held-reroll interval", $"{_fastShopRerollInterval.Value:0.00}s");
        if (GUI.Button(new Rect(400f, y + 14f, 28f, 24f), "−", OverlayGui.ButtonStyle))
            _fastShopRerollInterval.Value = Mathf.Round(
                Mathf.Clamp(_fastShopRerollInterval.Value - 0.05f, 0.15f, 1f) * 100f) / 100f;
        if (GUI.Button(new Rect(462f, y + 14f, 28f, 24f), "+", OverlayGui.ButtonStyle))
            _fastShopRerollInterval.Value = Mathf.Round(
                Mathf.Clamp(_fastShopRerollInterval.Value + 0.05f, 0.15f, 1f) * 100f) / 100f;

        OverlayGui.Fill(new Rect(16f, y + 62f, 488f, 72f), OverlayGui.PanelRaised);
        GUI.Label(new Rect(30f, y + 69f, 460f, 20f), "Shared actions remain explicit", OverlayGui.LabelStyle);
        GUI.Label(new Rect(30f, y + 91f, 460f, 36f),
            "Votes never auto-select. Transfers require confirmation and host validation. Rerolls keep native costs.",
            OverlayGui.MutedStyle);
        GUI.EndScrollView();
    }

    private static float DrawToggle(float y, string title, string description, ConfigEntry<bool> entry)
    {
        Rect row = new Rect(16f, y, 488f, 46f);
        OverlayGui.Fill(row, OverlayGui.PanelRaised);
        OverlayGui.Fill(new Rect(row.x, row.y, 4f, row.height), entry.Value ? OverlayGui.Accent : OverlayGui.Border);
        GUI.Label(new Rect(30f, y + 4f, 330f, 20f), title, OverlayGui.LabelStyle);
        GUI.Label(new Rect(30f, y + 23f, 360f, 18f), description, OverlayGui.MutedStyle);

        Color previousColor = GUI.backgroundColor;
        GUI.backgroundColor = entry.Value ? OverlayGui.Accent : OverlayGui.Border;
        if (GUI.Button(new Rect(424f, y + 10f, 64f, 26f), entry.Value ? "ON" : "OFF", OverlayGui.ButtonStyle))
            entry.Value = !entry.Value;
        GUI.backgroundColor = previousColor;
        return y + 52f;
    }

    private static void DrawStepperLabel(float y, string title, string value)
    {
        GUI.Label(new Rect(30f, y, 260f, 24f), title, OverlayGui.LabelStyle);
        GUI.Label(new Rect(350f, y, 44f, 24f), value, OverlayGui.RightStyle);
    }

    private static void DrawMultiplier(
        float y,
        string title,
        ConfigEntry<float> entry,
        float step,
        float minimum,
        float maximum)
    {
        DrawStepperLabel(y, title, $"{entry.Value:0.00}×");
        if (GUI.Button(new Rect(400f, y, 28f, 24f), "−", OverlayGui.ButtonStyle))
            entry.Value = Mathf.Round(Mathf.Clamp(entry.Value - step, minimum, maximum) * 100f) / 100f;
        if (GUI.Button(new Rect(462f, y, 28f, 24f), "+", OverlayGui.ButtonStyle))
            entry.Value = Mathf.Round(Mathf.Clamp(entry.Value + step, minimum, maximum) * 100f) / 100f;
    }

    private static void DrawScaleRow(float y, string title, ConfigEntry<float> scaleEntry)
    {
        OverlayGui.Fill(new Rect(16f, y, 488f, 52f), OverlayGui.PanelRaised);
        GUI.Label(new Rect(30f, y + 8f, 260f, 24f), title, OverlayGui.LabelStyle);
        OverlayGui.DrawScaleControls(scaleEntry, 362f, y + 14f);
    }

}
