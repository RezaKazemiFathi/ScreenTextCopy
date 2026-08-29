# Security Policy

ScreenTextCopy handles two things people care about: the pixels on your screen and,
optionally, an API key. This document explains how to report a problem and what the
app actually does with your data.

## Supported versions

| Version | Supported |
|---|---|
| 2.0.x | ✅ Yes — fixes land here |
| < 2.0 | ❌ No — please update to the latest release |

There is only one supported line at a time. Security fixes are released as a new
patch version on the newest minor line; older versions are not patched. Always
report against the [latest release](https://github.com/rezakazemifathi/ScreenTextCopy/releases/latest)
if you can reproduce there.

## Reporting a vulnerability

**Please report privately first. Do not open a public issue with exploit details.**

1. **Preferred — GitHub Security Advisories.** Go to the repository's **Security**
   tab and choose **"Report a vulnerability"**:
   <https://github.com/rezakazemifathi/ScreenTextCopy/security/advisories/new>.
   This creates a private thread visible only to you and the maintainer.
2. **Fallback — a GitHub issue with no sensitive detail.** If private reporting is
   unavailable to you, open a normal
   [issue](https://github.com/rezakazemifathi/ScreenTextCopy/issues) that says only
   that you have a security report and asks for a private channel. **Put no
   reproduction steps, no proof of concept, no logs and no affected paths in it.**

### Response expectations

- **Acknowledgement: within 5 business days.**
- Fixes are **best effort**. ScreenTextCopy is a one-person volunteer project with
  no security team and no paid support, so there is no guaranteed patch deadline. A
  realistic timeline will be agreed with you in the private thread.
- You will be credited in the release notes and the advisory unless you ask not to be.
- There is no bug bounty. Nothing is paid for reports.

### What to include

The more of this you provide, the faster it can be confirmed:

- **Version** of ScreenTextCopy (see the About section, e.g. `2.0.0`).
- **Windows build** — run `winver`, e.g. Windows 11 23H2 build 22631.4317, x64.
- **Reproduction steps**, precisely enough to follow from a clean install.
- **Impact** — what an attacker gains: code execution, file read or write outside
  the app's own folders, credential disclosure, privilege escalation, and so on.
- Relevant configuration (installer or portable, translation provider type, proxy
  mode), plus a crash dump or stack trace if you have one.

### Never include secrets in a report

> **Do NOT paste an API key, token, password, or session cookie into a report, an
> issue, a screenshot, a log file or a crash dump.** Redact them before sending. If
> you believe a key of yours has been exposed, revoke it at your provider
> immediately — that is faster and more effective than anything this project can do.

## Security posture

This section is the honest description of what the app does. It is useful both for
deciding whether to trust it and for judging whether something you found is actually
a vulnerability.

### What stays on your machine

- **OCR is fully local.** Recognition runs in-process against the bundled Tesseract
  engine. The temporary capture PNG is deleted in a `finally` block, so it is removed
  even when recognition throws.
- **No telemetry, no analytics, no accounts, no auto-update calls.** The app does not
  phone home, does not check for updates, and has no server side.
- **The QR code for "send to phone" is generated locally** and never uploaded. There
  is no relay service and no account involved.

### The only outbound traffic

1. **Text you explicitly translate**, sent only to the endpoint you configured
   yourself (the free MyMemory provider, or your own OpenAI-compatible base URL).
   Nothing is sent until you press Translate or use the overlay hotkey.
2. **OCR language packs you choose to install**, downloaded over **HTTPS** from
   [`github.com/tesseract-ocr/tessdata_fast`](https://github.com/tesseract-ocr/tessdata_fast).
   The download is restricted to an **allow-list of language codes from the built-in
   catalog**, so a value from the UI cannot be turned into an arbitrary path or host
   — this is the guard against path traversal in the constructed filename and URL.

Screenshots, recognised text you do not translate, settings and the QR payload never
leave the machine.

### API key storage — not encrypted at rest

Your API key is stored **only** in `%AppData%\ScreenTextCopy\settings.json`, on your
own machine. It is **never logged** and **never appears in error messages**, and it
is sent only to the endpoint you configured.

To be plain about it: **the key is stored in plain text and is NOT encrypted at
rest.** Any process running as your Windows user can read that file, as can anyone
who can read your disk offline. This is a deliberate trade-off for a portable,
no-dependency desktop app rather than an oversight — so:

- If disk-level exposure matters to you, rely on **BitLocker or another full-disk
  encryption** solution, which is the correct layer for this.
- Prefer a key scoped to this use, with a spending cap, that you can revoke.
- Reporting "the settings file contains the key in plain text" is not a new finding;
  it is documented behaviour. Reporting that the key **leaks** somewhere else — a
  log, an error dialog, a request to a host you did not configure — very much is.

### Hardening already in place

| Area | Measure |
|---|---|
| Command injection | The Tesseract subprocess is launched with `ProcessStartInfo.ArgumentList`, so each argument is escaped individually instead of being concatenated into one command line. |
| Path traversal | Language-pack downloads accept only codes present in the built-in catalog allow-list. |
| Untrusted URL launching | Links opened from the About screen are validated as **absolute `http`/`https` URIs** before being handed to the shell. |
| Temporary files | The capture PNG is deleted in a `finally` block. |
| Secret handling | The API key is never written to logs and never surfaced in error text. |

### Release integrity — builds are unsigned

Release builds are **not code-signed**, which is why Windows SmartScreen warns that
the publisher is unknown. The integrity check is the **`SHA256SUMS.txt` file
published with every release**:

```powershell
Get-FileHash .\ScreenTextCopy-Setup-2.0.0-win-x64.exe -Algorithm SHA256
```

Compare the output with the matching line in `SHA256SUMS.txt` from the same release
page. Only download from the official
[releases page](https://github.com/rezakazemifathi/ScreenTextCopy/releases) — a copy
from a download aggregator cannot be verified against anything meaningful.

## Out of scope

The following are known and documented, not vulnerabilities:

- The plain-text API key in `settings.json` (see above).
- Unsigned binaries and the resulting SmartScreen warning.
- Whatever your chosen translation provider does with the text you send it — that is
  governed by their terms, not this project's.
- Findings that require an attacker who already has code execution as your user, or
  physical access to an unlocked machine.
