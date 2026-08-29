# Building ScreenTextCopy

This page covers building the app from source, producing the release artifacts, and how the installer is put together.

## Requirements

| Tool | Needed for | Install |
|---|---|---|
| [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) | Everything | Installer from Microsoft |
| [Inno Setup 6](https://jrsoftware.org/isdl.php) | The installer only | `winget install --id JRSoftware.InnoSetup` |
| Windows x64 | Everything | The project targets `net8.0-windows`, x64 only |

## One-time setup: fetch Tesseract

The OCR engine is **not** in git, on purpose: `libtesseract-5.dll` alone is about
106 MB, which is over GitHub's hard 100 MB per-file limit. Committing it would make
the repository unclonable for many people.

```powershell
git clone https://github.com/rezakazemifathi/ScreenTextCopy.git
cd ScreenTextCopy
powershell -ExecutionPolicy Bypass -File scripts\fetch-tesseract.ps1
```

`scripts\fetch-tesseract.ps1`:

1. Looks for an existing Tesseract 5 installation (on `PATH` or in the usual
   locations).
2. Offers to install one with `winget install --id UB-Mannheim.TesseractOCR` if
   none is found.
3. Copies `tesseract.exe` and its native DLLs into
   `src\ScreenTextCopy\Tesseract`.
4. Downloads the `eng`, `fas` and `ara` models from `tessdata_fast` if they are
   missing.

The finished bundle is roughly 200 MB. Released builds already contain all of it,
so end users never run this script.

## Development loop

```powershell
dotnet run --project src\ScreenTextCopy\ScreenTextCopy.csproj
```

```powershell
dotnet build ScreenTextCopy.sln -c Debug
dotnet build ScreenTextCopy.sln -c Release
```

## Producing the release artifacts

```powershell
powershell -ExecutionPolicy Bypass -File scripts\build-release.ps1 -Version 2.0.0
```

The script runs five steps in order:

1. **Environment check** — verifies the .NET SDK is available and that the
   Tesseract bundle is in place, failing early with the `fetch-tesseract.ps1`
   command if it is not.
2. **Publish** — a self-contained `win-x64` build into `release\app`.
3. **Payload verification** — confirms the exe, the localization JSON files, the
   OCR engine and the `eng`/`fas` language data all made it into the output.
4. **Portable zip.**
5. **Installer** — invokes Inno Setup, if it can be found.

Finally it writes `SHA256SUMS.txt` covering every `.exe` and `.zip` in `release\`.

| Output in `release\` | What it is |
|---|---|
| `app\` | The published, self-contained application |
| `ScreenTextCopy-<ver>-win-x64-portable.zip` | Unzip-and-run build |
| `ScreenTextCopy-Setup-<ver>-win-x64.exe` | The one-click installer |
| `SHA256SUMS.txt` | Checksums for the above |

Use `-SkipInstaller` to build only the app and the portable zip, which is useful
when Inno Setup is not installed.

## Why the publish flags are what they are

| Flag | Reason |
|---|---|
| `--self-contained true` | Embeds the .NET 8 runtime, so end users need no prerequisites at all. |
| `PublishSingleFile=false` | A single-file WPF bundle has to extract its native libraries on first launch, which makes startup noticeably slower. |
| `PublishTrimmed=false` | WPF resolves types from XAML through reflection, so trimming silently breaks data binding at runtime rather than failing at build time. |

## How the installer is designed

`installer\ScreenTextCopy.iss` (Inno Setup 6) makes a few deliberate choices:

| Setting | Why |
|---|---|
| `AppId={{7C4B1F42-9E3A-4D58-9F21-5B6A0C7E31D4}` | A stable AppId is what lets a new version upgrade the old one in place instead of installing side by side. Never change it between releases. |
| `PrivilegesRequired=lowest` with `DefaultDirName={localappdata}\Programs\ScreenTextCopy` | The app downloads OCR language packs into `<app>\Tesseract\tessdata`. Inside `Program Files` that folder is read-only for a normal process, so installing a language would fail with access-denied. A per-user location keeps it writable and needs no UAC prompt. |
| `ArchitecturesAllowed=x64compatible` | The app is x64-only: WPF plus 64-bit native Tesseract DLLs. |
| `MinVersion=10.0` | Windows 10 or newer. |
| `CloseApplications=yes` | A running instance locks its own EXE; without this an upgrade fails halfway through the file copy. |
| `Compression=lzma2/max`, `SolidCompression=yes` | Keeps a ~200 MB payload as small as practical. |

The uninstaller removes the whole `tessdata` folder explicitly, because language
packs downloaded after installation are not in its file list. It then asks whether
to delete `%AppData%\ScreenTextCopy` (settings, API key, downloaded packs), with
**No** as the default so reinstalling stays painless.

## Cutting a release

1. Bump `<Version>` in `src\ScreenTextCopy\ScreenTextCopy.csproj`.
2. Add the release notes to `CHANGELOG.md`.
3. Build the artifacts:

   ```powershell
   powershell -ExecutionPolicy Bypass -File scripts\build-release.ps1 -Version 2.0.0
   ```

4. Tag and push:

   ```powershell
   git tag v2.0.0
   git push origin v2.0.0
   ```

   The tag triggers `.github\workflows\release.yml`.
5. Attach the contents of `release\` to the GitHub release.

The installer is about 200 MB, which is far above git's comfortable range but well
within the 2 GB per-asset limit for GitHub release assets. That is exactly why the
binaries belong in Releases and not in the repository.

## See also

- [architecture.md](architecture.md) — how the pieces fit together
- [source-map.md](source-map.md) — what lives in which file
- [development.md](development.md) — conventions and workflow
- [INSTALL.md](INSTALL.md) · [USAGE.md](USAGE.md) · [TROUBLESHOOTING.md](TROUBLESHOOTING.md)
- [../README.md](../README.md) — project overview
