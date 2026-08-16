[CmdletBinding()]
param(
    [string]$GameDir = ""
)

$ErrorActionPreference = "Stop"

$BepInExUrl = "https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.5/BepInEx_win_x64_5.4.23.5.zip"
$BepInExSha256 = "82f9878551030f54657792c0740d9d51a09500eeae1fba21106b0c441e6732c4"
$SourceArchiveUrl = "https://github.com/tempeste/SephiriaQoL/archive/refs/heads/main.zip"
$RepoDir = if ([string]::IsNullOrWhiteSpace($PSScriptRoot)) { "" } else { Split-Path -Parent $PSScriptRoot }
$TempDir = Join-Path ([IO.Path]::GetTempPath()) ("sephiria-qol-" + [guid]::NewGuid().ToString("N"))

function Get-VerifiedArchive {
    param(
        [Parameter(Mandatory = $true)][string]$Url,
        [Parameter(Mandatory = $true)][string]$ExpectedSha256,
        [Parameter(Mandatory = $true)][string]$Destination
    )

    Invoke-WebRequest -Uri $Url -OutFile $Destination -UseBasicParsing
    $ActualSha256 = (Get-FileHash -LiteralPath $Destination -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($ActualSha256 -ne $ExpectedSha256) {
        throw "Checksum mismatch for $Url`nExpected: $ExpectedSha256`nActual:   $ActualSha256"
    }
}

if ([string]::IsNullOrWhiteSpace($GameDir)) {
    $Candidates = @()
    if (${env:ProgramFiles(x86)}) {
        $Candidates += Join-Path ${env:ProgramFiles(x86)} "Steam\steamapps\common\Sephiria"
    }
    if ($env:ProgramFiles) {
        $Candidates += Join-Path $env:ProgramFiles "Steam\steamapps\common\Sephiria"
    }
    $Candidates += "D:\SteamLibrary\steamapps\common\Sephiria"
    $Candidates += "E:\SteamLibrary\steamapps\common\Sephiria"
    $GameDir = $Candidates | Where-Object { Test-Path (Join-Path $_ "Sephiria.exe") } | Select-Object -First 1
}

if ([string]::IsNullOrWhiteSpace($GameDir) -or -not (Test-Path (Join-Path $GameDir "Sephiria_Data\Managed\Assembly-CSharp.dll"))) {
    throw "Sephiria was not found. Pass its directory with -GameDir, for example: -GameDir 'D:\SteamLibrary\steamapps\common\Sephiria'"
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "The .NET 8 SDK is required but dotnet was not found in PATH."
}

New-Item -ItemType Directory -Path $TempDir | Out-Null
try {
    $ProjectPath = if ([string]::IsNullOrWhiteSpace($RepoDir)) {
        ""
    }
    else {
        Join-Path $RepoDir "src\SephiriaQoL\SephiriaQoL.csproj"
    }

    if ([string]::IsNullOrWhiteSpace($ProjectPath) -or -not (Test-Path $ProjectPath)) {
        Write-Host "Downloading the current Sephiria QoL source..."
        $SourceArchive = Join-Path $TempDir "source.zip"
        $SourceDirectory = Join-Path $TempDir "source"
        Invoke-WebRequest -Uri $SourceArchiveUrl -OutFile $SourceArchive -UseBasicParsing
        Expand-Archive -LiteralPath $SourceArchive -DestinationPath $SourceDirectory -Force
        $RepoDir = Get-ChildItem -LiteralPath $SourceDirectory -Directory |
            Where-Object { Test-Path (Join-Path $_.FullName "src\SephiriaQoL\SephiriaQoL.csproj") } |
            Select-Object -First 1 -ExpandProperty FullName
        if ([string]::IsNullOrWhiteSpace($RepoDir)) {
            throw "The downloaded source archive did not contain the Sephiria QoL project."
        }
    }

    $BepInExDll = Join-Path $GameDir "BepInEx\core\BepInEx.dll"
    if (-not (Test-Path $BepInExDll)) {
        Write-Host "Installing BepInEx 5.4.23.5..."
        $BepInExArchive = Join-Path $TempDir "bepinex.zip"
        Get-VerifiedArchive -Url $BepInExUrl -ExpectedSha256 $BepInExSha256 -Destination $BepInExArchive
        Expand-Archive -LiteralPath $BepInExArchive -DestinationPath $GameDir -Force
    }
    else {
        Write-Host "BepInEx is already installed."
    }

    $ManagedDir = Join-Path $GameDir "Sephiria_Data\Managed"
    $env:SEPHIRIA_GAME_DIR = $GameDir
    $env:SEPHIRIA_MANAGED_DIR = $ManagedDir

    Write-Host "Building Sephiria QoL..."
    & dotnet build (Join-Path $RepoDir "src\SephiriaQoL\SephiriaQoL.csproj") -c Release
    if ($LASTEXITCODE -ne 0) {
        throw "The QoL build failed with exit code $LASTEXITCODE."
    }

    $PluginDir = Join-Path $GameDir "BepInEx\plugins"
    New-Item -ItemType Directory -Path $PluginDir -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $RepoDir "src\SephiriaQoL\bin\Release\netstandard2.1\SephiriaQoL.dll") -Destination (Join-Path $PluginDir "SephiriaQoL.dll") -Force

    Write-Host ""
    Write-Host "Installed:"
    Write-Host "  $(Join-Path $PluginDir 'SephiriaQoL.dll')"
    Write-Host ""
    Write-Host "Launch Sephiria normally through Steam."
}
finally {
    if (Test-Path $TempDir) {
        Remove-Item -LiteralPath $TempDir -Recurse -Force
    }
}
