# Sephiria QoL

Independent BepInEx quality-of-life features for Sephiria, packaged in one
`SephiriaQoL.dll`.

## Feature catalog

| Feature | What it does | Default | Shortcut |
| --- | --- | --- | --- |
| QoL Control Center | Central settings, independent UI scaling, and customizable shortcuts | Available | `F11` or **QOL** |
| Run timer | Displays elapsed run time | On | — |
| Combat contribution | Color-coded dealt/taken bars with click-through DPS, HP, element, area, and source details | On | Click a player bar |
| Run summary and history | Shows the team result at game over and retains recent runs locally for comparison | On | `F8` |
| Endless Expedition | Lets an opted-in party continue through increasingly dangerous native procedural floors after the final boss | Off | Host chooses in the victory prompt |
| Encounter announcer | Names the first player to trigger a miniboss or boss room | On | — |
| Fast shop reroll | Repeats Sephiria's normal Sapphire-shop reroll while held | On | `R` |
| Hold to cast | Recasts held spell/artifact slots when ready; supports keys 1–8 and Cast Mode left-click | On | Existing game bindings |
| Party readiness | Shows synchronized loading, ready, menu, combat, downed, and floor states | On | `F5` |
| Hidden-room guidance | Points toward the nearest registered undiscovered entrance | On | — |
| Party voting | Collects manual room/loot votes and shows the tally to the host without auto-selecting | On | `F7` |
| Leaf transfer | Confirmed same-floor transfers with host-side identity, balance, amount, and rate validation | Off | `F6` |
| Additional preset slots | Extends Sephiria's native preset list from 15 to 30 by default, configurable up to 50 | On | — |
| First-choice anvil | Guarantees an anvil in the first playable room choice without deleting the displaced room | On | — |
| Journal search | Filters the artifact journal by keyword | On | — |
| Tablet optimizer | Uses Sephiria's own inventory arranger with additional positional-synergy scoring | On | `F10` |
| Spectator camera | Follows living teammates after the local player is defeated | On | Arrow keys |
| 2–16 player rooms | Expands the host lobby selector and connection capacity | On, max 16 | — |
| Compact party roster | Scales Sephiria's multiplayer HUD as the room fills | On | — |
| Party Scaling | Automatically reinforces enemies in 5–16 player rooms and supports manual host multipliers | Automatic for 5+ players | — |
| Extended leveling | Extends the run XP table beyond level 30 for high-density runs | On, max 100 | — |
| Native add-on bootstrap | Repairs local `AddOns` startup ordering when Sephiria's loader missed them | Automatic | — |

Use the compact **QOL** button or press `F11` to open the Control Center. It groups
the everyday, multiplayer, run-tool, and interface settings in one draggable panel.
The damage chart, run summary, Endless Expedition, tablet optimizer, spectator,
readiness, voting, leaf-transfer, hidden-room, and Control Center interfaces have
independent `− / AUTO / +` scale controls. Click a percentage to restore automatic sizing.
Auto mode uses normal sizing on Windows and scales up on high-resolution macOS/Retina
render surfaces. Manual values persist in the BepInEx configuration.

All mod panels share a dark-timber, worn-bronze, and gold fantasy theme with chunky
pixel-like frames, visible grain, and carved-plank controls inspired by Sephiria's
menu language. Player colors are paired with names, ranks, percentages, or status
text so color is never the only indicator.
The theme is drawn from simple runtime shapes and does not package the game's art.

Every Sephiria QoL shortcut can also be changed in the Control Center's
**Interface** tab. Click a binding and press the desired key or modifier
combination; press Escape to cancel or use `×` to leave that action unbound.
Changes are saved to the normal BepInEx configuration immediately. Hold to cast
follows Sephiria's own spell bindings instead.

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

## Installation

Run the installer directly from GitHub, or clone this repository and run the same
installer from the checkout. Both routes install the pinned BepInEx release when
needed, build the current source, and copy the single `SephiriaQoL.dll` into the
game. Rerunning either route after an update replaces the DLL while preserving the
existing BepInEx configuration.

BepInEx and the temporary macOS compatibility download are verified against
SHA-256 hashes committed to this repository. Third-party mod binaries are not
bundled or redistributed.

Prerequisites:

- A legal Steam installation of Sephiria using the Mono scripting backend
- Git only when using the clone workflow
- A game version compatible with the referenced runtime API (last validated on
  Sephiria 1.0.27)

The one-step Windows and macOS installers automatically install a user-scoped
.NET 8 SDK when a suitable SDK is missing. They also install BepInEx 5 when it
is missing. A manual .NET installation is only required when building without
the installer scripts. Later major SDKs are not substituted for .NET 8 because
this Unity Mono build targets the .NET Standard 2.1 reference pack; the SDKs can
remain installed side by side.

MelonLoader is not used or required. The spectator feature is already part of
`SephiriaQoL.dll`; do not install the upstream MelonLoader `Spectator.dll`.

### Windows

Close Sephiria, then choose either PowerShell workflow.

Without cloning:

```powershell
irm https://raw.githubusercontent.com/tempeste/SephiriaQoL/main/scripts/install-windows.ps1 | iex
```

For a non-default Steam library without cloning:

```powershell
& ([scriptblock]::Create((irm https://raw.githubusercontent.com/tempeste/SephiriaQoL/main/scripts/install-windows.ps1))) `
  -GameDir "F:\SteamLibrary\steamapps\common\Sephiria"
```

From a clone:

```powershell
git clone https://github.com/tempeste/SephiriaQoL.git
cd SephiriaQoL
.\scripts\install-windows.ps1
```

The script detects the normal Steam location plus common `D:` and `E:` Steam
libraries. For another location:

```powershell
.\scripts\install-windows.ps1 -GameDir "F:\SteamLibrary\steamapps\common\Sephiria"
```

The `powershell -ExecutionPolicy Bypass -File` prefix is not needed from an
already-open PowerShell prompt. If Windows blocks local scripts, use it once as a
fallback:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\install-windows.ps1
```

Launch Sephiria normally through Steam afterward.

### macOS

Close Sephiria, then choose either Terminal workflow.

Without cloning:

```bash
curl -fsSL https://raw.githubusercontent.com/tempeste/SephiriaQoL/main/scripts/install-macos.sh | bash
```

For a non-default Steam library without cloning:

```bash
curl -fsSL https://raw.githubusercontent.com/tempeste/SephiriaQoL/main/scripts/install-macos.sh | \
  bash -s -- "/Volumes/Games/steamapps/common/Sephiria"
```

From a clone:

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
is installed first. Because Sephiria 1.0.30 moved to Unity 6000.3, the installer
also installs a checksum-pinned macOS compatibility build from
[UnityDoorstop PR #110](https://github.com/NeighTools/UnityDoorstop/pull/110).
On Apple Silicon it installs Rosetta 2 when needed and configures Sephiria to run
through the x86_64 path that is currently known to load BepInEx reliably. Existing
loaders that have already started BepInEx successfully for the current game build
are preserved. This is a pinned upstream CI build and can be removed once an
official UnityDoorstop release contains the Unity 6000.3 fix.

The compatibility loader and Rosetta setup are automatic for both the cloned and
no-clone workflows. Friends should not need to copy `libdoorstop.dylib` or edit
`run_bepinex.sh` manually. The installer keeps the original official loader as
`libdoorstop.dylib.bepinex-5.4.23.5` before replacing it.

### Confirm it loaded

After one launch, check `BepInEx/LogOutput.log` for
`Loading [Sephiria QoL 0.10.0]`, followed by the current feature-catalog startup
message and no plugin exceptions.

## Multiplayer installation requirements

| Feature | Host | Other players | Why |
| --- | --- | --- | --- |
| Control Center, UI scaling, and hotkey settings | Not required | Only the player using them | Settings and rendering stay on that machine. |
| Run timer, damage chart/details, journal search | Not required | Only the player who wants the UI | These read synchronized game state and draw locally. |
| End-of-run summary/history | Not required | Only the player who wants the UI | Each client stores its own recent summaries in the BepInEx config directory. |
| Endless Expedition | Required and must enable it | Required and must enable it | Every client pauses the normal final-victory screen while the host chooses. The host generates, links, and synchronizes the temporary native floor segments. |
| Encounter announcer | Required for announcements | Not required | The host identifies the first player who starts a miniboss or boss encounter and sends Sephiria's normal custom message to the room. |
| Fast shop reroll | Not required | Only the player using it | This repeats Sephiria's existing local shop action, including its normal escalating Sapphire costs and checks. |
| Hold to cast | Not required | Only the player using it | It follows that player's existing input bindings and waits for locally synchronized cooldown readiness before using Sephiria's normal cast path. |
| Party readiness | Not required | Only the player who wants the UI | The panel reads player states already synchronized by Sephiria. |
| Hidden-room guidance | Not required | Only the player who wants the marker | Entrance tracking and rendering are entirely client-side. |
| Room/loot voting | Required to collect and show votes | Required for each voter | Votes are explicit plugin messages. Only the host displays the tally; no room or reward is selected automatically. |
| Leaf transfer | Required and must enable it | Required for the sender | The sender uses Sephiria's currency transfer, while the host validates identity, amount, balance, floor, life state, and rate. The recipient only needs normal game synchronization. |
| Additional preset slots | Not required | Only the player using extra slots | Sephiria's existing indexed preset UI/storage is extended locally. Disabling the feature hides slots above 15 but does not delete their data. |
| Tablet optimizer | Not required | Only the player using it | It calls Sephiria's built-in server-authoritative request for that player's inventory. |
| Spectator camera | Not required | Only the player who wants to spectate after dying | Camera selection and controls are local. |
| JustAnvil | Required to change the shared run | Not required | Remote clients are explicitly skipped; the host owns the floor graph. |
| Native add-on bootstrap | Not required | Only machines loading native `AddOns` | This only repairs local add-on startup ordering. |
| 16-player rooms | Required for rooms above the vanilla limit | Optional | The host owns the lobby and network limit. Clients get the compact large-party HUD and can host larger rooms themselves when installed. |
| Party Scaling | Required | Not required | Enemy health and spawn plans are changed only by the host and synchronized through Sephiria's normal networking. |
| Extended leveling | Required above level 30 | Recommended | The host owns XP and level progression. Installing on clients keeps their XP bar and maximum-level text consistent above 30. |

Endless Expedition is the exception: every player needs the same QoL version and
must enable it before the run. Voting needs the host plus each player who wants to
vote; leaf transfer needs the host plus the sender. Only the host is required for
larger room capacity and Party Scaling.

## Usage and configuration

Open the Control Center with the **QOL** button or `F11`. Changes are saved
immediately. The tablet optimizer also has its own `F10` shortcut. It changes only
the requesting player's inventory, but it rearranges the whole inventory rather
than tablets alone. Its local-player reference survives temporary network-identity
gaps; press `F10` again or use its `×` button to close it. Shortcuts remain
responsive while unrelated gameplay keys are held. Keep
`Prefer positional relic synergies` enabled to make
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

The `[RunSummary]` section controls the team summary that opens with Sephiria's
game-over screen. Click a player row for their four highest damage sources. Press
`F8` to reopen the latest locally saved summary, then use the arrow buttons to
browse up to 20 runs by default. Player rows compare dealt damage with the previous
saved run when the same name is present. History is stored locally at
`BepInEx/config/dev.tempeste.sephiria.qol.run-history`.

The `[BossAnnouncer]` section controls host-only miniboss/boss entry messages. Only
the first player who starts each encounter is announced; clients do not need the
plugin to receive it.

The `[FastShopReroll]` section enables held rerolls, sets the hotkey (`R` by
default), and controls the repeat interval. Hold the key while a Sapphire shop is
open. Every repeat calls Sephiria's normal reroll action, so escalating costs,
insufficient-funds checks, purchased-item checks, effects, and inventory refreshes
remain unchanged.

The `[HoldToCast]` section is client-side and enabled by default. Hold any of the
game's current quick-cast bindings for slots 1–8, or hold left-click in Cast Mode.
Spells and active artifacts retry once their native cooldown and cast checks are
ready. Cooldown-only failure feedback is suppressed while the input remains held;
mana and other failures are left unchanged.

The `[PartyReadiness]` panel opens with `F5`. It shows each connected player's
loading, ready, menu, combat, downed, and floor state using data Sephiria already
synchronizes. It is client-side and does not send readiness commands.

`[HiddenRoomGuidance]` is client-side. It registers hidden entrances as Sephiria
creates them and preserves that event-driven path. If a remote client misses those
callbacks and has no registered target, it scans only active, breakable entrances
with enabled colliders every two seconds without clearing the original cache. It
then points toward the nearest undiscovered entrance without scanning the scene
every frame.

The `[PartyVoting]` panel opens with `F7`. Players vote for numbered room/path or
loot/reward choices; a host with the same QoL version sees the `1 / 2 / 3` tally.
The overlay never clicks, chooses, or changes the run. Use **CLEAR** on the host
between decisions.

The `[LeafTransfer]` panel opens with `F6` and is disabled by default. Enable it on
the host and sender, choose an amount, then click **SEND** and **CONFIRM** on the
same teammate. The host rejects non-positive or oversized amounts, insufficient
balances, self-transfers, downed players, different floors, and rapid repeats.

The `[PresetSlots]` section raises Sephiria's native limit from 15 to 30 by default
and supports up to 50. The game already stores preset slots under indexed keys, so
existing 1–15 data is unchanged. If the feature is disabled later, higher slots
are hidden rather than erased and return when the limit is raised again.

The `[EndlessExpedition]` section is disabled by default. When every player enables
it before a run, the normal final-victory screen pauses and the host can either
finish normally or continue into an expedition. Continuing uses Sephiria's native
procedural stage generator, room events, rewards, merchants, hidden rooms, and
miniboss prefabs. The generated segments live only in the current network session
and floor movement does not write them to the run save.

The Continue button stays disabled until every connected player reports the same
Endless Expedition network protocol and an enabled setting. If anyone forgot to
enable it before victory, finish the run normally and enable it for the next run.

Enemy health and normal-enemy count increase per expedition stage, with a native
battle room promoted to a miniboss milestone at the configured interval. Stage 1
starts from the host's effective Party Scaling values, including automatic
large-party scaling, and later stages add the configured growth to that baseline.
The result is applied as one combined multiplier rather than applying both systems
twice. The host can finish from the expedition status panel and settle the original
victory. A full party defeat opens the defeat screen instead. Native room XP and
Sapphire rewards keep their normal behavior and remain part of the final run
settlement; the mod does not add a separate per-stage Sapphire grant.

The `[MaxPlayer]` section controls the independent large-party implementation:
`Enabled` toggles it, `MaximumPlayers` accepts 2–16, and
`CompactMultiplayerHud` scales Sephiria's native party roster as it fills.

The `[PartyScaling]` section automatically compensates for Sephiria's limited
enemy-count growth in rooms with five or more connected players. With
`AutoScaleLargeParties = true` (the default), every player above four adds 5% to
the QoL health multiplier and 15% to the normal-enemy multiplier. This produces
1.05× health / 1.15× count at five players and 1.60× health / 2.80× count at
sixteen players, after Sephiria applies its own multiplayer scaling.

Set `Enabled = true` to use `EnemyHealthMultiplier` (1.0–10.0) and
`EnemySpawnMultiplier` (1.0–4.0) as manual minimums. When both automatic and
manual scaling are active, the larger value wins instead of multiplying them
together. Disable `AutoScaleLargeParties` if the host wants entirely manual
control. Count scaling affects normal enemies only; minibosses, bosses, and
training targets are not duplicated. A safety ceiling prevents the multiplier
from pushing a generated phase above 96 normal enemies. Changes affect newly
generated encounters and newly spawned enemies.

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

## Potential refinements

These are planning priorities rather than release promises. Features will be
implemented independently against Sephiria's runtime APIs; third-party binaries
and source are not incorporated into this repository.

- Context labels for votes when stable room/reward names can be read safely.
- Optional visible transfer receipts without weakening host validation.
- Named run-history comparison filters and export.
- More precise hidden-room distance display after its generated-room coordinate
  behavior has been exercised across every floor type.

### Planning constraints

- Shared-run changes remain server-authoritative and fail safely when disabled.
- Persistent-profile features require explicit compatibility and recovery checks.
- New overlays reuse cached/synchronized state and avoid per-frame scene scans.
- Every user-facing option is configurable through BepInEx and the Control Center.

Current idea references include the
[Taehyun mod catalog](https://github.com/TaeHyun015/Sephiria-Mods-By-KimJangee/blob/main/mod_list.json),
the
[Hold to Cast on Cooldown behavior description](https://www.nexusmods.com/sephiria/mods/5),
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

- No `BepInEx/LogOutput.log` on macOS: close Sephiria and rerun the current macOS
  installer. It reapplies the checksum-pinned compatibility loader and Rosetta
  launcher setup before rebuilding the plugin.
- Plugin is absent from the log: confirm the DLL is directly under
  `BepInEx/plugins` and that only the BepInEx build is installed.
- Overlays are missing during a cinematic: full-screen game UI can cover IMGUI;
  check again after normal control resumes.
- Overlay is too small or large: use its `− / percentage / +` header controls, or
  edit `DamagePanelScale` / `PanelScale` in the QoL config. `0` restores auto mode.
- A host still offers only four slots: confirm `MaxPlayer.Enabled = true` and
  `MaxPlayer.MaximumPlayers = 16` in the QoL config, then recreate the lobby.
- Party Scaling appears inactive: confirm this machine is the host and enter a new
  encounter. Automatic scaling requires at least five connected players; manual
  values require `PartyScaling.Enabled = true`. Existing enemies are not
  retroactively changed.
- Endless Expedition is not offered: use the same QoL version on every machine,
  enable `EndlessExpedition.Enabled` before the run, and reach the final victory.
  Do not enable it on only part of a multiplayer party.
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
