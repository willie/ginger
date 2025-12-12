# Ginger Avalonia Port - 100% Parity Implementation Plan

## Current Status: ~70% Complete

### What Works
- Core generation engine (Generator.cs)
- Recipe system with all 173 recipe files
- Basic character editing UI
- All character card formats (TavernV2, TavernV3, Ginger, Agnaistic, Pygmalion, Faraday, TextGenWebUI, CHARX, BYAF)
- Export to multiple formats (PNG, JSON, YAML, CHARX, BYAF)
- 15 dialogs implemented (FileFormatDialog, VariablesDialog, RearrangeActorsDialog, AssetViewDialog, EnterUrlDialog, PasteTextDialog, EnterNameDialog, CreateRecipeDialog, CreateSnippetDialog, etc.)
- Backyard connection with Push/Pull sync and bulk export
- AppSettings with JSON persistence

### Remaining Gaps
- 4 dialogs still missing (EditModelSettingsDialog, LinkEditChatDialog, etc.)
- No chat log support
- Spell checking not wired to UI (service exists)

---

## Phase 1: File Format Support (Priority: Critical)

### 1.1 Integrate Existing Parsers
Files exist but aren't wired into CharacterCardService.LoadAsync():

| Format | Parser File | Action |
|--------|-------------|--------|
| GingerCardV1 | `Models/Formats/CharacterCards/GingerCardV1.cs` | Integrate into loader |
| TavernCardV3 | `Models/Formats/CharacterCards/TavernCardV3.cs` | Integrate into loader |
| AgnaisticCard | `Models/Formats/AgnaisticCard.cs` | Integrate into loader |
| PygmalionCard | `Models/Formats/PygmalionCard.cs` | Integrate into loader |
| TextGenWebUI | `Models/Formats/TextGenWebUICard.cs` | Already partial |

### 1.2 Copy Missing Format Parsers from Original
Source: `source/src/Model/Formats/CharacterCards/`

| Format | Original File | Lines |
|--------|---------------|-------|
| TavernCardV1 | TavernCardV1.cs | ~200 |
| FaradayCardV1 | FaradayCardV1.cs | ~150 |
| FaradayCardV2 | FaradayCardV2.cs | ~200 |
| FaradayCardV3 | FaradayCardV3.cs | ~250 |
| FaradayCardV4 | FaradayCardV4.cs | ~400 |

### 1.3 FileFormatDialog
Create dialog for export format selection:
- PNG (embedded JSON)
- JSON (plain)
- YAML
- CHARX (zip archive)
- Backyard Archive (.byaf)

---

## Phase 2: Backyard Integration (Priority: Critical)

### 2.1 Complete Push/Pull Commands
Location: `ViewModels/MainViewModel.cs` lines 1556-1617

**PushChanges Implementation:**
```
1. Get current link from Current.Link
2. Validate connection to Backyard database
3. Convert Current.Character to FaradayCard format
4. Call Integration.Backyard.UpdateCharacter()
5. Update link timestamp
6. Show success/error status
```

**PullChanges Implementation:**
```
1. Get current link from Current.Link
2. Validate connection to Backyard database
3. Call Integration.Backyard.ImportCharacter()
4. Convert FaradayCard to Current.Character
5. Refresh UI bindings
6. Mark as clean (not dirty)
```

### 2.2 Complete Import from Browser
Location: `ViewModels/MainViewModel.cs` line 1451

**Import Implementation:**
```
1. Call Integration.Backyard.ImportCharacter(characterId)
2. Convert FaradayCard to GingerCharacter
3. Load into Current.Instance
4. Create link if user requested
5. Refresh all UI bindings
```

### 2.3 Backyard Utilities
Copy from `source/src/Utility/Integration/`:

| Utility | Purpose | Priority |
|---------|---------|----------|
| BulkExporter | Export multiple characters | Medium |
| BulkImporter | Import from folder | Medium |
| BackupUtil | Backup/restore | Low |

---

## Phase 3: Missing Dialogs (Priority: High)

### 3.1 Critical Dialogs

**FileFormatDialog** - Export format picker
```
- ComboBox for format selection
- Preview of output structure
- Option checkboxes (embed image, include lore, etc.)
```

**VariablesDialog** - Custom variables editor
```
- DataGrid of name/value pairs
- Add/Remove/Edit buttons
- Variable reference preview
```

**RearrangeActorsDialog** - Multi-actor management
```
- ListBox with drag-drop reorder
- Add Character / Remove Character buttons
- Actor name editing
```

### 3.2 Medium Priority Dialogs

**AssetViewDialog** - View/manage embedded assets
```
- Grid of asset thumbnails
- Import/Export/Delete buttons
- Asset metadata display
```

**LinkEditChatDialog** - Edit Backyard chat settings
```
- Chat history list
- Message editing
- Regenerate/Delete options
```

### 3.3 Low Priority Dialogs

| Dialog | Purpose |
|--------|---------|
| EnterUrlDialog | URL input for web imports |
| PasteTextDialog | Handle pasted character data |
| EditModelSettingsDialog | Backyard model parameters |

---

## Phase 4: Chat Log Support (Priority: Medium)

### 4.1 Core Chat Infrastructure
Create: `Models/Formats/ChatLogs/`

**Base Classes:**
- ChatLog.cs - Base chat log structure
- ChatMessage.cs - Individual message
- ChatExporter.cs - Export interface
- ChatImporter.cs - Import interface

### 4.2 Format Implementations
Copy from `source/src/Model/Formats/ChatLogs/`:

| Format | Use Case | Priority |
|--------|----------|----------|
| BackyardChatV2 | Backyard sync | High |
| GingerChatV2 | Native format | High |
| TavernChat | SillyTavern compat | Medium |
| TextFileChat | Plain text | Medium |
| AgnaiChat | Agnai.chat | Low |

---

## Phase 5: UI Enhancements (Priority: Medium)

### 5.1 Spell Checking Integration
- Wire SpellCheckService to text editors
- Dictionary files already in `Dictionaries/`
- Use WeCantSpell.Hunspell NuGet package

### 5.2 Token Counter
- Add token estimation to Generator output
- Display in status bar (already has placeholder)
- Update on text changes

### 5.3 Recipe Categories
- Collapsible groups in recipe list
- Category headers with expand/collapse
- Remember expanded state

### 5.4 Output Preview Modes
- SillyTavern format preview
- Faraday format preview
- Plain text preview
- JSON structure preview

---

## Phase 6: Polish & Completeness (Priority: Low)

### 6.1 Undo/Redo Enhancement
- Expand UndoService for all operations
- Recipe add/remove/reorder
- Parameter changes
- Lorebook edits

### 6.2 Clipboard System
Copy from original:
- RecipeClipboard.cs
- LoreClipboard.cs
- Enable copy/paste between instances

### 6.3 Templates
- New from template dialog
- Template selection in New command
- Template files in Content/en/Templates/

### 6.4 Update Checker
- GitHub release check on startup
- Optional update notification
- Link to releases page

---

## Implementation Order (Recommended)

### Week 1: Critical File Support
1. [x] Integrate GingerCardV1 parser
2. [x] Integrate TavernCardV3 parser
3. [x] Integrate AgnaisticCard parser
4. [x] Integrate PygmalionCard parser
5. [x] Create FileFormatDialog
6. [x] Wire format selection to Save/Export

### Week 2: Backyard Completion
7. [x] Implement PushChanges command
8. [x] Implement PullChanges command
9. [x] Implement full import in BrowseBackyard
10. [ ] Copy and adapt BulkExporter
11. [ ] Copy and adapt BulkImporter

### Week 3: Missing Dialogs
12. [x] Create VariablesDialog
13. [x] Create RearrangeActorsDialog
14. [x] Create AssetViewDialog
15. [x] Wire dialogs to menu commands

### Week 4: Chat & Polish
16. [ ] Implement BackyardChatV2
17. [ ] Implement GingerChatV2
18. [ ] Wire spell checking
19. [ ] Add token counter
20. [ ] Implement clipboard system

---

## Files to Copy from Original

### Direct Copy (minimal changes)
```
source/src/Model/Formats/CharacterCards/TavernCardV1.cs
source/src/Model/Formats/CharacterCards/FaradayCardV1.cs
source/src/Model/Formats/CharacterCards/FaradayCardV2.cs
source/src/Model/Formats/CharacterCards/FaradayCardV3.cs
source/src/Model/Formats/CharacterCards/FaradayCardV4.cs
source/src/Model/Formats/ChatLogs/*.cs (all 9 files)
source/src/Utility/Clipboard/RecipeClipboard.cs
source/src/Utility/Clipboard/LoreClipboard.cs
```

### Adapt for Avalonia
```
source/src/Interface/Forms/Dialogs/FileFormatDialog.cs → AXAML
source/src/Interface/Forms/Dialogs/VariablesDialog.cs → AXAML
source/src/Interface/Forms/Dialogs/RearrangeActorsDialog.cs → AXAML
source/src/Interface/Forms/Dialogs/AssetViewDialog.cs → AXAML
```

---

## Success Criteria for 100% Parity

### Must Have
- [ ] All character card formats load correctly
- [ ] All character card formats save correctly
- [ ] Backyard Push/Pull work end-to-end
- [ ] All 19 dialogs implemented
- [ ] Settings persist between sessions
- [ ] Multi-actor characters work

### Should Have
- [ ] Chat log import/export
- [ ] Spell checking works
- [ ] Token counting works
- [ ] Clipboard copy/paste works

### Nice to Have
- [ ] Update checker
- [ ] Syntax highlighting
- [ ] All keyboard shortcuts

---

## Estimated Effort

| Phase | Effort | Dependencies |
|-------|--------|--------------|
| Phase 1: File Formats | 2-3 days | None |
| Phase 2: Backyard | 2-3 days | Phase 1 |
| Phase 3: Dialogs | 3-4 days | None |
| Phase 4: Chat Logs | 2-3 days | Phase 1 |
| Phase 5: UI Polish | 2-3 days | Phases 1-3 |
| Phase 6: Completeness | 2-3 days | All above |

**Total: ~15-20 days for 100% parity**
