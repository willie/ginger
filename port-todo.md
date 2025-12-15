Port Parity TODO (Avalonia vs Original WinForms)

**Status: 2 parity gaps remain** (updated December 2024)

## Open Issues

- **Faraday Party preview** still single-character only (`source.avalonia/ViewModels/MainViewModel.cs:RegenerateOutput`). WinForms uses `Generator.GenerateMany()` and array-based preview to show each actor's persona/lore/greetings. Port always calls `Generator.Generate(options)` and formats a single output string, so party previews omit all secondary actors.

- **Background images not persisted**. UI keeps `_backgroundData` for preview/effects but `LoadFromCard`/`ToCard` never read/write background assets, so backgrounds are lost on save/load and aren't included in embedded assets/Backyard flows. WinForms stores background in `Current.Card.assets`.

## Recently Fixed (December 2024)

- ✅ **Token budget reporting** - `PermanentTokensFaraday`/`PermanentTokensSillyTavern` now set in `OnTokenCountCompleted()`. Stats grid bindings work.

- ✅ **Orphaned code cleaned up** - Deleted 10 orphaned files:
  - Chat formats: TextFileChat.cs, AgnaiChat.cs, GingerChatV1.cs, BackyardChatBackupV1.cs, TextGenWebUIChat.cs
  - Services: BulkUtilities.cs, GeneratorService.cs
  - Clipboard: ChatClipboard.cs, ChatParametersClipboard.cs, ChatStagingClipboard.cs

- ✅ **Dead properties removed** - Deleted unused `_filter*` legacy properties from MainViewModel

- ✅ **SyntaxHighlightService** - Now wired to WriteDialog for syntax highlighting
