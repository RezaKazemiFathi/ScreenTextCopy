# Installing ScreenTextCopy

This page covers downloading, installing, verifying, upgrading and removing ScreenTextCopy 2.0.0 on Windows.

## Requirements

| | |
|---|---|
| Operating system | Windows 10 (1809 or newer) or Windows 11 |
| Architecture | x64 only |
| Prerequisites | **None** |
| Administrator rights | Not required |

There is genuinely nothing to install first. The .NET 8 runtime is embedded in the
build (self-contained publish), and the Tesseract 5 OCR engine with English,
Persian and Arabic language data ships inside the package. No .NET install, no
Tesseract install, no Visual C++ redistributable.

## Which download do I want?

Both are on the [latest release page](https://github.com/rezakazemifathi/ScreenTextCopy/releases/latest).

| Asset | Choose it when |
|---|---|
| `ScreenTextCopy-Setup-<version>-win-x64.exe` | You want Start Menu and desktop shortcuts, an entry in *Installed apps*, and in-place upgrades. Recommended. |
| `ScreenTextCopy-<version>-win-x64-portable.zip` | You cannot or do not want to install anything, or you want to run it from a USB stick. |
| `SHA256SUMS.txt` | Always — it lets you verify whichever file you downloaded. |

## Option A — the installer

1. Download `ScreenTextCopy-Setup-<version>-win-x64.exe`.
2. Run it. If Windows SmartScreen appears, see [SmartScreen](#smartscreen-unknown-publisher) below.
3. Accept the license and continue through the wizard.
4. Pick your options:

   | Option | Default |
   |---|---|
   | Create a desktop shortcut | Checked |
   | Start ScreenTextCopy automatically when Windows starts | Unchecked |
   | Launch ScreenTextCopy when setup finishes | Offered on the last page |

   A Start Menu entry and an uninstaller are always created.
5. Finish. Press `Ctrl + Shift + X` to make your first capture.

### Where it installs, and why there is no UAC prompt

The installer is **per-user** and writes to:

```text
%LocalAppData%\Programs\ScreenTextCopy
```

That is a deliberate design choice, not a shortcut. The app stores OCR language
packs that you download on demand next to its own executable, in
`<app>\Tesseract\tessdata`. Inside `Program Files` that folder is read-only for a
normal process, so installing a new language from Settings would fail with
access-denied unless the whole app ran elevated. A per-user location keeps the
folder writable and means no administrator password is ever needed.

## Option B — portable

1. Download `ScreenTextCopy-<version>-win-x64-portable.zip`.
2. Extract it anywhere you like, including a USB drive.
3. Run `ScreenTextCopy.exe`.

Your settings still live in `%AppData%\ScreenTextCopy` on the machine you run it
on, so a portable copy does not carry its configuration between computers.

## SmartScreen: "unknown publisher"

The release build is **not code-signed**, so Windows may show *"Windows protected
your PC"* or warn that the publisher is unknown. This is expected for an unsigned
build and is not a statement about the file's contents.

To continue: click **More info**, then **Run anyway**.

The real integrity check is the checksum. Compare the hash of your download with
the matching line in `SHA256SUMS.txt`:

```powershell
Get-FileHash .\ScreenTextCopy-Setup-2.0.0-win-x64.exe -Algorithm SHA256
```

If the hash matches, the file is byte-for-byte what was published. If it does not,
delete it and download again.

## Upgrading

1. Download the newer installer.
2. Run it over your existing installation.

The installer uses a stable AppId, so a new version upgrades in place instead of
installing side by side, and a running instance is closed automatically before the
files are replaced. Your settings, API key and downloaded language packs are
preserved.

## Uninstalling

1. Open Windows **Settings → Apps → Installed apps**.
2. Find **ScreenTextCopy** and choose **Uninstall**.
3. The uninstaller asks whether it should also delete
   `%AppData%\ScreenTextCopy` — your settings, saved API key and downloaded OCR
   language packs. The default answer is **No**, which keeps them for a future
   reinstall. Answer **Yes** only if you want them gone.

For a portable copy, delete the folder you extracted; remove
`%AppData%\ScreenTextCopy` too if you also want the settings gone.

## Files and folders

| Path | Contents |
|---|---|
| `%LocalAppData%\Programs\ScreenTextCopy` | The application (installer only) |
| `<app>\Tesseract\tessdata` | Bundled and downloaded OCR language packs |
| `%AppData%\ScreenTextCopy\settings.json` | All settings, including your API key |

Deleting `settings.json` resets everything to defaults; the app recreates it on
the next start.

## See also

- [USAGE.md](USAGE.md) — every feature and setting explained
- [TROUBLESHOOTING.md](TROUBLESHOOTING.md) — fixes for the errors people actually hit
- [BUILD.md](BUILD.md) — building the app and its installer from source
- [../README.md](../README.md) — project overview
