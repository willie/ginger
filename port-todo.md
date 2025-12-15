Port Parity TODO (Avalonia vs Original WinForms)

## Completed

- Recipe panel parity [DONE]
  - Per-recipe actions: save as snippet, save as recipe, bake single, set primary greeting.
  - Per-recipe toggles: raw/text-formatting toggle, NSFW flag, per-recipe detail level selector.
  - Per-recipe category badge display.

- Side panel / output controls [DONE]
  - Include/omit toggles for output sections.
  - Persona placement flags (user persona in scenario, style grammar toggle).
  - Permanent tokens per format display.
  - Spellcheck dictionaries selection (en_US/en_GB).

- Toolbar / add buttons [DONE]
  - Category buttons for Other, Snippets, Lore, Components with correct drawer/category mapping.

- Lorebook UI parity [DONE]
  - Copy/paste/duplicate lore entries.
  - Import/export formats (Ginger, SillyTavern/Tavern WorldBook).

- Core data flow / preview accuracy [PARTIAL]
  - Plain Text output preview mode. [DONE]
  - Output toggles & include flags hooked to Generator. [DONE]

- MRU / Recent Files [DONE]
  - Populate "Open Recent" list from AppSettings and update on open/save.

- Actor handling parity [PARTIAL]
  - Visible actor selector dropdown for multi-actor editing. [DONE]

## Remaining

- Side panel / output controls
  - Per-actor background override.

- Core data flow / preview accuracy [DONE]
  - SyncToCurrent() now called before RegenerateOutput() to ensure Generator uses latest UI values.
  - Added textStyle and detailLevel to SyncToCurrent().

- Recipe parameter UI coverage [PARTIAL]
  - Added UI for list (comma-separated text), multi-choice (checkbox list), range (slider with value display), measurement (numeric + unit). [DONE]
  - Actor-choice, lorebook/chat parameters remain to be done.

- Assets handling
  - Per-actor portrait overrides, animation tags display, portrait resize, asset viewer parity with metadata, purge unused images utility wiring in UI.

- Lorebook UI parity [DONE]
  - Move up/down/remove buttons are always visible (no need for toggle mode).
  - Merge lorebook flow handled via import/export.

- Output generation logic [DONE]
  - All omit flags implemented in Generator.cs and wired from MainViewModel property change handlers.
  - Author note write/omit, post-history combine, user persona merge all handled.
  - Party preview covers all actors and alt/group greetings.

- Backyard integration UI gaps [DONE]
  - All features wired: push/pull, revert, save-as-new, new party, chat history, bulk ops, backups, repairs, reset model location/settings, purge unused images.

- Clipboard/snippet flows [PARTIAL]
  - Save-as-snippet/recipe actions implemented with simplified dialogs. Full multi-channel snippet dialog not yet ported.

- Localization/theme [DONE]
  - Both implementations have only `en` locale - already aligned.

- Tests/checks [DONE]
  - Generator output respects include flags (verified in Generator.cs lines 626-647).
  - Recipe ordering synced via SyncToCurrent() before generation.
  - Current.Character.recipes stays in sync via undo-aware add/remove/reorder operations.

- Actor handling parity
  - Implement per-actor portrait/background overrides and paste/resize flows; sidebar uses single shared portrait/background for all actors.

- Undo/redo [PARTIAL]
  - Undo/Redo now work for: add/remove recipes, reorder recipes, add/remove lorebook entries, move lorebook entries.
  - Clear undo history on New and LoadFile.
  - Still missing: text field changes, property edits.
