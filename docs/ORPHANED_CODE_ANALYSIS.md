# Orphaned Code Analysis - Avalonia Port

## Executive Summary

**Status: RESOLVED** (December 2024)

All confirmed orphaned code has been removed. This document is kept for historical reference.

## Removed Files (December 2024)

The following orphaned files were identified and **deleted**:

### Chat Log Formats (Never Wired Up)
| File | Reason for Removal |
|------|-------------------|
| `Models/Formats/ChatLogs/TextFileChat.cs` | No external references |
| `Models/Formats/ChatLogs/AgnaiChat.cs` | No external references |
| `Models/Formats/ChatLogs/GingerChatV1.cs` | Superseded by GingerChatV2 |
| `Models/Formats/ChatLogs/BackyardChatBackupV1.cs` | Superseded by V2 |
| `Models/Formats/ChatLogs/TextGenWebUIChat.cs` | No external references |

### Services (Never Instantiated)
| File | Reason for Removal |
|------|-------------------|
| `Services/Backyard/Utilities/BulkUtilities.cs` | Duplicate BackupData class, never wired up |
| `Services/GeneratorService.cs` | Never instantiated, direct Generator usage preferred |

### Clipboard Classes (Never Used)
| File | Reason for Removal |
|------|-------------------|
| `Models/Clipboard/ChatClipboard.cs` | No external references |
| `Models/Clipboard/ChatParametersClipboard.cs` | No external references |
| `Models/Clipboard/ChatStagingClipboard.cs` | No external references |

### Dead Properties Removed
- `_filterModelInstructions`, `_filterAttributes`, `_filterPersonality`, `_filterScenario`, `_filterGreeting`, `_filterExample`, `_filterLore` - Legacy properties with `[ObservableProperty]` but no XAML bindings or code usage

## Files Confirmed In Use

### SyntaxHighlightService.cs
**Location**: `source.avalonia/Services/SyntaxHighlightService.cs`

**Status**: ✅ **IN USE** - Wired to WriteDialog

Used by `Views/Dialogs/WriteDialog.axaml.cs` for syntax highlighting in the extended text editor.

### Clipboard Classes (Kept)
| File | Usage |
|------|-------|
| `Models/Clipboard/LoreClipboard.cs` | Used by MainViewModel |
| `Models/Clipboard/RecipeClipboard.cs` | Used by MainViewModel, RecipeViewModel |

## Verification Notes

When checking for orphaned code:
- Search for class name usage outside its own file
- Check XAML bindings for properties
- Verify service instantiation in DI or direct usage
- Check for clipboard class usage patterns

## Prevention

To avoid future orphaned code:
- Delete ported code that won't be wired up
- Don't port features that aren't needed
- Wire up code immediately after porting or delete it