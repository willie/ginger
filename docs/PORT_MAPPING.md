# Port Mapping: Windows Forms to Avalonia

This document maps the original Windows Forms codebase (`source/src/`) to the Avalonia port (`source.avalonia/`).

## Overview

| Metric | WinForms | Avalonia | Notes |
|--------|----------|----------|-------|
| Total C# Files | ~370 | ~160 | 55% reduction |
| Total LOC | ~112,650 | ~69,500 | WinForms-specific code removed |
| Architecture | Code-behind + Designer | MVVM | CommunityToolkit.Mvvm |
| UI Framework | WinForms | Avalonia 11.2.1 | Cross-platform |

---

## Directory Structure Mapping

| WinForms Directory | Avalonia Directory | Notes |
|-------------------|-------------------|-------|
| `Application/` | `App.axaml.cs` + `Services/AppSettings.cs` | App lifecycle consolidated |
| `Model/` | `Models/` | Structure preserved |
| `Model/Formats/` | `Models/Formats/` + `Services/CharacterCardService.cs` | Format parsers partially consolidated |
| `Model/Generation/` | `Utility/` | Generator, Recipe, GingerString moved |
| `Model/Parameters/` | `Utility/Parameters/` | Structure preserved |
| `Model/Recipe/` | `Utility/` | Recipe.cs, RecipeBook.cs flattened |
| `Model/Lorebook/` | `Utility/` | Lorebook.cs, Lorebooks.cs flattened |
| `Interface/Forms/` | `ViewModels/MainViewModel.cs` | Form code-behind → MVVM |
| `Interface/Forms/Dialogs/` | `Views/Dialogs/` | 19 WinForms → 21 Avalonia dialogs |
| `Interface/Controls/` | `Views/Controls/` | Ported to Avalonia controls |
| `Interface/Parameters/` | Inline in Views | Parameter panels in AXAML |
| `Interface/Theme/` | Built-in Avalonia theming | Removed |
| `Utility/` | `Utility/` + `Services/` | Core logic preserved |
| `Utility/Integration/` | `Services/Backyard/` | Relocated, SQLite library swap |
| `Utility/ContextString/` | `Utility/` | Flattened into Utility root |
| `Utility/Condition/` | `Utility/Condition/` | Structure preserved |
| `Utility/Extensions/` | `Utility/Extensions/` | Partially preserved |
| `Utility/Random/` | `Utility/` | Flattened into Utility root |
| `Utility/ThirdParty/` | `Utility/ThirdParty/` | Heavily pruned |

---

## File-by-File Mapping (Verified with diff)

### Category A: Identical (No Changes)

Files copied verbatim with no modifications. These do not require origin comments.

| WinForms Path | Avalonia Path |
|---------------|---------------|
| `Utility/ContextString/ContextString.cs` | `Utility/ContextString.cs` |
| `Utility/ContextString/StringBank.cs` | `Utility/StringBank.cs` |
| `Utility/ContextString/StringHandle.cs` | `Utility/StringHandle.cs` |
| `Utility/ContextString/Text.cs` | `Utility/Text.cs` |
| `Utility/Condition/Conditional.cs` | `Utility/Condition/Conditional.cs` |
| `Utility/Condition/RuleBank.cs` | `Utility/Condition/RuleBank.cs` |
| `Utility/Extensions/StringExtensions.cs` | `Utility/Extensions/StringExtensions.cs` |
| `Utility/Extensions/LinqExtensions.cs` | `Utility/Extensions/LinqExtensions.cs` |
| `Utility/Extensions/DateTimeExtensions.cs` | `Utility/Extensions/DateTimeExtensions.cs` |
| `Utility/Extensions/XmlExtensions.cs` | `Utility/Extensions/XmlExtensions.cs` |
| `Utility/Random/Squirrel3.cs` | `Utility/Squirrel3.cs` |
| `Utility/Random/RandomDefault.cs` | `Utility/RandomDefault.cs` |
| `Utility/ThirdParty/ExifData/ExifData.cs` | `Utility/ThirdParty/ExifData.cs` |
| `Utility/ThirdParty/Cuid/Cuid.cs` | `Utility/ThirdParty/Cuid.cs` |

### Category B: Trivial Changes (BOM/Whitespace/Using Only)

Files with only byte-order-mark removal, trailing whitespace fixes, or added `using` statements.
These do not require origin comments.

| WinForms Path | Avalonia Path | Changes |
|---------------|---------------|---------|
| `Model/Generation/GingerString.cs` | `Utility/GingerString.cs` | Added `using Ginger.Models;` |
| `Utility/GenderSwap.cs` | `Utility/GenderSwap.cs` | BOM removal, trailing whitespace |
| `Utility/FindReplace.cs` | `Utility/FindReplace.cs` | BOM removal, trailing whitespace |
| `Model/Parameters/ChoiceParameter.cs` | `Utility/Parameters/ChoiceParameter.cs` | ~4 diff lines (trivial) |
| `Model/Parameters/NumberParameter.cs` | `Utility/Parameters/NumberParameter.cs` | ~8 diff lines (trivial) |

### Category C: Adapted (Meaningful Changes)

Files with meaningful code changes that require origin comments.

#### Core Business Logic

| WinForms Path | Avalonia Path | Diff Lines | Primary Changes |
|---------------|---------------|------------|-----------------|
| `Model/Generation/Generator.cs` | `Utility/Generator.cs` | ~279 | WinForms UI code removed |
| `Model/Recipe/Recipe.cs` | `Utility/Recipe.cs` | ~650 | Major adaptations |
| `Model/Recipe/RecipeBook.cs` | `Utility/RecipeBook.cs` | ~251 | Adaptations |
| `Model/Lorebook/Lorebook.cs` | `Utility/Lorebook.cs` | ~445 | Adaptations |
| `Utility/Measurement.cs` | `Utility/Measurement.cs` | ~57 | Minor adaptations |
| `Model/Parameters/TextParameter.cs` | `Utility/Parameters/TextParameter.cs` | ~20 | Minor adaptations |

#### Backyard Integration (SQLite Library Swap)

| WinForms Path | Avalonia Path | Diff Lines | Primary Changes |
|---------------|---------------|------------|-----------------|
| `Utility/Integration/Backyard.cs` | `Services/Backyard/Backyard.cs` | ~56 | `System.Data.SQLite` → `Microsoft.Data.Sqlite` |
| `Utility/Integration/Revisions/BackyardDatabase_v28.cs` | `Services/Backyard/Revisions/BackyardDatabase_v28.cs` | ~482 | SQLite library swap |
| `Utility/Integration/Revisions/BackyardDatabase_v37.cs` | `Services/Backyard/Revisions/BackyardDatabase_v37.cs` | ~668 | SQLite library swap |
| `Utility/Integration/Utilities/BackupUtil.cs` | `Services/Backyard/BackupUtil.cs` | ~2080 | Major restructuring + `System.Drawing` removal |

#### Format Parsers

| WinForms Path | Avalonia Path | Diff Lines | Primary Changes |
|---------------|---------------|------------|-----------------|
| `Model/Formats/CharacterCards/TavernCardV2.cs` | `Models/Formats/TavernCardV2.cs` | ~831 | Major restructuring |
| `Model/Formats/CharacterCards/TavernCardV3.cs` | `Models/Formats/TavernCardV3.cs` | ~196 | Adaptations |
| `Model/Formats/CharacterCards/GingerCardV1.cs` | `Models/Formats/GingerCardV1.cs` | ~16 | Minor adaptations |
| `Model/Formats/CharacterCards/AgnaisticCard.cs` | `Models/Formats/AgnaisticCard.cs` | ~155 | Adaptations |
| `Model/Formats/CharacterCards/PygmalionCard.cs` | `Models/Formats/PygmalionCard.cs` | ~66 | Adaptations |

#### Chat Log Formats

| WinForms Path | Avalonia Path | Diff Lines | Primary Changes |
|---------------|---------------|------------|-----------------|
| `Model/Formats/ChatLogs/GingerChatV1.cs` | `Models/Formats/ChatLogs/GingerChatV1.cs` | ~899 | Major adaptations |
| `Model/Formats/ChatLogs/GingerChatV2.cs` | `Models/Formats/ChatLogs/GingerChatV2.cs` | ~752 | Major adaptations |
| `Model/Formats/ChatLogs/TavernChat.cs` | `Models/Formats/ChatLogs/TavernChat.cs` | ~99 | Adaptations |
| `Model/Formats/ChatLogs/BackyardChat.cs` | `Models/Formats/ChatLogs/BackyardChat.cs` | ~46 | Minor adaptations |

### Category D: Consolidated

Multiple WinForms files merged into single Avalonia files.

| WinForms Files | Avalonia File | Notes |
|----------------|---------------|-------|
| `Interface/Forms/MainForm.cs` + `Interface/Forms/MainFunctions.cs` + `Interface/Forms/BackyardFunctions.cs` | `ViewModels/MainViewModel.cs` | 3 files (~7,035 LOC) → 1 file (~7,001 LOC) |
| Multiple format files in `Model/Formats/` | `Services/CharacterCardService.cs` | Format I/O logic consolidated |
| `Application/AppSettings.cs` + Properties.Settings | `Services/AppSettings.cs` | JSON-based configuration |

### Category E: Removed (WinForms-Specific)

Files not ported because they are Windows-specific or replaced by Avalonia equivalents.

| WinForms Path | Reason |
|---------------|--------|
| `Utility/Win32.cs` | P/Invoke Windows APIs |
| `Utility/Extensions/ControlExtensions.cs` | WinForms Control API |
| `Utility/Extensions/RichTextBoxExtensions.cs` | WinForms RichTextBox API |
| `Utility/ThirdParty/PNGNet/*` (~50 files) | Replaced by SkiaSharp |
| `Utility/ThirdParty/WinFormsSyntaxHighlighter/*` (~20 files) | Replaced by Avalonia.AvaloniaEdit |
| `Utility/ThirdParty/CustomTabControl/*` (~8 files) | Avalonia TabControl with styles |
| `Utility/IniFileParser/*` (~30 files) | Replaced by JSON configuration |
| `Interface/Theme/*` | Avalonia built-in theming |
| `Utility/Undo/*` | Replaced by `Services/UndoService.cs` |

### Category F: New for Avalonia

Files that exist only in the Avalonia port.

| Avalonia Path | Purpose |
|---------------|---------|
| `ViewModels/MainViewModel.cs` | MVVM ViewModel for main window |
| `ViewModels/RecipeViewModel.cs` | Recipe instance ViewModel |
| `Views/*.axaml` | All Avalonia XAML views |
| `Views/Dialogs/*.axaml` | 21 dialog windows |
| `Services/DialogService.cs` | Centralized dialog management |
| `Services/ImageService.cs` | Cross-platform image handling |
| `Services/FileService.cs` | File I/O abstraction |
| `Services/SpellCheckService.cs` | WeCantSpell.Hunspell integration |
| `Services/TokenizerService.cs` | Token counting service |
| `Services/UndoService.cs` | Undo/redo management |
| `Services/CharacterCardService.cs` | Character card I/O |
| `Services/RecipeService.cs` | Recipe loading |
| `Converters/*.cs` | Avalonia value converters |

---

## Key Mapping Patterns

### 1. Form → ViewModel

WinForms forms with code-behind become MVVM ViewModels:
- `MainForm.cs` (2,945 LOC) + `MainFunctions.cs` (1,795 LOC) + `BackyardFunctions.cs` (2,295 LOC) → `MainViewModel.cs` (7,001 LOC)

### 2. Model → Models

Data structures move with minimal changes:
- `Model/` → `Models/`
- Namespace: `Ginger` → `Ginger.Models`

### 3. Utility → Utility + Services

Core algorithms stay in Utility; integration/services move:
- Business logic: `Utility/` → `Utility/`
- Integration: `Utility/Integration/` → `Services/Backyard/`
- Platform services: New `Services/` directory

### 4. ContextString Flattening

Nested directory flattened to root:
- `Utility/ContextString/ContextString.cs` → `Utility/ContextString.cs`
- `Utility/ContextString/StringBank.cs` → `Utility/StringBank.cs`
- etc.

### 5. ThirdParty Pruning

WinForms-specific libraries removed:
- PNGNet → SkiaSharp
- WinFormsSyntaxHighlighter → Avalonia.AvaloniaEdit
- CustomTabControl → Avalonia styles
- IniFileParser → JSON configuration
