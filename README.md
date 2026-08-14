# Sephiria QoL

Clean BepInEx implementations of quality-of-life features for Sephiria.

Current scope:

- Run timer and color-coded multiplayer damage contribution display
- Click-through player damage details: run/area totals, DPS, damage taken, HP,
  elemental mix, and top damage sources
- Guaranteed first-choice anvil room
- Native `AddOns` compatibility alongside BepInEx
- Journal keyword search
- In-game tablet/inventory optimizer panel (F10)
- Multiplayer spectator camera after death

The damage chart and tablet optimizer have independent `− / AUTO / +` scale
controls in their headers. Click the percentage to restore automatic sizing. Auto
mode uses normal sizing on Windows and scales up on high-resolution macOS/Retina
render surfaces. Manual values persist in the BepInEx configuration.

The optimizer exposes Sephiria's own server-authoritative layout routine. It scores
active charm levels and tablet bonuses, then adds condition-aware scoring for
positional dependencies. It understands Needle chains, left/right Grimoire supports,
Auto Magic, adjacent-level damage, same-column Grimoire fireworks, adjacent Planet
enhancement, same-row companions, White Paper category matching, and Wooden Box's
top-row bonus. Sephiria's own activation pass handles its reusable top/bottom/edge/
inside/neighbor criteria for every candidate layout. It rearranges only the requesting
player's inventory and can rotate tablets. Start with one pass; additional passes
search more layouts but can briefly pause the game.

The complete Sephiria 1.0.27 conditional inventory and coverage notes are in
[`docs/CONDITIONALS.md`](docs/CONDITIONALS.md).

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

## Who needs each mod?

| Feature | Host | Other players | Why |
| --- | --- | --- | --- |
| Run timer, damage chart/details, journal search | Not required | Only the player who wants the UI | These read synchronized game state and draw locally. |
| Tablet optimizer | Not required | Only the player using it | It calls Sephiria's built-in server-authoritative request for that player's inventory. |
| Spectator camera | Not required | Only the player who wants to spectate after dying | Camera selection and controls are local. |
| JustAnvil | Required to change the shared run | Not required | Remote clients are explicitly skipped; the host owns the floor graph. |
| Native add-on bootstrap | Not required | Only machines loading native `AddOns` | This only repairs local add-on startup ordering. |
| MaxPlayer_16 | Required for rooms above the vanilla limit | Optional | The host patches Steam/Mirror slot limits. Client installation adds the expanded party-list UI and also lets that player host larger rooms. |

There are no QoL features that must be installed by every player. For the most
consistent UI in a 5–16 player room, install MaxPlayer_16 on everyone, but only the
host is required for the larger capacity.

## Install and run on Windows

1. Close Sephiria.
2. Download BepInEx 5.4.23.5 for Windows x64 from the official BepInEx release
   and extract it into the Sephiria game directory, alongside `Sephiria.exe`.
3. Start the game once and close it after `BepInEx/LogOutput.log` is created.
4. Build this project or obtain a trusted `SephiriaQoL.dll`, then copy it to
   `Sephiria/BepInEx/plugins/`.
5. Start Sephiria normally through Steam.
6. Confirm `Loading [Sephiria QoL` and, when applicable, `Loaded 1 native
   AddOn(s)` appear in `BepInEx/LogOutput.log`.

### MaxPlayer_16 on Windows

1. Download `MaxPlayer_16.zip` from
   [Sephiria-Mods-By-KimJangee](https://github.com/TaeHyun015/Sephiria-Mods-By-KimJangee/).
2. Extract the archive as `Sephiria/AddOns/MaxPlayer_16/`. The resulting folder
   must directly contain `metadata.json`, `HarmonyBase.dll`, `SetMaxPlayer.dll`,
   and `Libs/`—not another nested `MaxPlayer_16` folder.
3. Launch the game and look for `[AddOnLoader] ... MaxPlayer_16` in the log.

Do not put MaxPlayer's DLLs in `BepInEx/plugins`. It is a separately licensed
native Sephiria add-on and is intentionally not distributed by this repository.
The upstream standalone `Spectator.dll` is a MelonLoader mod; this project provides
an independent BepInEx spectator implementation, so do not install that DLL or a
second mod loader.

The tablet optimizer is controlled with `F10`. It changes only the requesting
player's inventory, but it rearranges the whole inventory rather than tablets
alone. Keep `Prefer positional relic synergies` enabled to make productive
damage and support links outrank raw levels on unrelated artifacts. Effects whose
best side depends on the player's chosen element (such as Fire/Ice conversion relics)
remain neutral rather than guessing the build. Start with one pass and wait for the
inventory to settle before clicking again.

Configuration is stored in
`BepInEx/config/dev.tempeste.sephiria.qol.cfg`. Delete only that configuration
file to restore defaults; it will be recreated on the next launch.

## macOS

Sephiria ships a macOS Mono build, and this plugin is platform-neutral managed C#.
The loader is the limiting part: BepInEx 5.4.23.5's macOS build is x64, so Apple
Silicon must launch Sephiria's x64 slice through Rosetta. Native ARM loading is not
supported by that package. Sephiria 1.0.27 also uses Unity 6; verify the loader
first because a working `BepInEx/LogOutput.log` is the prerequisite for every mod.

1. Close Sephiria and back up saves.
2. Download the official BepInEx 5.4.23.5 `macos_x64` archive and extract it into
   `~/Library/Application Support/Steam/steamapps/common/Sephiria/`.
3. Set `executable_name="Sephiria.app"` in `run_bepinex.sh` and make the script
   executable with `chmod u+x run_bepinex.sh`.
4. On Apple Silicon, configure Steam/the launcher to use the x86_64 game slice
   through Rosetta, then launch once. Do not continue until the BepInEx log exists.
5. Clone this repository and run `./scripts/install-macos.sh`.
6. Launch through the same BepInEx/Rosetta path and verify the plugin load line.

The script defaults to Steam's usual macOS library location. Pass a different
Sephiria game directory as its first argument when needed. It verifies that the
game's managed assemblies and BepInEx are present before building or copying
anything. `dotnet` SDK 8 or newer is required to build the plugin.

Utility, JustAnvil, journal search, and the tablet optimizer are expected to be
portable. The built-in spectator feature is also managed and client-side.

### MaxPlayer_16 on macOS

After the base QoL plugin works, extract `MaxPlayer_16.zip` to
`Sephiria/Sephiria.app/AddOns/MaxPlayer_16/`. Unlike Windows, Sephiria resolves the
macOS built-in `AddOns` directory inside the app bundle. The resulting folder must
directly contain `metadata.json`, `HarmonyBase.dll`, `SetMaxPlayer.dll`, and `Libs/`.

Its assemblies are managed, but the package is third-party and should be tested
separately. A successful load produces the `MaxPlayer_16` and `AddOnLoader`
messages in the Unity player log. If it fails, remove only that
`Sephiria.app/AddOns/MaxPlayer_16` folder; the QoL plugin remains independent.

## Troubleshooting

- No `BepInEx/LogOutput.log`: BepInEx itself did not initialize; fix the loader
  before debugging this plugin.
- Plugin is absent from the log: confirm the DLL is directly under
  `BepInEx/plugins` and that only the BepInEx build is installed.
- Overlays are missing during a cinematic: full-screen game UI can cover IMGUI;
  check again after normal control resumes.
- Overlay is too small or large: use its `− / percentage / +` header controls, or
  edit `DamagePanelScale` / `PanelScale` in the QoL config. `0` restores auto mode.
- Build references are missing: set `SEPHIRIA_GAME_DIR`, or set
  `SEPHIRIA_MANAGED_DIR` directly to the game's `Managed` directory.
- Behavior changes after a Sephiria update: compare the affected runtime member
  names/signatures before changing Harmony patches.

## Development

Read [`AGENTS.md`](AGENTS.md) before modifying hooks, add-on bootstrapping, or
deployment behavior. It contains the architecture map, repository boundaries,
validation checklist, and release workflow intended for both humans and coding
agents.
