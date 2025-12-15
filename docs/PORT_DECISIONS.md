# Port Decisions: Why Changes Were Made

This document explains the rationale behind adaptation decisions made during the Windows Forms to Avalonia port.

---

## 1. SQLite Library Change

### What Changed
- **Original**: `System.Data.SQLite` (Windows-native wrapper)
- **Port**: `Microsoft.Data.Sqlite` (Pure .NET implementation)

### Why
`System.Data.SQLite` requires native Windows DLLs and is not cross-platform. `Microsoft.Data.Sqlite` is:
- Pure managed .NET code
- The official Microsoft SQLite provider for .NET Core/5+
- Cross-platform (Windows, macOS, Linux)
- Maintained as part of the .NET ecosystem

### Impact
~30 API changes across Backyard integration files:
- `SQLiteConnection` → `SqliteConnection`
- `SQLiteDataReader` → `SqliteDataReader`
- `SQLiteException` → `SqliteException`
- `TypeAffinity` enum removed (not available in Microsoft.Data.Sqlite)
- `DateTimeExtensions.FromUnixTime()` → `DateTimeOffset.FromUnixTimeMilliseconds()`

### Files Affected
- `Services/Backyard/Backyard.cs`
- `Services/Backyard/Revisions/BackyardDatabase_v28.cs`
- `Services/Backyard/Revisions/BackyardDatabase_v37.cs`

### Pattern Applied
Systematic find-replace of class/type names. Logic remains identical.

---

## 2. Image Processing Library Change

### What Changed
- **Original**: `System.Drawing` (GDI+ based)
- **Port**: `SkiaSharp` (Skia-based)

### Why
`System.Drawing` is:
- Windows-only (GDI+ dependency)
- Not supported on .NET Core for non-Windows platforms
- Deprecated for cross-platform use

`SkiaSharp` is:
- Avalonia's recommended graphics library
- Cross-platform (Windows, macOS, Linux, iOS, Android)
- GPU-accelerated where available
- Modern API design

### Impact
All image manipulation code rewritten:
- `System.Drawing.Image` → `SkiaSharp.SKBitmap`
- `System.Drawing.Bitmap` → `SkiaSharp.SKBitmap`
- `Graphics` operations → `SKCanvas` operations
- `ImageFormat` → `SKEncodedImageFormat`

### Files Affected
- `Services/ImageService.cs` (new)
- `Services/Backyard/BackupUtil.cs` (major restructuring)
- `Models/PngMetadata.cs`
- Format parsers with image handling

---

## 3. Configuration Storage Change

### What Changed
- **Original**: Windows Registry + INI files (`IniFileParser`)
- **Port**: JSON file (`%APPDATA%/Ginger/settings.json`)

### Why
Windows Registry:
- Not available on macOS/Linux
- Requires elevated permissions for some operations
- Not portable between machines

INI files:
- Required third-party parser (~30 files)
- Less structured than JSON

JSON configuration:
- Cross-platform
- Human-readable and editable
- Native .NET support (`System.Text.Json`)
- Easy to backup and transfer

### Impact
- Entire `IniFileParser` directory removed (~30 files)
- `AppSettings.cs` completely rewritten
- Settings now serialized as JSON

### Files Affected
- `Services/AppSettings.cs`

---

## 4. UI Architecture Change

### What Changed
- **Original**: WinForms code-behind with Designer files
- **Port**: MVVM with CommunityToolkit.Mvvm

### Why
WinForms code-behind:
- Tightly couples UI and logic
- Difficult to test business logic
- No reactive data binding

MVVM with CommunityToolkit.Mvvm:
- Avalonia's recommended pattern
- Clean separation of concerns
- Reactive data binding with `[ObservableProperty]`
- Commands with `[RelayCommand]`
- Easier to maintain and test

### Impact
- `MainForm.cs` + `MainFunctions.cs` + `BackyardFunctions.cs` → `MainViewModel.cs`
- All dialogs have AXAML + code-behind
- Data binding replaces manual UI updates

### Files Affected
- `ViewModels/MainViewModel.cs` (7,001 LOC)
- `ViewModels/RecipeViewModel.cs`
- All `Views/*.axaml` files

---

## 5. Dialog System Change

### What Changed
- **Original**: WinForms modal dialogs with `ShowDialog()`
- **Port**: Avalonia async dialogs via `DialogService`

### Why
Avalonia dialogs:
- Are async by design (return `Task<T>`)
- Use `Window.ShowDialog<T>(owner)` pattern
- Require proper owner window management

Centralized `DialogService`:
- Consistent dialog behavior
- Easier to manage window ownership
- Testable/mockable

### Impact
- All 19 WinForms dialogs rewritten as 21 Avalonia dialogs
- Dialog calls use `await` pattern
- Results returned via typed methods

### Files Affected
- `Services/DialogService.cs`
- All `Views/Dialogs/*.axaml.cs` files

---

## 6. Third-Party Library Removal

### PNGNet → SkiaSharp

**Original**: Custom PNG library (~50 files) for chunk manipulation
**Port**: SkiaSharp handles PNG natively

**Why**: SkiaSharp provides full PNG support including metadata. Custom library no longer needed.

### WinFormsSyntaxHighlighter → Avalonia.AvaloniaEdit

**Original**: Custom WinForms syntax highlighting (~20 files)
**Port**: Avalonia.AvaloniaEdit built-in

**Why**: AvaloniaEdit provides professional text editing with:
- Syntax highlighting
- Line numbers
- Code folding
- Search/replace

### CustomTabControl → Avalonia Styles

**Original**: Custom themed tab control (~8 files)
**Port**: Avalonia TabControl with styles

**Why**: Avalonia's styling system provides theming without custom controls.

### IniFileParser → JSON

**Original**: INI parsing framework (~30 files)
**Port**: JSON via System.Text.Json

**Why**: JSON is cross-platform, better structured, and has native .NET support.

---

## 7. Text Editor Change

### What Changed
- **Original**: Custom `RichTextBox` extensions
- **Port**: Avalonia.AvaloniaEdit

### Why
WinForms RichTextBox:
- Limited functionality
- Required custom extensions
- Windows-specific

AvaloniaEdit:
- Professional text editor control
- Built-in syntax highlighting
- Cross-platform
- Actively maintained

### Impact
- `RichTextBoxExtensions.cs` removed
- `SyntaxHighlightService.cs` uses AvaloniaEdit APIs
- Text editor features now built-in

---

## 8. Spell Checking Library Change

### What Changed
- **Original**: NHunspell (native interop)
- **Port**: WeCantSpell.Hunspell (pure .NET)

### Why
NHunspell:
- Uses native Hunspell library
- Requires platform-specific binaries
- Harder to deploy cross-platform

WeCantSpell.Hunspell:
- Pure .NET implementation
- Cross-platform
- No native dependencies
- API compatible

### Files Affected
- `Services/SpellCheckService.cs`

---

## 9. Directory Flattening Decisions

### ContextString Flattening

**Original**: Nested `Utility/ContextString/` directory
**Port**: Files moved to `Utility/` root

**Why**: The nested directory added no organizational value. Files are closely related to other Utility classes.

### Random Flattening

**Original**: Nested `Utility/Random/` directory
**Port**: Files moved to `Utility/` root

**Why**: Only 5 small files; nesting added unnecessary navigation depth.

### Integration → Services

**Original**: `Utility/Integration/` for Backyard code
**Port**: `Services/Backyard/`

**Why**: Integration code is really a "service" in MVVM terminology. Moving to Services aligns with the architecture.

---

## 10. Namespace Changes

| WinForms Namespace | Avalonia Namespace |
|-------------------|-------------------|
| `Ginger` | `Ginger` (unchanged for core) |
| `Ginger.Integration` | `Ginger.Integration` (preserved) |
| `Ginger.Model` | `Ginger.Models` |
| N/A | `Ginger.ViewModels` (new) |
| N/A | `Ginger.Services` (new) |
| N/A | `Ginger.Converters` (new) |

---

## Summary of Key Adaptations

| Area | Original | Port | Reason |
|------|----------|------|--------|
| SQLite | System.Data.SQLite | Microsoft.Data.Sqlite | Cross-platform |
| Images | System.Drawing | SkiaSharp | Cross-platform |
| Config | Registry + INI | JSON | Cross-platform |
| UI | Code-behind | MVVM | Separation of concerns |
| Dialogs | Sync ShowDialog | Async DialogService | Avalonia design |
| PNG | Custom PNGNet | SkiaSharp | Ecosystem library |
| Text Editor | RichTextBox | AvaloniaEdit | Better features |
| Spell Check | NHunspell | WeCantSpell | Pure .NET |
