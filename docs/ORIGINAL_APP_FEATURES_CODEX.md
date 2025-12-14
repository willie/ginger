**Usage Modes & Primary Flows**
- Create new card from recipes: start blank/new-from-template, add recipes by category buttons, fill parameters, regenerate preview, bake when ready (MainForm.Designer buttons, MainFunctions.Regenerate/BakeAll).
- Write from scratch: open Write Dialog for longform editing, then paste into recipe text parameters or bake outputs (WriteDialog files, RecipePanel text fields).
- Import/remix existing: load PNG/JSON/CHARX/YAML/BYAF, recipes/lore auto-populated, edit panels, regenerate, export to chosen format (MainFunctions.ImportCharacter/ImportLorebook/Export*).
- Convert formats: select preview/export format (Default/SillyTavern/Faraday/FaradayParty/PlainText), export to PNG/JSON/CHARX/YAML/Backyard/Agnai/Pygmalion/TextGenWebUI, or Ginger multi-format PNG/Character Backup ZIP via format picker (AppSettings.Settings.OutputPreviewFormat, FileUtil, FileFormatDialog).
- Group/party authoring: add actors via Additional Characters menu, per-actor recipes/portraits, bake actor individually or preview FaradayParty outputs (RecipePanel greetings, MainFunctions.BakeActor, OutputPreview party handling).
- Backyard-linked editing: connect, import characters/parties, live edit and save back, reestablish/break links, bulk import/export, manage chats and model settings, create/restore backups (BackyardFunctions.*).
- Lorebook creation/remix: add lore recipes, edit entries with paging/filtering, merge lorebooks, rearrange entries, import/export multiple worldbook formats (LorebookParameterPanel, mergeLoreMenuItem).
- Snippet/recipe authoring: save any recipe panel as snippet/recipe, create snippets from text selection, reload recipe library, toggle NSFW visibility (RecipePanel events, Tools menu).
- Backup/repair utilities: BYAF backup/restore, repair broken images/legacy chats, purge unused images, reset model settings/locations (BackyardFunctions utilities).

**Port Completeness Checklist (must-implement behaviors)**
- Screen parity: SidePanel groups, Recipe tab with add-row buttons, Output tab preview, Notes tab; status bar with token/connection/asset indicators; menus and dialogs callable via shortcuts and menu entries.
- File I/O: open/save/revert/incremental save; import PNG/JSON/CHARX/YAML/BYAF; export PNG/JSON/CHARX/YAML/Backyard plus Ginger multi-format PNG embed and Character Backup ZIP; supports Agnai/Pygmalion/TextGenWebUI outputs; preserve/round-trip embedded assets, portraits/backgrounds, alt greetings, lorebooks, MRU list, window positions; expose format picker (FileFormatDialog) with solo/group variants.
- Chat logs: import/export/read Tavern/TextGenWebUI/Agnai/Ginger chat logs and backups for Backyard utilities and staging (Models/Formats/ChatLogs).
- Generation pipeline: Regenerate honors all include/exclude toggles, persona placement flags, scenario prune, style grammar, detail level, text style, preview format; token counts reflect selected format; Bake All/Bake Actor produce editable recipes matching output text including lore and greetings.
- Recipes UI: add/reorder/enable/disable/collapse; per-recipe detail level, raw toggle, NSFW flag; bake single; set primary greeting; save as snippet/recipe; save/rename lorebook; copy/paste with undo; syntax highlighting toggle respects performance settings.
- Parameters: all parameter types (text/number/range/list/multi-choice/choice slider/boolean/measurement/hint/lorebook/chat parameters/actor choice/code); undo support; placeholder replacement (user/character markers) and gender swap/gender overrides applied across text and lore.
- SidePanel behaviors: character name lock when linked group member; user placeholder disabled when AutoConvertNames off; creator/notes/version/tags; gender and user gender overrides; detail level/text style; lore count; background and portrait replace/paste/remove/resize; background effects blur/darken/desaturate; include toggles and persona placement; grammar styling toggle; collapse state persisted.
- Output preview logic: mode-specific section labels, persona merge rules, `{original}` replacement, combining system/post-history when author note disabled, party preview for FaradayParty; Plain Text render option; author note and user persona include/omit rules honored.
- Group/actor handling: actor dropdown/additional characters menu; per-actor portraits/assets; actor-specific recipes; primary/alt greetings preserved; Bake Actor uses current actor context; placeholder resolution uses actor names.
- Lorebooks: paging/filtering/add/remove, keyphrase/position/tags/probability fields, token counts; merge lorebooks; rearrange lore mode; import/export Tavern/Agnaistic/Ginger worldbooks.
- Clipboard/snippets: snippet creation and insertion; recipe/snippet save from panel; clipboard staging for chat parameters/lore/recipes/chat messages.
- Find/replace: find, find next/previous, replace with match-case/whole-word options; optional lorebook replace; remembers last settings/location.
- Spellcheck: enable/disable, dictionary selection; auto-disable on init failure; applies to text boxes.
- Token budget: selectable presets displayed in UI; counts stored per format (total/permanent) and refresh on change.
- Undo/redo: covers recipe list mutations, parameter edits, text edits, lore edits; suspend/resume during batch ops (bake/import).
- Backyard integration: connect/disconnect; import/export/update characters and parties; create new linked character/party; reestablish/break link; reimport; chat history; model settings edit/bulk edit; bulk import/export/delete; backup/restore; repair broken images/legacy chats; reset models location/settings; purge unused images; options (autosave, always link on import, chat settings apply scope, portrait as background, import alt greetings, write user persona/author note, edit export model settings).
- Assets: embedded asset viewer; portrait/background overrides per actor; use portrait as background option; animation tags respected; purge unused images utility.
- Localization/theme/fonts: locale selection, Hunspell dictionaries; light/dark themes; global font setting; form-level double buffering toggle; recipe list gradient; icons.
- Templates/recipes/snippets: built-in recipe library load/reload; template-based new cards/snippets; NSFW visibility gated by settings; show NSFW recipes toggle respected.
- Updates/help: check for updates via GitHub; open help/GitHub/about dialogs.

**Essential UI (Required/High-Impact Controls)**
- SidePanel Card Info/User groups: character name (locked for linked group member), user placeholder, spoken name, creator fields, tags; detail level and text style directly affect generation (SidePanel.RefreshValues).
- SidePanel Output Settings/Components: include/exclude toggles for system/post-history, scenario, attributes/personality, greetings, example, lore, user persona, grammar; persona placement (persona vs scenario), scenario prune toggle, style grammar toggle—these gates control what Regenerate outputs (SidePanel checkboxes, rbUserInPersona/rbUserInScenario, cbPruneScenario, cbStyleGrammar).
- Token display: total/permanent token counts and budget in SidePanel and status bar; choose budget presets from Options menu (TokenQueue_onTokenCount, tokenBudget menu).
- Add-row buttons: category-specific buttons to append recipes (Model/Character/Mind/Traits/World/Other/Snippets/Lore); core entry point for building content (MainForm.Designer buttonRow).
- RecipePanels: expand/collapse, enable/disable, raw toggle, NSFW toggle, per-panel detail level; reorder controls are essential for output ordering; primary greeting marker for alt greetings (RecipePanel events).
- OutputPreview tab: verifies final composed text per format, merges persona/user persona rules, shows lore/grammar; Plain Text preview for rendered (OutputPreview.SetOutput).
- Notes tab: persistent per-card notes for authors; optional but stored in file (UserNotes).
- Additional Characters menu: actor switching in group cards; required to edit specific actor recipes/portrait (MainForm.Designer additionalCharactersMenuItem).
- Embedded Assets menu and portrait/background controls: manage required images; use portrait-as-background option (SidePanel events, embeddedAssetsMenuItem).
- Backyard Link menu: connect/disconnect, import/export, link management, autosave toggle; mandatory for live Backyard workflows (BackyardFunctions).

**Main Window Layout**
- Split view: left `SidePanel` for metadata/settings, right tab control with `Recipes`, `Output`, `Notes` (MainForm.Designer).
- Recipe tab: scrollable `RecipeList` plus add-buttons row for `Model`, `Character`, `Mind`, `Traits`, `World`, `Other`, `Snippets`, `Lore`.
- Output tab: formatted preview (`OutputPreview`) with optional debug raw JSON boxes in debug builds.
- Notes tab: freeform rich text notes (`UserNotes`) stored per character file.

**Side Panel: Card & User Metadata**
- Character name/spoken name/user placeholder with undo; name locked for linked group member; user placeholder editable only when AutoConvertNames enabled (SidePanel.cs).
- Creator name, creator notes/comment, version string, comma-separated tags (SidePanel.RefreshValues).
- Gender: preset dropdown + custom field + per-actor overrides; user gender separate; changes propagate to placeholders (SidePanel.RefreshGender, comboBox_gender/userGender handlers).
- Detail level selector and text style selector; per-card defaults driving generation flavor (comboBox_Detail/textStyle).
- Token stats: total tokens and permanent tokens (Faraday/SillyTavern) pulled from tokenizer queue; lore count display (TokenQueue_onTokenCount, SetLoreCount).
- Include/exclude toggles: Model instructions, Scenario, Attributes, Personality, Greeting(s), Example chat, Lorebook, User persona, Grammar; persona placement toggles and scenario prune; style grammar toggle (tableFilters, rbUserInPersona/rbUserInScenario, cbPruneScenario, cbStyleGrammar).
- Collapse/expand groups: Card Info, User, Output Settings, Output Components, Background, Stats (Group_* handlers).

**Side Panel: Portrait & Background Management**
- Portrait change/resize/paste/remove; animated tag detection; per-actor portrait overrides (ChangePortraitImage/ResizePortraitImage events, RefreshValues).
- Background change/from-portrait/paste/remove; preview respects sizing; greys out when missing (ChangeBackgroundImage, BackgroundFromPortrait, RefreshBackgroundPreview).
- Background effects: blur, darken, desaturate via event (BackgroundEffect, BackgroundImageEffect).

**Recipe List & Panels**
- RecipePanel controls: expand/collapse, reorder (up/down/top/bottom), remove, enable/disable, toggle raw text, toggle NSFW flag, change detail level, bake single recipe, save as snippet/recipe, save/rename lorebook, set primary greeting, copy/paste (RecipeList.AddRecipePanel wiring).
- Layout: add multiple panels at once, manual layout with gradient background/shadow, hide horizontal scrollbar; updates on idle (OnPaint, IIdleHandler).
- Syntax highlighting toggle for performance; refresh parameter visibility/layout; scroll to panel or top (RefreshSyntaxHighlighting, ScrollToPanel/ScrollToTop).
- Clipboard integration with undo for copy/paste recipes (OnCopy/OnPaste).

**Recipe Management & Generation Nuances**
- Regenerate applies selected preview format: Default, SillyTavern V2, Faraday, Faraday party (multi-actor), Plain text rendered (MainFunctions.Regenerate).
- Bake All converts output back into editable recipes for all channels; Bake Actor does per-actor bake inside group context (BakeAll, BakeActor).
- Parameter types supported: text, number, range, list, multi-choice, choice slider, boolean/toggle, measurement, hint, lorebook, chat parameters, actor choice, code fields (Interface/Parameters/*).
- NSFW recipes hidden unless AllowNSFW+ConfirmNSFW settings permit; toggle per panel (AppSettings.Settings.AllowNSFW).
- Detail level per recipe affects inclusion/formatting; raw toggle bypasses baking formatting (RecipePanel).

**Output Preview**
- Sections: Model instructions, Post-history instructions, Character persona, Personality summary, User persona, Scenario, Example chat, Greeting(s)/First message, Lorebook entries, Grammar, Author note (OutputPreview.SetOutput).
- Mode-specific logic: SillyTavern labels vs Faraday labels; Faraday merges author note/system when Backyard author note disabled; `{original}` placeholder replaced with model-specific text (OutputPreview logic).
- Persona merge: user persona can merge into persona or scenario when Faraday/SillyTavern or when user persona writing disabled (OutputPreview logic).
- Party preview: shows selected actor output when FaradayParty selected (MainFunctions.Regenerate).

**Notes**
- Rich text with word wrap, forced vertical scroll, accepts tab, no syntax highlighting; marks card dirty on change (UserNotes.cs).

**File Menu Functions**
- New, New window, New from template; Open; MRU list; Save/Save As; incremental save; revert file; persists window placement/paths (MainFunctions, MRUList).
- Import character: PNG (embedded JSON chunks/exif), JSON, CHARX (SillyTavern v3), YAML with portrait sidecar, Backyard Archive `.byaf` (ImportCharacter).
- Import lorebook: supports multiple filters/formats (ImportLorebook).
- Export character: PNG/JSON/CHARX/YAML/Backyard; export lorebook similarly (Export*).
- Change UI language and spellcheck dictionary; exit with save confirmation (changeLanguageMenuItem, OnClosing).

**Edit Menu Functions**
- Undo/redo via global stack; copy/paste recipe text; find/find next/find previous/find & replace (including lorebooks); gender swap/pronoun replacement (ReplaceNamePlaceholders, swapGenderMenuItem).

**View Menu Functions**
- Tab shortcuts to Recipe/Output/Notes; embedded assets viewer; custom variables dialog (`$variable` pairs); collapse/expand all recipes; toggle recipe category headers; sort recipes; actor selection (view menu items, additionalCharactersMenuItem).

**Options Menu**
- Token budget presets; output preview format selection; enable/disable spell checking and dictionary; auto-convert names toggle; auto line-break toggle; rearrange lore mode; show NSFW recipes (AppSettings.Settings flags).

**Tools Menu**
- Create recipe/snippet dialogs; reload recipes; merge lorebooks; bake all; bake actor (Tools menu handlers).

**Backyard AI Integration (Link Menu)**
- Connect/disconnect with validation; import Backyard character/party with optional live link; pull portraits/icons/backgrounds and optional user persona (ConnectToBackyard, ImportCharacterFromBackyard, ImportGroupFromBackyard).
- Save as new or update linked instance; chat staging for alternate greetings; author note/user persona write options; handle group member staging/clearing (SaveCharacterAsNewToBackyard, SaveCharacterChangesToBackyard, UpdateCharacterInBackyard).
- Reestablish/break link; reimport linked content; chat history browser (ReestablishLink, BreakLink, OpenChatHistory).
- Model settings: edit current chat parameters, bulk edit across characters/parties; bulk import/export/delete; backups and repairs; reset models location/settings; purge unused images (BackyardFunctions).
- Options: autosave linked data, always link on import, apply chat settings to last/first/all chats, use portrait as background, import alt greetings into chats, write user persona/author note, edit export model settings (optionsToolStripMenuItem1 entries).

**Write Dialog & Text Editing Utilities**
- Write dialog: syntax highlighting, word wrap, name/number/pronoun highlighting, configurable font/size/location, auto line-break option (AppSettings.WriteDialog).
- Additional dialogs: Paste Text (linebreak handling), Enter URL, Find/Replace, Create Snippet, Rearrange Actors, Link Edit Chat, Progress dialogs, Gender Swap configuration (Dialogs folder).

**Lorebook Management**
- Lorebook parameter editor: paging, filters, add/remove entries, keyphrase/position fields, per-entry tags/probabilities, token count display (LorebookParameterPanel/LorebookEntryPanel).
- Merge lorebooks, import/export multiple worldbook formats (Tavern, Agnaistic, Ginger); Rearrange Lore mode for ordering (mergeLoreMenuItem, EnableRearrangeLoreMode).

**Character & Group Support Nuances**
- Multi-actor cards: actor dropdown, per-actor portraits, group greetings, primary greeting selection, alternative greetings preserved when exporting (RecipePanel greeting features).
- Placeholder automation: AutoConvertNames replaces `{character}`/`{user}` markers and performs whole-word replacements across parameters and lore (ReplaceNamePlaceholders).
- Extra flags: include/exclude components, persona placement flags, scenario pruning; user persona write vs merge controlled by settings (SidePanel SetExtraFlag usage).

**Assets Handling**
- Embedded assets (portraits/icons/backgrounds) stored in card asset list; asset viewer shows metadata; supports animated tags and per-actor overrides (AssetViewDialog, AssetImageCache).
- Clipboard paste for portrait/background; resize portrait; use portrait as background option (SidePanel events, Backyard options).

**Spell Check & Dictionaries**
- Hunspell spell checking with en_US/en_GB; toggle on/off; dictionary selection; initialization failure auto-disables (SpellChecker.Initialize).

**Tokenizer & Budgets**
- Async tokenizer queue counts tokens for current output; stores per-format permanent counts; updates SidePanel/status bar; respects token budget presets (TokenizerQueue_onTokenCount).

**Undo/Redo Infrastructure**
- Global undo stack covers recipe list changes, parameter edits, text edits, lore edits; suspend/resume during batch ops (Undo.cs, RecipeList).

**Status Bar**
- Shows transient status messages, actor count, embedded assets indicator, Backyard connection icon, token info refreshed by timer (MainForm.Designer status bar items).

**File Format Compatibility**
- Character cards: Ginger XML (multi-format PNG embed), SillyTavern V2/V3 (PNG chunks chara/ccv3), Faraday (Backyard AI), Agnai/Pygmalion JSON, TextGenWebUI YAML, Character Backup ZIP, BYAF archives (Resources/Schemas, Model/Formats/*).
- Lorebooks/worldbooks: Tavern worldbook, Agnaistic characterbook, Ginger lorebook; import/export through LorebookParameter/FileUtil.
- Chat logs: Tavern, TextGenWebUI, Agnai, Ginger backups; used for Backyard utilities and chat staging (Models/Formats/ChatLogs/*).
- Backups: Backyard Archive BYAF import/export for characters, scenarios, manifests (Model/Formats/BackyardArchive/*).

**Themes & Appearance**
- Light/Dark theme toggle; form-level double buffering; global font setting; recipe list gradient/background styling; icon sets for light/dark (AppSettings.Settings.DarkTheme, EnableFormLevelBuffering, Resources/Icons).

**Localization**
- Locale selection; content packs for recipes/templates; Hunspell dictionaries shipped (Content/en, Dictionaries/*.dic).

**Templates, Recipes, Snippets**
- Built-in recipes (150+ across Character/Story/NSFW/Personality/etc.) and templates for assistant/companion/chat-bot/roleplay/classic (Content/en/Recipes, Content/en/Templates, Resources/Templates).
- Snippet storage/insertion; save recipe as snippet via panel; snippet creation dialog uses template (snippet_template.txt).

**Find/Replace & Clipboard Helpers**
- Find dialog remembers match settings/position; Replace dialog can target lorebooks optionally; clipboard staging for chat parameters, lore, recipes, chat messages (FindReplaceDialog, Models/Clipboard/*).

**Autosave & MRU**
- Autosave for Backyard-linked data; incremental saves; MRU list for recent files; persisted window locations and find dialog state (AppSettings.User, MRUList).

**Update & Help**
- Check for updates via GitHub release JSON; Help/GitHub/About menu entries (CheckLatestRelease, About dialog).
