#!/usr/bin/env bash
set -euo pipefail

readonly bepinex_url="https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.5/BepInEx_macos_universal_5.4.23.5.zip"
readonly bepinex_sha256="01c2ae782eb016dfd6c345a18dbd2dcafffb3d9d318449d6486689f426b4a323"
readonly doorstop_compat_url="https://nightly.link/NeighTools/UnityDoorstop/actions/runs/32021385084/doorstop_macos_release.zip"
readonly doorstop_compat_sha256="5d81d114220c11b1f042db7571035c7cb47b59f8d5bba6cde341acf1d7504432"
readonly doorstop_compat_dylib_sha256="24fb87d691af5344eac1766e29468393ba3c5a0f688d3dc04232146dd5e34a3d"
readonly dotnet_install_url="https://dot.net/v1/dotnet-install.sh"
readonly source_archive_url="https://github.com/tempeste/SephiriaQoL/archive/refs/heads/main.tar.gz"

script_dir="$(cd "$(dirname "$0")" && pwd -P)"
repo_dir="$(cd "$script_dir/.." && pwd -P)"
game_dir="${1:-$HOME/Library/Application Support/Steam/steamapps/common/Sephiria}"
managed_dir="$game_dir/Sephiria.app/Contents/Resources/Data/Managed"
plugin_dir="$game_dir/BepInEx/plugins"
temp_dir=""
dotnet_cmd=""

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

install_doorstop_compatibility() {
  local current_loader="$game_dir/libdoorstop.dylib"
  local loader_backup="$game_dir/libdoorstop.dylib.bepinex-5.4.23.5"
  local log_file="$game_dir/BepInEx/LogOutput.log"
  local unity_player="$game_dir/Sephiria.app/Contents/Frameworks/UnityPlayer.dylib"
  local current_sha=""
  local compatibility_dir="$temp_dir/doorstop-compat"

  if [[ -f "$current_loader" ]]; then
    current_sha="$(shasum -a 256 "$current_loader" | awk '{print $1}')"
  fi

  if [[ "$current_sha" == "$doorstop_compat_dylib_sha256" ]]; then
    echo "UnityDoorstop compatibility loader is already installed."
    return
  fi

  # Preserve installations that have already loaded BepInEx successfully for
  # the current game build. This includes known-good locally built loaders.
  if [[ -f "$log_file" ]] && grep -q "Chainloader startup complete" "$log_file" &&
     [[ ! -f "$unity_player" || ! "$unity_player" -nt "$log_file" ]]; then
    echo "Existing UnityDoorstop loader has already started BepInEx successfully; preserving it."
    return
  fi

  echo "Installing the pinned Unity 6000.3 macOS compatibility loader..."
  download_verified "$doorstop_compat_url" "$doorstop_compat_sha256" "$temp_dir/doorstop-compat.zip"
  mkdir -p "$compatibility_dir"
  unzip -q "$temp_dir/doorstop-compat.zip" -d "$compatibility_dir"

  local downloaded_loader="$compatibility_dir/universal/libdoorstop.dylib"
  if [[ ! -f "$downloaded_loader" ]]; then
    echo "The compatibility archive did not contain universal/libdoorstop.dylib." >&2
    exit 1
  fi

  local downloaded_sha
  downloaded_sha="$(shasum -a 256 "$downloaded_loader" | awk '{print $1}')"
  if [[ "$downloaded_sha" != "$doorstop_compat_dylib_sha256" ]]; then
    echo "Checksum mismatch for the extracted UnityDoorstop compatibility loader." >&2
    echo "Expected: $doorstop_compat_dylib_sha256" >&2
    echo "Actual:   $downloaded_sha" >&2
    exit 1
  fi

  if [[ -f "$current_loader" && ! -f "$loader_backup" ]]; then
    cp -p "$current_loader" "$loader_backup"
  fi
  cp "$downloaded_loader" "$current_loader"
  chmod u+x "$current_loader"
  xattr -d com.apple.quarantine "$current_loader" 2>/dev/null || true
}

configure_apple_silicon_launcher() {
  local launcher="$game_dir/run_bepinex.sh"
  local translated=""

  translated="$(/usr/sbin/sysctl -in sysctl.proc_translated 2>/dev/null || true)"

  if [[ "$(uname -m)" != "arm64" && "$translated" != "1" ]]; then
    return
  fi

  if ! /usr/bin/arch -x86_64 /usr/bin/true >/dev/null 2>&1; then
    echo "Installing Rosetta 2 for Sephiria's BepInEx launcher..."
    /usr/sbin/softwareupdate --install-rosetta --agree-to-license
  fi

  if ! /usr/bin/arch -x86_64 /usr/bin/true >/dev/null 2>&1; then
    echo "Rosetta 2 is required, but an x86_64 process could not be started." >&2
    exit 1
  fi

  /usr/bin/sed -i '' \
    -e 's/export ARCHPREFERENCE="arm64,x86_64"/export ARCHPREFERENCE="x86_64,arm64"/' \
    -e 's/exec arch -e DYLD_INSERT_LIBRARIES=/exec arch -x86_64 -e DYLD_INSERT_LIBRARIES=/' \
    "$launcher"

  if ! grep -q 'export ARCHPREFERENCE="x86_64,arm64"' "$launcher" ||
     ! grep -q 'exec arch -x86_64 -e DYLD_INSERT_LIBRARIES=' "$launcher"; then
    echo "Could not configure run_bepinex.sh to launch through Rosetta." >&2
    exit 1
  fi
}

has_dotnet_8_sdk() {
  local candidate="$1"

  [[ -x "$candidate" ]] || return 1
  "$candidate" --list-sdks 2>/dev/null | awk -F. '$1 == 8 { found = 1 } END { exit !found }'
}

install_dotnet_8_sdk() {
  local path_candidate=""
  local install_dir="$HOME/.dotnet"
  local installed_candidate="$install_dir/dotnet"
  local install_script="$temp_dir/dotnet-install.sh"

  if command -v dotnet >/dev/null 2>&1; then
    path_candidate="$(command -v dotnet)"
    if has_dotnet_8_sdk "$path_candidate"; then
      dotnet_cmd="$path_candidate"
      return
    fi
  fi

  if has_dotnet_8_sdk "$installed_candidate"; then
    export DOTNET_ROOT="$install_dir"
    export PATH="$install_dir:$PATH"
    dotnet_cmd="$installed_candidate"
    return
  fi

  echo "Installing the .NET 8 SDK for the current user..."
  curl --fail --location --progress-bar "$dotnet_install_url" --output "$install_script"
  bash "$install_script" --channel 8.0 --install-dir "$install_dir" --no-path
  if ! has_dotnet_8_sdk "$installed_candidate"; then
    echo "The .NET 8 SDK installation completed, but a usable SDK was not found at: $installed_candidate" >&2
    exit 1
  fi

  export DOTNET_ROOT="$install_dir"
  export PATH="$install_dir:$PATH"
  dotnet_cmd="$installed_candidate"
}

if [[ ! -f "$managed_dir/Assembly-CSharp.dll" ]]; then
  echo "Sephiria was not found at: $game_dir" >&2
  echo "Pass its game directory as the first argument." >&2
  exit 1
fi

temp_dir="$(mktemp -d "${TMPDIR:-/tmp}/sephiria-qol.XXXXXX")"
install_dotnet_8_sdk

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
install_doorstop_compatibility
configure_apple_silicon_launcher
xattr -d com.apple.quarantine "$game_dir/run_bepinex.sh" 2>/dev/null || true

export SEPHIRIA_GAME_DIR="$game_dir"
export SEPHIRIA_MANAGED_DIR="$managed_dir"

echo "Building Sephiria QoL..."
(
  cd "$repo_dir"
  "$dotnet_cmd" build "src/SephiriaQoL/SephiriaQoL.csproj" -c Release
)
mkdir -p "$plugin_dir"
cp "$repo_dir/src/SephiriaQoL/bin/Release/netstandard2.1/SephiriaQoL.dll" "$plugin_dir/SephiriaQoL.dll"

echo
echo "Installed:"
echo "  $plugin_dir/SephiriaQoL.dll"
echo
echo "Set this exact Steam launch option, then start Sephiria:"
echo "\"$game_dir/run_bepinex.sh\" %command%"
