# Port Parity TODO (Avalonia vs Original WinForms)

**Status: Complete - 100% feature parity achieved**

## Resolved Issues (December 2024)

### Faraday Party preview is single-character only
**FIXED**: `RegenerateOutput()` now uses `GenerateMany()` when format is `FaradayParty` and displays per-actor personas with headers like "CHARACTER PERSONA (Name)".

### Background images not persisted
**FIXED**: Background images are now stored in `Current.Card.assets` as embedded assets with `AssetType.Background`. All background commands (`LoadBackground`, `ClearBackground`, `PasteBackground`, `UsePortraitAsBackground`, blur/darken/desaturate effects) persist changes. `LoadFromCard()` restores backgrounds on load.
