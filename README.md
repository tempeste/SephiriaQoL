# Sephiria QoL

Clean BepInEx implementations of quality-of-life features for Sephiria.

Current scope:

- Run timer and multiplayer damage contribution display
- Guaranteed first-choice anvil room
- Native `AddOns` compatibility alongside BepInEx
- Journal keyword search
- In-game tablet/inventory optimizer panel (F10)

The optimizer exposes Sephiria's own server-authoritative layout routine. It scores
active charm levels and tablet bonuses, then adds damage-aware scoring for positional
dependencies. For example, a Needle of the North is rewarded only when its upward
chain terminates at an artifact that actually deals direct damage. It rearranges only
the requesting player's inventory and can rotate tablets. Start with one pass;
additional passes search more layouts but can briefly pause the game.

This repository intentionally excludes private gameplay-altering projects,
progression/save manipulation, third-party mod binaries, and decompiler output.
Implementations here are maintained independently against the game's runtime APIs
and observed behavior.

## Prerequisites

- A legal Steam installation of Sephiria using the Mono scripting backend
- BepInEx 5 for Unity Mono initialized in the Sephiria game directory
- .NET SDK 8 or newer when building from source
- A game version compatible with the referenced runtime API (last validated on
  Sephiria 1.0.27)

MelonLoader is not used or required.

## Building

The project references assemblies from the local Sephiria installation. By default it
uses `E:\SteamLibrary\steamapps\common\Sephiria`. Override that path with the
`SEPHIRIA_GAME_DIR` environment variable if needed.

```powershell
dotnet build .\src\SephiriaQoL\SephiriaQoL.csproj -c Release
```

Copy `SephiriaQoL.dll` to `BepInEx\plugins`.

## Install and run on Windows

1. Close Sephiria.
2. Build the project or obtain a trusted build.
3. Copy `SephiriaQoL.dll` to
   `E:\SteamLibrary\steamapps\common\Sephiria\BepInEx\plugins\` (adjust for
   your Steam library).
4. Keep native third-party add-ons such as MaxPlayer in Sephiria's `AddOns`
   directory; do not copy their DLLs into this repository.
5. Start Sephiria normally through Steam.
6. Confirm `Loading [Sephiria QoL` and, when applicable, `Loaded 1 native
   AddOn(s)` appear in `BepInEx/LogOutput.log`.

The tablet optimizer is controlled with `F10`. It changes only the requesting
player's inventory, but it rearranges the whole inventory rather than tablets
alone. Keep `Prefer damage synergies` enabled to make productive Needle-to-damage
links outrank raw levels on unrelated artifacts. Start with one pass and wait for
the inventory to settle before clicking again.

Configuration is stored in
`BepInEx/config/dev.tempeste.sephiria.qol.cfg`. Delete only that configuration
file to restore defaults; it will be recreated on the next launch.

## macOS

Sephiria ships a macOS Mono build, and this plugin is platform-neutral managed C#.
The loader is the limiting part: BepInEx 5's documented macOS Mono build is x64,
so an Apple Silicon Mac will normally need Sephiria's x64 slice launched through
Rosetta. Native ARM loading is not yet verified.

After BepInEx has successfully generated `BepInEx/LogOutput.log`, clone this
repository and run:

```bash
./scripts/install-macos.sh
```

The script defaults to Steam's usual macOS library location. Pass a different
Sephiria game directory as its first argument when needed. It verifies that the
game's managed assemblies and BepInEx are present before building or copying
anything. `dotnet` SDK 8 or newer is required to build the plugin.

Utility, JustAnvil, journal search, and the tablet optimizer are expected to be
portable. MaxPlayer_16 consists of managed assemblies but remains unverified on
macOS; install and test it separately after the base QoL plugin works.

## Troubleshooting

- No `BepInEx/LogOutput.log`: BepInEx itself did not initialize; fix the loader
  before debugging this plugin.
- Plugin is absent from the log: confirm the DLL is directly under
  `BepInEx/plugins` and that only the BepInEx build is installed.
- Overlays are missing during a cinematic: full-screen game UI can cover IMGUI;
  check again after normal control resumes.
- Build references are missing: set `SEPHIRIA_GAME_DIR`, or set
  `SEPHIRIA_MANAGED_DIR` directly to the game's `Managed` directory.
- Behavior changes after a Sephiria update: compare the affected runtime member
  names/signatures before changing Harmony patches.

## Development

Read [`AGENTS.md`](AGENTS.md) before modifying hooks, add-on bootstrapping, or
deployment behavior. It contains the architecture map, repository boundaries,
validation checklist, and release workflow intended for both humans and coding
agents.
