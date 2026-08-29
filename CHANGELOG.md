# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

Nothing yet.

## [2.0.0] - 2026-08-28

### Added

- **Custom OpenAI-compatible translation provider** — enter a base URL, an optional
  API key and any model name, and translate through OpenAI, OpenRouter, Groq,
  DeepSeek, Together, Azure OpenAI, Ollama, LM Studio, vLLM or anything else that
  speaks the same `/chat/completions` shape.
- **Model discovery** from the provider's `/models` route, filling the model picker
  automatically; the chosen model is persisted across restarts.
- **Automatic model failover** — when a model times out, the next known model is
  tried instead of failing the translation. Authentication failures (401/403) are
  excluded, since retrying a bad key on every model only wastes requests. Each model
  gets a **20-second timeout**.
- **"Test connection"** button reporting reachability, latency, and how many models
  were found.
- **In-place overlay translation** on `Ctrl + Shift + Z` — the translation appears in
  a floating panel pinned next to the selected region, with a **Retry** button. Built
  for games, videos and subtitles.
- **Configurable network mode** — *System proxy*, *Direct*, or *Manual*. Manual
  accepts `http`, `https`, `socks4`, `socks4a` and `socks5`, and a bare `host:port`
  is treated as `http`. The mode is applied per request, so it takes effect on Save
  without restarting the app.
- **Rebindable global hotkeys** for both capture and overlay translation, working
  while the app sits in the tray.
- **System tray integration** with minimise-to-tray, so closing the window keeps the
  app one keystroke away instead of quitting.
- **Installable OCR language packs** with a per-row progress bar: French, German,
  Spanish, Italian, Russian, Turkish, Chinese Simplified, Japanese, Korean, Hindi and
  Portuguese.
- **QR "send to phone"** — the recognised text is turned into a locally generated QR
  code.
- **About section** with author and support links.
- **Bundled Vazirmatn font** for correct Persian and Arabic script rendering.
- **Light, dark and system themes.**
- **Live English/Persian UI switching** with a full right-to-left layout.
- **Self-contained installer and portable zip**, requiring no prerequisites — no
  .NET install, no Tesseract install.

### Changed

- OCR tuned for small text and for mixed Persian + English lines.
- Dark-theme contrast improved.
- The settings scrollbar was modernised, in both themes.
- The per-model AI timeout was lowered to **20 s** so failover triggers before the
  shared 30 s HTTP timeout cancels the whole request.

### Fixed

- Provider errors are now surfaced to the user instead of failing silently.
- The model picker commits the selected model, and keeps it after the list is
  refreshed.
- Selection coordinates are DPI-correct across multiple monitors.
- Persian RTL layout and LTR isolation, so results are no longer scrambled.
- Checkbox rendering in RTL layouts.
- "Send to phone" QR reliability.
- Emoji are now reported as unrecognisable instead of being emitted as garbage
  characters — Tesseract cannot read emoji.
- A settings crash caused by a missing resource.

[Unreleased]: https://github.com/rezakazemifathi/ScreenTextCopy/compare/v2.0.0...HEAD
[2.0.0]: https://github.com/rezakazemifathi/ScreenTextCopy/releases/tag/v2.0.0
