<#
.SYNOPSIS
    Puts the Tesseract 5 OCR engine into src\ScreenTextCopy\Tesseract so the
    project can be built and run.

.DESCRIPTION
    The engine is a ~200 MB set of native binaries and one of its files
    (libtesseract-5.dll) is larger than GitHub's 100 MB per-file limit, so it is
    deliberately NOT committed to this repository. Run this script once after
    cloning; released builds already contain it.

    The script, in order:
      1. looks for an existing Tesseract 5 installation,
      2. offers to install one with winget if none is found,
      3. copies the engine + its DLLs into src\ScreenTextCopy\Tesseract,
      4. downloads the eng / fas / ara language data if it is missing.

.PARAMETER Languages
    Language data to make sure is present. Defaults to English, Persian, Arabic.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\fetch-tesseract.ps1
#>
[CmdletBinding()]
param(
    [string[]] $Languages = @('eng', 'fas', 'ara')
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$RepoRoot = Split-Path -Parent $PSScriptRoot
$Target   = Join-Path $RepoRoot 'src\ScreenTextCopy\Tesseract'
$TessData = Join-Path $Target 'tessdata'

function Write-Step { param([string] $T) Write-Host "`n==> $T" -ForegroundColor Cyan }
function Write-Ok   { param([string] $T) Write-Host "    $T" -ForegroundColor Green }
function Write-Warn { param([string] $T) Write-Host "    $T" -ForegroundColor Yellow }

function Find-TesseractDir {
    # An existing bundle in the repo wins: nothing to do.
    if (Test-Path -LiteralPath (Join-Path $Target 'tesseract.exe')) { return $Target }

    $candidates = @(
        "$env:ProgramFiles\Tesseract-OCR",
        "${env:ProgramFiles(x86)}\Tesseract-OCR",
        "$env:LOCALAPPDATA\Programs\Tesseract-OCR",
        "$env:LOCALAPPDATA\Tesseract-OCR"
    )

    $onPath = Get-Command 'tesseract.exe' -ErrorAction SilentlyContinue
    if ($onPath) { $candidates = @((Split-Path -Parent $onPath.Source)) + $candidates }

    foreach ($dir in $candidates) {
        if ($dir -and (Test-Path -LiteralPath (Join-Path $dir 'tesseract.exe'))) { return $dir }
    }
    return $null
}

Write-Step '1/3  Locating a Tesseract 5 installation'
$source = Find-TesseractDir

if (-not $source) {
    Write-Warn 'No Tesseract installation found on this machine.'
    if (-not (Get-Command winget -ErrorAction SilentlyContinue)) {
        throw @"
Tesseract is missing and winget is unavailable, so it cannot be installed
automatically. Install it manually from
    https://github.com/UB-Mannheim/tesseract/wiki
and re-run this script.
"@
    }
    Write-Step 'Installing Tesseract with winget (this downloads ~70 MB)'
    & winget install --id UB-Mannheim.TesseractOCR --accept-package-agreements --accept-source-agreements
    $source = Find-TesseractDir
    if (-not $source) {
        throw 'Tesseract still cannot be found after the winget install. Install it manually and re-run this script.'
    }
}
Write-Ok "Using: $source"

Write-Step '2/3  Copying the engine into src\ScreenTextCopy\Tesseract'

New-Item -ItemType Directory -Force -Path $TessData | Out-Null

if ($source -ne $Target) {
    # tesseract.exe plus every native DLL it loads. Sub-tools and the bundled
    # tessdata are skipped; the language data is fetched in step 3 instead so the
    # repo always gets the small, fast "tessdata_fast" models.
    Get-ChildItem -LiteralPath $source -File |
        Where-Object { $_.Extension -in '.exe', '.dll' } |
        Copy-Item -Destination $Target -Force
    Write-Ok "Copied $((Get-ChildItem -LiteralPath $Target -File).Count) engine files"
} else {
    Write-Ok 'Engine already in place - nothing to copy'
}

if (-not (Test-Path -LiteralPath (Join-Path $Target 'tesseract.exe'))) {
    throw "Copy finished but tesseract.exe is missing from $Target."
}

Write-Step '3/3  Making sure the language data is present'

# tessdata_fast: the small LSTM models. Served over HTTPS from the official
# tesseract-ocr organisation.
$baseUrl = 'https://github.com/tesseract-ocr/tessdata_fast/raw/main'

foreach ($lang in $Languages) {
    if ($lang -notmatch '^[a-z]{3}(_[a-z]+)?$') {
        throw "Refusing to download '$lang': language codes must look like 'eng' or 'chi_sim'."
    }
    $file = Join-Path $TessData "$lang.traineddata"
    if (Test-Path -LiteralPath $file) {
        Write-Ok "$lang.traineddata already present"
        continue
    }
    Write-Host "    downloading $lang.traineddata ..."
    Invoke-WebRequest -Uri "$baseUrl/$lang.traineddata" -OutFile $file -UseBasicParsing
    Write-Ok "$lang.traineddata downloaded"
}

$totalMb = [math]::Round((Get-ChildItem -LiteralPath $Target -Recurse -File |
    Measure-Object -Property Length -Sum).Sum / 1MB, 1)

Write-Host "`nTesseract is ready ($totalMb MB) at:" -ForegroundColor Cyan
Write-Host "  $Target"
Write-Host "You can now run:  dotnet run --project src\ScreenTextCopy\ScreenTextCopy.csproj"
