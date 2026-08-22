# Agent guidance

## Project purpose

This repository contains independently maintained quality-of-life
features for Sephiria. It targets BepInEx 5 on the game's Unity Mono build.
MelonLoader is intentionally not part of the architecture.

The last validated environment is Sephiria 1.0.27 with BepInEx 5.4.23.5 on
Windows. Treat game updates as compatibility events: inspect changed types and
signatures before altering a patch.

## Repository boundaries

Never commit:

- Unrelated mod source or configuration
- Game assemblies, BepInEx binaries, downloaded mod DLLs, or archives
- Build output (`bin`/`obj`), logs, saves, Steam data, or generated configuration
- Runtime inspection tables, memory dumps, decompiler output, tokens, or credentials
- Source/assets copied from third-party mods

The repository should contain only independent source, documentation, scripts,
and build metadata. Before every push, inspect `git status`, `git diff`, and
`git ls-files`.

## Source map

- `Plugin.cs`: BepInEx entry point, configuration binding, lifecycle, and delayed
  native add-on bootstrap trigger.
- `UtilityOverlay.cs`: run timer and multiplayer combat-contribution display,
  including live dealt and damage-taken run totals.
- `RunSummaryOverlay.cs` and `RunSummaryHistoryStore.cs`: capture the combat
  tracker at game over, persist recent local summaries, and compare player output.
- `BossEntryAnnouncer.cs`: host-only miniboss/boss-start message identifying the
  player who triggered the encounter through Sephiria's normal custom messages.
- `FastShopRerollFeature.cs`: rate-limited held hotkey around Sephiria's existing
  replenishment action; it does not replace shop cost or inventory handling.
- `HoldToCastFeature.cs`: client-side held input for spell/active-artifact slots and
  Cast Mode left-click. It waits for native cooldown readiness before calling the
  existing integrated action controller.
- `PartyReadinessOverlay.cs`: client-side dashboard over Sephiria's synchronized
  loading, menu, combat, ready, and floor state.
- `HiddenRoomGuidanceOverlay.cs`: cached client-side tracking of generated hidden
  entrances with a scalable nearest-entrance marker.
- `PartyVoteOverlay.cs`: explicit two-byte room/loot votes; the host keeps a
  display-only tally and never changes the run automatically.
- `LeafTransferOverlay.cs`: confirmed UI around Sephiria's native currency transfer
  with host-side identity, balance, amount, floor, life-state, and rate validation.
- `AdditionalPresetSlotsFeature.cs`: raises the native dynamic preset limit while
  preserving Sephiria's indexed storage and all existing slots.
- `QoLControlCenter.cs`: F11/tabbed configuration UI for everyday, multiplayer,
  run-tool, Party Scaling, and independent overlay sizing controls.
- `JournalSearch.cs`: artifact journal text filtering and related Harmony patch.
- `JustAnvilFeature.cs`: guarantees an anvil in the first playable room choice
  while preserving the displaced room later in the floor graph.
- `TabletOptimizerOverlay.cs`: F10 UI around Sephiria's own server-authoritative
  inventory auto-arranger. It does not implement a parallel item-mutation path.
- `ConditionalSynergyScoring.cs`: registry that extends the native candidate score
  for validated artifact-specific positional dependencies. It covers chained
  Needles, Grimoire supports, Auto Magic, adjacent-level damage, same-column
  Grimoire fireworks, adjacent Planets, same-row companions, White Paper, and
  Wooden Box. The native refresh/scorer remains responsible for tablet queries and
  reusable `CharmActivateCriteria` components.
- `NativeAddOnBootstrap.cs`: asks Sephiria's built-in `AddOnLoader` to load native
  `AddOns` when startup ordering caused it to miss them.
- `PartyScalingFeature.cs`: host-only scaling of generated normal-enemy counts and
  regular, random-phase, and boss enemy health after Sephiria applies its own
  difficulty and multiplayer bonuses. Minibosses, bosses, and training targets
  are never duplicated.
- `ExtendedLevelingFeature.cs`: expands Sephiria's cumulative run-XP table for
  high-density Party Scaling runs while preserving every standard threshold and
  the game's server-authoritative level-up path.
- `MaxPlayerFeature.cs`: clean-room 2–16 player support. It expands Sephiria's
  lobby selector, raises the host's Mirror connection cap, and compacts the native
  multiplayer HUD without depending on a third-party add-on.
- `docs/CONDITIONALS.md`: audited runtime inventory of reusable criteria,
  artifact-specific positional effects, neutral build-dependent effects, and the
  boundary between native tablet scoring and the QoL score postfix.
- `Directory.Build.props`: resolves Windows/macOS game and managed-assembly paths.
- `scripts/install-windows.ps1` and `scripts/install-macos.sh`: bootstrap a
  user-scoped .NET 8 SDK and BepInEx when needed, then build and install the
  plugin from either a checkout or a one-line remote invocation.

## Design constraints

- Prefer public game APIs and narrow Harmony patches over raw memory hooks.
- Preserve server authority for inventory changes. The optimizer must call
  `RequestAutoArrangeInventoryForBestCharmLevels`; do not force-remove/re-add
  items or directly synthesize inventory entries.
- Keep conditional awareness in the score postfix rather than adding a second arranger.
  Positional dependency chains must reject empty cells, cycles, and non-attackable
  terminal artifacts.
- Avoid per-frame scene-wide searches. Cache references and poll infrequently to
  prevent the stutter seen in earlier experiments.
- Native add-on loading must remain idempotent. Check `AddOnLoader.LoadedMods`
  before calling `LoadAll`.
- Fast shop reroll must call `UI_ShopPanel.DoReplenishment`; do not duplicate or
  bypass its Sapphire, availability, or inventory logic.
- Hold-to-cast must use the player's current Input System actions and the native
  integrated action controller. Never hard-code 1–8 bindings or send retries while
  a slot is unavailable.
- Boss-entry announcements must remain host-only and use Sephiria's targeted
  custom-message path so unmodified clients can receive them.
- Voting must remain explicit and advisory. Validate its compact Mirror message on
  the host, and never select a room or reward from a tally.
- Leaf transfers must use `UnitAvatar.GiveMoney`; validate both the host's direct
  path and its generated client command before Sephiria changes synchronized money.
- Additional presets must extend `UI_PresetPanel.GetSlotLimitCount`; do not replace
  or rewrite profile serialization.
- Party Scaling must remain host-authoritative, disabled by default, and applied
  only to newly generated/spawned enemies. Preserve the normal-enemy phase cap and
  do not duplicate minibosses, bosses, or training targets.
- Extended leveling must preserve Sephiria's standard XP thresholds and normal
  `AddExp`, `LocalAddExp`, reward, healing, and synchronization paths. Do not
  replace progression methods or write to save/profile data.
- Do not package the third-party MaxPlayer add-on or other third-party binaries.
  The compatibility layer may load separately installed `AddOns`, but the built-in
  `MaxPlayerFeature` must remain an independent implementation.
- Keep all user-facing toggles in BepInEx configuration and document new keys.

## Build and validation

Required environment:

- .NET SDK 8+
- Sephiria's Mono `Managed` assemblies
- BepInEx 5 core assemblies

The installer scripts bootstrap the SDK and BepInEx when missing. These remain
manual prerequisites only for direct builds that do not use an installer.

Windows defaults to `E:/SteamLibrary/steamapps/common/Sephiria`. Override it with
`SEPHIRIA_GAME_DIR`; override only the assembly directory with
`SEPHIRIA_MANAGED_DIR`.

Run before committing:

```powershell
dotnet build .\src\SephiriaQoL\SephiriaQoL.csproj -c Release
git diff --check
```

For runtime validation:

1. Close Sephiria before replacing the DLL.
2. Copy the release DLL into `BepInEx/plugins`.
3. Launch through Steam.
4. Verify both the plugin load line and absence of plugin exceptions in
   `BepInEx/LogOutput.log` and the Unity `Player.log`.
5. Test the affected feature in a normal run. Do not invoke inventory optimization
   without a real inventory, and use one pass first.

## macOS workflow

Validate BepInEx/Rosetta independently before debugging plugin code. Build against
the Mac installation's own managed assemblies, then use `scripts/install-macos.sh`.
Treat the built-in 16-player feature and native ARM loading as unverified until
tested with a real large lobby on actual Apple Silicon hardware.

## Release workflow

1. Update `PluginVersion` for behavior changes.
2. Update README/config documentation when user-visible behavior changes.
3. Build with zero warnings and errors.
4. Confirm no ignored, generated, proprietary, or unrelated files are staged.
5. Smoke-test locally when runtime behavior changed.
6. Commit intentionally and push `main` to the configured GitHub remote.
