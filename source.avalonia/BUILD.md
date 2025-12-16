# Ginger (Avalonia Port)

Cross-platform port of Ginger using Avalonia UI. Runs on Windows, macOS, and Linux.

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

Verify installation:
```bash
dotnet --version  # Should show 9.x.x
```

## Building

```bash
# Clone the repository
git clone https://github.com/DominaeDev/ginger.git
cd ginger

# Build
dotnet build source.avalonia/Ginger.Avalonia.csproj

# Build and run
dotnet run --project source.avalonia/Ginger.Avalonia.csproj
```

## Release Build

```bash
# Self-contained executable (includes .NET runtime)
dotnet publish source.avalonia/Ginger.Avalonia.csproj -c Release

# Output location
# Windows: source.avalonia/bin/Release/net9.0/publish/
# macOS:   source.avalonia/bin/Release/net9.0/publish/
# Linux:   source.avalonia/bin/Release/net9.0/publish/
```

### Platform-Specific Publish

```bash
# Windows
dotnet publish source.avalonia/Ginger.Avalonia.csproj -c Release -r win-x64 --self-contained

# macOS (Intel)
dotnet publish source.avalonia/Ginger.Avalonia.csproj -c Release -r osx-x64 --self-contained

# macOS (Apple Silicon)
dotnet publish source.avalonia/Ginger.Avalonia.csproj -c Release -r osx-arm64 --self-contained

# Linux
dotnet publish source.avalonia/Ginger.Avalonia.csproj -c Release -r linux-x64 --self-contained
```

## Troubleshooting

### Clean Build

If you encounter build issues:
```bash
dotnet clean source.avalonia/Ginger.Avalonia.csproj
dotnet build source.avalonia/Ginger.Avalonia.csproj
```

### Restore Dependencies

```bash
dotnet restore source.avalonia/Ginger.Avalonia.csproj
```

## Project Structure

```
source.avalonia/
├── ViewModels/       # MVVM ViewModels (MainViewModel.cs is the main hub)
├── Views/            # Avalonia AXAML UI files
│   └── Dialogs/      # Dialog windows
├── Services/         # Business logic services
│   └── Backyard/     # Backyard AI integration
├── Models/           # Data structures and format parsers
│   └── Formats/      # Character card and chat log formats
├── Utility/          # Core algorithms (ported from original)
├── Converters/       # Avalonia value converters
├── Content/          # Recipes, snippets, templates (copied from source/)
└── Dictionaries/     # Spell check dictionaries
```

## Dependencies

- **Avalonia 11.2.1** - Cross-platform UI framework
- **CommunityToolkit.Mvvm** - MVVM infrastructure
- **Microsoft.Data.Sqlite** - SQLite database access
- **SkiaSharp** - Image processing
- **WeCantSpell.Hunspell** - Spell checking
- **Newtonsoft.Json** - JSON serialization
- **YamlDotNet** - YAML serialization

## Feature Parity

This port has 100% feature parity with the original Windows Forms implementation. See [docs/PORT_MAPPING.md](../docs/PORT_MAPPING.md) for details on how the code was ported.
