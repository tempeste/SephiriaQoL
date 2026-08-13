#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "$0")" && pwd -P)"
repo_dir="$(cd "$script_dir/.." && pwd -P)"
game_dir="${1:-$HOME/Library/Application Support/Steam/steamapps/common/Sephiria}"
managed_dir="$game_dir/Sephiria.app/Contents/Resources/Data/Managed"
plugin_dir="$game_dir/BepInEx/plugins"

if [[ ! -f "$managed_dir/Assembly-CSharp.dll" ]]; then
  echo "Sephiria managed assemblies were not found at: $managed_dir" >&2
  echo "Pass the Sephiria game directory as the first argument." >&2
  exit 1
fi

if [[ ! -f "$game_dir/BepInEx/core/BepInEx.dll" ]]; then
  echo "BepInEx is not initialized at: $game_dir/BepInEx" >&2
  echo "Install and launch BepInEx successfully before installing the plugin." >&2
  exit 1
fi

if ! command -v dotnet >/dev/null 2>&1; then
  echo "The dotnet SDK is required but was not found in PATH." >&2
  exit 1
fi

export SEPHIRIA_GAME_DIR="$game_dir"
export SEPHIRIA_MANAGED_DIR="$managed_dir"

dotnet build "$repo_dir/src/SephiriaQoL/SephiriaQoL.csproj" -c Release
mkdir -p "$plugin_dir"
cp "$repo_dir/src/SephiriaQoL/bin/Release/netstandard2.1/SephiriaQoL.dll" "$plugin_dir/SephiriaQoL.dll"

echo "Installed SephiriaQoL.dll to: $plugin_dir"
