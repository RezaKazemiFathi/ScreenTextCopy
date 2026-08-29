# Development guide

Practical notes for working on ScreenTextCopy: the loop, what to test, and what to
check before a release. For build commands see [BUILD.md](BUILD.md); for the code
conventions a change must follow see [../CONTRIBUTING.md](../CONTRIBUTING.md).

## The loop

```text
issue  →  small branch  →  build with 0 warnings  →  manual test  →  PR  →  CI  →  merge
```

One branch does one thing. A change that also reformats unrelated files is much
harder to review than two changes.

CI is not a substitute for running the app. It proves the solution builds with
warnings as errors and that `en.json` and `fa.json` have identical key sets; it
cannot tell you the overlay appears in the wrong place on a 150% display.

## What to test by hand

### OCR input

| Case | Why it matters |
|---|---|
| English | Baseline |
| Persian | Right-to-left shaping |
| Persian + English in one line | The classic scrambling case |
| Persian + digits | Digit forms and direction runs |
| URLs, file paths | Must stay left-to-right and unbroken |
| Windows error codes, GUIDs, hex | Reversal or "cleanup" here is silently destructive |
| Small text, then the same text zoomed in | Confirms the zoom advice we give users |
| Emoji | Must produce the honest toast, never garbage characters |

### UI and display

| Case | Why it matters |
|---|---|
| One monitor, then two | The overlay covers the whole virtual desktop |
| 100%, 125%, 150% DPI | Device-pixel conversion; wrong scaling crops the wrong region |
| Light and dark theme | Every new brush must exist in both palettes |
| English (LTR) and Persian (RTL) | Layout mirroring, checkboxes, scrollbars |
| Selection cancelled with `Esc` | Must leave no overlay and no stuck input capture |
| App closed to tray, then hotkey pressed | Hotkeys must keep working while hidden |

### Translation and network

| Case | Why it matters |
|---|---|
| Free provider, short and long text | Chunking |
| Custom AI with a hosted endpoint | Base URL must include the version segment |
| Custom AI with a local server and no key | An empty key must be allowed |
| An endpoint with no `/models` route | Must degrade to a typed model name, not an error |
| A wrong key | Must report 401/403 and must **not** fail over |
| An endpoint that is down | Must give up in bounded time, not hang |
| *System proxy* / *Direct* / *Manual* | Each must take effect on **Save**, with no restart |
| A language-pack install behind a proxy | Uses the same network mode |

### Privacy expectations

These are behavioural guarantees, so treat a regression here as a bug, not a
nice-to-have:

- No screenshot and no recognised text is ever uploaded.
- The temporary PNG handed to the Tesseract CLI is deleted in a `finally` block.
- Outbound requests happen only for translation the user asked for and for language
  packs the user chose to install. There is no telemetry and no update ping.
- The API key never appears in a log, an error message, or the UI after entry.

## OCR quality work

A useful preprocessing pipeline for screenshots looks like this:

```text
capture → crop → upscale 2x → grayscale → contrast → (optional threshold)
        → (optional deskew) → OCR → normalize → preserve LTR tokens
```

Do not turn every step on unconditionally. Screenshots vary far more than scanned
pages: thresholding rescues low-contrast text and destroys anti-aliased text, and
deskew is pointless on a screenshot that is already axis-aligned. Anything added
here belongs behind the existing *Enhance image before recognition* setting or a new
explicit one.

When measuring an improvement, compare on a fixed set of captures. A change that
helps Persian body text and quietly breaks error codes is not an improvement.

## Adding a UI string

1. Add the key to **both** `Localization/en.json` and `Localization/fa.json`.
2. Use it as `{loc:Loc your.key}`; never hard-code display text in XAML.
3. Check it in Persian too — a string that fits in English often does not fit in
   Persian, and RTL exposes padding assumptions.

CI fails if the two files disagree, so a forgotten translation cannot be merged.

## Release checklist

- [ ] `dotnet build ScreenTextCopy.sln -c Release` — 0 warnings, 0 errors
- [ ] Manual pass over the tables above on at least one multi-monitor, scaled setup
- [ ] Version bumped in the `.csproj` and in `CHANGELOG.md`
- [ ] `CHANGELOG.md` entry written for humans, not from the commit log
- [ ] `scripts\fetch-tesseract.ps1` then `scripts\build-release.ps1 -Version <x.y.z>`
      produce all three assets
- [ ] Installer tested on a machine with **no** .NET and **no** Tesseract installed
- [ ] Upgrade tested over the previous version: settings, API key and downloaded
      language packs survive
- [ ] Uninstall tested, including the "keep my settings" default
- [ ] Portable zip runs from a folder that is not `Program Files`
- [ ] `SHA256SUMS.txt` matches the published files
- [ ] Tag pushed as `vX.Y.Z`, and the release workflow's assets checked

## Versioning

Semantic versioning. The Inno `AppId` never changes, which is what makes an upgrade
replace the previous install instead of sitting beside it — see
[architecture.md](architecture.md) §9.

## See also

- [architecture.md](architecture.md) — why the pieces are split this way
- [source-map.md](source-map.md) — what lives in which file
- [BUILD.md](BUILD.md) — building the app and installer from source
