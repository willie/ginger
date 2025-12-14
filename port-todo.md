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

- Core data flow / preview accuracy
  - Sync UI fields (name, persona, scenario, greetings, user placeholder/gender, text style/detail, notes, etc.) into `Current` before `RegenerateOutput`/bake so the generator uses current edits; today property setters only mark dirty and the preview/export/backyard paths still read stale `Current` data (MainViewModel.cs).

- Recipe parameter UI coverage
  - Add UI/editor support for list, multi-choice, measurement, range sliders, actor-choice, lorebook/chat parameters, and code/text-mode options; current UI only renders text/bool/number/single-choice and hides set-var/set-flag/erase/hint so many recipe types are unusable (MainWindow.axaml: recipe parameter templating).

- Assets handling
  - Per-actor portrait overrides, animation tags display, portrait resize, asset viewer parity with metadata, purge unused images utility wiring in UI.

- Lorebook UI parity
  - Rearrange lore mode toggle; merge lorebook flow.

- Output generation logic
  - Honor include grammar/user persona toggles, author note write/omit rules, post-history combine rules, scenario omission flags, user persona merge vs separate; ensure party preview covers all actors and alt/group greetings.

- Backyard integration UI gaps
  - Ensure push/pull/revert/save-as-new/new party, chat history, bulk ops, backups, repairs, reset model location/settings, purge unused images all exposed and wired.

- Clipboard/snippet flows
  - Save-as-snippet/recipe actions wired to dialogs with proper content (including grammar/post-history/user persona when present).

- Localization/theme
  - Ensure locale menu aligns with content packs.

- Tests/checks
  - Verify generator output matches include flags and per-recipe ordering after reordering/insert; ensure Current.Character.recipes stays in sync with UI operations (add/remove/reorder/paste).

- Actor handling parity
  - Implement per-actor portrait/background overrides and paste/resize flows; sidebar uses single shared portrait/background for all actors.

- Undo/redo
  - Hook edits into `UndoService` (RecordAction/RecordPropertyChange) so Undo/Redo work; currently the service is unused and commands do nothing (UndoService.cs, MainViewModel.cs).
