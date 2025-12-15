# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Ginger is an application for creating and editing AI character cards. It supports multiple character card formats (PNG, CHARX, JSON, YAML) compatible with AI chat frontends like SillyTavern, Backyard AI, Agnai.chat, JanitorAI, Chub.ai, and others.

**Two implementations exist:**
- `source/` - Original Windows Forms (.NET Framework 4.6)
- `source.avalonia/` - Cross-platform Avalonia port (.NET 9) with 100% feature parity

## Build Commands

### Avalonia Port (Recommended for Development)
```bash
# Build
dotnet build source.avalonia/Ginger.Avalonia.csproj

# Build and run
dotnet run --project source.avalonia/Ginger.Avalonia.csproj

# Release build (creates self-contained executable)
dotnet publish source.avalonia/Ginger.Avalonia.csproj -c Release

# Clean build (if experiencing issues)
dotnet clean source.avalonia/Ginger.Avalonia.csproj && dotnet build source.avalonia/Ginger.Avalonia.csproj
```

### Original Windows Forms (Windows Only)
```bash
nuget restore source/Ginger.sln
msbuild source/Ginger.sln /p:Configuration=Release /p:Platform=x64
```

**Note:** There are no automated tests in this project.

## Avalonia Port Architecture (`source.avalonia/`)

Uses MVVM pattern with CommunityToolkit.Mvvm. `MainViewModel.cs` (~6,700 lines) is the central hub containing all character editing state, file operations, and commands.

### Key Directories
- **ViewModels/** - `MainViewModel.cs` contains all character editing state, file operations, and commands
- **Views/** - Avalonia AXAML UI with 21 dialogs in `Dialogs/`
- **Services/** - Business logic: `CharacterCardService.cs` (format I/O), `RecipeService.cs`, `Backyard/` (SQLite integration), `SpellCheckService.cs`, `TokenizerService.cs`
- **Models/** - Data structures and format parsers in `Formats/`
- **Utility/** - Core business logic ported from original (see Code Reuse section)

### Dependencies
- Avalonia 11.2.1, Avalonia.AvaloniaEdit, CommunityToolkit.Mvvm, Microsoft.Data.Sqlite, WeCantSpell.Hunspell, SkiaSharp, Newtonsoft.Json, YamlDotNet

## Original Windows Forms Architecture (`source/src/`)

- **Model/** - `GingerCharacter.cs` (central data class), `Generation/` (Generator.cs, GingerString.cs, Recipe.cs), `Formats/` (all parsers)
- **Interface/Forms/** - Main forms and dialogs
- **Utility/** - Helper classes including `Integration/Backyard.cs`

### Dependencies (WinForms)
- Newtonsoft.Json, NHunspell, DarkNet, YamlDotNet, System.Data.SQLite

## Key Concepts

**Recipes** - XML building blocks in `Content/en/Recipes/` (162 files). Categories: Character, Model, Personality, NSFW, etc. Recipes contain customizable parameters that generate character descriptions.

**Character Card Formats** - Reads/writes:
- Ginger native (GingerCardV1), TavernCardV2/V3 (SillyTavern), FaradayCard (Backyard AI V1-V4)
- AgnaisticCard, PygmalionCard, TextGenWebUICard, CHARX archives, BYAF archives

**GingerString** - Central class handling placeholder conversion between formats (`{{char}}`/`{{user}}` ↔ `{char}`/`{user}` ↔ internal markers). Located in `Utility/GingerString.cs`.

**Backyard Integration** - Direct SQLite access to Backyard AI's local database (`Services/Backyard/`). Supports push/pull sync, bulk export/import, chat history viewing. Uses `Microsoft.Data.Sqlite` (Avalonia) or `System.Data.SQLite` (WinForms). Database schema versions handled by `Revisions/BackyardDatabase_v*.cs`.

## Content Files

- `Content/en/Recipes/` - 162 recipe XML definitions (identical in both implementations)
- `Content/en/Snippets/` - Reusable text snippets
- `Content/en/Templates/` - Card templates
- `Content/en/Internal/` - Global macros and styles
- `Dictionaries/` - Spell check dictionaries (en_US, en_GB)

## Code Reuse Between Implementations

The Avalonia port directly reuses original code wherever possible. These files are identical or near-identical:

| File | Status |
|------|--------|
| `GingerString.cs` | Identical |
| `ContextString.cs` | Identical |
| `StringBank.cs`, `StringHandle.cs`, `Text.cs` | Identical |
| `Backyard.cs` | Near-identical (SQLite library swap only) |
| `Generator.cs`, `Recipe.cs` | Adapted (WinForms code removed) |
| All chat log formats | Adapted |
| Content XML files | Copied verbatim |

**When porting features:** Copy from original `source/src/` with minimal changes. Only adapt for:
- `System.Drawing` → `Avalonia.Media`
- `System.Data.SQLite` → `Microsoft.Data.Sqlite`
- WinForms dialogs → Avalonia AXAML + code-behind

## Development Guidelines

- Do not write new code; check existing code first and modify from there
- Use original WinForms code (`source/src/`) when possible; copy and adapt rather than rewriting
- The Avalonia port has 100% feature parity with the original (see `port-todo.md`)