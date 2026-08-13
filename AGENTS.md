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

- Private gameplay-altering mod source or configuration
- Game assemblies, BepInEx binaries, downloaded mod DLLs, or archives
- Build output (`bin`/`obj`), logs, saves, Steam data, or generated configuration
- Memory-editor tables, memory dumps, decompiler output, tokens, or credentials
- Source/assets copied from third-party mods

The repository should contain only independent source, documentation, scripts,
and build metadata. Before every push, inspect `git status`, `git diff`, and
`git ls-files`.

## Source map

- `Plugin.cs`: BepInEx entry point, configuration binding, lifecycle, and delayed
  native add-on bootstrap trigger.
- `UtilityOverlay.cs`: run timer and multiplayer damage-contribution display.
- `JournalSearch.cs`: artifact journal text filtering and related Harmony patch.
- `JustAnvilFeature.cs`: guarantees an anvil in the first playable room choice
  while preserving the displaced room later in the floor graph.
- `TabletOptimizerOverlay.cs`: F10 UI around Sephiria's own server-authoritative
  inventory auto-arranger. It does not implement a parallel item-mutation path.
- `ConditionalSynergyScoring.cs`: registry that extends the native candidate score
  for validated positional dependencies. It currently understands chained Needles
  of the North, Glowing Hourglass, and Ray's Star Fragment using runtime types,
  offsets, current levels, and prefab-configured percentage arrays.
- `NativeAddOnBootstrap.cs`: asks Sephiria's built-in `AddOnLoader` to load native
  `AddOns` when startup ordering caused it to miss them.
- `Directory.Build.props`: resolves Windows/macOS game and managed-assembly paths.
- `scripts/install-macos.sh`: validates, builds, and installs on macOS after
  BepInEx itself is working.

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
- Do not package MaxPlayer or other third-party binaries. The compatibility layer
  may load them from the user's `AddOns` directory, but they remain separately
  sourced and licensed.
- Keep all user-facing toggles in BepInEx configuration and document new keys.

## Build and validation

Required environment:

- .NET SDK 8+
- Sephiria's Mono `Managed` assemblies
- BepInEx 5 core assemblies

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
Treat MaxPlayer and native ARM loading as unverified until tested on actual Apple
Silicon hardware.

## Release workflow

1. Update `PluginVersion` for behavior changes.
2. Update README/config documentation when user-visible behavior changes.
3. Build with zero warnings and errors.
4. Confirm no ignored, generated, proprietary, or private files are staged.
5. Smoke-test locally when runtime behavior changed.
6. Commit intentionally and push `main` to the private GitHub remote.
