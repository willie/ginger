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
# Build and run
dotnet build source.avalonia/Ginger.Avalonia.csproj
dotnet run --project source.avalonia/Ginger.Avalonia.csproj

# Release build
dotnet publish source.avalonia/Ginger.Avalonia.csproj -c Release
```

### Original Windows Forms (Windows Only)
```bash
nuget restore source/Ginger.sln
msbuild source/Ginger.sln /p:Configuration=Release /p:Platform=x64
```

## Avalonia Port Architecture (`source.avalonia/`)

Uses MVVM pattern with CommunityToolkit.Mvvm:

- **ViewModels/** - Main application state and commands
  - `MainViewModel.cs` - Central ViewModel with all character editing state, file operations, Backyard integration
  - `RecipeViewModel.cs` - Recipe parameter management

- **Views/** - Avalonia AXAML UI
  - `MainWindow.axaml` - Main application window
  - `Dialogs/` - 20+ dialog windows (FileFormatDialog, BackyardBrowserDialog, RecipeBrowserDialog, etc.)

- **Services/** - Business logic separated from UI
  - `CharacterCardService.cs` - Load/save character cards in all formats
  - `RecipeService.cs` - Recipe XML parsing and management
  - `GeneratorService.cs` - Text generation from recipes
  - `DialogService.cs` - Dialog presentation
  - `SpellCheckService.cs` - WeCantSpell.Hunspell integration
  - `UndoService.cs` - Undo/redo support
  - `Backyard/` - Backyard AI SQLite database integration with versioned database schemas

- **Models/** - Data structures
  - `CardData.cs` - Character card metadata
  - `TavernCardV2.cs`, `FaradayCard.cs` - Format-specific models
  - `Formats/` - Additional format implementations

- **Utility/** - Shared helpers
  - `GingerString.cs` - Placeholder conversion between formats
  - `ContextString/` - Text processing engine
  - `Parameters/` - Recipe parameter types

### Dependencies (Avalonia)
- Avalonia 11.2.1 - Cross-platform UI
- CommunityToolkit.Mvvm - MVVM infrastructure
- Microsoft.Data.Sqlite - Backyard database access
- WeCantSpell.Hunspell - Spell checking
- SkiaSharp - Image processing

## Original Windows Forms Architecture (`source/src/`)

- **Application/** - App startup, settings, constants
- **Model/** - Data structures and file format handling
  - `GingerCharacter.cs` - Central character data class
  - `Formats/` - All format parsers
- **Interface/** - Windows Forms UI
  - `Forms/` - Main forms and dialogs
  - `Controls/` - Custom controls
- **Utility/** - Helper classes

### Dependencies (WinForms)
- Newtonsoft.Json, NHunspell, DarkNet, YamlDotNet, System.Data.SQLite

## Key Concepts

**Recipes** - XML building blocks in `Content/en/Recipes/` (173 files). Categories: Character, Model, Personality, NSFW, etc. Recipes contain customizable parameters.

**Character Card Formats** - Reads/writes:
- Ginger native (GingerCardV1)
- TavernCardV2/V3 (SillyTavern)
- FaradayCardV4 (Backyard AI)
- AgnaisticCard, PygmalionCard, TextGenWebUICard, CHARX, BYAF

**GingerString** - Handles placeholder conversion (`{{char}}`/`{{user}}` ↔ `<char>`/`<user>`)

**Backyard Integration** - Direct SQLite access to Backyard AI's local database for push/pull sync, bulk export/import, and chat history.

## Content Files

- `Content/en/Recipes/` - Recipe XML definitions
- `Content/en/Internal/` - Global macros and styles
- `Dictionaries/` - Spell check dictionaries (en_US, en_GB)

## Development Guidelines

- Use original code as much as possible when porting features
- Don't mock tests
- See `source.avalonia/IMPLEMENTATION_PLAN.md` for feature parity tracking