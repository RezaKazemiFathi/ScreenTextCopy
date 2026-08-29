<#
.SYNOPSIS
    Builds every release artifact for ScreenTextCopy in one shot.

.DESCRIPTION
    Produces, under <repo>\release:

      app\                                        the published application
      ScreenTextCopy-<ver>-win-x64-portable.zip   unzip-and-run build
      ScreenTextCopy-Setup-<ver>-win-x64.exe      one-click installer

    The published app is SELF-CONTAINED: the .NET 8 runtime is embedded and the
    Tesseract 5 OCR engine ships alongside it, so the machine that runs the app
    needs no prerequisites whatsoever.

.PARAMETER Version
    Version stamped into the assemblies, the installer and the file names.

.PARAMETER SkipInstaller
    Build only the app + portable zip (useful when Inno Setup is unavailable).

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\build-release.ps1
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\build-release.ps1 -Version 2.1.0
#>
[CmdletBinding()]
param(
    [string] $Version = '2.0.0',
    [switch] $SkipInstaller
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$RepoRoot   = Split-Path -Parent $PSScriptRoot
$Project    = Join-Path $RepoRoot 'src\ScreenTextCopy\ScreenTextCopy.csproj'
$TessSource = Join-Path $RepoRoot 'src\ScreenTextCopy\Tesseract'
$ReleaseDir = Join-Path $RepoRoot 'release'
$PayloadDir = Join-Path $ReleaseDir 'app'
$IssFile    = Join-Path $RepoRoot 'installer\ScreenTextCopy.iss'

function Write-Step { param([string] $Text) Write-Host "`n==> $Text" -ForegroundColor Cyan }
function Write-Ok   { param([string] $Text) Write-Host "    $Text" -ForegroundColor Green }
function Write-Warn { param([string] $Text) Write-Host "    $Text" -ForegroundColor Yellow }

function Get-SizeMb {
    param([string] $Path)
    if (Test-Path -LiteralPath $Path -PathType Container) {
        $bytes = (Get-ChildItem -LiteralPath $Path -Recurse -File |
                  Measure-Object -Property Length -Sum).Sum
    } else {
        $bytes = (Get-Item -LiteralPath $Path).Length
    }
    return [math]::Round($bytes / 1MB, 1)
}

# ---------------------------------------------------------------- 1. sanity ---
Write-Step '1/5  Checking the build environment'

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'The .NET SDK was not found. Install .NET 8 SDK from https://dotnet.microsoft.com/download/dotnet/8.0 and reopen the terminal.'
}
Write-Ok ".NET SDK $(dotnet --version)"

if (-not (Test-Path -LiteralPath $Project)) {
    throw "Project not found: $Project"
}

$engine = Join-Path $TessSource 'tesseract.exe'
if (-not (Test-Path -LiteralPath $engine)) {
    throw @"
The bundled Tesseract engine is missing at:
    $TessSource
Run this first, then re-run this script:
    powershell -ExecutionPolicy Bypass -File scripts\fetch-tesseract.ps1
"@
}
Write-Ok "Tesseract engine present ($(Get-SizeMb $TessSource) MB)"

# --------------------------------------------------------------- 2. publish ---
Write-Step '2/5  Publishing a self-contained win-x64 build'

if (Test-Path -LiteralPath $ReleaseDir) {
    Remove-Item -LiteralPath $ReleaseDir -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $PayloadDir | Out-Null

# NOTES ON THE FLAGS
#   --self-contained true   embeds the .NET 8 runtime -> no runtime install.
#   PublishSingleFile=false keeps startup fast; a single-file WPF bundle has to
#                           extract native libraries on first launch.
#   PublishTrimmed=false    WPF resolves types from XAML through reflection, so
#                           trimming silently breaks data binding at runtime.
& dotnet publish $Project `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:PublishTrimmed=false `
    -p:DebugType=none `
    -p:Version=$Version `
    -p:AssemblyVersion=$Version `
    -p:FileVersion=$Version `
    --output $PayloadDir

if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE." }

$exe = Join-Path $PayloadDir 'ScreenTextCopy.exe'
if (-not (Test-Path -LiteralPath $exe)) { throw "Publish finished but $exe is missing." }
Write-Ok "Published to release\app ($(Get-SizeMb $PayloadDir) MB)"

# ------------------------------------------------------------ 3. OCR payload ---
Write-Step '3/5  Verifying the bundled OCR payload'

# dotnet publish already copies Tesseract\** and Localization\*.json because the
# csproj marks them CopyToOutputDirectory. Copy anything still missing so the
# payload is correct even if those csproj entries change later.
$payloadTess = Join-Path $PayloadDir 'Tesseract'
if (-not (Test-Path -LiteralPath (Join-Path $payloadTess 'tesseract.exe'))) {
    Write-Warn 'Tesseract missing from the publish output - copying it manually.'
    Copy-Item -LiteralPath $TessSource -Destination $PayloadDir -Recurse -Force
}

foreach ($required in @(
    'ScreenTextCopy.exe',
    'Localization\en.json',
    'Localization\fa.json',
    'Tesseract\tesseract.exe',
    'Tesseract\tessdata\eng.traineddata',
    'Tesseract\tessdata\fas.traineddata'
)) {
    $full = Join-Path $PayloadDir $required
    if (-not (Test-Path -LiteralPath $full)) { throw "Release payload is incomplete: $required is missing." }
}
Write-Ok 'App, localization files, OCR engine and eng/fas language data all present'

# ---------------------------------------------------------------- 4. portable ---
Write-Step '4/5  Creating the portable zip'

$zipPath = Join-Path $ReleaseDir "ScreenTextCopy-$Version-win-x64-portable.zip"
Compress-Archive -Path (Join-Path $PayloadDir '*') -DestinationPath $zipPath -CompressionLevel Optimal
Write-Ok "$(Split-Path -Leaf $zipPath)  ($(Get-SizeMb $zipPath) MB)"

# --------------------------------------------------------------- 5. installer ---
Write-Step '5/5  Building the one-click installer'

function Find-Iscc {
    $cmd = Get-Command 'ISCC.exe' -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    foreach ($candidate in @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
    )) {
        if ($candidate -and (Test-Path -LiteralPath $candidate)) { return $candidate }
    }
    return $null
}

if ($SkipInstaller) {
    Write-Warn 'Skipped on request (-SkipInstaller).'
} else {
    $iscc = Find-Iscc
    if (-not $iscc) {
        Write-Warn 'Inno Setup 6 was not found, so only the portable zip was built.'
        Write-Warn 'Install it once with:   winget install --id JRSoftware.InnoSetup'
        Write-Warn 'then re-run this script to get the installer as well.'
    } else {
        & $iscc "/DAppVersion=$Version" "/DPayloadDir=$PayloadDir" $IssFile
        if ($LASTEXITCODE -ne 0) { throw "Inno Setup failed with exit code $LASTEXITCODE." }

        $setup = Join-Path $ReleaseDir "ScreenTextCopy-Setup-$Version-win-x64.exe"
        if (-not (Test-Path -LiteralPath $setup)) { throw "Inno Setup reported success but $setup is missing." }
        Write-Ok "$(Split-Path -Leaf $setup)  ($(Get-SizeMb $setup) MB)"
    }
}

# ------------------------------------------------------------------ checksums ---
Write-Step 'Writing SHA256SUMS.txt'

$lines = Get-ChildItem -LiteralPath $ReleaseDir -File |
    Where-Object { $_.Extension -in '.exe', '.zip' } |
    ForEach-Object { "$((Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLower())  $($_.Name)" }
$lines | Set-Content -LiteralPath (Join-Path $ReleaseDir 'SHA256SUMS.txt') -Encoding ascii
$lines | ForEach-Object { Write-Ok $_ }

Write-Host "`nDone. Upload everything in '$ReleaseDir' as GitHub release assets." -ForegroundColor Cyan
