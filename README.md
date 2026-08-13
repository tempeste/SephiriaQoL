# Sephiria QoL

Clean BepInEx implementations of quality-of-life features for Sephiria.

Current scope:

- Run timer and multiplayer damage contribution display
- Guaranteed first-choice anvil room
- Native `AddOns` compatibility alongside BepInEx
- Journal keyword search
- In-game tablet/inventory optimizer panel (F10)

The optimizer exposes Sephiria's own server-authoritative layout routine. It scores
active charm levels and tablet bonuses, rearranges only the requesting player's
inventory, and can rotate tablets. Start with one pass; additional passes search
more layouts but can briefly pause the game.

This repository intentionally excludes gameplay-altering mods, progression unlocks, save manipulation,
and the private `SephiriaSoloMod` project. Third-party mod binaries and decompiler output
are also excluded; implementations here are maintained independently against the game's
public runtime types and observed behavior.

## Building

The project references assemblies from the local Sephiria installation. By default it
uses `E:\SteamLibrary\steamapps\common\Sephiria`. Override that path with the
`SEPHIRIA_GAME_DIR` environment variable if needed.

```powershell
dotnet build .\src\SephiriaQoL\SephiriaQoL.csproj -c Release
```

Copy `SephiriaQoL.dll` to `BepInEx\plugins`.

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
