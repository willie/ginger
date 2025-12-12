# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Ginger is a Windows Forms (.NET Framework 4.6) application for creating and editing AI character cards. It supports multiple character card formats (PNG, CHARX, JSON, YAML) compatible with various AI chat frontends like SillyTavern, Backyard AI, Agnai.chat, and others.

## Build Commands

```bash
# Restore NuGet packages
nuget restore source/Ginger.sln

# Build x64 Release
msbuild source/Ginger.sln /p:Configuration=Release /p:Platform=x64

# Build x86 Release
msbuild source/Ginger.sln /p:Configuration=Release /p:Platform=x86

# Build Debug
msbuild source/Ginger.sln /p:Configuration=Debug /p:Platform=x64
```

Output binaries are placed in `source/bin/{Platform}/{Configuration}/`.

## Architecture

### Source Structure (`source/src/`)

- **Application/** - App startup, settings, constants, dictionaries, localization
  - `Program.cs` - Entry point, initializes locales, dictionaries, settings, and Backyard AI connection
  - `AppSettings.cs` - User settings persisted to Settings.ini
  - `Constants.cs` - Recipe colors, URLs, default values

- **Model/** - Core data structures and file format handling
  - `GingerCharacter.cs` - Central class for character data, handles reading from multiple card formats (Ginger, Faraday, Tavern V2/V3, Agnaistic, Pygmalion, TextGenWebUI)
  - `CardData.cs` - Character card metadata (name, creator, tags, portrait, etc.)
  - `CharacterData.cs` - Per-character data within a card
  - `Recipe/` - Recipe system (building blocks for character creation)
  - `Lorebook/` - Lorebook/world info system
  - `Formats/` - File format serialization (CharacterCards, Lorebooks, BackyardArchive, Assets, ChatLogs)

- **Interface/** - Windows Forms UI
  - `Forms/MainForm.cs` - Main application window
  - `Forms/MainFunctions.cs` - Core UI logic
  - `Forms/BackyardFunctions.cs` - Backyard AI integration UI
  - `Controls/` - Custom UI controls (RecipePanel, SnippetPanel, SidePanel, etc.)
  - `Parameters/` - Parameter editor panels for recipe system
  - `Theme/` - Dark mode support

- **Utility/** - Helper classes
  - `Integration/Backyard.cs` - Backyard AI SQLite database integration
  - `SpellChecking/` - NHunspell spell checker
  - `ContextString/` - Text processing with placeholders
  - `GenderSwap.cs` - Pronoun replacement for gender swapping
  - `FindReplace.cs` - Search and replace functionality

### Key Concepts

**Recipes** - Building blocks for character creation stored as XML in `source/Content/en/Recipes/`. Categories include Character, Model, Personality, NSFW, and more. Recipes contain parameters that users can customize.

**Character Card Formats** - The app reads/writes multiple formats:
- Ginger native format (GingerCardV1)
- TavernCardV2/V3 (SillyTavern format)
- FaradayCardV4 (Backyard AI format)
- AgnaisticCard, PygmalionCard, TextGenWebUICard

**GingerString** - Text processing class that handles placeholder conversion between different formats (e.g., `{{char}}`, `{{user}}` vs `<char>`, `<user>`).

### Dependencies

- Newtonsoft.Json - JSON serialization
- NHunspell - Spell checking
- DarkNet - Windows dark mode support
- YamlDotNet - YAML parsing
- System.Data.SQLite - Backyard AI database access

## Content Files

- `source/Content/en/Recipes/` - Recipe XML definitions organized by category
- `source/Content/en/Internal/` - Global macros, recipes, and styles
- `source/Dictionaries/` - Spell check dictionaries
- use original code as much as possible
- don't mock tests