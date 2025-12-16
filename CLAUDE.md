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

# Platform-specific builds
dotnet publish source.avalonia/Ginger.Avalonia.csproj -c Release -r osx-arm64 --self-contained  # macOS Apple Silicon
dotnet publish source.avalonia/Ginger.Avalonia.csproj -c Release -r osx-x64 --self-contained    # macOS Intel
dotnet publish source.avalonia/Ginger.Avalonia.csproj -c Release -r win-x64 --self-contained    # Windows
dotnet publish source.avalonia/Ginger.Avalonia.csproj -c Release -r linux-x64 --self-contained  # Linux

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

Uses MVVM pattern with CommunityToolkit.Mvvm.

### Key Entry Points
- **`MainViewModel.cs`** (~7,000 lines) - Central hub for all character editing. Methods organized into `#region` blocks: File Operations, Character State, Recipe Management, Lorebook, Backyard Integration, etc.
- **`Current.cs`** - Global state holder accessed throughout as `Current.Card`, `Current.Character`, `Current.Characters[]`, `Current.SelectedCharacter`
- **`Generator.cs`** - Converts recipes + parameters into final character output text

### Key Directories
- **ViewModels/** - `MainViewModel.cs` plus `RecipeViewModel.cs` for recipe parameter editing
- **Views/** - Avalonia AXAML UI with 21 dialogs in `Dialogs/`
- **Services/** - Business logic: `CharacterCardService.cs` (format I/O), `RecipeService.cs`, `Backyard/` (SQLite integration), `SpellCheckService.cs`, `TokenizerService.cs`
- **Models/** - Data structures and format parsers in `Formats/`
- **Utility/** - Core business logic ported from original

### Dependencies
- Avalonia 11.2.1, Avalonia.AvaloniaEdit, CommunityToolkit.Mvvm, Microsoft.Data.Sqlite, WeCantSpell.Hunspell, SkiaSharp, Newtonsoft.Json, YamlDotNet

## Original Windows Forms Architecture (`source/src/`)

- **Model/** - `GingerCharacter.cs` (central data class), `Generation/` (Generator.cs, GingerString.cs, Recipe.cs), `Formats/` (all parsers)
- **Interface/Forms/** - Main forms and dialogs
- **Utility/** - Helper classes including `Integration/Backyard.cs`

### Dependencies (WinForms)
- Newtonsoft.Json, NHunspell, DarkNet, YamlDotNet, System.Data.SQLite

## Key Concepts

**Recipe System** - XML building blocks in `Content/en/Recipes/` (162 files):
- `RecipeBook.cs` - Loads and manages all recipe XML definitions
- `Recipe.cs` - Single recipe with parameters and `<Block>` template elements
- `RecipeViewModel.cs` - UI binding for recipe parameter editing
- Recipe XML uses `<Block>` elements with conditional `<Condition>` logic
- Categories: Character, Model, Personality, NSFW, etc.

**Character Card Formats** - Reads/writes:
- Ginger native (GingerCardV1), TavernCardV2/V3 (SillyTavern), FaradayCard (Backyard AI V1-V4)
- AgnaisticCard, PygmalionCard, TextGenWebUICard, CHARX archives, BYAF archives

**GingerString** - Central class handling placeholder conversion between formats (`{{char}}`/`{{user}}` ↔ `{char}`/`{user}` ↔ internal markers). Located in `Utility/GingerString.cs`.

**Backyard Integration** - Direct SQLite access to Backyard AI's local database (`Services/Backyard/`):
- Supports push/pull sync, bulk export/import, chat history viewing
- Database versions v28 and v37 are separate complete implementations (~7,000 lines each), not incremental
- `Backyard.cs` detects version and routes to `BackyardDatabase_v28.cs` or `BackyardDatabase_v37.cs`
- Uses `Microsoft.Data.Sqlite` (Avalonia) or `System.Data.SQLite` (WinForms)

## Content Files

- `source/Content/en/Recipes/` - 162 recipe XML definitions (copied to Avalonia output during build)
- `source/Content/en/Snippets/` - Reusable text snippets
- `source/Content/en/Templates/` - Card templates
- `source/Content/en/Internal/` - Global macros and styles
- `source/Dictionaries/` - Spell check dictionaries (en_US, en_GB)

## Code Reuse Between Implementations

The Avalonia port directly reuses original code wherever possible. Many `Utility/` files are identical or near-identical between implementations.

**For detailed port documentation, see:**
- [`docs/PORT_MAPPING.md`](docs/PORT_MAPPING.md) - File-by-file mapping between implementations
- [`docs/PORT_DECISIONS.md`](docs/PORT_DECISIONS.md) - Rationale for adaptation decisions

**When porting features:** Copy from original `source/src/` with minimal changes. Only adapt for:
- `System.Drawing` → `SkiaSharp` (via `ImageService`)
- `System.Data.SQLite` → `Microsoft.Data.Sqlite`
- WinForms dialogs → Avalonia AXAML + code-behind

## Development Guidelines

- Do not write new code; check existing code first and modify from there
- Use original WinForms code (`source/src/`) when possible; copy and adapt rather than rewriting
- The Avalonia port has 100% feature parity with the original