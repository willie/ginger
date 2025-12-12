# Avalonia Port - Feature Parity Analysis & Plan

## Executive Summary

The Avalonia port is approximately **30% complete** based on file count (114 vs 370 files) and functionality coverage. The core text processing/generation engine has been ported, but significant gaps exist in UI, dialogs, content files, and Backyard integration utilities.

---

## Current State Analysis

### File Count Comparison
| Category | Original | Port | Coverage |
|----------|----------|------|----------|
| Total .cs files | 370 | 114 | 31% |
| Main Form Lines | 7,035 | 1,495 | 21% |
| Dialogs | 17 | 7 | 41% |
| Content Files | 173 | 0 | 0% |

### What's Working Well

1. **Core Generation Engine** - Generator.cs, GingerString.cs, ContextString system
2. **Recipe System** - Recipe.cs, RecipeBook.cs, RecipeMaker.cs, all Parameter types
3. **Basic Card Loading** - TavernV2, Faraday, BYAF, TextGenWebUI formats
4. **Backyard Database Access** - Core database queries and revisions
5. **Basic UI** - MainWindow with tabs, recipe list, lorebook editing
6. **Core Services** - CharacterCardService, RecipeService, DialogService

### What's Using Original Code Properly

| Component | Status | Notes |
|-----------|--------|-------|
| Utility/*.cs | ✅ Good | Most utilities copied directly |
| Parameters/*.cs | ✅ Good | All parameter types present |
| Condition/*.cs | ✅ Good | CustomRule, Conditional, etc. |
| Extensions/*.cs | ✅ Good | String, List, Dictionary extensions |
| ThirdParty/AvsAn | ✅ Good | Indefinite article detection |
| ThirdParty/Cuid | ✅ Good | CUID generation |
| Backyard Database | ✅ Good | v28, v37 revisions present |

---

## Critical Gaps

### 1. Content Files (CRITICAL - 0% complete)
**Impact: App cannot function without recipes**

Missing entirely:
- `Content/en/Recipes/` - All recipe XML definitions (Character, Model, Personality, NSFW, Story, Traits)
- `Content/en/Snippets/` - Sample snippets
- `Content/en/Internal/` - Global macros, styles, internal recipes
- `Content/en/Templates/` - Character templates

**Fix:** Copy entire Content folder from source to source.avalonia

### 2. Missing Dialogs (10 critical dialogs)

| Dialog | Priority | Purpose |
|--------|----------|---------|
| CreateRecipeDialog | High | Create custom recipes |
| CreateSnippetDialog | High | Save text as snippet |
| EnterNameDialog | High | Generic name input |
| FileFormatDialog | Medium | Export format selection |
| RearrangeActorsDialog | Medium | Multi-actor management |
| VariablesDialog | Medium | Custom variables |
| EditModelSettingsDialog | Low | Backyard model settings |
| AssetViewDialog | Low | View embedded assets |
| LinkEditChatDialog | Low | Backyard chat editing |
| Link selection dialogs | Low | Backyard character selection |

### 3. Card Format Gaps

| Format | Original | Port | Status |
|--------|----------|------|--------|
| GingerCardV1 | ✅ | ❌ | Missing native format! |
| TavernCardV1 | ✅ | ❌ | Legacy but needed |
| TavernCardV3 | ✅ | ❌ | Missing (has ccv3 detection but no parser) |
| FaradayCardV1-V3 | ✅ | ❌ | Only V4 combined |

### 4. CharacterCard Model Incomplete

Original `GingerCharacter.cs` has 12 read methods:
- ReadGingerCard ❌
- ReadFaradayCard ✅ (partial)
- ReadFaradayCardAsNewActor ❌
- ReadTavernCard ✅ (partial)
- ReadTavernCardAsNewActor ❌
- ReadAgnaisticCard ❌
- ReadTextGenWebUICard ✅
- ReadPygmalionCard ❌

Port `CharacterCard.cs` only has:
- FromTavernV2 ✅
- ToTavernV2 ✅

### 5. Missing Core Model Classes

| File | Lines | Status | Impact |
|------|-------|--------|--------|
| CharacterData.cs | 445 | ❌ | Multi-actor support |
| GlobalState.cs | 733 | Partial (Current.cs = 275) | App state management |
| GlobalUndo.cs | 206 | ❌ | Sophisticated undo/redo |
| Clipboard/*.cs | 222 | ❌ | Copy/paste recipes, lore |
| CustomVariable.cs | - | ❌ | User variables |

### 6. Chat Log Formats (Entirely Missing)

All 9 chat formats missing:
- AgnaiChat, BackyardChat, BackyardChatBackupV1/V2
- GingerChatV1/V2, TavernChat
- TextFileChat, TextGenWebUIChat

### 7. Backyard Integration Utilities

| Utility | Status | Purpose |
|---------|--------|---------|
| BulkExporter.cs | ❌ | Export multiple characters |
| BulkImporter.cs | ❌ | Import multiple characters |
| BackupUtil.cs | ❌ | Create/restore backups |
| BulkUpdateModelSettings.cs | ❌ | Mass model settings update |
| LegacyChatUpdater.cs | ❌ | Fix old chat formats |

### 8. Application Settings

Original `AppSettings.cs` handles:
- Settings persistence (INI file)
- User preferences (undo steps, token budget, fonts)
- Export/import filters
- Window layout
- Backyard settings
- Spell checking preferences

Port has: Minimal stub in `Stubs.cs`

---

## Implementation Plan

### Phase 1: Foundation (CRITICAL)
**Goal: Make the app functional with recipes**

1. **Copy Content folder**
   ```bash
   cp -r source/Content source.avalonia/Content
   ```
   Update csproj to include Content files

2. **Implement GingerCardV1 parser**
   - Port GingerCardV1.cs from original
   - Add ReadGingerCard() to CharacterCard
   - This is the native format

3. **Add CharacterData model**
   - Port CharacterData.cs from original
   - Enable proper multi-actor support

### Phase 2: Core Features
**Goal: Match basic editing capabilities**

4. **Missing dialogs (High Priority)**
   - CreateRecipeDialog - for custom recipes
   - CreateSnippetDialog - save selections as snippets
   - EnterNameDialog - generic text input
   - FileFormatDialog - export format picker

5. **Card format parsers**
   - TavernCardV3.cs
   - Complete Agnaistic, Pygmalion reading
   - Add "AsNewActor" variants for multi-char

6. **Clipboard models**
   - RecipeClipboard for copy/paste recipes
   - LoreClipboard for copy/paste lore entries

### Phase 3: AppSettings & Persistence
**Goal: Remember user preferences**

7. **Implement real AppSettings**
   - Create Settings service with JSON persistence
   - Recent files (MRUList equivalent)
   - Window size/position
   - Export preferences
   - Spell check settings

8. **Spell checking integration**
   - Wire up SpellCheckService to text boxes
   - Add dictionary selection UI
   - Use the copied dictionary files

### Phase 4: Backyard Integration
**Goal: Full Backyard AI feature parity**

9. **Backyard utilities**
   - BulkExporter.cs
   - BulkImporter.cs
   - BackupUtil.cs

10. **Backyard dialogs**
    - EditModelSettingsDialog
    - LinkEditChatDialog
    - Link selection dialogs

11. **Chat log support**
    - BackyardChat.cs
    - BackyardChatBackupV1/V2

### Phase 5: Advanced Features
**Goal: Complete feature parity**

12. **Custom variables system**
    - VariablesDialog
    - CustomVariable model

13. **Advanced UI controls**
    - Syntax highlighting for text boxes
    - Collapsible recipe groups
    - Output preview modes

14. **Chat history features**
    - Chat log import/export
    - Chat editing dialogs

### Phase 6: Polish
**Goal: Production quality**

15. **Update checker**
    - CheckLatestRelease.cs port

16. **Localization**
    - Locales.cs port
    - String resources

17. **Performance**
    - Async loading
    - Token counting queue

---

## Code Reuse Audit

### Files that should be copied verbatim:
```
source/src/Model/Formats/CharacterCards/GingerCardV1.cs
source/src/Model/Formats/CharacterCards/TavernCardV3.cs
source/src/Model/CharacterData.cs
source/src/Model/CustomVariable.cs
source/src/Model/Clipboard/*.cs
source/src/Model/Recipe/RecipeClipboard.cs
source/src/Model/Formats/ChatLogs/*.cs
source/src/Model/Formats/Lorebooks/TavernWorldbook.cs
source/src/Utility/Integration/Utilities/*.cs
source/src/Application/MRUList.cs
```

### Files needing adaptation (Windows → Cross-platform):
```
source/src/Application/AppSettings.cs → JSON instead of INI
source/src/Utility/SpellChecking/*.cs → WeCantSpell integration
source/src/Interface/Forms/Dialogs/*.cs → Avalonia dialogs
```

### Files to NOT copy (Windows-specific):
```
source/src/Utility/Win32.cs
source/src/Utility/ThirdParty/CustomTabControl/*
source/src/Utility/ThirdParty/WebPWrapper/*
source/src/Interface/Controls/Overrides/* (Windows Forms overrides)
```

---

## Immediate Actions

### Today
1. ✅ Copy Content folder
2. Copy GingerCardV1.cs and adapt
3. Copy CharacterData.cs

### This Week
4. Implement CreateRecipeDialog
5. Implement CreateSnippetDialog
6. Complete card format reading

### Next Week
7. AppSettings with JSON persistence
8. Wire up spell checking
9. Bulk Backyard operations

---

## Metrics for Completion

| Milestone | Target | Current |
|-----------|--------|---------|
| File coverage | 100% essential | ~50% |
| Card formats | 11/11 | 5/11 |
| Dialogs | 17/17 | 7/17 |
| Content files | 173 | 0 |
| Main features | 100% | ~40% |

**Estimated effort to 100%:** 2-3 more sessions of focused work
