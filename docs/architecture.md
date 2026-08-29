# Architecture

How ScreenTextCopy 2.0 is put together, and why each decision was made that way.
For a file-by-file index see [source-map.md](source-map.md).

## Shape of the app

```text
hotkey  →  selection overlay  →  capture  →  preprocess  →  OCR  →  cleanup
                                                                      ↓
                                              clipboard / result panel / QR
                                                                      ↓
                                                     translation (optional)
```

There is no background service, no database and no server. A single WPF process
owns everything, and every long step is `async` so the UI thread never blocks.

## Composition root

`App.xaml.cs` builds every service once, by hand, into an `AppServices` aggregate
and passes it to the view models. There is no DI container: the graph is small,
fully known at compile time, and a container would add a dependency and a layer of
indirection without removing any real work.

Two things are deliberately built exactly once:

- **One `HttpClient`**, because a per-request client exhausts sockets.
- **One `SettingsWebProxy`** wrapped inside it, so the proxy mode can change
  without rebuilding the client.

## MVVM boundary

View models own all behaviour; code-behind is limited to things that are genuinely
view concerns (window placement, mouse capture on the overlay, tray icon
plumbing). This is what keeps the OCR and translation pipelines testable in
isolation and is enforced in review — see [../CONTRIBUTING.md](../CONTRIBUTING.md).

## 1. Hotkeys

`GlobalHotkeyService` registers chords with the Win32 `RegisterHotKey` API:
`Ctrl+Shift+X` for capture and `Ctrl+Shift+Z` for overlay translation, both
rebindable.

Win32 rather than a WPF-level hook because it works globally, needs no window
focus, requires no keyboard hook (which antivirus software distrusts and which
would see every keystroke), and is a stable OS primitive.

Windows grants a chord to whoever registers it first, so registration failure is a
normal outcome, not an error: the app reports the conflict and the user rebinds.
Re-registration happens live when a shortcut changes — no restart.

## 2. Selection and capture

`ScreenCaptureService` captures the Windows *virtual desktop*, so a selection can
span monitors. `SelectionOverlay` covers that whole virtual desktop, dims it, and
returns the chosen rectangle in **device pixels**.

Device pixels matter: WPF works in device-independent units, and on a 125% or 150%
display a rectangle passed through unscaled crops the wrong area — the class of bug
where the OCR result is the text slightly above what you selected.

## 3. OCR

The UI depends on `IOcrEngine`, never on Tesseract directly, so a future engine
(Windows OCR, PaddleOCR) can be added without touching a view model.

`TesseractOcrEngine` drives the bundled `tesseract.exe`:

- Arguments go through `ProcessStartInfo.ArgumentList`, which escapes each one
  individually. Nothing is concatenated into a command line, so a path or language
  code can never inject an extra argument.
- Language data is resolved next to the executable, at
  `<app>\Tesseract\tessdata`. This single fact drives the whole packaging design
  (see below).
- The screenshot lives in memory; a temporary PNG exists only because the
  Tesseract CLI takes a file, and it is deleted in a `finally` block.

`ImagePreprocessor` (upscale → grayscale → contrast) runs first when *Enhance image
before recognition* is on. It is a setting rather than an unconditional pipeline
because screenshots vary enormously and aggressive preprocessing makes some inputs
worse, not better.

Selecting many OCR languages at once lowers accuracy as well as speed — the engine
gets more ways to be wrong — so the UI encourages selecting only what is on screen
instead of enabling everything.

## 4. Bidirectional text

Tesseract returns text in **logical order**, which is already correct. The
scrambling users report comes from *rendering*: a logically-correct Persian string
shown in a control whose base flow direction is left-to-right, or a
logically-correct English string shown inside a right-to-left Persian shell.

So the app never reverses strings. `TextDirection` decides each paragraph's base
direction from the text itself, using the Unicode first-strong-character rule that
browsers and word processors use, and the result panel is rendered with that
direction rather than inheriting the UI's. URLs, file paths, GUIDs, error codes,
version numbers and hex values therefore survive intact.

`TextCleanup` stays conservative for the same reason: it normalises whitespace and
obvious artefacts and leaves the semantics of the text alone, because the user can
see the original on screen and edit the result.

Emoji are not recognised at all — Tesseract has no model for them. The app says so
with a toast instead of emitting plausible-looking garbage.

## 5. Translation

`TranslationService` picks a provider from settings behind `ITranslationProvider`:

| Provider | Notes |
|---|---|
| `FreeTranslationProvider` | MyMemory, no key, requests chunked to respect its length limit |
| `CustomAiTranslationProvider` | Any OpenAI-compatible `/chat/completions` endpoint |

The custom provider is intentionally generic rather than vendor-specific: OpenAI,
OpenRouter, Groq, DeepSeek, Together, Azure OpenAI, Ollama, LM Studio and vLLM all
speak the same shape, so one implementation covers hosted and fully local setups —
and a local server means translation without anything leaving the machine.

Models are discovered from the provider's `/models` route and cached in settings, so
the picker keeps working without probing again. A missing `/models` route is not an
error: the box is editable and the user types the model name.

**Failover.** Each model gets a 20-second timeout. On a timeout or a
model-specific error the request is retried against the other known models until
one succeeds. The number of attempts is bounded, so an endpoint that is simply
unreachable cannot hang the app indefinitely. Authentication failures (401/403) are
excluded on purpose — a rejected key is rejected by every model, so retrying only
wastes requests and time.

## 6. Networking and proxy

Users in restricted regions are a first-class case, not an afterthought.
`SettingsWebProxy` implements `IWebProxy` and re-reads the current settings on
**every** request, which is what allows *System proxy* / *Direct* / *Manual* to
switch instantly, with no restart and no `HttpClient` rebuild.

The same mode governs OCR language-pack downloads, so one setting fixes both. A
stale system proxy pointing at a VPN port that is not listening — the "target
machine actively refused it" error — is fixed by choosing *Direct*.

Manual mode accepts `http`, `https`, `socks4`, `socks4a` and `socks5`.

## 7. Privacy and security

- **OCR is entirely local.** No screenshot and no recognised text is ever
  uploaded, and the temporary PNG is deleted immediately.
- **Network access is limited and always the user's choice:** translation when you
  ask for it, and language-pack downloads when you install a language. Nothing
  else. There is no telemetry, no analytics and no update ping.
- **QR codes are generated locally.** *Send to phone* involves no service.
- **The API key never leaves the machine** except as the `Authorization` header of
  the endpoint the user configured. It is stored in
  `%AppData%\ScreenTextCopy\settings.json`, is never logged, and is never included
  in an error message.
- **Language-pack downloads are constrained:** HTTPS only, from the official
  `tesseract-ocr/tessdata_fast` repository, with an allowlist of catalogue codes
  and a path-traversal guard on the destination filename.
- **Outbound links** are validated as absolute `http`/`https` URLs before being
  handed to the shell, so a stray tag cannot launch an arbitrary process.

## 8. Localization and theming

`LocalizationService` reads flat JSON key/value files and swaps them live; the
`{loc:Loc key}` markup extension resolves each string at render time, so switching
language does not require a restart. A missing key renders as the key itself, which
is ugly on purpose — it is immediately visible in review, and CI fails when
`en.json` and `fa.json` do not have identical key sets.

Persian gets a genuine right-to-left layout, with the Vazirmatn font bundled so it
looks the same on a machine that has never had a Persian font installed.

`ThemeService` swaps `Palette.Light.xaml` for `Palette.Dark.xaml` at runtime.
Controls reference *semantic* tokens (surface, border, accent, muted text) rather
than raw colours, so a palette change needs no edit to any control style.

## 9. Packaging

Three choices in `scripts/build-release.ps1` and `installer/ScreenTextCopy.iss` are
worth explaining, because each looks wrong until you know why:

| Choice | Reason |
|---|---|
| `--self-contained true` | The .NET 8 runtime ships inside the build. The user installs nothing — the central requirement for this project. |
| `PublishSingleFile=false` | A single-file WPF app must extract its native libraries on first launch, which is slow and occasionally fails on locked-down machines. A folder is boring and reliable. |
| `PublishTrimmed=false` | WPF resolves XAML types by reflection. Trimming removes types the trimmer cannot see being used, and the failure shows up as a broken binding at runtime, not as a build error. |

The installer is **per-user**, into `%LocalAppData%\Programs\ScreenTextCopy`, with
`PrivilegesRequired=lowest`. That follows directly from §3: downloaded OCR language
packs are written to `<app>\Tesseract\tessdata`. Inside `Program Files` that folder
is read-only for a normal process, so installing a language from Settings would
fail with access-denied unless the whole app ran elevated. A per-user install keeps
it writable and removes the UAC prompt entirely.

The Inno `AppId` is fixed, so a new version upgrades in place instead of installing
beside the old one, and `CloseApplications=yes` shuts a running instance down before
the files are replaced.

Finally, the Tesseract engine is **not committed**: `libtesseract-5.dll` is about
106 MB and GitHub's hard per-file limit is 100 MB. Contributors run
`scripts\fetch-tesseract.ps1` once after cloning; released builds already contain
it, because a Release *asset* may be up to 2 GB.

## 10. Deliberately not done

- No ARM64 build. The bundled Tesseract binaries are x64.
- No code signing. A certificate is a recurring cost; `SHA256SUMS.txt` is published
  instead so a download can be verified.
- No automatic updates. That means a network callback and an update server, which
  contradicts §7.
- No cloud OCR. It would be more accurate on hard input and would also mean
  uploading your screen.

## See also

- [source-map.md](source-map.md) — what lives in which file
- [development.md](development.md) — workflow, test matrix, release checklist
- [BUILD.md](BUILD.md) — building the app and installer from source
