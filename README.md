<p align="center">
  <img src="src/ClearAIText.App/Assets/app_icon_256.png" alt="Clear AI Text Logo" width="256" height="256" />
</p>

<h1 align="center">Clear AI Text</h1>

<p align="center">
  Windows utility for automated clipboard text normalization, typography sanitization, and custom rule-based character replacement.
</p>

## Overview

**Clear AI Text** runs unobtrusively in the background, intercepting text from the Windows clipboard to standardize irregular typographic characters, curved quotes, non-breaking whitespace variations, and Markdown artifacts commonly introduced by AI models and modern web editors.

The application is built with **WinUI 3** and **.NET 10**, utilizing native Win32 clipboard listener APIs for zero idle CPU usage.

## Key Features

- **Event-Driven Clipboard Processing**: Uses native Win32 `AddClipboardFormatListener` message pump with zero polling overhead.
- **Three-Tier Transformation Engine**:
  - **Tier 1 (Safe Typography)**: Normalizes dashes, curved quotes, ellipses, invisible control characters, and non-breaking spaces.
  - **Tier 2 (Destructive Cleaning)**: Opt-in stripping of diacritics, emoji grapheme clusters, and Markdown markup.
  - **Tier 3 (Heuristic Analysis)**: Interactive detection of mixed-script homoglyphs in the Sandbox.
- **Custom Replacement Rules**: User-defined string and regex substitution rules with priority ordering.
- **Interactive Sandbox**: Real-time testing workbench with preset profiles, character count deltas, and millisecond timing stats.
- **Loop Prevention & Lock Safety**: Custom clipboard marker (`ClearAIText.InternalMarker`), sequence number checks, and exponential backoff retry backoff.
- **Modern Fluent UI**: WinUI 3 desktop interface with Mica material, system theme tracking (Light/Dark/Auto), and dual-language localization (Russian / English).
- **Privacy & Security**: All operations occur entirely in volatile memory. No telemetry, network requests, or disk caching of clipboard content.

## Transformation Matrix

| Tier | Category | Default | Description |
| :--- | :--- | :---: | :--- |
| **Tier 1** | Safe Normalization | Enabled | Normalizes em/en dashes (`—`, `–` &rarr; `-`), curly quotes (`“`, `”`, `«`, `»` &rarr; `"` / `'`), spaces (`NBSP`, `NNBSP`, `ZWSP` &rarr; standard space), ellipses (`…` &rarr; `...`), and removes leading UTF-8 BOM. |
| **Custom** | User Rules | User-defined | Custom literal and regular expression replacement rules configured via Settings. |
| **Tier 2** | Destructive Cleaning | Disabled | Strips diacritics/accents (preserving Cyrillic `ё`/`й`), emoji grapheme clusters, and Markdown delimiters (`**bold**`, `# heading`). |
| **Tier 3** | Heuristic Detection | Sandbox only | Highlights mixed-alphabet confusable characters (e.g., Latin homoglyphs in Cyrillic words) for inspection without automatic modification. |

## Building from Source

### Prerequisites
- Windows 10 (version 1809 / build 17763 or later) or Windows 11
- .NET 10 SDK

### Build Commands

```powershell
# Restore dependencies and build solution in Release configuration
dotnet build ClearAIText.slnx -c Release

# Publish portable application
dotnet publish src/ClearAIText.App/ClearAIText.App.csproj -c Release -r win-x64 --self-contained
```

## License

This project is licensed under the [MIT License](LICENSE).
