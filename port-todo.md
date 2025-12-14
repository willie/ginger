Port Parity TODO (Avalonia vs Original WinForms)

- Recipe panel parity
  - Add per-recipe actions: save as snippet, save as recipe, save lorebook, rename lorebook, bake single, copy/paste per panel (clipboard already exists), set primary greeting, move to top/bottom.
  - Expose per-recipe toggles: raw/text-formatting toggle, NSFW flag (respect AllowNSFW settings), per-recipe detail level selector, per-recipe enable/disable.
  - Show per-recipe category badge and drawer, honor base/internal/component flags in layout.

- Side panel / output controls
  - Background image management: load/paste/remove/background-from-portrait, background preview, effects (blur/darken/desaturate), per-actor background override, use-portrait-as-background option.
  - Include/omit toggles: user persona, grammar, post-history instructions, omit scenario flag, include model/system, scenario, attributes/personality, greeting(s), example, lorebook.
  - Persona placement flags: user persona in persona vs scenario, prune scenario, style grammar toggle.
  - Token stats: permanent tokens per format, actor count, embedded asset indicator, Backyard connection status in status bar.
  - Spellcheck dictionaries selection (en_US/en_GB at minimum) in UI, not just a single toggle.

- Toolbar / add buttons
  - Restore category buttons for Other, Snippets, Lore (and ensure Components/Lore buttons are wired) with correct drawer/category mapping for adds.

- Assets handling
  - Per-actor portrait overrides, animation tags display, portrait resize, clipboard paste for portrait/background, asset viewer parity with metadata, purge unused images utility wiring in UI.

- Lorebook UI parity
  - Per-entry fields: tags, probability, keyphrase/position, selective/constant, insertion order; token counts per entry.
  - Rearrange lore mode toggle and entry move controls; merge lorebook flow; copy/paste lore entries; import/export formats (Tavern V2/V3, Agnaistic, Ginger) from UI.

- Output generation logic
  - Honor include grammar/user persona toggles, author note write/omit rules, post-history combine rules, scenario omission flags, user persona merge vs separate; ensure party preview covers all actors and alt/group greetings.

- Backyard integration UI gaps
  - Surface live link indicators, actor count, connection state; ensure push/pull/revert/save-as-new/new party, chat history, bulk ops, backups, repairs, reset model location/settings, purge unused images all exposed and wired.

- Clipboard/snippet flows
  - Per-recipe copy/paste in context menu (RecipeClipboard format) and save-as-snippet/recipe actions wired to dialogs with proper content (including grammar/post-history/user persona when present).

- Status bar
  - Add actor count, embedded assets presence, Backyard connection icon/state, token info refresh timer (currently only message/tokens/lore/recipes shown).

- Localization/theme
  - Language selection for spellcheck dictionaries; ensure locale menu aligns with content packs; maintain light/dark theme toggle.

- Tests/checks
  - Verify generator output matches include flags and per-recipe ordering after reordering/insert; ensure Current.Character.recipes stays in sync with UI operations (add/remove/reorder/paste).
