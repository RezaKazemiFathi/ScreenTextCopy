# Contributing to ScreenTextCopy

Thanks for taking the time to help. ScreenTextCopy is a small, volunteer-run
Windows desktop app, so the bar is simple: **changes should build cleanly, keep the
bilingual UI intact, and be easy for the next person to read.**

- **Bugs and ideas** → open an [issue](https://github.com/rezakazemifathi/ScreenTextCopy/issues).
- **Security problems** → do *not* open a public issue; follow [SECURITY.md](SECURITY.md).
- **Code** → read the rest of this document, then send a pull request.

---

## Table of contents

- [Prerequisites](#prerequisites)
- [First-time setup](#first-time-setup)
- [Development loop](#development-loop)
- [Before you open a pull request](#before-you-open-a-pull-request)
- [Branch naming](#branch-naming)
- [Commit messages](#commit-messages)
- [Pull request checklist](#pull-request-checklist)
- [Coding conventions](#coding-conventions)
- [MVVM discipline](#mvvm-discipline)
- [Localization rule](#localization-rule)
- [Manual test matrix](#manual-test-matrix)
- [What not to commit](#what-not-to-commit)
- [Project layout](#project-layout)
- [License of contributions](#license-of-contributions)

---

## Prerequisites

| Tool | Why |
|---|---|
| [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) | Builds and runs the app (`net8.0-windows`). |
| Windows 10 (1809+) or Windows 11, **x64** | The app is WPF and Windows-only; it cannot be built or run on Linux or macOS. |
| [Inno Setup 6](https://jrsoftware.org/isdl.php) | **Only** if you want to produce the installer. Not needed for normal development. |

An IDE is optional. Visual Studio 2022, JetBrains Rider and VS Code with the C#
extension all work; nothing in the repository depends on a particular editor.

## First-time setup

```powershell
git clone https://github.com/rezakazemifathi/ScreenTextCopy.git
cd ScreenTextCopy

# One-time: put the Tesseract OCR engine and language data in place.
powershell -ExecutionPolicy Bypass -File scripts\fetch-tesseract.ps1
```

**The `fetch-tesseract.ps1` step is not optional.** The Tesseract engine is *not*
in the repository: one of its binaries, `libtesseract-5.dll`, is around 106 MB,
which is above GitHub's hard 100 MB per-file limit. It is therefore gitignored,
and the script fetches it for you — reusing an existing Tesseract installation if
it finds one, offering to install one via `winget` otherwise, and downloading the
OCR language data. Without this step the app builds but OCR fails at runtime.

## Development loop

```powershell
dotnet run --project src\ScreenTextCopy\ScreenTextCopy.csproj
```

That is the whole loop: edit, run, try it. Use the tray icon to exit fully — closing
the window only hides it.

## Before you open a pull request

```powershell
dotnet build ScreenTextCopy.sln -c Release
```

This **must finish with 0 warnings and 0 errors.** Warnings are treated as real
problems here: nullable-reference warnings in particular usually mean a genuine
null bug, and a PR that adds warnings will be asked to remove them before review.

## Branch naming

Work on a branch, never on `main`:

| Prefix | For |
|---|---|
| `feat/` | new functionality — `feat/ocr-language-picker` |
| `fix/` | bug fixes — `fix/rtl-checkbox-alignment` |
| `docs/` | documentation only — `docs/arabic-readme` |
| `chore/` | build, tooling, dependencies, housekeeping — `chore/bump-qrcoder` |

## Commit messages

Use [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<optional scope>): <short imperative summary>

<optional body explaining WHY>
```

Types in use: `feat`, `fix`, `docs`, `chore`, `refactor`, `perf`, `test`, `build`,
`ci`. Examples:

```
feat(translation): add per-model timeout with automatic failover
fix(ocr): keep DPI scaling correct on secondary monitors
docs(readme): add Arabic translation
chore(deps): bump QRCoder to 1.6.0
```

Keep the summary under ~72 characters, in the imperative mood ("add", not "added").
Put the reasoning in the body — that is what reviewers and future maintainers read.

## Pull request checklist

Copy this into your PR description and tick it off:

- [ ] `dotnet build ScreenTextCopy.sln -c Release` finishes with **0 warnings, 0 errors**.
- [ ] The change was actually run (`dotnet run …`), not only compiled.
- [ ] Any new UI string exists in **both** `en.json` and `fa.json` under the same key.
- [ ] Tested in **Light** and **Dark** themes.
- [ ] Tested with the UI in **English** and in **Persian (RTL)**.
- [ ] No files from `bin/`, `obj/`, `release/` or `src/ScreenTextCopy/Tesseract/` are staged.
- [ ] No secrets, API keys, tokens or personal endpoints in the diff.
- [ ] Commits follow Conventional Commits; the branch uses a `feat/` `fix/` `docs/` `chore/` prefix.
- [ ] The PR description says **what** changed and **why**, and names the issue it closes if any.

Keep pull requests focused. One logical change per PR reviews faster than a mixed
bag, and unrelated reformatting hides the real diff — please don't bundle it in.

## Coding conventions

- **C# 12** (`LangVersion` is `latest`), **nullable reference types enabled**,
  **`ImplicitUsings` enabled** — don't re-add usings the SDK already provides.
- **File-scoped namespaces**: `namespace ScreenTextCopy.Services;`
- **4-space indentation**, no tabs.
- **`sealed` by default.** Only leave a class open when something actually derives
  from it.
- **XML doc comments** on public types and on any non-obvious member. A one-line
  `<summary>` that says something real beats a paragraph that restates the name.
- **Comments explain WHY, not WHAT.** The code already says what it does; the
  comment should say why it has to be that way — a Windows quirk, a DPI rounding
  rule, an API that lies about its status codes.
- **Never swallow exceptions silently.** No empty `catch { }`. Either handle the
  failure, surface it to the user through the view model, or let it propagate.
  A silent catch is how bugs become "it just doesn't work".
- **No hard-coded secrets** — no API keys, tokens, endpoints tied to your account,
  or personal paths. Ever, including in tests and sample config.
- Prefer `async`/`await` over blocking calls for I/O, and pass a
  `CancellationToken` where the caller can plausibly cancel.
- Match the surrounding code. If a file already does something a certain way,
  follow it rather than introducing a second style in the same folder.

## MVVM discipline

The app uses **MVVM** with
[CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet). This is not
decorative — please keep to it:

- **No business logic in code-behind.** `*.xaml.cs` should be empty apart from
  `InitializeComponent()` and genuinely view-only concerns (window chrome, focus,
  animation).
- Use the source generators: **`[ObservableProperty]`** for bindable state and
  **`[RelayCommand]`** for actions. Don't hand-write `INotifyPropertyChanged`
  plumbing or `ICommand` classes.
- **Views bind to view models.** A view should never reach into a service directly;
  the view model owns that.
- Long-running work belongs in a service behind an interface, called from an async
  `[RelayCommand]`, with progress and error state exposed as observable properties.

## Localization rule

The UI is fully bilingual and switchable at runtime, so **every user-visible string
is localized. No exceptions.**

If you add a string:

1. Add the key to **`src\ScreenTextCopy\Localization\en.json`**.
2. Add **the same key** to **`src\ScreenTextCopy\Localization\fa.json`** with the
   Persian translation.
3. Consume it in XAML through the markup extension:

   ```xml
   <TextBlock Text="{loc:Loc settings.network.mode}" />
   ```

A key that exists in one file but not the other is a bug: **a missing key renders as
the raw key**, so the user sees `settings.network.mode` sitting in the interface. If
you cannot write the Persian text, say so in the PR and put a best-effort
placeholder in `fa.json` — but do add the key.

## Manual test matrix

There is no automated UI test suite, so a small amount of manual checking is
expected before you submit. At minimum, exercise the area you touched in all four
combinations:

| | Light theme | Dark theme |
|---|---|---|
| **English (LTR)** | ✔ | ✔ |
| **Persian (RTL)** | ✔ | ✔ |

RTL is where layout bugs hide — mirrored margins, misaligned checkboxes, scrambled
mixed Persian/English lines. Dark theme is where contrast bugs hide. Switching both
takes seconds in **Settings** and catches most regressions.

## What not to commit

Never stage anything from:

- `bin/` and `obj/` — build output.
- `release/` — packaged artifacts, installers, zips, checksums.
- `src/ScreenTextCopy/Tesseract/` — the OCR engine and language data. This is
  fetched by `scripts\fetch-tesseract.ps1` and gitignored because
  `libtesseract-5.dll` alone exceeds GitHub's 100 MB per-file limit.

Also keep out personal `settings.json` files, anything containing an API key, and
editor-specific local config. If `git status` shows one of these, fix `.gitignore`
in the same PR rather than staging around it.

## Project layout

| Path | What it holds |
|---|---|
| `src\ScreenTextCopy\` | the WPF application (`ScreenTextCopy.csproj`) |
| `src\ScreenTextCopy\Localization\` | `en.json` and `fa.json` UI strings |
| `scripts\` | `fetch-tesseract.ps1`, `build-release.ps1` |
| `installer\` | the Inno Setup script |
| `docs\` | install, usage, build, architecture, source map, troubleshooting |

`docs\architecture.md` and `docs\source-map.md` are the fastest way to find where
something lives; `docs\development.md` covers workflow in more depth than this file.

## License of contributions

ScreenTextCopy is released under the [MIT License](LICENSE). By submitting a pull
request you agree that your contribution is licensed under the same terms, and that
you have the right to submit it. Do not paste code from sources whose license you
have not checked — if a change is adapted from elsewhere, say so in the PR and name
the license.
