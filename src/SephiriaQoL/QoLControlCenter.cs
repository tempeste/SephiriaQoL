using BepInEx.Configuration;
using UnityEngine;

namespace SephiriaQoL;

internal sealed class QoLControlCenter
{
    private enum Page
    {
        General,
        Multiplayer,
        Interface
    }

    private readonly ConfigEntry<bool> _visible;
    private readonly ConfigEntry<KeyboardShortcut> _hotkey;
    private readonly ConfigEntry<float> _panelScale;
    private readonly ConfigEntry<bool> _showTimer;
    private readonly ConfigEntry<bool> _showDamage;
    private readonly ConfigEntry<float> _damageScale;
    private readonly ConfigEntry<bool> _journalSearch;
    private readonly ConfigEntry<bool> _guaranteedAnvil;
    private readonly ConfigEntry<bool> _showTabletOptimizer;
    private readonly ConfigEntry<bool> _allowTabletRotation;
    private readonly ConfigEntry<bool> _preferConditionalSynergies;
    private readonly ConfigEntry<float> _tabletScale;
    private readonly ConfigEntry<bool> _spectatorEnabled;
    private readonly ConfigEntry<float> _spectatorScale;
    private readonly ConfigEntry<bool> _maxPlayerEnabled;
    private readonly ConfigEntry<int> _maxPlayers;
    private readonly ConfigEntry<bool> _compactMultiplayerHud;
    private readonly ConfigEntry<bool> _partyScalingEnabled;
    private readonly ConfigEntry<float> _enemyHealthMultiplier;
    private readonly ConfigEntry<float> _enemySpawnMultiplier;
    private readonly ConfigEntry<bool> _extendedLevelingEnabled;
    private readonly ConfigEntry<int> _maximumLevel;

    private Rect _windowRect = new Rect(90f, 76f, 520f, 650f);
    private Page _page;

    internal QoLControlCenter(
        ConfigEntry<bool> visible,
        ConfigEntry<KeyboardShortcut> hotkey,
        ConfigEntry<float> panelScale,
        ConfigEntry<bool> showTimer,
        ConfigEntry<bool> showDamage,
        ConfigEntry<float> damageScale,
        ConfigEntry<bool> journalSearch,
        ConfigEntry<bool> guaranteedAnvil,
        ConfigEntry<bool> showTabletOptimizer,
        ConfigEntry<bool> allowTabletRotation,
        ConfigEntry<bool> preferConditionalSynergies,
        ConfigEntry<float> tabletScale,
        ConfigEntry<bool> spectatorEnabled,
        ConfigEntry<float> spectatorScale,
        ConfigEntry<bool> maxPlayerEnabled,
        ConfigEntry<int> maxPlayers,
        ConfigEntry<bool> compactMultiplayerHud,
        ConfigEntry<bool> partyScalingEnabled,
        ConfigEntry<float> enemyHealthMultiplier,
        ConfigEntry<float> enemySpawnMultiplier,
        ConfigEntry<bool> extendedLevelingEnabled,
        ConfigEntry<int> maximumLevel)
    {
        _visible = visible;
        _hotkey = hotkey;
        _panelScale = panelScale;
        _showTimer = showTimer;
        _showDamage = showDamage;
        _damageScale = damageScale;
        _journalSearch = journalSearch;
        _guaranteedAnvil = guaranteedAnvil;
        _showTabletOptimizer = showTabletOptimizer;
        _allowTabletRotation = allowTabletRotation;
        _preferConditionalSynergies = preferConditionalSynergies;
        _tabletScale = tabletScale;
        _spectatorEnabled = spectatorEnabled;
        _spectatorScale = spectatorScale;
        _maxPlayerEnabled = maxPlayerEnabled;
        _maxPlayers = maxPlayers;
        _compactMultiplayerHud = compactMultiplayerHud;
        _partyScalingEnabled = partyScalingEnabled;
        _enemyHealthMultiplier = enemyHealthMultiplier;
        _enemySpawnMultiplier = enemySpawnMultiplier;
        _extendedLevelingEnabled = extendedLevelingEnabled;
        _maximumLevel = maximumLevel;
    }

    internal void Update()
    {
        if (_hotkey.Value.IsDown())
            _visible.Value = !_visible.Value;
    }

    internal void OnGUI()
    {
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
        DrawTab(new Rect(14f, 52f, 156f, 30f), Page.General, "GENERAL");
        DrawTab(new Rect(182f, 52f, 156f, 30f), Page.Multiplayer, "MULTIPLAYER");
        DrawTab(new Rect(350f, 52f, 156f, 30f), Page.Interface, "INTERFACE");
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
        y = DrawToggle(y, "Journal search", "Filter the artifact journal by keyword.", _journalSearch);
        y = DrawToggle(y, "First-choice anvil", "Guarantee one early anvil room on the host.", _guaranteedAnvil);
        y = DrawToggle(y, "Tablet optimizer panel", "Show the inventory layout assistant.", _showTabletOptimizer);
        y = DrawToggle(y, "Allow tablet rotation", "Try rotated tablets while optimizing.", _allowTabletRotation);
        DrawToggle(y, "Prefer positional synergies", "Reward productive relic and tablet links.", _preferConditionalSynergies);

        GUI.Label(new Rect(16f, 615f, 488f, 22f),
            $"Press {_hotkey.Value.MainKey} anywhere to show or hide this panel.", OverlayGui.MutedStyle);
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
        GUI.Label(new Rect(16f, 94f, 488f, 24f), "PANEL SIZING", OverlayGui.TitleStyle);
        GUI.Label(new Rect(16f, 119f, 488f, 38f),
            "Each overlay keeps its own scale. AUTO detects high-resolution macOS displays; manual values work on every platform.",
            OverlayGui.MutedStyle);

        DrawScaleRow(174f, "Damage chart", _damageScale);
        DrawScaleRow(238f, "Tablet optimizer", _tabletScale);
        DrawScaleRow(302f, "Spectator panel", _spectatorScale);
        DrawScaleRow(366f, "Control center", _panelScale);

        OverlayGui.Fill(new Rect(16f, 440f, 488f, 54f), OverlayGui.PanelRaised);
        GUI.Label(new Rect(30f, 448f, 460f, 20f), "Sizing is saved immediately", OverlayGui.LabelStyle);
        GUI.Label(new Rect(30f, 469f, 460f, 18f),
            "Click the percentage to return that panel to automatic sizing.", OverlayGui.MutedStyle);
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
