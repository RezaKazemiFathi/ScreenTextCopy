# Using ScreenTextCopy

This page covers capturing text, translating it, sending it to your phone, and every setting in the app.

## The two hotkeys

Both work globally — including while the app is hidden in the system tray — and
both are rebindable in Settings.

| Hotkey | What it does |
|---|---|
| `Ctrl + Shift + X` | **Capture.** Recognise text in a region and show it in the main window. |
| `Ctrl + Shift + Z` | **Overlay translation.** Recognise *and* translate a region, showing the result in a small floating popup pinned next to your selection. |

## Capturing text

1. Press `Ctrl + Shift + X`. The screen dims and the cursor becomes a crosshair.
   You can also start a capture from the tray menu or the **Capture text** button
   in the main window.
2. Drag a rectangle over the text. A live size badge follows the selection.
3. Release the mouse. OCR runs locally and the result appears in the main window.
4. Press `Esc` at any point to cancel without capturing.

With **auto-copy** on (the default), the recognised text is on your clipboard the
moment OCR finishes, and a toast confirms it. You do not need to press anything.

### Reading the result panel

| Field | Meaning |
|---|---|
| Text | The recognised text, editable before you copy it |
| Confidence % | How sure the OCR engine is about what it read |
| Elapsed time | How long recognition took |
| Character count | Characters in the result |
| Word count | Words in the result |

| Button | Action |
|---|---|
| **Copy** | Put the text on the clipboard again |
| **Translate** | Translate it into the target language from Settings |
| **Send to phone** | Show the text as a QR code |
| **Clear** | Empty the panel |

## Overlay translation for games, videos and subtitles

`Ctrl + Shift + Z` is built for text you cannot pause or select — game dialogue,
a video subtitle, a streamed slide.

1. Press `Ctrl + Shift + Z` and drag over the text.
2. The region is recognised, translated, and the translation appears in a small
   floating popup pinned near the selection, so you keep watching the original.
3. If the result is wrong or the request failed, press **Retry** in the popup.
4. Pressing the hotkey again re-translates.

## Send to phone

**Send to phone** renders the recognised text as a QR code, generated entirely on
your own machine, and shows it in a window. Point your phone's camera at it and
the text arrives on the phone — no account, no server, no upload.

QR codes have a practical capacity limit, so when the text is long enough that
scanning becomes unreliable the app shows a warning. For long passages, copy the
text instead.

## Getting good OCR results

OCR quality depends far more on the input than on any setting, so:

- **Zoom in before you capture.** Larger on-screen text is recognised far more
  reliably than small text.
- **Keep "Enhance image before recognition" on** (it is on by default).
- **Select only the languages that are actually on screen.** Enabling many
  languages at once slows recognition down *and* reduces accuracy, because the
  engine has more ways to be wrong.
- **Avoid low-contrast or heavily anti-aliased text** where you can.
- **Emoji cannot be recognised at all.** Tesseract has no model for them, so the
  app tells you with a toast rather than emitting garbage characters.

## OCR languages

English, Persian and Arabic ship with the app and are ready immediately.

These are installable on demand from **Settings → Text recognition**, where each
row has its own **Install** button and progress bar:

| Installable on demand |
|---|
| French, German, Spanish, Italian, Russian, Turkish, Chinese (Simplified), Japanese, Korean, Hindi, Portuguese |

Packs are downloaded over HTTPS from the official
[`tesseract-ocr/tessdata_fast`](https://github.com/tesseract-ocr/tessdata_fast)
repository and stored next to the application, so they survive an upgrade.

Persian and English mixed in one line is handled without scrambling, with correct
right-to-left and left-to-right treatment.

## Translation

You can translate into 14 languages: English, Persian, Arabic, French, German,
Spanish, Italian, Russian, Turkish, Chinese, Japanese, Korean, Hindi and
Portuguese.

There are two providers:

| | Free | Custom AI |
|---|---|---|
| Backend | MyMemory | Any OpenAI-compatible `/chat/completions` endpoint |
| API key | Not needed | Yours, or none for a local server |
| Good for | Short text | Longer text and idiomatic phrasing |
| Model picker | — | Discovered from the provider's `/models` route |

### Setting up the Custom AI provider

1. Open **Settings → Translation** and choose **Custom AI**.
2. Enter the **base URL**, including the version segment — for example
   `https://api.openai.com/v1`. Endpoints from OpenAI, OpenRouter, Groq,
   DeepSeek, Together, Azure OpenAI, Ollama, LM Studio and vLLM all work.
3. Enter your **API key**, or leave it empty for a local server that does not
   require one.
4. Press **Refresh model list**. The app queries the provider's `/models` route
   and fills an editable dropdown; if the endpoint has no `/models` route, simply
   type the model name yourself. Discovered models are cached in your settings, so
   the picker keeps working without probing again, and your chosen model persists
   across restarts.
5. Press **Test connection**. A green or red status dot reports reachability,
   along with the latency and how many models were found.

### Model failover

**Automatically switch to another model if one times out** is on by default. When
a translation times out or the provider returns a model-specific error, the
request is retried against the other known models until one succeeds. The
per-model timeout is 20 seconds.

Authentication failures (401/403) never trigger failover — a bad key would fail on
every model, so retrying it is pointless.

### Network and proxy

**Settings → Translation → Network** has three modes:

| Mode | Behaviour |
|---|---|
| **System proxy** (default) | Uses the Windows proxy settings, like your browser. |
| **Direct** | Ignores the system proxy entirely. |
| **Manual** | Uses the address you type. |

Manual mode accepts `http`, `https`, `socks4`, `socks4a` and `socks5`; a bare
`host:port` is treated as `http`. For example `socks5://127.0.0.1:10808` (the
v2rayN/Xray default) or `http://127.0.0.1:10809`.

The mode is re-read on every request, so **Save** takes effect immediately — no
restart needed.

## Settings reference

| Section | What you can change |
|---|---|
| **Appearance** | Theme: *Follow system* / *Light* / *Dark*. App language: *English* / *Persian*. |
| **Text recognition** | OCR language checklist with a per-row **Install** button and progress bar; *Enhance image before recognition* preprocessing toggle (on by default). |
| **Behavior** | Auto-copy recognised text; automatic translation with a target language; global shortcut and overlay shortcut, each with a **Change...** button that captures the new combination; *keep running in the system tray when closed* (on by default). |
| **Translation** | Provider, base URL, API key, model picker, **Refresh model list**, model failover checkbox, **Test connection** with status dot, latency and model count; Network/proxy mode. |
| **About** | Author links and support links. |

Switching the app language takes effect live, and Persian gets a correct
right-to-left layout using the bundled Vazirmatn font.

## The system tray

With *keep running in the system tray when closed* on (the default), closing the
main window only hides it — the hotkeys keep working and the app stays one
keystroke away. Use **Exit** in the tray menu to actually quit, or turn the
setting off if you prefer the close button to close the app.

The tray menu can also start a capture directly.

## Where your settings live

Everything you configure — including your API key and the list of downloaded
language packs — is stored in:

```text
%AppData%\ScreenTextCopy\settings.json
```

Deleting that file resets the app to its defaults.

## See also

- [INSTALL.md](INSTALL.md) — download, install, upgrade, uninstall
- [TROUBLESHOOTING.md](TROUBLESHOOTING.md) — fixes for the errors people actually hit
- [BUILD.md](BUILD.md) — building the app and its installer from source
- [../README.md](../README.md) — project overview
