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
  - Move up/down/remove buttons are always visible (no need for toggle mode).

- Core data flow / preview accuracy [DONE]
  - Plain Text output preview mode.
  - Output toggles & include flags hooked to Generator.
  - SyncToCurrent() called before RegenerateOutput() to ensure Generator uses latest UI values.
  - Added textStyle and detailLevel to SyncToCurrent().

- MRU / Recent Files [DONE]
  - Populate "Open Recent" list from AppSettings and update on open/save.

- Output generation logic [DONE]
  - All omit flags implemented in Generator.cs and wired from MainViewModel property change handlers.
  - Author note write/omit, post-history combine, user persona merge all handled.
  - Party preview covers all actors and alt/group greetings.

- Backyard integration UI [DONE]
  - All features wired: push/pull, revert, save-as-new, new party, chat history, bulk ops, backups, repairs, reset model location/settings, purge unused images.

- Localization/theme [DONE]
  - Both implementations have only `en` locale - already aligned.

- Tests/checks [DONE]
  - Generator output respects include flags.
  - Recipe ordering synced via SyncToCurrent() before generation.
  - Current.Character.recipes stays in sync via undo-aware add/remove/reorder operations.

- Actor handling parity [DONE]
  - Visible actor selector dropdown for multi-actor editing.
  - Per-actor portrait display when switching actors.
  - Per-actor portrait load/clear commands.
  - Fallback display (dimmed main portrait) when actor has no portrait.

- Recipe parameter UI coverage [DONE]
  - Added UI for list, multi-choice, range (slider), measurement parameters.
  - Actor-choice parameters with dynamic user/actor selection.
  - Lorebook/chat parameters not needed (rarely used in recipes).

## Partial Implementation

- Undo/redo [PARTIAL]
  - Works for: add/remove recipes, reorder recipes, add/remove lorebook entries, move lorebook entries.
  - Clear undo history on New and LoadFile.
  - Still missing: text field changes, property edits.

- Clipboard/snippet flows [PARTIAL]
  - Save-as-snippet/recipe actions implemented with simplified dialogs.
  - Full multi-channel snippet dialog not yet ported.

## Remaining

- Assets handling
  - Animation tags display, portrait resize, asset viewer parity with metadata.
  - Per-actor background override (needs per-actor asset selection).
