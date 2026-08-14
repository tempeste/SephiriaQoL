# Sephiria QoL

Clean BepInEx implementations of quality-of-life features for Sephiria.

Current scope:

- Run timer and color-coded multiplayer dealt/damage-taken display
- Click-through player damage details: run/area totals, DPS, damage taken, HP,
  elemental mix, and top damage sources
- Guaranteed first-choice anvil room
- Native `AddOns` compatibility alongside BepInEx
- Journal keyword search
- In-game tablet/inventory optimizer panel (F10)
- Multiplayer spectator camera after death
- Configurable 2–16 player rooms with a compact large-party HUD
- A unified QoL Control Center for toggles and overlay sizing
- Optional host-side Party Scaling for enemy health and normal-enemy counts
- Extended run leveling for the additional XP available in high-density runs

Use the compact **QOL** button or press `F11` to open the Control Center. It groups
the everyday, multiplayer, and interface settings in one draggable panel. The
damage chart, tablet optimizer, spectator panel, and Control Center have independent
`− / AUTO / +` scale controls. Click a percentage to restore automatic sizing.
Auto mode uses normal sizing on Windows and scales up on high-resolution macOS/Retina
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

This repository intentionally excludes permanent profile/save editing,
third-party mod binaries, and decompiler output. Implementations here are
maintained independently against the game's runtime APIs and observed behavior.

## One-command setup

Friends do not need to hunt down individual mod files. Clone this repository and
run the installer for the current platform. It installs the pinned BepInEx release
when needed, then builds and installs the single `SephiriaQoL.dll` containing the
QoL, spectator, 16-player, Party Scaling, and extended-leveling features.

The installers verify the BepInEx download with a committed SHA-256 hash. This
repository does not copy or redistribute third-party mod binaries.

Prerequisites:

- A legal Steam installation of Sephiria using the Mono scripting backend
- Git and .NET SDK 8 or newer
- A game version compatible with the referenced runtime API (last validated on
  Sephiria 1.0.27)

MelonLoader is not used or required. The spectator feature is already part of
`SephiriaQoL.dll`; do not install the upstream MelonLoader `Spectator.dll`.

### Windows

Close Sephiria, then run in PowerShell:

```powershell
git clone https://github.com/tempeste/SephiriaQoL.git
cd SephiriaQoL
powershell -ExecutionPolicy Bypass -File .\scripts\install-windows.ps1
```

The script detects the normal Steam location plus common `D:` and `E:` Steam
libraries. For another location:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\install-windows.ps1 `
  -GameDir "F:\SteamLibrary\steamapps\common\Sephiria"
```

Launch Sephiria normally through Steam afterward.

### macOS

Close Sephiria, then run in Terminal:

```bash
git clone https://github.com/tempeste/SephiriaQoL.git
cd SephiriaQoL
./scripts/install-macos.sh
```

For a non-default Steam library, pass the Sephiria directory:

```bash
./scripts/install-macos.sh "/Volumes/Games/steamapps/common/Sephiria"
```

The script prints the exact Steam launch option to paste into
**Sephiria → Properties → General → Launch Options**. It looks like:

```text
"/Users/YOU/Library/Application Support/Steam/steamapps/common/Sephiria/run_bepinex.sh" %command%
```

Then launch Sephiria through Steam. The official BepInEx 5.4.23.5 macOS package
is universal; its launcher handles the appropriate game architecture on Intel and
Apple Silicon.

### Confirm it loaded

After one launch, check `BepInEx/LogOutput.log` for
`Loading [Sephiria QoL 0.7.1]`. The plugin's next line lists its enabled QoL,
spectator, and 16-player features.

## Who needs each mod?

| Feature | Host | Other players | Why |
| --- | --- | --- | --- |
| Run timer, damage chart/details, journal search | Not required | Only the player who wants the UI | These read synchronized game state and draw locally. |
| Tablet optimizer | Not required | Only the player using it | It calls Sephiria's built-in server-authoritative request for that player's inventory. |
| Spectator camera | Not required | Only the player who wants to spectate after dying | Camera selection and controls are local. |
| JustAnvil | Required to change the shared run | Not required | Remote clients are explicitly skipped; the host owns the floor graph. |
| Native add-on bootstrap | Not required | Only machines loading native `AddOns` | This only repairs local add-on startup ordering. |
| 16-player rooms | Required for rooms above the vanilla limit | Optional | The host owns the lobby and network limit. Clients get the compact large-party HUD and can host larger rooms themselves when installed. |
| Party Scaling | Required | Not required | Enemy health and spawn plans are changed only by the host and synchronized through Sephiria's normal networking. |
| Extended leveling | Required above level 30 | Recommended | The host owns XP and level progression. Installing on clients keeps their XP bar and maximum-level text consistent above 30. |

There are no QoL features that must be installed by every player. For the most
consistent UI in a 5–16 player room, install Sephiria QoL on everyone, but only the
host is required for the larger capacity.

## Usage and configuration

Open the Control Center with the **QOL** button or `F11`. Changes are saved
immediately. The tablet optimizer also has its own `F10` shortcut. It changes only
the requesting player's inventory, but it rearranges the whole inventory rather
than tablets alone. Keep `Prefer positional relic synergies` enabled to make
productive damage and support links outrank raw levels on unrelated artifacts.
Effects whose best side depends on the player's chosen element (such as Fire/Ice
conversion relics) remain neutral rather than guessing the build. Start with one
pass and wait for the inventory to settle before clicking again.

Configuration is stored in
`BepInEx/config/dev.tempeste.sephiria.qol.cfg`. Delete only that configuration
file to restore defaults; it will be recreated on the next launch.

The `[Utility]` section controls the run timer and combat tracker.
`ShowDamageTaken` is enabled by default and adds every active player's cumulative
incoming damage plus the party's total incoming damage to the live contribution
panel. Click a player for their HP, average DPS, elemental mix, and top sources.

The `[MaxPlayer]` section controls the independent large-party implementation:
`Enabled` toggles it, `MaximumPlayers` accepts 2–16, and
`CompactMultiplayerHud` scales Sephiria's native party roster as it fills.

The `[PartyScaling]` section is disabled by default. When `Enabled = true` on the
host, `EnemyHealthMultiplier` accepts 1.0–10.0 and applies after Sephiria's own
difficulty and multiplayer health bonuses. `EnemySpawnMultiplier` accepts 1.0–4.0
and affects normal enemies only; minibosses, bosses, and training targets are not
duplicated. A safety ceiling prevents the multiplier from pushing a generated
phase above 96 normal enemies. Changes affect newly generated encounters and newly
spawned enemies, so set the values before entering the next room.

Other players do not need the plugin for Party Scaling. They receive the host's
spawned enemies and synchronized health values through the game's normal network
state.

The `[ExtendedLeveling]` section is enabled by default because increased enemy
counts create substantially more XP. `MaximumLevel` accepts 30–200 and defaults
to 100. Every standard XP threshold through level 30 is preserved; above it, the
XP required for the next level increases by 200 every three levels. Sephiria's
normal server-authoritative XP, level-up rewards, healing, inventory bonuses, and
network synchronization remain in control. For consistent level UI above 30,
install Sephiria QoL on every player in the room.

## Future plans

These are planning priorities rather than release promises. Features will be
implemented independently against Sephiria's runtime APIs; third-party binaries
and source are not incorporated into this repository.

### Next candidates

1. **Fast shop reroll** — streamline repeated Sapphire-shop rerolls while keeping
   Sephiria's normal costs, inventory updates, and server authority.
2. **Boss-entry announcer** — show a system message identifying the first player
   to enter a miniboss room, helping large parties coordinate before engagement.
3. **End-of-run team summary** — reuse the combat tracker to show dealt damage,
   damage taken, average DPS, contribution, and top sources when a run ends.

### Later candidates

- **Visual hidden-room guidance** — optionally highlight or point toward the
  current floor's undiscovered entrance as an extension of the existing tracker.
- **Host-authoritative leaf transfer** — provide confirmed player-to-player
  transfers with limits, validation, and visible transaction messages.
- **Preset-slot expansion** — increase the number of saved presets only after the
  profile serialization and backward-compatibility behavior are fully audited.

### Planning constraints

- Shared-run changes remain server-authoritative and fail safely when disabled.
- Persistent-profile features require explicit compatibility and recovery checks.
- New overlays reuse cached/synchronized state and avoid per-frame scene scans.
- Every user-facing option is configurable through BepInEx and the Control Center.

Current idea references include the
[Taehyun mod catalog](https://github.com/TaeHyun015/Sephiria-Mods-By-KimJangee/blob/main/mod_list.json)
and the
[Mira mod-manager catalog](https://github.com/Mira090/SephiriaModManager-Releases/blob/main/Mods/mod_list.json).

## Building manually

The project references assemblies from the local Sephiria installation. Windows
defaults to `E:\SteamLibrary\steamapps\common\Sephiria`; override it with
`SEPHIRIA_GAME_DIR` when needed.

```powershell
dotnet build .\src\SephiriaQoL\SephiriaQoL.csproj -c Release
```

Copy the resulting `SephiriaQoL.dll` into `BepInEx/plugins`. The clean-room
16-player implementation is included in that DLL and does not require a native
`AddOns` package.

## Troubleshooting

- No `BepInEx/LogOutput.log`: BepInEx itself did not initialize; fix the loader
  before debugging this plugin.
- Plugin is absent from the log: confirm the DLL is directly under
  `BepInEx/plugins` and that only the BepInEx build is installed.
- Overlays are missing during a cinematic: full-screen game UI can cover IMGUI;
  check again after normal control resumes.
- Overlay is too small or large: use its `− / percentage / +` header controls, or
  edit `DamagePanelScale` / `PanelScale` in the QoL config. `0` restores auto mode.
- A host still offers only four slots: confirm `MaxPlayer.Enabled = true` and
  `MaxPlayer.MaximumPlayers = 16` in the QoL config, then recreate the lobby.
- Party Scaling appears inactive: confirm this machine is the host, enable
  `PartyScaling.Enabled`, and enter a new encounter. Existing enemies are not
  retroactively changed.
- Leveling stops at 30: confirm `ExtendedLeveling.Enabled = true` on the host and
  set `ExtendedLeveling.MaximumLevel` above 30 before earning the next threshold.
- Build references are missing: set `SEPHIRIA_GAME_DIR`, or set
  `SEPHIRIA_MANAGED_DIR` directly to the game's `Managed` directory.
- Behavior changes after a Sephiria update: compare the affected runtime member
  names/signatures before changing Harmony patches.

## Development

Read [`AGENTS.md`](AGENTS.md) before modifying hooks, add-on bootstrapping, or
deployment behavior. It contains the architecture map, repository boundaries,
validation checklist, and release workflow intended for both humans and coding
agents.
