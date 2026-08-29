# Troubleshooting

This page covers the errors people actually hit, what causes each one, and how to fix it.

## Quick index

| Symptom | Jump to |
|---|---|
| *"target machine actively refused it (127.0.0.1:10808)"* | [Connection refused](#could-not-reach-the-ai-endpoint--actively-refused-it) |
| Test connection is green but the text is unchanged | [Text comes back unchanged](#test-connection-is-green-but-the-text-comes-back-unchanged) |
| Test connection is green but no model list | [No model list](#test-connection-is-green-but-there-is-no-model-list) |
| 401 or 403 while translating | [Auth failures](#401-or-403-while-translating) |
| Every model times out | [All models time out](#every-model-times-out) |
| A hotkey does nothing | [Hotkey conflict](#a-hotkey-does-nothing) |
| *"Windows protected your PC"* | [SmartScreen](#windows-protected-your-pc-on-first-run) |
| OCR text is garbled or empty | [OCR quality](#ocr-output-is-garbled-or-empty) |
| Emoji are missing from the result | [Emoji](#emoji-are-dropped-from-the-result) |
| A language pack will not install | [Language pack download](#a-language-pack-fails-to-install) |
| The app vanished when I closed it | [Tray behaviour](#the-app-does-not-appear-after-i-close-it) |

## Translation and network

### "Could not reach the AI endpoint ... actively refused it"

The full message reads: *"Could not reach the AI endpoint ... No connection could
be made because the target machine actively refused it. (127.0.0.1:10808)"*

**Cause.** Your Windows system proxy points at a local VPN/proxy port that is not
listening. Port 10808 is v2rayN/Xray's default SOCKS5 port, so this usually means
a VPN client is configured system-wide but is not currently running.

**Fix.** Go to **Settings → Translation → Network** and either:

1. Choose **Direct** if you do not need a proxy at all, so the system proxy is
   ignored; or
2. Choose **Manual** and type the address that actually works, for example
   `socks5://127.0.0.1:10808` once your client is running.

The mode is re-read on every request, so saving is enough — no restart.

### Test connection is green but the text comes back unchanged

**Cause.** The detected source language is the same as your chosen target
language, so there is nothing to translate and the app returns the text as-is.

**Fix.** Pick a different target language in **Settings → Behavior**.

### Test connection is green but there is no model list

**Cause.** The endpoint does not expose a `/models` route, so there is nothing for
**Refresh model list** to read. Many self-hosted and gateway endpoints are like
this, and it does not affect translation.

**Fix.** Type the model name by hand into the editable model box.

### 401 or 403 while translating

**Cause.** The API key is missing, wrong, or not valid for that endpoint.

**Fix.** Re-enter the key in **Settings → Translation**. Note that model failover
is deliberately skipped for authentication errors: a rejected key would be
rejected by every model, so retrying would only waste requests.

### Every model times out

**Cause.** Either the endpoint is unreachable (usually a proxy problem — see
[connection refused](#could-not-reach-the-ai-endpoint--actively-refused-it)),
or the base URL is wrong.

**Fix.** Check that the base URL includes the version segment. It must be
`https://api.openai.com/v1`, not `https://api.openai.com`. The per-model timeout
is 20 seconds, so a completely unreachable endpoint takes a while to give up on
every model in turn.

## Capture and OCR

### A hotkey does nothing

**Cause.** Another application registered the same key combination first, and
Windows gives it to whoever asked first.

**Fix.** Rebind it: **Settings → Behavior → Global shortcut** (or **Overlay
shortcut**) → **Change...**, then press the new combination.

### OCR output is garbled or empty

**Cause.** The input is too small, too low-contrast, or the engine is being asked
to consider languages that are not on the screen.

**Fix.**

1. Zoom in before capturing — bigger text is recognised much more reliably.
2. Keep **Enhance image before recognition** turned on.
3. In **Settings → Text recognition**, select **only** the languages present in
   the region you are capturing. Selecting many languages hurts accuracy as well
   as speed.
4. Avoid low-contrast or heavily anti-aliased text where you have the choice.

### Emoji are dropped from the result

**Cause.** Tesseract cannot recognise emoji at all — there is no model for them.

**Fix.** None; this is a limitation of the OCR engine. The app reports it with a
toast instead of emitting garbage characters in their place.

### A language pack fails to install

**Cause.** No internet connection, or a proxy is blocking `github.com`. Packs are
downloaded over HTTPS from the official `tesseract-ocr/tessdata_fast` repository.

**Fix.** Check your connection, then check **Settings → Translation → Network** —
the same network mode applies. Try **Direct**, or **Manual** with a working proxy
address.

## Application behaviour

### "Windows protected your PC" on first run

**Cause.** The build is not code-signed, so SmartScreen cannot identify the
publisher.

**Fix.** Click **More info → Run anyway**. To confirm the file is exactly what was
published, verify its checksum against `SHA256SUMS.txt` from the release:

```powershell
Get-FileHash .\ScreenTextCopy-Setup-2.0.0-win-x64.exe -Algorithm SHA256
```

### The app does not appear after I close it

**Cause.** By design. *Keep running in the system tray when closed* is on by
default, so closing the window hides it and leaves the hotkeys active.

**Fix.** Click the tray icon to bring the window back, use **Exit** in the tray
menu to quit for real, or turn the setting off in **Settings → Behavior** if you
want the close button to close the app.

### Starting completely fresh

If settings are in a state you cannot untangle:

1. Quit the app from the tray menu (**Exit**).
2. Delete `%AppData%\ScreenTextCopy\settings.json`.
3. Start the app again. Everything returns to its defaults.

This also clears your saved API key and the cached model list.

## Reporting a problem

Open an issue at
<https://github.com/rezakazemifathi/ScreenTextCopy/issues> and include:

- your Windows version,
- the app version,
- what you were doing when it happened,
- the exact error text.

> **Never paste your API key into an issue**, and check that any screenshot or log
> you attach does not contain it.

## See also

- [INSTALL.md](INSTALL.md) — download, install, upgrade, uninstall
- [USAGE.md](USAGE.md) — every feature and setting explained
- [BUILD.md](BUILD.md) — building the app and its installer from source
- [../README.md](../README.md) — project overview
