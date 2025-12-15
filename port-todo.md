# Port Parity TODO (Avalonia vs Original WinForms)

**Status: 2 gaps remain**

## Open Issues

### Faraday Party preview is single-character only
`RegenerateOutput()` uses `Generator.Generate()` for UI preview even when format is `FaradayParty`. Should use `GenerateMany()` and show each actor's persona/lore/greetings like WinForms does.

Note: The actual Backyard push correctly uses `GenerateMany()` - only the UI preview is affected.

### Background images not persisted
`_backgroundData` is used for UI preview/effects but `LoadFromCard`/`ToCard` never read/write it. Backgrounds are lost on save/load and not included in embedded assets or Backyard flows.
