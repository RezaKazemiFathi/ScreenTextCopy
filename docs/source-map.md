# Source map

Every file in `src/ScreenTextCopy` and what it is responsible for. Use this to find
the right place for a change before you start editing.

## Shell and composition

| File | Responsibility |
|---|---|
| `App.xaml` / `App.xaml.cs` | Composition root: manual DI via an `AppServices` aggregate, theme + locale bootstrap, the shared `HttpClient` (built once around `SettingsWebProxy`), single-instance and startup wiring |
| `MainWindow.xaml` / `.cs` | Main shell UI; system-tray icon and close-to-tray behaviour; registers both global hotkeys |

## Models

| File | Responsibility |
|---|---|
| `Models/AppSettings.cs` | Every persisted setting, plus `HotkeyConfig` (capture and overlay), theme / provider / network-mode enums |
| `Models/OcrResult.cs` | Immutable OCR result record: text, confidence, elapsed time, languages used |

## Services

| File | Responsibility |
|---|---|
| `Services/SettingsService.cs` | Loads and saves `%AppData%\ScreenTextCopy\settings.json`; never throws on a corrupt or missing file |
| `Services/LocalizationService.cs` | JSON-backed i18n with live switching and the matching `FlowDirection` |
| `Services/ThemeService.cs` | Runtime light/dark palette swap; follows the Windows theme |
| `Services/GlobalHotkeyService.cs` | Win32 `RegisterHotKey` wrapper; supports several independently rebindable chords |
| `Services/ScreenCaptureService.cs` | Captures a virtual-desktop region (all monitors, DPI-correct) |
| `Services/ImagePreprocessor.cs` | Upscale + grayscale + contrast pass that runs before OCR when *Enhance image* is on |
| `Services/IOcrEngine.cs` | OCR abstraction the UI depends on, so no view model knows about Tesseract |
| `Services/TesseractOcrEngine.cs` | Bundled Tesseract 5 CLI implementation; resolves `<app>\Tesseract\tessdata`, passes arguments via `ProcessStartInfo.ArgumentList` (no shell string, so no injection) |
| `Services/TextCleanup.cs` | Conservative normalisation of OCR output — never reverses right-to-left text |
| `Services/TextDirection.cs` | Picks a paragraph's base direction from the text itself (Unicode first-strong-character rule) and detects its language, so results are not scrambled by a Persian UI shell |
| `Services/LanguagePackService.cs` | Lists and downloads Tesseract language packs on demand over HTTPS, with a catalogue-code allowlist and path-traversal guard |
| `Services/ITranslationProvider.cs` | Translation abstraction |
| `Services/FreeTranslationProvider.cs` | MyMemory provider, no key, chunked requests |
| `Services/CustomAiTranslationProvider.cs` | Any OpenAI-compatible `/chat/completions` endpoint: model discovery via `/models`, per-model timeout, and model failover |
| `Services/TranslationService.cs` | Chooses the provider from settings and owns the supported-language list |
| `Services/SettingsWebProxy.cs` | `IWebProxy` that re-reads settings on every request, so a proxy change applies without restarting the app |
| `Services/QrCodeService.cs` | Local QR generation for *Send to phone* — nothing is uploaded |

## View models

| File | Responsibility |
|---|---|
| `ViewModels/MainViewModel.cs` | Capture → OCR → copy / translate / QR pipeline and result state |
| `ViewModels/SettingsViewModel.cs` | All settings state: live theme and language, pack install with progress, provider test-connection and model list |
| `ViewModels/TranslationOverlayViewModel.cs` | State for the floating in-place translation popup, including its retry command |
| `ViewModels/HotkeyEditor.cs` | Reusable editor for one rebindable chord; one instance per shortcut |

## Views, theming, localization

| File | Responsibility |
|---|---|
| `Views/SelectionOverlay.xaml` / `.cs` | Dimmed full-desktop selection overlay; returns a DPI-correct device-pixel region |
| `Views/SettingsWindow.xaml` / `.cs` | Settings dialog |
| `Views/TranslationOverlayWindow.xaml` / `.cs` | Small floating translation popup pinned next to the selection |
| `Views/QrWindow.xaml` / `.cs` | QR display window |
| `Themes/Palette.Light.xaml` / `Palette.Dark.xaml` | Semantic colour tokens per theme |
| `Themes/Controls.xaml` | Shared control styles, typography, radii, scrollbars |
| `Localization/en.json` / `fa.json` | UI strings; the two files must always have identical key sets |
| `Localization/LocExtension.cs` | The `{loc:Loc key}` XAML markup extension |
| `Converters/CommonConverters.cs` | Bool / visibility / enum value converters |

## Repository tooling

| File | Responsibility |
|---|---|
| `scripts/fetch-tesseract.ps1` | Downloads the Tesseract engine into `src/ScreenTextCopy/Tesseract` — required once after cloning, because that folder is not committed |
| `scripts/build-release.ps1` | Self-contained publish, portable zip, Inno Setup compile and `SHA256SUMS.txt` |
| `installer/ScreenTextCopy.iss` | Inno Setup 6 script for the per-user, zero-prerequisite installer |
| `.github/workflows/build.yml` | CI: build with warnings as errors, plus an `en.json`/`fa.json` key-parity gate |
| `.github/workflows/release.yml` | Tag-driven release: builds all three assets and publishes them |

## See also

- [architecture.md](architecture.md) — why the pieces are split this way
- [BUILD.md](BUILD.md) — building the app and installer from source
- [../CONTRIBUTING.md](../CONTRIBUTING.md) — conventions a change must follow
