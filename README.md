<div align="center">

<img src="src/ScreenTextCopy/Assets/logo.png" alt="ScreenTextCopy" width="120" />

# ScreenTextCopy

**Grab text from anywhere on your Windows screen — then copy, translate, or send it to your phone.**

Error dialogs that won't let you select text. Text baked into an image. Subtitles in a video.
A PDF viewer that fights you. ScreenTextCopy reads all of it, locally.

[![Build](https://github.com/rezakazemifathi/ScreenTextCopy/actions/workflows/build.yml/badge.svg)](https://github.com/rezakazemifathi/ScreenTextCopy/actions/workflows/build.yml)
[![Release](https://img.shields.io/github/v/release/rezakazemifathi/ScreenTextCopy?display_name=tag&sort=semver)](https://github.com/rezakazemifathi/ScreenTextCopy/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/rezakazemifathi/ScreenTextCopy/total)](https://github.com/rezakazemifathi/ScreenTextCopy/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Windows-10%20%7C%2011%20x64-0078D6)](#requirements)

**English** · [فارسی](README.fa.md) · [العربية](README.ar.md)

### [⬇️ Download the installer](https://github.com/rezakazemifathi/ScreenTextCopy/releases/latest)

No prerequisites. No .NET install. No Tesseract install. One file, one click.

</div>

---

## Table of contents

- [Why it exists](#why-it-exists)
- [Features](#features)
- [Install](#install)
- [Quick start](#quick-start)
- [Translation providers](#translation-providers)
- [Network / proxy](#network--proxy)
- [Privacy](#privacy)
- [Requirements](#requirements)
- [Build from source](#build-from-source)
- [Documentation](#documentation)
- [Troubleshooting](#troubleshooting)
- [Contributing](#contributing)
- [License](#license)
- [Author & support](#author--support)

---

## Why it exists

Windows is full of text you cannot copy. An installer error you need to search for.
A scanned invoice. A screenshot a colleague sent you. A game's dialogue in a
language you don't read. The usual answer is "retype it by hand".

ScreenTextCopy replaces that with a keystroke: press `Ctrl + Shift + X`, drag a
box, and the text is already on your clipboard — recognised **on your own
machine**, with no screenshot ever leaving it.

## Features

| | |
|---|---|
| 🖱️ **Capture anything** | Drag a box over any part of any window, at any DPI, on any monitor. |
| 🔒 **Offline OCR** | Tesseract 5 ships inside the app. Screenshots never touch the network. |
| 📋 **Auto-copy** | The recognised text lands on the clipboard the moment OCR finishes. |
| 🌍 **14 OCR languages** | English, Persian, Arabic bundled; French, German, Spanish, Italian, Russian, Turkish, Chinese, Japanese, Korean, Hindi, Portuguese installable from Settings with a progress bar. |
| 🔤 **Mixed scripts** | Persian + English in one line is recognised without scrambling, with correct RTL/LTR handling. |
| 🈯 **Translate to 14 languages** | Free provider needs no key at all, or plug in **any** OpenAI-compatible endpoint. |
| 🎮 **In-place overlay mode** | `Ctrl + Shift + Z` translates a region into a floating popup pinned next to it — built for games, videos and subtitles. |
| 🔁 **Automatic model failover** | If one AI model times out, the next known model is tried automatically. |
| 🌐 **Proxy aware** | System proxy, forced-direct, or a manual `http` / `https` / `socks4` / `socks5` proxy — switchable at runtime. |
| 📱 **Send to phone** | A locally generated QR code. No account, no server, no upload. |
| ⌨️ **Rebindable global hotkeys** | Both shortcuts are configurable and work while the app is hidden in the tray. |
| 🎨 **Real dark & light themes** | Follows Windows by default, with proper contrast in both. |
| 🇬🇧 🇮🇷 **Bilingual UI** | Live English ⇄ Persian switching with full right-to-left layout and the bundled Vazirmatn font. |
| 🪟 **Tray resident** | Closing the window keeps it one keystroke away instead of quitting. |

## Install

### Recommended — the installer

1. Download **`ScreenTextCopy-Setup-<version>-win-x64.exe`** from the
   [latest release](https://github.com/rezakazemifathi/ScreenTextCopy/releases/latest).
2. Run it. Windows SmartScreen may warn that the publisher is unknown because the
   build is not code-signed — choose **More info → Run anyway**.
3. Finish the wizard. Optionally tick *desktop shortcut* and *start with Windows*.

There is **nothing else to install**. The .NET 8 runtime and the Tesseract OCR
engine are inside the package. No administrator password is required: the app is
installed per-user under `%LocalAppData%\Programs\ScreenTextCopy`.

### Alternative — portable

Download `ScreenTextCopy-<version>-win-x64-portable.zip`, extract it anywhere
(including a USB stick), and run `ScreenTextCopy.exe`. Nothing is written outside
the folder except your settings in `%AppData%\ScreenTextCopy`.

> Verify your download against `SHA256SUMS.txt`:
> `Get-FileHash .\ScreenTextCopy-Setup-2.0.0-win-x64.exe -Algorithm SHA256`

Full, screenshot-by-screenshot instructions: **[docs/INSTALL.md](docs/INSTALL.md)**
· **[راهنمای فارسی](docs/INSTALL.fa.md)**

## Quick start

1. Press **`Ctrl + Shift + X`** — the screen dims and the cursor becomes a crosshair.
2. **Drag** a rectangle around the text.
3. **Release.** OCR runs locally; the text appears in the window and is already copied.
4. Optionally pick a target language and press **Translate**, or **Send to phone**.

For games and videos, press **`Ctrl + Shift + Z`** instead: the translation appears
in a small floating panel next to what you selected, with a **Retry** button.

Everything — theme, UI language, OCR languages, both hotkeys, translation
provider and proxy — lives in **Settings**.

More: **[docs/USAGE.md](docs/USAGE.md)** · **[راهنمای استفاده](docs/USAGE.fa.md)**

## Translation providers

| | Free | Custom AI |
|---|---|---|
| API key | not needed | yours |
| Backend | MyMemory | any OpenAI-compatible `/chat/completions` |
| Works with | — | OpenAI, OpenRouter, Groq, DeepSeek, Together, Azure OpenAI, Ollama, LM Studio, vLLM, … |
| Quality | fine for short text | far better, especially for idioms and long passages |
| Model picker | — | auto-discovered from the provider's `/models` route |

For the custom provider you enter a **base URL** (e.g. `https://api.openai.com/v1`),
an optional **API key**, and a **model**. Press **Test connection** and the app
reports reachability, latency, and how many models it found; the picker is then
filled in for you and your choice is remembered across restarts.

Leave **"automatically switch to another model if one times out"** on and a stalled
model no longer means a failed translation — the next known model is tried
instead. Authentication failures (401/403) never trigger failover, because retrying
a bad key on ten models just wastes ten requests.

> Your API key is stored **only** in `%AppData%\ScreenTextCopy\settings.json` on
> your own computer. It is never logged, never shown in error messages, and never
> sent anywhere except the endpoint you configured.

## Network / proxy

Many AI endpoints are unreachable from some regions, and a stale Windows system
proxy is the single most common cause of *"No connection could be made because the
target machine actively refused it"*. **Settings → Translation → Network** gives you
three explicit choices:

| Mode | What it does |
|---|---|
| **System proxy** (default) | Uses the Windows proxy, like your browser. |
| **Direct** | Ignores the system proxy entirely — the fix when a dead proxy is configured system-wide. |
| **Manual** | Routes through an address you type, e.g. `socks5://127.0.0.1:10808` (v2rayN/Xray default) or `http://127.0.0.1:10809`. |

The setting is re-read on every request, so it takes effect the moment you save —
no restart needed.

## Privacy

> What is visible on your screen stays on your machine.

- **OCR is 100 % local.** The temporary capture PNG is deleted in a `finally` block.
- **"Send to phone"** renders the QR code locally; nothing is uploaded.
- **No telemetry, no analytics, no accounts, no auto-update calls.**
- The **only** bytes that ever leave your computer are (a) text you explicitly
  translate, sent only to the provider you chose, and (b) OCR language packs you
  ask to install, fetched over HTTPS from the official
  [`tesseract-ocr/tessdata_fast`](https://github.com/tesseract-ocr/tessdata_fast) repository.

## Requirements

**To run:** Windows 10 (1809+) or Windows 11, 64-bit. Nothing else — the release
build carries its own .NET runtime and OCR engine.

**To build:** the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0),
plus [Inno Setup 6](https://jrsoftware.org/isdl.php) only if you want to produce
the installer.

## Build from source

```powershell
git clone https://github.com/rezakazemifathi/ScreenTextCopy.git
cd ScreenTextCopy

# One-time: put the Tesseract engine in place (see the note below).
powershell -ExecutionPolicy Bypass -File scripts\fetch-tesseract.ps1

dotnet run --project src\ScreenTextCopy\ScreenTextCopy.csproj
```

> **Why isn't Tesseract in the repository?** One of its binaries,
> `libtesseract-5.dll`, is ~106 MB — above GitHub's hard 100 MB per-file limit.
> Committing it would make the repo unclonable for many people. `fetch-tesseract.ps1`
> reuses an existing Tesseract install, offers to install one via `winget`, and
> downloads the language data. Released builds already contain everything.

### Producing the release artifacts

```powershell
powershell -ExecutionPolicy Bypass -File scripts\build-release.ps1 -Version 2.0.0
```

That writes into `release\`:

| File | What it is |
|---|---|
| `app\` | the published, self-contained application |
| `ScreenTextCopy-2.0.0-win-x64-portable.zip` | unzip-and-run build |
| `ScreenTextCopy-Setup-2.0.0-win-x64.exe` | the one-click installer |
| `SHA256SUMS.txt` | checksums for all of the above |

Details and design notes: **[docs/BUILD.md](docs/BUILD.md)**.

## Documentation

| Document | |
|---|---|
| [Install guide](docs/INSTALL.md) · [فارسی](docs/INSTALL.fa.md) | Step by step, from download to first capture |
| [Usage guide](docs/USAGE.md) · [فارسی](docs/USAGE.fa.md) | Every feature and setting explained |
| [Troubleshooting](docs/TROUBLESHOOTING.md) · [فارسی](docs/TROUBLESHOOTING.fa.md) | Concrete fixes for the errors people actually hit |
| [Build guide](docs/BUILD.md) | Building, publishing, packaging |
| [Architecture](docs/architecture.md) | How the pieces fit together |
| [Source map](docs/source-map.md) | What lives in which file |
| [Development](docs/development.md) | Conventions and workflow |
| [Publishing to GitHub](docs/PUBLISH-TO-GITHUB.fa.md) | Git/GitHub walkthrough (Persian) |
| [Changelog](CHANGELOG.md) | What changed, when |

## Troubleshooting

The three issues that account for most reports:

| Symptom | Fix |
|---|---|
| *"target machine actively refused it (127.0.0.1:10808)"* | A dead system proxy. **Settings → Network** → **Direct**, or **Manual** with your real proxy address. |
| Connection test passes but nothing translates | The source and target language are the same, so the text is returned unchanged. Pick a different target. |
| Hotkey does nothing | Another app already owns that combination. Rebind it in **Settings → Global shortcut → Change**. |

The full list, including emoji limitations and OCR accuracy tips, is in
**[docs/TROUBLESHOOTING.md](docs/TROUBLESHOOTING.md)**.

## Contributing

Issues and pull requests are welcome — see [CONTRIBUTING.md](CONTRIBUTING.md) for
the workflow and coding conventions, and [SECURITY.md](SECURITY.md) for reporting
vulnerabilities privately.

## License

[MIT](LICENSE). Third-party components keep their own licenses — Tesseract is
Apache-2.0, Vazirmatn is SIL OFL 1.1; see
[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

## Author & support

Built by **Reza Kazemi Fathi**.

[![GitHub](https://img.shields.io/badge/GitHub-rezakazemifathi-181717?logo=github)](https://github.com/rezakazemifathi)
[![Instagram](https://img.shields.io/badge/Instagram-rkfcode-E4405F?logo=instagram)](https://instagram.com/rkfcode)
[![YouTube](https://img.shields.io/badge/YouTube-rkfcode-FF0000?logo=youtube)](https://youtube.com/rkfcode)

If this saved you some typing, a ⭐ on the repository genuinely helps.
Support the project: [Daramet (IRR)](https://daramet.com/RKFi) ·
[Donatr.ee (USD / crypto)](https://donatr.ee/rkfcode/)


