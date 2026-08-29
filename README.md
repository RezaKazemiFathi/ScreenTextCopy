<div align="center">

<img src="src/ScreenTextCopy/Assets/logo.png" alt="ScreenTextCopy" width="120" />

ScreenTextCopy

Grab text from anywhere on your Windows screen — then copy, translate, or send it to your phone.

Error dialogs that won't let you select text. Text baked into an image. Subtitles in a video.
A PDF viewer that fights you. ScreenTextCopy reads all of it, locally.








English · فارسی · العربية

<br>

<a href="https://github.com/rezakazemifathi/ScreenTextCopy/releases/latest">
  <img src="https://img.shields.io/badge/⬇️%20Download%20Latest%20Release-2EA44F?style=for-the-badge" alt="Download Latest Release" />
</a>
<a href="https://github.com/rezakazemifathi/ScreenTextCopy/releases">
  <img src="https://img.shields.io/badge/📦%20All%20Releases-0969DA?style=for-the-badge" alt="All Releases" />
</a>
<a href="docs/INSTALL.md">
  <img src="https://img.shields.io/badge/📖%20Installation%20Guide-6F42C1?style=for-the-badge" alt="Installation Guide" />
</a>

<br><br>

No prerequisites. No separate .NET install. No separate Tesseract install. One installer, one click.

🎬 See ScreenTextCopy in action

<img src="docs/assets/demo.gif" alt="ScreenTextCopy Demo" />

</div>

📑 Table of contents

Why it exists

Features

Install

Quick start

Translation providers

Network / proxy

Privacy

Requirements

Build from source

Documentation

Troubleshooting

Contributing

License

Author & support

✨ Why it exists

Windows is full of text you cannot copy.

An installer error you need to search for. A scanned invoice. A screenshot a colleague sent you. A game's dialogue in a language you don't read. Subtitles in a video. Text inside a PDF viewer.

The usual answer is:

Retype it by hand.

ScreenTextCopy replaces that with a single shortcut:

Press Ctrl + Shift + X → select an area → the text is recognized and copied to your clipboard.

OCR runs locally on your computer, so the screen capture itself does not need to leave your device.

🚀 Features





🖱️ Capture anything

Drag a box over any part of any window, at any DPI, on any monitor.

🔒 Offline OCR

Tesseract 5 ships inside the app. Screenshots never touch the network during OCR.

📋 Auto-copy

The recognized text lands on the clipboard the moment OCR finishes.

🌍 14 OCR languages

English, Persian, Arabic bundled; French, German, Spanish, Italian, Russian, Turkish, Chinese, Japanese, Korean, Hindi, Portuguese installable from Settings with a progress bar.

🔤 Mixed scripts

Persian + English in one line is recognized with correct RTL/LTR handling.

🈯 Translate to 14 languages

Use the free provider without an API key, or connect any OpenAI-compatible endpoint.

🎮 In-place overlay mode

Ctrl + Shift + Z translates a region into a floating popup pinned next to it — built for games, videos and subtitles.

🔁 Automatic model failover

If one AI model times out, the next known model is tried automatically.

🌐 Proxy aware

System proxy, forced-direct, or manual http / https / socks4 / socks5 proxy — switchable at runtime.

📱 Send to phone

A locally generated QR code. No account, no server, no upload.

⌨️ Rebindable global hotkeys

Both shortcuts are configurable and work while the app is hidden in the tray.

🎨 Real dark & light themes

Follows Windows by default, with proper contrast in both.

🇬🇧 🇮🇷 Bilingual UI

Live English ⇄ Persian switching with full right-to-left layout and the bundled Vazirmatn font.

🪟 Tray resident

Closing the window keeps it one keystroke away instead of quitting.

📥 Install

Recommended — Windows installer

Open the latest release.

Download ScreenTextCopy-Setup-<version>-win-x64.exe.

Run the installer.

If Windows SmartScreen appears because the application is not code-signed, select More info → Run anyway.

Finish the installation wizard.

There is nothing else to install. The .NET 8 runtime and the Tesseract OCR engine are included in the release package.

The application is installed per-user under:

%LocalAppData%\Programs\ScreenTextCopy

Alternative — portable version

Download:

ScreenTextCopy-<version>-win-x64-portable.zip

Extract it anywhere — including a USB drive — and run:

ScreenTextCopy.exe

Nothing is written outside the portable folder except your settings under:

%AppData%\ScreenTextCopy

🔐 Verify the download

The release also contains SHA256SUMS.txt.

Example:

Get-FileHash .\ScreenTextCopy-Setup-2.0.0-win-x64.exe -Algorithm SHA256

Then compare the resulting SHA256 hash with the value in SHA256SUMS.txt.

For the complete installation walkthrough:

📖 Installation guide · راهنمای فارسی

⚡ Quick start

1. Capture and copy text

Press:

Ctrl + Shift + X

The screen dims and the cursor becomes a crosshair.

Drag a rectangle around the text and release.

OCR runs locally. The recognized text appears in the application and is automatically copied to the clipboard.

2. Translate

After OCR finishes, choose a target language and press:

Translate

3. Send text to your phone

Choose:

Send to phone

ScreenTextCopy generates a QR code locally. Scan it with your phone to transfer the recognized text.

4. Translate directly over games and videos

For games, videos and subtitles, press:

Ctrl + Shift + Z

The translation appears in a small floating panel next to the selected region, with a Retry option.

5. Customize everything from Settings

Settings contains the controls for:

Theme

UI language

OCR languages

Global hotkeys

Translation provider

AI model

Network / proxy

Other application options

More:

📖 Usage guide · راهنمای استفاده

🌐 Translation providers



Free

Custom AI

API key

Not needed

Yours

Backend

MyMemory

Any OpenAI-compatible /chat/completions

Works with

—

OpenAI, OpenRouter, Groq, DeepSeek, Together, Azure OpenAI, Ollama, LM Studio, vLLM, …

Quality

Fine for short text

Better for idioms and longer passages

Model picker

—

Auto-discovered from the provider's /models route

Custom AI provider

Enter:

Base URL — for example https://api.openai.com/v1

API key — optional depending on the provider

Model

Then press:

Test connection

The application reports reachability, latency and the number of models it found. The model picker is then populated automatically and your selection is remembered across restarts.

Automatic model failover

If automatically switch to another model if one times out is enabled, a stalled model does not necessarily mean a failed translation.

The application can try the next known model automatically.

Authentication errors such as 401/403 are not treated as transient timeouts, so an invalid API key is not repeatedly retried across every model.

API key storage: Your API key is stored in %AppData%\ScreenTextCopy\settings.json on your own computer. It is not intentionally logged or displayed in error messages and is sent only to the endpoint you configure.

🌐 Network / proxy

Some AI endpoints may be unreachable depending on your network configuration.

The most common local error is:

No connection could be made because the target machine actively refused it

Open:

Settings → Translation → Network

Available modes:

Mode

What it does

System proxy

Uses the Windows system proxy, like your browser.

Direct

Ignores the system proxy entirely. Useful when a stale proxy is configured system-wide.

Manual

Routes traffic through an address you specify.

Examples:

socks5://127.0.0.1:10808

http://127.0.0.1:10809

The network setting is re-read for subsequent requests, so changing it does not require an application restart.

🔒 Privacy

What is visible on your screen stays on your machine.

OCR is 100% local. Screen captures used for OCR are processed on your computer.

Temporary capture data is cleaned up after OCR processing.

Send to phone is local. The QR code is generated locally; the recognized text is not uploaded to a remote QR service.

No account is required.

No telemetry or analytics are intentionally included.

When you use translation, the text you explicitly submit is sent to the provider you selected.

Additional OCR language packs may be downloaded when you choose to install them.

For the exact implementation and third-party components, see the source code and:

THIRD-PARTY-NOTICES.md

💻 Requirements

To run

Windows 10 (1809+) or Windows 11

64-bit Windows

No separate .NET runtime required for the published self-contained release

No separate Tesseract installation required for the release package

To build

.NET 8 SDK

Inno Setup 6 — only when producing the Windows installer

🛠️ Build from source

Clone the repository:

git clone https://github.com/rezakazemifathi/ScreenTextCopy.git
cd ScreenTextCopy

Prepare the Tesseract engine:

powershell -ExecutionPolicy Bypass -File scripts\fetch-tesseract.ps1

Run the application:

dotnet run --project src\ScreenTextCopy\ScreenTextCopy.csproj

Why isn't Tesseract in the repository?

One of its binaries, libtesseract-5.dll, is approximately 106 MB — above GitHub's hard 100 MB per-file limit.

Committing it would make the repository difficult or impossible to clone normally.

scripts\fetch-tesseract.ps1 handles the development setup and downloads the required language data.

Released builds already contain the required runtime and OCR components.

📦 Producing release artifacts

Run:

powershell -ExecutionPolicy Bypass -File scripts\build-release.ps1 -Version 2.0.0

The script creates:

release\
├── app\
├── ScreenTextCopy-2.0.0-win-x64-portable.zip
├── ScreenTextCopy-Setup-2.0.0-win-x64.exe
└── SHA256SUMS.txt

File

Purpose

app\

Published self-contained application

ScreenTextCopy-2.0.0-win-x64-portable.zip

Unzip-and-run portable build

ScreenTextCopy-Setup-2.0.0-win-x64.exe

One-click Windows installer

SHA256SUMS.txt

SHA256 checksums

For build and packaging details:

📦 Build guide

🚀 Publishing a GitHub Release

The Download Latest Release button and release badges become meaningful after at least one published GitHub Release exists.

For version 2.0.0:

Push the repository to GitHub.

Build the release artifacts.

Open the repository's Releases page.

Click Draft a new release.

Create/select the tag:

v2.0.0

Set the release title:

ScreenTextCopy v2.0.0

Upload these files from release\:

ScreenTextCopy-Setup-2.0.0-win-x64.exe
ScreenTextCopy-2.0.0-win-x64-portable.zip
SHA256SUMS.txt

Click Publish release.

Important

Do not leave the release as a draft.

A draft release is not available to normal visitors through:

/releases/latest

Once the release is published, the README's Download Latest Release button will automatically point to the latest published release.

📚 Documentation

Document

Description

Install guide · فارسی

Step-by-step installation and first capture

Usage guide · فارسی

Features, shortcuts and settings

Troubleshooting · فارسی

Common errors and practical fixes

Build guide

Building, publishing and packaging

Architecture

Project architecture

Source map

Project file map

Development

Development workflow

Publishing guide

Persian GitHub publishing walkthrough

Changelog

Version history

🧩 Troubleshooting

target machine actively refused it (127.0.0.1:10808)

A local proxy may be unavailable.

Go to:

Settings → Translation → Network

Then try Direct, or configure your actual proxy address under Manual.

Translation returns the original text

Check that the source and target languages are different.

Hotkey does nothing

Another application may already be using the shortcut.

Change it from:

Settings → Global shortcut → Change

For more issues, see:

🛠️ Troubleshooting guide

🤝 Contributing

Issues and pull requests are welcome.

Before contributing, please read:

CONTRIBUTING.md

SECURITY.md

📄 License

This project is licensed under the MIT License.

Third-party components retain their respective licenses.

See:

THIRD-PARTY-NOTICES.md

👨‍💻 Author & support

Built by Reza Kazemi Fathi.





If ScreenTextCopy saves you time, consider giving the repository a ⭐.

Support the project:

Daramet (IRR) · Donatr.ee (USD / crypto)

<div align="center">

ScreenTextCopy — Copy what Windows won't let you copy.

⭐ Star the repository if you find it useful.
