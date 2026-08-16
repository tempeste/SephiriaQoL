#!/usr/bin/env bash
set -euo pipefail

readonly bepinex_url="https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.5/BepInEx_macos_universal_5.4.23.5.zip"
readonly bepinex_sha256="01c2ae782eb016dfd6c345a18dbd2dcafffb3d9d318449d6486689f426b4a323"
readonly source_archive_url="https://github.com/tempeste/SephiriaQoL/archive/refs/heads/main.tar.gz"

script_dir="$(cd "$(dirname "$0")" && pwd -P)"
repo_dir="$(cd "$script_dir/.." && pwd -P)"
game_dir="${1:-$HOME/Library/Application Support/Steam/steamapps/common/Sephiria}"
managed_dir="$game_dir/Sephiria.app/Contents/Resources/Data/Managed"
plugin_dir="$game_dir/BepInEx/plugins"
temp_dir=""

cleanup() {
  if [[ -n "$temp_dir" && -d "$temp_dir" ]]; then
    rm -rf -- "$temp_dir"
  fi
}
trap cleanup EXIT

download_verified() {
  local url="$1"
  local expected_sha="$2"
  local destination="$3"
  local actual_sha

  curl --fail --location --progress-bar "$url" --output "$destination"
  actual_sha="$(shasum -a 256 "$destination" | awk '{print $1}')"
  if [[ "$actual_sha" != "$expected_sha" ]]; then
    echo "Checksum mismatch for: $url" >&2
    echo "Expected: $expected_sha" >&2
    echo "Actual:   $actual_sha" >&2
    exit 1
  fi
}

if [[ ! -f "$managed_dir/Assembly-CSharp.dll" ]]; then
  echo "Sephiria was not found at: $game_dir" >&2
  echo "Pass its game directory as the first argument." >&2
  exit 1
fi

if ! command -v dotnet >/dev/null 2>&1; then
  echo "The .NET 8 SDK is required but dotnet was not found in PATH." >&2
  exit 1
fi

temp_dir="$(mktemp -d "${TMPDIR:-/tmp}/sephiria-qol.XXXXXX")"

if [[ ! -f "$repo_dir/src/SephiriaQoL/SephiriaQoL.csproj" ]]; then
  echo "Downloading the current Sephiria QoL source..."
  curl --fail --location --progress-bar "$source_archive_url" --output "$temp_dir/source.tar.gz"
  mkdir -p "$temp_dir/source"
  tar -xzf "$temp_dir/source.tar.gz" -C "$temp_dir/source" --strip-components=1
  repo_dir="$temp_dir/source"
fi

if [[ ! -f "$game_dir/BepInEx/core/BepInEx.dll" ]]; then
  echo "Installing BepInEx 5.4.23.5..."
  download_verified "$bepinex_url" "$bepinex_sha256" "$temp_dir/bepinex.zip"
  unzip -q "$temp_dir/bepinex.zip" -d "$game_dir"
else
  echo "BepInEx is already installed."
fi

if [[ ! -f "$game_dir/run_bepinex.sh" ]]; then
  echo "BepInEx did not provide run_bepinex.sh." >&2
  exit 1
fi

chmod u+x "$game_dir/run_bepinex.sh"
/usr/bin/sed -i '' 's/^executable_name=.*/executable_name="Sephiria.app"/' "$game_dir/run_bepinex.sh"

export SEPHIRIA_GAME_DIR="$game_dir"
export SEPHIRIA_MANAGED_DIR="$managed_dir"

echo "Building Sephiria QoL..."
dotnet build "$repo_dir/src/SephiriaQoL/SephiriaQoL.csproj" -c Release
mkdir -p "$plugin_dir"
cp "$repo_dir/src/SephiriaQoL/bin/Release/netstandard2.1/SephiriaQoL.dll" "$plugin_dir/SephiriaQoL.dll"

echo
echo "Installed:"
echo "  $plugin_dir/SephiriaQoL.dll"
echo
echo "Set this exact Steam launch option, then start Sephiria:"
echo "\"$game_dir/run_bepinex.sh\" %command%"
