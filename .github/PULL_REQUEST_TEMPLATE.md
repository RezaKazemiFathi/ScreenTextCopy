<!--
  Thanks for contributing. Fill this in honestly — an incomplete checklist is
  far more useful than a fully ticked one that is not true.
-->

## What does this change?

<!-- One or two sentences. What is different after this PR? -->

## Why?

<!-- The problem being solved. Link the issue: "Fixes #123" or "Part of #123". -->

Fixes #

## How was it tested?

<!--
  Describe what you actually ran and clicked. "Built and it compiles" is not
  testing for a UI change.
-->

- [ ] `dotnet build ScreenTextCopy.sln -c Release` finishes with **0 warnings, 0 errors**
- [ ] Ran the app and exercised the changed behaviour manually

## Checklist

- [ ] The change is focused — it does one thing and does not reformat unrelated files
- [ ] I followed the existing code style (file-scoped namespaces, nullable enabled, 4-space indent)
- [ ] No business logic was added to code-behind; view models own the behaviour
- [ ] Exceptions are handled intentionally and are never silently swallowed
- [ ] No API keys, tokens, passwords or personal data are committed, logged, or shown in error messages

### If this touches the UI

- [ ] Every new string exists in **both** `Localization\en.json` **and** `Localization\fa.json` under the same key, and is used via `{loc:Loc some.key}`
- [ ] Checked in **light** and **dark** theme
- [ ] Checked in **English (LTR)** and **Persian (RTL)** layout

### If this touches packaging, OCR or networking

- [ ] `scripts\build-release.ps1` still produces a working payload
- [ ] OCR language packs still install into `<app>\Tesseract\tessdata` without elevation
- [ ] Outbound requests still honour the Network mode setting (System proxy / Direct / Manual)

## Screenshots

<!-- Strongly encouraged for anything visual. Before/after if you can. -->

## Anything reviewers should know?

<!-- Trade-offs you made, things you were unsure about, follow-up work you left out. -->
