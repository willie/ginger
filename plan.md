# Plan: Fix Build Warnings in Avalonia Port

## Summary

**Total: 1,741 warnings** after clean build

### Warning Categories

| Code | Count | Description |
|------|-------|-------------|
| CS8625 | 1,066 | Cannot convert null literal to non-nullable reference type |
| CS8600 | 490 | Converting null to non-nullable type |
| CS8618 | 456 | Non-nullable field/property must contain non-null value at constructor exit |
| CS0168 | 356 | Variable declared but never used |
| CS8603 | 350 | Possible null reference return |
| CS8601 | 278 | Possible null reference assignment |
| CS8604 | 234 | Possible null reference argument |
| CS8602 | 106 | Dereference of possibly null reference |
| MVVMTK0034 | 28 | ObservableProperty field directly referenced |
| CS8619 | 28 | Nullability mismatch in type |
| CS0618 | 24 | Obsolete API usage |
| CA2022 | 22 | Avoid inexact Stream.Read |
| CS8765 | 14 | Nullability mismatch in parameter override |
| CS8622 | 8 | Nullability mismatch in delegate |
| CS8767 | 6 | Nullability mismatch in interface implementation |
| CS8714 | 6 | Nullability constraint on generic type parameter |
| SYSLIB0021 | 4 | Obsolete SYSLIB API |
| CS8629 | 4 | Nullable value type may be null |
| CS1998 | 2 | Async method lacks await |

### Files with Most Warnings

| File | Count | Category |
|------|-------|----------|
| BackyardDatabase_v37.cs | 754 | Database schema |
| BackyardDatabase_v28.cs | 608 | Database schema |
| BackyardStubs.cs | 164 | Test data |
| Backyard.cs | 112 | Database integration |
| XmlExtensions.cs | 104 | Utility (ported) |
| Generator.cs | 94 | Core (ported) |
| Recipe.cs | 84 | Core (ported) |
| StringBank.cs | 80 | Core (ported) |
| MainViewModel.cs | 80 | New Avalonia code |
| ExifData.cs | 80 | Third-party |
| Utility.cs | 78 | Utility (ported) |

---

## Recommended Strategy

Given the project guidelines:
- The Avalonia port directly reuses original code with minimal changes
- Many files are "identical" or "near-identical" to the WinForms version
- Focus should be on maintaining feature parity, not refactoring

### Phase 1: Quick Wins (Non-Breaking)

**1.1 Fix CS0168 - Unused Variables (356 warnings)**
- Simple find and remove unused variable declarations
- No risk of behavior change

**1.2 Fix MVVMTK0034 - ObservableProperty Field Access (28 warnings)**
- Change `_fieldName` to `FieldName` in MainViewModel.cs
- CommunityToolkit.Mvvm best practice

**1.3 Fix CS1998 - Async Without Await (2 warnings)**
- Either remove async keyword or add await
- Quick and safe

### Phase 2: Suppress Warnings for Ported/Third-Party Code

**2.1 Add `#nullable disable` to ported utility files**

Files that are "identical" to WinForms version (per CLAUDE.md) should disable nullable to avoid diverging from original:
- GingerString.cs
- ContextString.cs
- StringBank.cs, StringHandle.cs, Text.cs
- Conditional.cs, RuleBank.cs
- All Extensions/*.cs

**2.2 Add `#nullable disable` to third-party code**
- Utility/ThirdParty/ExifData.cs
- Utility/ThirdParty/Tokenizer/LlamaTokenizer.cs
- Utility/ThirdParty/AvsAn/*

### Phase 3: Targeted Fixes for Avalonia-Specific Code

**3.1 Fix CS8618 - Uninitialized Non-Nullable Fields (456 warnings)**

For new Avalonia code, properly initialize fields or mark as nullable:
- Models/CardData.cs
- Models/Current.cs
- ViewModels/MainViewModel.cs

Options per field:
- Initialize in constructor: `public string Name { get; set; } = "";`
- Mark as nullable: `public string? Name { get; set; }`
- Use `required` modifier: `public required string Name { get; set; }`

**3.2 Fix CS8625/CS8600/CS8603 - Null Assignment/Return (bulk)**

For Avalonia-specific code, add null checks or adjust types:
```csharp
// Before
return dictionary.TryGetValue(key, out var value) ? value : null;

// After
return dictionary.TryGetValue(key, out var value) ? value : default!;
// OR
return dictionary.GetValueOrDefault(key);
```

### Phase 4: Database Layer (High Volume, Low Risk)

**4.1 Disable nullable for database schema files**

The BackyardDatabase files are schema definitions with many nullable columns:
- Services/Backyard/Revisions/BackyardDatabase_v28.cs
- Services/Backyard/Revisions/BackyardDatabase_v37.cs
- Services/Backyard/BackyardStubs.cs
- Services/Backyard/Backyard.cs

Add `#nullable disable` at top of these files since they model external SQLite database with nullable columns.

### Phase 5: Handle Specific Warning Types

**5.1 Fix CA2022 - Stream.Read (22 warnings)**
```csharp
// Before
stream.Read(buffer, 0, length);

// After
stream.ReadExactly(buffer, 0, length);
// OR
int bytesRead = stream.Read(buffer, 0, length);
if (bytesRead < length) throw new EndOfStreamException();
```

**5.2 Fix CS0618/SYSLIB0021 - Obsolete APIs (28 warnings)**
- Review each obsolete API and migrate to recommended alternative
- Common: BinaryFormatter → System.Text.Json, MD5/SHA1 → SHA256

**5.3 Fix CS8765/CS8767 - Interface Nullability (20 warnings)**
```csharp
// Before
public override bool Equals(object obj)

// After
public override bool Equals(object? obj)
```

---

## Implementation Order

1. **Phase 4** first - Database files (~1,600 warnings gone with `#nullable disable`)
2. **Phase 2** - Third-party and ported utility files (~400 warnings)
3. **Phase 1** - Quick wins (unused vars, MVVM, async)
4. **Phase 3** - Avalonia-specific code fixes
5. **Phase 5** - Specific warning types

## Expected Results

| Phase | Warnings Fixed | Method |
|-------|---------------|--------|
| Phase 4 | ~1,600 | `#nullable disable` on DB files |
| Phase 2 | ~400 | `#nullable disable` on ported/3rd-party |
| Phase 1 | ~386 | Code fixes |
| Phase 3 | ~300 | Code fixes (nullable annotations) |
| Phase 5 | ~55 | API updates |
| **Total** | **~1,741** | **Zero warnings** |

---

## Alternative: Project-Wide Nullable Disable

If the above is too invasive, could disable nullable for the entire project:

```xml
<!-- In Ginger.Avalonia.csproj -->
<Nullable>disable</Nullable>
```

**Pros:** Instant fix, no code changes
**Cons:** Loses all nullable reference type safety

**Not recommended** since the project is already using `<Nullable>enable</Nullable>` and new Avalonia code benefits from nullable safety.

---

## Files to Modify

### Phase 4 (Database)
- `Services/Backyard/Revisions/BackyardDatabase_v28.cs`
- `Services/Backyard/Revisions/BackyardDatabase_v37.cs`
- `Services/Backyard/BackyardStubs.cs`
- `Services/Backyard/Backyard.cs`

### Phase 2 (Ported/Third-Party)
- `Utility/GingerString.cs`
- `Utility/ContextString.cs`
- `Utility/StringBank.cs`
- `Utility/StringHandle.cs`
- `Utility/Text.cs`
- `Utility/Condition/Conditional.cs`
- `Utility/Condition/RuleBank.cs`
- `Utility/Extensions/*.cs` (all files)
- `Utility/ThirdParty/ExifData.cs`
- `Utility/ThirdParty/Tokenizer/LlamaTokenizer.cs`
- `Utility/ThirdParty/AvsAn/*.cs`
- `Utility/Generator.cs`
- `Utility/Recipe.cs`
- `Utility/RecipeBook.cs`
- `Utility/Lorebook.cs`
- `Utility/BlockBuilder.cs`
- `Utility/Parameter.cs`
- `Utility/RecipeMaker.cs`
- `Utility/Context.cs`
- `Utility/Utility.cs`
- `Utility/ValueComparison.cs`
- `Utility/ImageRef.cs`

### Phase 1 (Code Fixes)
- Various files with unused variables (CS0168)
- `ViewModels/MainViewModel.cs` (MVVMTK0034)

### Phase 3 & 5 (Targeted Fixes)
- `Models/CardData.cs`
- `Models/Current.cs`
- `Models/CustomVariable.cs`
- `Models/Formats/*.cs`
- `Models/Clipboard/*.cs`
