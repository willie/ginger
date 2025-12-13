using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ginger.Models;
using Ginger.Services;

namespace Ginger.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IFileService _fileService;
    private readonly CharacterCardService _cardService;
    private readonly RecipeService _recipeService;
    private readonly DialogService _dialogService;
    private readonly UndoService _undoService;
    private CharacterCard? _currentCard;
    private string? _currentFilePath;
    private bool _isDirty;
    private string? _contentPath;

    #region Window Properties

    [ObservableProperty]
    private string _windowTitle = "Untitled - Ginger";

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isDarkMode;

    #endregion

    #region Character Properties

    [ObservableProperty]
    private string _characterName = "";

    [ObservableProperty]
    private string _spokenName = "";

    [ObservableProperty]
    private string? _selectedGender;

    [ObservableProperty]
    private Bitmap? _portraitImage;

    private byte[]? _portraitData;

    #endregion

    #region Card Information

    [ObservableProperty]
    private string _creator = "";

    [ObservableProperty]
    private string _version = "";

    [ObservableProperty]
    private string _tags = "";

    [ObservableProperty]
    private string _comment = "";

    #endregion

    #region Character Content

    [ObservableProperty]
    private string _persona = "";

    [ObservableProperty]
    private string _personality = "";

    [ObservableProperty]
    private string _scenario = "";

    [ObservableProperty]
    private string _greeting = "";

    [ObservableProperty]
    private string _exampleMessages = "";

    [ObservableProperty]
    private string _systemPrompt = "";

    #endregion

    #region User / Point of View

    [ObservableProperty]
    private string _userPlaceholder = "User";

    [ObservableProperty]
    private string? _userGender;

    #endregion

    #region Output Format

    [ObservableProperty]
    private string? _selectedTextStyle;

    [ObservableProperty]
    private string? _selectedDetailLevel;

    [ObservableProperty]
    private bool _useGrammar;

    #endregion

    #region Output Settings

    [ObservableProperty]
    private bool _userPersonaInPersona = true;

    [ObservableProperty]
    private bool _userPersonaInScenario;

    [ObservableProperty]
    private bool _pruneScenario;

    [ObservableProperty]
    private bool _filterModelInstructions = true;

    [ObservableProperty]
    private bool _filterAttributes = true;

    [ObservableProperty]
    private bool _filterPersonality = true;

    [ObservableProperty]
    private bool _filterScenario = true;

    [ObservableProperty]
    private bool _filterGreeting = true;

    [ObservableProperty]
    private bool _filterExample = true;

    [ObservableProperty]
    private bool _filterLore = true;

    #endregion

    #region Statistics

    [ObservableProperty]
    private int _tokenCount;

    [ObservableProperty]
    private int _permanentTokens;

    [ObservableProperty]
    private int _loreCount;

    [ObservableProperty]
    private int _recipeCount;

    #endregion

    #region Tab State

    [ObservableProperty]
    private bool _isRecipeTabActive = true;

    [ObservableProperty]
    private bool _isOutputTabActive;

    [ObservableProperty]
    private bool _isNotesTabActive;

    [ObservableProperty]
    private bool _isLorebookTabActive;

    #endregion

    #region Content

    [ObservableProperty]
    private string _outputPreview = "";

    [ObservableProperty]
    private string _notes = "";

    #endregion

    #region Collections

    public ObservableCollection<string> GenderOptions { get; } = new()
    {
        "Male",
        "Female",
        "Non-binary",
        "Other"
    };

    public ObservableCollection<string> TextStyleOptions { get; } = new()
    {
        "Chat (asterisks)",
        "Novel (quotes)",
        "Mixed",
        "Decorative quotes",
        "Bold",
        "Parentheses"
    };

    public ObservableCollection<string> DetailLevelOptions { get; } = new()
    {
        "Less detail",
        "Normal detail",
        "More detail"
    };

    public ObservableCollection<string> RecentFiles { get; } = new();

    public ObservableCollection<RecipeViewModel> Recipes { get; } = new();

    public ObservableCollection<LorebookEntryViewModel> LorebookEntries { get; } = new();

    // Recipe Library
    public ObservableCollection<RecipeLibraryItem> ModelRecipes { get; } = new();
    public ObservableCollection<RecipeLibraryItem> CharacterRecipes { get; } = new();
    public ObservableCollection<RecipeLibraryItem> PersonalityRecipes { get; } = new();
    public ObservableCollection<RecipeLibraryItem> TraitRecipes { get; } = new();
    public ObservableCollection<RecipeLibraryItem> StoryRecipes { get; } = new();

    #endregion

    public MainViewModel() : this(new FileService(), new CharacterCardService(), new RecipeService(), new DialogService())
    {
    }

    public MainViewModel(IFileService fileService, CharacterCardService cardService, RecipeService recipeService, DialogService dialogService)
    {
        _fileService = fileService;
        _cardService = cardService;
        _recipeService = recipeService;
        _dialogService = dialogService;
        _undoService = new UndoService();
        _selectedDetailLevel = "Normal detail";

        // Subscribe to undo state changes
        _undoService.StateChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
        };

        // Try to find content path
        FindContentPath();
        LoadRecipeLibrary();

        NewCommand.Execute(null);
    }

    public bool CanUndo => _undoService.CanUndo;
    public bool CanRedo => _undoService.CanRedo;

    private void FindContentPath()
    {
        // Look for Content directory relative to app location
        var appDir = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(appDir, "Content", "en"),
            Path.Combine(appDir, "..", "Content", "en"),
            Path.Combine(appDir, "..", "..", "Content", "en"),
            Path.Combine(appDir, "..", "..", "..", "source", "Content", "en"),
            Path.GetFullPath(Path.Combine(appDir, "..", "..", "..", "..", "source", "Content", "en")),
        };

        foreach (var path in candidates)
        {
            if (Directory.Exists(path))
            {
                _contentPath = path;
                return;
            }
        }
    }

    private void LoadRecipeLibrary()
    {
        if (string.IsNullOrEmpty(_contentPath))
            return;

        int count = _recipeService.LoadRecipes(_contentPath);
        StatusMessage = $"Loaded {count} recipes";

        // Populate library collections
        PopulateRecipeLibrary("Model", ModelRecipes);
        PopulateRecipeLibrary("Character", CharacterRecipes);
        PopulateRecipeLibrary("Personality", PersonalityRecipes);
        PopulateRecipeLibrary("Trait", TraitRecipes);
        PopulateRecipeLibrary("Story", StoryRecipes);
    }

    private void PopulateRecipeLibrary(string category, ObservableCollection<RecipeLibraryItem> collection)
    {
        collection.Clear();
        foreach (var recipe in _recipeService.GetRecipesByCategory(category))
        {
            collection.Add(new RecipeLibraryItem
            {
                Id = recipe.id.ToString(),
                Name = recipe.name ?? "",
                Title = recipe.title ?? "",
                Description = recipe.description ?? "",
            });
        }
    }

    [RelayCommand]
    private void AddRecipeFromLibrary(string recipeId)
    {
        var recipe = _recipeService.GetRecipe(recipeId);
        if (recipe == null)
            return;

        var cloned = _recipeService.CloneRecipe(recipe);
        var vm = new RecipeViewModel(this, cloned);
        Recipes.Add(vm);
        MarkDirty();
        RegenerateOutput();
        StatusMessage = $"Added recipe: {recipe.title}";
    }

    [RelayCommand]
    private void ReloadRecipes()
    {
        LoadRecipeLibrary();
    }

    [RelayCommand]
    private async Task LoadPortraitAsync()
    {
        var file = await _fileService.OpenFileAsync(
            "Open Portrait Image",
            new[] { "*.png", "*.jpg", "*.jpeg", "*.gif", "*.bmp", "*.webp" });

        if (file != null)
        {
            try
            {
                _portraitData = await File.ReadAllBytesAsync(file);
                using var stream = new MemoryStream(_portraitData);
                PortraitImage = new Bitmap(stream);
                MarkDirty();
                StatusMessage = "Portrait loaded";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading portrait: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    private void ClearPortrait()
    {
        _portraitData = null;
        PortraitImage = null;
        MarkDirty();
        StatusMessage = "Portrait cleared";
    }

    public void LoadPortraitFromData(byte[] data)
    {
        try
        {
            _portraitData = data;
            using var stream = new MemoryStream(data);
            PortraitImage = new Bitmap(stream);
            MarkDirty();
            StatusMessage = "Portrait loaded";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading portrait: {ex.Message}";
        }
    }

    #region Property Change Handlers

    partial void OnCharacterNameChanged(string value)
    {
        MarkDirty();
        UpdateWindowTitle();
        RegenerateOutput();
    }

    partial void OnSpokenNameChanged(string value)
    {
        MarkDirty();
        RegenerateOutput();
    }

    partial void OnCreatorChanged(string value) => MarkDirty();
    partial void OnVersionChanged(string value) => MarkDirty();
    partial void OnTagsChanged(string value) => MarkDirty();
    partial void OnCommentChanged(string value) => MarkDirty();
    partial void OnNotesChanged(string value) => MarkDirty();
    partial void OnPersonaChanged(string value) { MarkDirty(); RegenerateOutput(); }
    partial void OnPersonalityChanged(string value) { MarkDirty(); RegenerateOutput(); }
    partial void OnScenarioChanged(string value) { MarkDirty(); RegenerateOutput(); }
    partial void OnGreetingChanged(string value) { MarkDirty(); RegenerateOutput(); }
    partial void OnExampleMessagesChanged(string value) { MarkDirty(); RegenerateOutput(); }
    partial void OnSystemPromptChanged(string value) { MarkDirty(); RegenerateOutput(); }

    partial void OnUserPlaceholderChanged(string value)
    {
        MarkDirty();
        RegenerateOutput();
    }

    partial void OnSelectedGenderChanged(string? value)
    {
        MarkDirty();
        RegenerateOutput();
    }

    partial void OnIsDarkModeChanged(bool value)
    {
        // Apply dark mode
        if (Application.Current != null)
        {
            Application.Current.RequestedThemeVariant = value
                ? Avalonia.Styling.ThemeVariant.Dark
                : Avalonia.Styling.ThemeVariant.Light;
        }
    }

    #endregion

    #region Helper Methods

    private void MarkDirty()
    {
        _isDirty = true;
        UpdateWindowTitle();
    }

    private void UpdateWindowTitle()
    {
        var fileName = string.IsNullOrEmpty(_currentFilePath)
            ? "Untitled"
            : Path.GetFileName(_currentFilePath);

        var characterDisplay = string.IsNullOrEmpty(CharacterName)
            ? fileName
            : $"{CharacterName} - {fileName}";

        WindowTitle = _isDirty
            ? $"*{characterDisplay} - Ginger"
            : $"{characterDisplay} - Ginger";
    }

    private void RegenerateOutput()
    {
        var output = new System.Text.StringBuilder();
        string charName = !string.IsNullOrEmpty(SpokenName) ? SpokenName : CharacterName;
        string userName = !string.IsNullOrEmpty(UserPlaceholder) ? UserPlaceholder : "User";

        // Helper to format text with placeholders
        string FormatText(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return "";
            // Clean and process the text
            text = TextProcessing.CleanText(text);
            text = TextProcessing.RemoveComments(text);
            return text;
        }

        // System prompt
        if (!string.IsNullOrEmpty(SystemPrompt) && FilterModelInstructions)
        {
            output.AppendLine("=== SYSTEM PROMPT ===");
            output.AppendLine(FormatText(SystemPrompt));
            output.AppendLine();
        }

        // Persona / Description
        if (!string.IsNullOrEmpty(Persona))
        {
            output.AppendLine("=== PERSONA ===");
            output.AppendLine(FormatText(Persona));
            output.AppendLine();
        }

        // Personality
        if (!string.IsNullOrEmpty(Personality) && FilterPersonality)
        {
            output.AppendLine("=== PERSONALITY ===");
            output.AppendLine(FormatText(Personality));
            output.AppendLine();
        }

        // Scenario
        if (!string.IsNullOrEmpty(Scenario) && FilterScenario)
        {
            output.AppendLine("=== SCENARIO ===");
            output.AppendLine(FormatText(Scenario));
            output.AppendLine();
        }

        // Example messages
        if (!string.IsNullOrEmpty(ExampleMessages) && FilterExample)
        {
            output.AppendLine("=== EXAMPLE MESSAGES ===");
            output.AppendLine(FormatText(ExampleMessages));
            output.AppendLine();
        }

        // Greeting
        if (!string.IsNullOrEmpty(Greeting) && FilterGreeting)
        {
            output.AppendLine("=== GREETING ===");
            output.AppendLine(FormatText(Greeting));
            output.AppendLine();
        }

        // Recipes
        if (Recipes.Count > 0)
        {
            output.AppendLine("=== RECIPES ===");
            foreach (var recipe in Recipes.Where(r => r.IsEnabled))
            {
                if (!string.IsNullOrEmpty(recipe.Content))
                {
                    output.AppendLine($"[{recipe.Name}]");
                    output.AppendLine(FormatText(recipe.Content));
                    output.AppendLine();
                }
            }
        }

        OutputPreview = output.ToString();
        RecipeCount = Recipes.Count(r => r.IsEnabled);
        LoreCount = LorebookEntries.Count(e => e.IsEnabled);

        // Use the improved token estimation
        TokenCount = TextProcessing.EstimateTokenCount(OutputPreview);
        PermanentTokens = TokenCount; // For now, same as total
    }

    private void LoadFromCard(CharacterCard card)
    {
        _currentCard = card;

        CharacterName = card.Name;
        SpokenName = card.SpokenName;
        Creator = card.Creator;
        Version = card.Version;
        Comment = card.CreatorNotes;
        Tags = string.Join(", ", card.Tags);
        UserPlaceholder = card.UserPlaceholder;
        Notes = card.Notes;

        // Character content
        Persona = card.Persona;
        Personality = card.Personality;
        Scenario = card.Scenario;
        Greeting = card.Greeting;
        ExampleMessages = card.Example;
        SystemPrompt = card.System;

        // Portrait
        _portraitData = card.PortraitData;
        if (card.PortraitData != null && card.PortraitData.Length > 0)
        {
            try
            {
                using var stream = new MemoryStream(card.PortraitData);
                PortraitImage = new Bitmap(stream);
            }
            catch
            {
                PortraitImage = null;
            }
        }
        else
        {
            PortraitImage = null;
        }

        // Lorebook
        LorebookEntries.Clear();
        if (card.Lorebook != null)
        {
            foreach (var entry in card.Lorebook.Entries)
            {
                LorebookEntries.Add(new LorebookEntryViewModel(this)
                {
                    Keys = string.Join(", ", entry.Keys),
                    Content = entry.Content,
                    IsEnabled = entry.Enabled,
                    Name = entry.Name,
                });
            }
        }

        // Clear recipes (will be populated from actual recipe system later)
        Recipes.Clear();

        // Add persona as a recipe for now
        if (!string.IsNullOrEmpty(Persona))
        {
            Recipes.Add(new RecipeViewModel(this)
            {
                Name = "Persona",
                Content = Persona,
                IsExpanded = true
            });
        }

        if (!string.IsNullOrEmpty(Personality))
        {
            Recipes.Add(new RecipeViewModel(this)
            {
                Name = "Personality",
                Content = Personality,
                IsExpanded = false
            });
        }

        if (!string.IsNullOrEmpty(Scenario))
        {
            Recipes.Add(new RecipeViewModel(this)
            {
                Name = "Scenario",
                Content = Scenario,
                IsExpanded = false
            });
        }

        RegenerateOutput();
    }

    private CharacterCard ToCard()
    {
        var card = _currentCard ?? new CharacterCard();

        card.Name = CharacterName;
        card.SpokenName = SpokenName;
        card.Creator = Creator;
        card.Version = Version;
        card.CreatorNotes = Comment;
        card.Tags = new System.Collections.Generic.HashSet<string>(
            Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        card.UserPlaceholder = UserPlaceholder;
        card.Notes = Notes;

        // Character content
        card.Persona = Persona;
        card.Personality = Personality;
        card.Scenario = Scenario;
        card.Greeting = Greeting;
        card.Example = ExampleMessages;
        card.System = SystemPrompt;

        // Portrait
        card.PortraitData = _portraitData;

        // Lorebook
        if (LorebookEntries.Count > 0)
        {
            card.Lorebook = new Ginger.Models.Lorebook();
            int id = 1;
            foreach (var entry in LorebookEntries)
            {
                card.Lorebook.Entries.Add(new Ginger.Models.Lorebook.LorebookEntry
                {
                    Id = id++,
                    Keys = entry.Keys.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                    Content = entry.Content,
                    Enabled = entry.IsEnabled,
                });
            }
        }

        return card;
    }

    #endregion

    #region Tab Commands

    [RelayCommand]
    private void ShowRecipeTab()
    {
        IsRecipeTabActive = true;
        IsOutputTabActive = false;
        IsNotesTabActive = false;
        IsLorebookTabActive = false;
    }

    [RelayCommand]
    private void ShowOutputTab()
    {
        IsRecipeTabActive = false;
        IsOutputTabActive = true;
        IsNotesTabActive = false;
        IsLorebookTabActive = false;
    }

    [RelayCommand]
    private void ShowNotesTab()
    {
        IsRecipeTabActive = false;
        IsOutputTabActive = false;
        IsNotesTabActive = true;
        IsLorebookTabActive = false;
    }

    [RelayCommand]
    private void ShowLorebookTab()
    {
        IsRecipeTabActive = false;
        IsOutputTabActive = false;
        IsNotesTabActive = false;
        IsLorebookTabActive = true;
    }

    #endregion

    #region Lorebook Commands

    [RelayCommand]
    private void AddLorebookEntry()
    {
        var entry = new LorebookEntryViewModel(this)
        {
            Keys = "",
            Content = "",
            IsEnabled = true,
            IsExpanded = true,
            Name = $"Entry {LorebookEntries.Count + 1}",
        };
        LorebookEntries.Add(entry);
        MarkDirty();
        RegenerateOutput();
    }

    public void RemoveLorebookEntry(LorebookEntryViewModel entry)
    {
        LorebookEntries.Remove(entry);
        MarkDirty();
        RegenerateOutput();
    }

    public void MoveLorebookEntryUp(LorebookEntryViewModel entry)
    {
        var index = LorebookEntries.IndexOf(entry);
        if (index > 0)
        {
            LorebookEntries.Move(index, index - 1);
            MarkDirty();
        }
    }

    public void MoveLorebookEntryDown(LorebookEntryViewModel entry)
    {
        var index = LorebookEntries.IndexOf(entry);
        if (index < LorebookEntries.Count - 1)
        {
            LorebookEntries.Move(index, index + 1);
            MarkDirty();
        }
    }

    public void OnLorebookChanged()
    {
        MarkDirty();
        RegenerateOutput();
    }

    [RelayCommand]
    private async Task ExportLorebook()
    {
        if (LorebookEntries.Count == 0)
        {
            StatusMessage = "No lorebook entries to export";
            return;
        }

        // Convert ViewModels to Lorebook.Entry
        var entries = LorebookEntries.Select(e => new Lorebook.Entry
        {
            key = e.Keys,
            value = e.Content,
            isEnabled = e.IsEnabled,
        }).ToList();

        var filters = new[]
        {
            new FilePickerFileType("JSON Files") { Patterns = new[] { "*.json" } },
            new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } }
        };

        var path = await _dialogService.ShowSaveFileDialogAsync("Export Lorebook", "lorebook.json", filters);
        if (string.IsNullOrEmpty(path))
            return;

        try
        {
            var lorebook = new Lorebook { entries = entries };
            var json = System.Text.Json.JsonSerializer.Serialize(new
            {
                entries = entries.Select(e => new
                {
                    keys = e.keys,
                    content = e.value,
                    enabled = e.isEnabled,
                    name = e.key,
                }).ToArray()
            }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

            await File.WriteAllTextAsync(path, json);
            StatusMessage = $"Exported {entries.Count} lorebook entries";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ImportLorebook()
    {
        var filters = new[]
        {
            new FilePickerFileType("JSON Files") { Patterns = new[] { "*.json" } },
            new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } }
        };

        var path = await _dialogService.ShowOpenFileDialogAsync("Import Lorebook", filters);
        if (string.IsNullOrEmpty(path))
            return;

        try
        {
            var json = await File.ReadAllTextAsync(path);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            int importCount = 0;

            // Try to find entries array in common lorebook formats
            System.Text.Json.JsonElement entriesElement = default;
            if (root.TryGetProperty("entries", out entriesElement) ||
                root.TryGetProperty("character_book", out var cb) && cb.TryGetProperty("entries", out entriesElement))
            {
                foreach (var entry in entriesElement.EnumerateArray())
                {
                    string keys = "";
                    string content = "";
                    bool enabled = true;

                    // Try various key formats
                    if (entry.TryGetProperty("keys", out var keysEl))
                    {
                        if (keysEl.ValueKind == System.Text.Json.JsonValueKind.Array)
                            keys = string.Join(", ", keysEl.EnumerateArray().Select(k => k.GetString()));
                        else if (keysEl.ValueKind == System.Text.Json.JsonValueKind.String)
                            keys = keysEl.GetString() ?? "";
                    }
                    else if (entry.TryGetProperty("key", out var keyEl))
                        keys = keyEl.GetString() ?? "";
                    else if (entry.TryGetProperty("name", out var nameEl))
                        keys = nameEl.GetString() ?? "";

                    // Try various content formats
                    if (entry.TryGetProperty("content", out var contentEl))
                        content = contentEl.GetString() ?? "";
                    else if (entry.TryGetProperty("value", out var valueEl))
                        content = valueEl.GetString() ?? "";

                    // Enabled status
                    if (entry.TryGetProperty("enabled", out var enabledEl))
                        enabled = enabledEl.GetBoolean();
                    else if (entry.TryGetProperty("isEnabled", out var isEnabledEl))
                        enabled = isEnabledEl.GetBoolean();

                    if (!string.IsNullOrEmpty(keys) || !string.IsNullOrEmpty(content))
                    {
                        LorebookEntries.Add(new LorebookEntryViewModel(this)
                        {
                            Keys = keys,
                            Content = content,
                            IsEnabled = enabled,
                            Name = keys.Split(',').FirstOrDefault()?.Trim() ?? $"Entry {LorebookEntries.Count + 1}",
                        });
                        importCount++;
                    }
                }
            }

            if (importCount > 0)
            {
                MarkDirty();
                RegenerateOutput();
                StatusMessage = $"Imported {importCount} lorebook entries";
            }
            else
            {
                StatusMessage = "No valid lorebook entries found in file";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Import failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task CopyLorebook()
    {
        if (LorebookEntries.Count == 0)
        {
            StatusMessage = "No lorebook entries to copy";
            return;
        }

        var entries = LorebookEntries.Select(e => new Lorebook.Entry
        {
            key = e.Keys,
            value = e.Content,
            isEnabled = e.IsEnabled,
        }).ToList();

        var clipboard = LoreClipboard.FromLoreEntries(entries);
        var json = System.Text.Json.JsonSerializer.Serialize(clipboard);

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var topLevel = desktop.MainWindow;
            if (topLevel?.Clipboard != null)
            {
                await topLevel.Clipboard.SetTextAsync(json);
                StatusMessage = $"Copied {entries.Count} lorebook entries to clipboard";
            }
        }
    }

    [RelayCommand]
    private async Task PasteLorebook()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        var topLevel = desktop.MainWindow;
        if (topLevel?.Clipboard == null)
            return;

        var text = await topLevel.Clipboard.GetTextAsync();
        if (string.IsNullOrWhiteSpace(text))
        {
            StatusMessage = "Clipboard is empty";
            return;
        }

        try
        {
            var clipboard = System.Text.Json.JsonSerializer.Deserialize<LoreClipboard>(text);
            if (clipboard == null)
            {
                StatusMessage = "Invalid clipboard data";
                return;
            }

            var entries = clipboard.ToEntries();
            if (entries == null || entries.Count == 0)
            {
                StatusMessage = "No valid lorebook entries in clipboard";
                return;
            }

            foreach (var entry in entries)
            {
                LorebookEntries.Add(new LorebookEntryViewModel(this)
                {
                    Keys = entry.key,
                    Content = entry.value,
                    IsEnabled = entry.isEnabled,
                    Name = entry.keys.FirstOrDefault() ?? $"Entry {LorebookEntries.Count + 1}",
                });
            }

            MarkDirty();
            RegenerateOutput();
            StatusMessage = $"Pasted {entries.Count} lorebook entries";
        }
        catch (System.Text.Json.JsonException)
        {
            StatusMessage = "Invalid clipboard format - expected lorebook data";
        }
    }

    #endregion

    #region File Commands

    [RelayCommand]
    private void New()
    {
        _currentFilePath = null;
        _currentCard = null;
        _isDirty = false;
        _portraitData = null;

        CharacterName = "";
        SpokenName = "";
        Creator = "";
        Version = "";
        Tags = "";
        Comment = "";
        UserPlaceholder = "User";
        SelectedGender = null;
        UserGender = null;
        PortraitImage = null;
        Notes = "";

        Persona = "";
        Personality = "";
        Scenario = "";
        Greeting = "";
        ExampleMessages = "";
        SystemPrompt = "";

        Recipes.Clear();
        LorebookEntries.Clear();

        // Add default persona recipe
        var personaRecipe = new RecipeViewModel(this)
        {
            Name = "Persona",
            Content = "",
            IsExpanded = true
        };
        Recipes.Add(personaRecipe);

        UpdateWindowTitle();
        RegenerateOutput();
        StatusMessage = "New character created";
    }

    [RelayCommand]
    private async Task NewFromTemplate(string? templateName)
    {
        if (string.IsNullOrEmpty(templateName))
        {
            StatusMessage = "No template selected";
            return;
        }

        // Check for unsaved changes
        if (_isDirty)
        {
            var result = await _dialogService.ShowConfirmationDialogAsync(
                "Unsaved Changes",
                "You have unsaved changes. Do you want to continue without saving?");
            if (!result)
                return;
        }

        // Find the template
        var preset = RecipeBook.GetPresetByID(templateName);
        if (preset == null)
        {
            StatusMessage = $"Template '{templateName}' not found";
            return;
        }

        // Create new character from template
        New();

        // Add recipes from the preset
        Current.NewCharacter();
        var instances = Current.MainCharacter.AddRecipePreset(preset);

        // Clear the default recipe and add the preset recipes
        Recipes.Clear();
        foreach (var recipe in instances)
        {
            Recipes.Add(new RecipeViewModel(this, recipe));
        }

        Current.IsDirty = false;
        _isDirty = false;
        UpdateWindowTitle();
        RegenerateOutput();
        StatusMessage = $"Created new character from template: {preset.name}";
    }

    /// <summary>
    /// Gets the list of available templates for the menu.
    /// </summary>
    public IEnumerable<(string Id, string Name)> GetAvailableTemplates()
    {
        foreach (var preset in RecipeBook.allPresets)
        {
            yield return (preset.id.ToString(), preset.name);
        }
    }

    [RelayCommand]
    private void Duplicate()
    {
        // Clear file path so it becomes a new file
        _currentFilePath = null;

        // Append "Copy" to the name if there isn't already a copy suffix
        if (!string.IsNullOrEmpty(CharacterName))
        {
            if (!CharacterName.EndsWith(" (Copy)"))
                CharacterName = CharacterName + " (Copy)";
        }

        MarkDirty();
        UpdateWindowTitle();
        StatusMessage = "Character duplicated - save to create a new file";
    }

    [RelayCommand]
    private async Task OpenAsync()
    {
        var file = await _fileService.OpenFileAsync(
            "Open Character Card",
            new[] { "*.png", "*.json", "*.charx", "*.byaf" });

        if (file != null)
        {
            await LoadFileAsync(file);
        }
    }

    public async Task LoadFileAsync(string filePath)
    {
        try
        {
            StatusMessage = $"Loading {Path.GetFileName(filePath)}...";

            var (result, card) = await _cardService.LoadAsync(filePath);

            switch (result)
            {
                case CharacterCardService.LoadResult.Success when card != null:
                    LoadFromCard(card);
                    _currentFilePath = filePath;
                    _isDirty = false;
                    UpdateWindowTitle();
                    StatusMessage = $"Loaded {card.Name} ({card.SourceFormat})";
                    break;
                case CharacterCardService.LoadResult.FileNotFound:
                    StatusMessage = "File not found";
                    break;
                case CharacterCardService.LoadResult.NoDataFound:
                    StatusMessage = "No character data found in file";
                    break;
                case CharacterCardService.LoadResult.InvalidFormat:
                    StatusMessage = "Unrecognized file format";
                    break;
                default:
                    StatusMessage = "Error loading file";
                    break;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrEmpty(_currentFilePath))
        {
            await SaveAsAsync();
        }
        else
        {
            await SaveToFileAsync(_currentFilePath);
        }
    }

    [RelayCommand]
    private async Task SaveAsAsync()
    {
        var file = await _fileService.SaveFileAsync(
            "Save Character Card",
            string.IsNullOrEmpty(CharacterName) ? "character" : CharacterName,
            new[] { "*.png", "*.json" });

        if (file != null)
        {
            await SaveToFileAsync(file);
        }
    }

    private async Task SaveToFileAsync(string filePath)
    {
        try
        {
            StatusMessage = $"Saving {Path.GetFileName(filePath)}...";

            var card = ToCard();
            bool success = await _cardService.SaveAsync(filePath, card);

            if (success)
            {
                _currentFilePath = filePath;
                _isDirty = false;
                UpdateWindowTitle();
                StatusMessage = "Saved successfully";
            }
            else
            {
                StatusMessage = "Failed to save file";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ImportFromFolder()
    {
        var folder = await _dialogService.ShowFolderDialogAsync("Select Folder to Import From");
        if (string.IsNullOrEmpty(folder))
            return;

        try
        {
            var files = Directory.GetFiles(folder)
                .Where(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".charx", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (files.Length == 0)
            {
                StatusMessage = "No character files found in folder";
                return;
            }

            int imported = 0;
            int failed = 0;

            foreach (var file in files)
            {
                var (result, _) = await _cardService.LoadAsync(file);
                if (result == CharacterCardService.LoadResult.Success)
                    imported++;
                else
                    failed++;
            }

            StatusMessage = $"Imported {imported} files ({failed} failed)";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Import error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ExportToFolder()
    {
        if (_currentCard == null)
        {
            StatusMessage = "No character to export";
            return;
        }

        var folder = await _dialogService.ShowFolderDialogAsync("Select Export Folder");
        if (string.IsNullOrEmpty(folder))
            return;

        try
        {
            var card = ToCard();
            var fileName = string.IsNullOrWhiteSpace(card.Name) ? "character" : SanitizeFileName(card.Name);

            // Export as PNG (default)
            var pngPath = Path.Combine(folder, $"{fileName}.png");
            if (await _cardService.SaveAsync(pngPath, card))
            {
                StatusMessage = $"Exported to {fileName}.png";
            }
            else
            {
                StatusMessage = "Export failed";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ExportAsJson()
    {
        if (_currentCard == null)
        {
            StatusMessage = "No character to export";
            return;
        }

        var file = await _fileService.SaveFileAsync(
            "Export as JSON",
            string.IsNullOrEmpty(CharacterName) ? "character" : CharacterName,
            new[] { "*.json" });

        if (file != null)
        {
            try
            {
                var card = ToCard();
                if (await _cardService.SaveAsync(file, card))
                    StatusMessage = $"Exported as JSON";
                else
                    StatusMessage = "Export failed";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export error: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    private async Task ExportAs()
    {
        if (_currentCard == null)
        {
            StatusMessage = "No character to export";
            return;
        }

        // Show format selection dialog
        var (success, format) = await _dialogService.ShowFileFormatDialogAsync();
        if (!success)
            return;

        // Get file extension and filter for selected format
        string extension = format switch
        {
            Views.Dialogs.FileFormatDialog.ExportFormat.Png => ".png",
            Views.Dialogs.FileFormatDialog.ExportFormat.Json => ".json",
            Views.Dialogs.FileFormatDialog.ExportFormat.Yaml => ".yaml",
            Views.Dialogs.FileFormatDialog.ExportFormat.Charx => ".charx",
            Views.Dialogs.FileFormatDialog.ExportFormat.Byaf => ".byaf",
            _ => ".png"
        };

        string filter = format switch
        {
            Views.Dialogs.FileFormatDialog.ExportFormat.Png => "*.png",
            Views.Dialogs.FileFormatDialog.ExportFormat.Json => "*.json",
            Views.Dialogs.FileFormatDialog.ExportFormat.Yaml => "*.yaml",
            Views.Dialogs.FileFormatDialog.ExportFormat.Charx => "*.charx",
            Views.Dialogs.FileFormatDialog.ExportFormat.Byaf => "*.byaf",
            _ => "*.png"
        };

        var fileName = string.IsNullOrWhiteSpace(CharacterName) ? "character" : CharacterName;
        var file = await _fileService.SaveFileAsync(
            $"Export as {extension.ToUpperInvariant().TrimStart('.')}",
            fileName,
            new[] { filter });

        if (file != null)
        {
            try
            {
                // Ensure correct extension
                if (!file.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                    file = Path.ChangeExtension(file, extension);

                var card = ToCard();
                if (await _cardService.SaveAsync(file, card))
                {
                    StatusMessage = $"Exported as {Path.GetFileName(file)}";
                }
                else
                {
                    if (format == Views.Dialogs.FileFormatDialog.ExportFormat.Png && card.PortraitData == null)
                        StatusMessage = "Export failed: PNG format requires a portrait image";
                    else
                        StatusMessage = "Export failed";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export error: {ex.Message}";
            }
        }
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(name.Where(c => !invalid.Contains(c)));
    }

    [RelayCommand]
    private void Exit()
    {
        Environment.Exit(0);
    }

    #endregion

    #region Edit Commands

    [RelayCommand]
    private void Undo()
    {
        if (_undoService.Undo())
        {
            var desc = _undoService.GetRedoDescription();
            StatusMessage = $"Undid: {desc}";
            RegenerateOutput();
        }
    }

    [RelayCommand]
    private void Redo()
    {
        if (_undoService.Redo())
        {
            var desc = _undoService.GetUndoDescription();
            StatusMessage = $"Redid: {desc}";
            RegenerateOutput();
        }
    }

    // Store last search for Find Next/Previous
    private string _lastSearchTerm = "";
    private bool _lastSearchMatchCase = false;
    private bool _lastSearchWholeWord = false;

    [RelayCommand]
    private void Find() => ShowFindDialog(findOnly: true);

    [RelayCommand]
    private void FindReplace() => ShowFindDialog(findOnly: false);

    [RelayCommand]
    private void FindNext()
    {
        if (string.IsNullOrEmpty(_lastSearchTerm))
        {
            Find();
            return;
        }
        int count = CountOccurrences(_lastSearchTerm, _lastSearchMatchCase, _lastSearchWholeWord);
        if (count > 0)
            StatusMessage = $"Found {count} occurrence(s) of \"{_lastSearchTerm}\"";
        else
            StatusMessage = $"No matches found for \"{_lastSearchTerm}\"";
    }

    [RelayCommand]
    private void FindPrevious()
    {
        if (string.IsNullOrEmpty(_lastSearchTerm))
        {
            Find();
            return;
        }
        // For now, same as FindNext (would need cursor tracking for true previous)
        int count = CountOccurrences(_lastSearchTerm, _lastSearchMatchCase, _lastSearchWholeWord);
        if (count > 0)
            StatusMessage = $"Found {count} occurrence(s) of \"{_lastSearchTerm}\"";
        else
            StatusMessage = $"No matches found for \"{_lastSearchTerm}\"";
    }

    private void ShowFindDialog(bool findOnly)
    {
        _dialogService.ShowFindReplaceDialog(
            onFind: (search, replace, matchCase, wholeWord) =>
            {
                // Store search params for Find Next/Previous
                _lastSearchTerm = search;
                _lastSearchMatchCase = matchCase;
                _lastSearchWholeWord = wholeWord;

                // Find in all recipes
                int count = CountOccurrences(search, matchCase, wholeWord);
                if (count > 0)
                    StatusMessage = $"Found {count} occurrence(s) of \"{search}\"";
                else
                    StatusMessage = $"No matches found for \"{search}\"";
            },
            onReplace: (search, replace, matchCase, wholeWord) =>
            {
                // Replace first occurrence (for now, replace all)
                int count = PerformReplaceAll(search, replace, matchCase, wholeWord);
                if (count > 0)
                {
                    StatusMessage = $"Replaced {count} occurrence(s)";
                    MarkDirty();
                    RegenerateOutput();
                }
                else
                    StatusMessage = $"No matches found for \"{search}\"";
            },
            onReplaceAll: (search, replace, matchCase, wholeWord) =>
            {
                int count = PerformReplaceAll(search, replace, matchCase, wholeWord);
                if (count > 0)
                {
                    StatusMessage = $"Replaced {count} occurrence(s)";
                    MarkDirty();
                    RegenerateOutput();
                }
                else
                    StatusMessage = $"No matches found for \"{search}\"";
            }
        );
    }

    private int CountOccurrences(string search, bool matchCase, bool wholeWord)
    {
        int count = 0;
        var comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        foreach (var recipe in Current.AllRecipes)
        {
            foreach (var param in recipe.parameters.OfType<TextParameter>())
            {
                if (string.IsNullOrEmpty(param.value))
                    continue;

                if (wholeWord)
                {
                    var matches = Utility.FindWholeWords(param.value, search, comparison);
                    count += matches?.Length ?? 0;
                }
                else
                {
                    var matches = Utility.FindWords(param.value, search, comparison);
                    count += matches?.Length ?? 0;
                }
            }
        }
        return count;
    }

    private int PerformReplaceAll(string search, string replace, bool matchCase, bool wholeWord)
    {
        return Ginger.FindReplace.Replace(Current.AllRecipes, search, replace, wholeWord, !matchCase, bIncludeLorebooks: true);
    }

    [RelayCommand]
    private async Task GenderSwap()
    {
        var result = await _dialogService.ShowGenderSwapDialogAsync();
        if (!result.success)
            return;

        int changes = Ginger.GenderSwap.SwapGenders(
            Current.AllRecipes,
            result.charFrom, result.charTo,
            result.userFrom, result.userTo,
            result.swapChar, result.swapUser);

        if (changes > 0)
        {
            MarkDirty();
            RegenerateOutput();
            StatusMessage = $"Gender swap complete. {changes} replacement(s) made.";
        }
        else
        {
            StatusMessage = "Gender swap complete. No changes made.";
        }
    }

    #endregion

    #region Actor Commands

    [ObservableProperty]
    private int _selectedActorIndex;

    public bool CanRemoveActor => Current.Characters.Count > 1;
    public bool IsMultiCharacter => Current.Characters.Count > 1;

    [RelayCommand]
    private void AddActor()
    {
        Current.AddCharacter();
        SelectedActorIndex = Current.SelectedCharacter;
        OnPropertyChanged(nameof(CanRemoveActor));
        OnPropertyChanged(nameof(IsMultiCharacter));
        MarkDirty();
        StatusMessage = $"Added new actor (total: {Current.Characters.Count})";
    }

    [RelayCommand]
    private void RemoveActor()
    {
        if (Current.Characters.Count <= 1)
        {
            StatusMessage = "Cannot remove the last actor";
            return;
        }

        int removeIndex = Current.SelectedCharacter;
        Current.Characters.RemoveAt(removeIndex);
        if (Current.SelectedCharacter >= Current.Characters.Count)
            Current.SelectedCharacter = Current.Characters.Count - 1;
        SelectedActorIndex = Current.SelectedCharacter;
        OnPropertyChanged(nameof(CanRemoveActor));
        OnPropertyChanged(nameof(IsMultiCharacter));
        MarkDirty();
        RegenerateOutput();
        StatusMessage = $"Removed actor (remaining: {Current.Characters.Count})";
    }

    partial void OnSelectedActorIndexChanged(int value)
    {
        if (value >= 0 && value < Current.Characters.Count)
        {
            Current.SelectedCharacter = value;
            LoadCharacterIntoUI();
        }
    }

    private void LoadCharacterIntoUI()
    {
        var character = Current.Character;
        CharacterName = character.name ?? "";
        SpokenName = character.spokenName ?? "";
        SelectedGender = character.gender;
        Persona = character.persona ?? "";
        Personality = character.personality ?? "";
        Scenario = character.scenario ?? "";
        Greeting = character.greeting ?? "";
        ExampleMessages = character.example ?? "";
        SystemPrompt = character.system ?? "";

        // Reload recipes for this character
        Recipes.Clear();
        foreach (var recipe in character.recipes)
        {
            Recipes.Add(new RecipeViewModel(this, recipe));
        }
    }

    #endregion

    #region Recipe Commands

    [RelayCommand]
    private void AddRecipe()
    {
        var recipe = new RecipeViewModel(this)
        {
            Name = $"Recipe {Recipes.Count + 1}",
            Content = "",
            IsExpanded = true
        };
        Recipes.Add(recipe);
        MarkDirty();
        RegenerateOutput();
    }

    [RelayCommand]
    private void ExpandAll()
    {
        foreach (var recipe in Recipes)
            recipe.IsExpanded = true;
    }

    [RelayCommand]
    private void CollapseAll()
    {
        foreach (var recipe in Recipes)
            recipe.IsExpanded = false;
    }

    public void RemoveRecipe(RecipeViewModel recipe)
    {
        Recipes.Remove(recipe);
        MarkDirty();
        RegenerateOutput();
    }

    public void MoveRecipeUp(RecipeViewModel recipe)
    {
        var index = Recipes.IndexOf(recipe);
        if (index > 0)
        {
            Recipes.Move(index, index - 1);
            MarkDirty();
        }
    }

    public void MoveRecipeDown(RecipeViewModel recipe)
    {
        var index = Recipes.IndexOf(recipe);
        if (index < Recipes.Count - 1)
        {
            Recipes.Move(index, index + 1);
            MarkDirty();
        }
    }

    public void OnRecipeChanged()
    {
        MarkDirty();
        RegenerateOutput();
    }

    public void SetStatusMessage(string message)
    {
        StatusMessage = message;
    }

    [RelayCommand]
    private async Task CopyAllRecipes()
    {
        var sourceRecipes = Recipes
            .Select(r => r.GetSourceRecipe())
            .Where(r => r != null)
            .Cast<Recipe>()
            .ToList();

        if (sourceRecipes.Count == 0)
        {
            StatusMessage = "No recipes to copy";
            return;
        }

        var clipboard = RecipeClipboard.FromRecipes(sourceRecipes);
        var json = System.Text.Json.JsonSerializer.Serialize(clipboard);

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var topLevel = desktop.MainWindow;
            if (topLevel?.Clipboard != null)
            {
                await topLevel.Clipboard.SetTextAsync(json);
                StatusMessage = $"Copied {sourceRecipes.Count} recipe(s) to clipboard";
            }
        }
    }

    [RelayCommand]
    private async Task PasteRecipes()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        var topLevel = desktop.MainWindow;
        if (topLevel?.Clipboard == null)
            return;

        var text = await topLevel.Clipboard.GetTextAsync();
        if (string.IsNullOrWhiteSpace(text))
        {
            StatusMessage = "Clipboard is empty";
            return;
        }

        try
        {
            var clipboard = System.Text.Json.JsonSerializer.Deserialize<RecipeClipboard>(text);
            if (clipboard == null)
            {
                StatusMessage = "Invalid clipboard data";
                return;
            }

            var recipes = clipboard.ToRecipes();
            if (recipes == null || recipes.Count == 0)
            {
                StatusMessage = "No valid recipes in clipboard";
                return;
            }

            foreach (var recipe in recipes)
            {
                Current.Character.recipes.Add(recipe);
                Recipes.Add(new RecipeViewModel(this, recipe));
            }

            MarkDirty();
            RegenerateOutput();
            StatusMessage = $"Pasted {recipes.Count} recipe(s)";
        }
        catch (System.Text.Json.JsonException)
        {
            StatusMessage = "Invalid clipboard format - expected recipe data";
        }
    }

    #endregion

    #region Extended Edit Commands

    [RelayCommand]
    private async Task EditPersona()
    {
        var (success, text) = await _dialogService.ShowWriteDialogAsync(Persona, "Edit Persona");
        if (success)
        {
            Persona = text;
            MarkDirty();
            RegenerateOutput();
        }
    }

    [RelayCommand]
    private async Task EditPersonality()
    {
        var (success, text) = await _dialogService.ShowWriteDialogAsync(Personality, "Edit Personality");
        if (success)
        {
            Personality = text;
            MarkDirty();
            RegenerateOutput();
        }
    }

    [RelayCommand]
    private async Task EditScenario()
    {
        var (success, text) = await _dialogService.ShowWriteDialogAsync(Scenario, "Edit Scenario");
        if (success)
        {
            Scenario = text;
            MarkDirty();
            RegenerateOutput();
        }
    }

    [RelayCommand]
    private async Task EditGreeting()
    {
        var (success, text) = await _dialogService.ShowWriteDialogAsync(Greeting, "Edit Greeting");
        if (success)
        {
            Greeting = text;
            MarkDirty();
            RegenerateOutput();
        }
    }

    [RelayCommand]
    private async Task EditExampleMessages()
    {
        var (success, text) = await _dialogService.ShowWriteDialogAsync(ExampleMessages, "Edit Example Messages");
        if (success)
        {
            ExampleMessages = text;
            MarkDirty();
            RegenerateOutput();
        }
    }

    [RelayCommand]
    private async Task EditSystemPrompt()
    {
        var (success, text) = await _dialogService.ShowWriteDialogAsync(SystemPrompt, "Edit System Prompt");
        if (success)
        {
            SystemPrompt = text;
            MarkDirty();
            RegenerateOutput();
        }
    }

    #endregion

    #region Other Commands

    [RelayCommand]
    private async Task AboutAsync()
    {
        await _dialogService.ShowAboutAsync();
    }

    [RelayCommand]
    private async Task CheckForUpdates()
    {
        StatusMessage = "Checking for updates...";

        var updateInfo = await Services.UpdateService.CheckForUpdatesAsync();

        if (updateInfo.UpdateAvailable)
        {
            var message = $"A new version is available!\n\n" +
                          $"Current version: {updateInfo.CurrentVersion}\n" +
                          $"Latest version: {updateInfo.LatestVersion}\n\n" +
                          $"Would you like to open the download page?";

            var result = await _dialogService.ConfirmAsync("Update Available", message);
            if (result)
            {
                // Open the release page in the default browser
                try
                {
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = updateInfo.ReleaseUrl,
                        UseShellExecute = true
                    };
                    System.Diagnostics.Process.Start(psi);
                }
                catch
                {
                    StatusMessage = "Could not open browser";
                }
            }
            StatusMessage = $"Update available: v{updateInfo.LatestVersion}";
        }
        else if (!string.IsNullOrEmpty(updateInfo.ReleaseNotes) && updateInfo.ReleaseNotes.StartsWith("Error"))
        {
            StatusMessage = updateInfo.ReleaseNotes;
        }
        else
        {
            StatusMessage = $"You're up to date (v{updateInfo.CurrentVersion})";
        }
    }

    [RelayCommand]
    private async Task BrowseRecipes()
    {
        var selectedRecipe = await _dialogService.ShowRecipeBrowserAsync(_recipeService.AllRecipes);
        if (selectedRecipe != null)
        {
            // Clone the recipe and add it
            var clone = selectedRecipe.Clone() as Recipe;
            if (clone != null)
            {
                Current.Character.recipes.Add(clone);
                Recipes.Add(new RecipeViewModel(this, clone));
                MarkDirty();
                RegenerateOutput();
                StatusMessage = $"Added recipe: {clone.name ?? clone.id.ToString()}";
            }
        }
    }

    [RelayCommand]
    private async Task CreateRecipe()
    {
        var (success, name, category, component, description, content) =
            await _dialogService.ShowCreateRecipeDialogAsync();

        if (!success || string.IsNullOrWhiteSpace(name))
            return;

        // Create a simple recipe with the provided content
        var recipe = new Recipe
        {
            id = Utility.CreateGUID(),
            name = name,
            category = category,
            isEnabled = true,
            description = description,
        };

        // Add a text parameter with the content
        var textParam = new TextParameter();
        textParam.SetupForCreation("text", "Content", description, content);
        recipe.parameters.Add(textParam);

        // Add a template targeting the selected component
        var template = new Recipe.Template
        {
            channel = component,
            text = "{#text}",
        };
        recipe.templates.Add(template);

        // Add to current character
        Current.Character.recipes.Add(recipe);
        Recipes.Add(new RecipeViewModel(this, recipe));

        MarkDirty();
        RegenerateOutput();
        StatusMessage = $"Created recipe: {name}";
    }

    [RelayCommand]
    private async Task CreateSnippet()
    {
        var (success, filename, output) = await _dialogService.ShowCreateSnippetDialogAsync();

        if (!success || string.IsNullOrEmpty(filename))
            return;

        try
        {
            // Ensure directory exists
            var dir = System.IO.Path.GetDirectoryName(filename);
            if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);

            // Serialize the output as a snippet file (using GingerCardV1 format for snippets)
            // For now, just save as a simple text file with the content
            var content = new System.Text.StringBuilder();
            if (!output.persona.IsNullOrEmpty())
                content.AppendLine(output.persona.ToString());
            if (!output.scenario.IsNullOrEmpty())
                content.AppendLine(output.scenario.ToString());
            if (!output.system.IsNullOrEmpty())
                content.AppendLine(output.system.ToString());
            if (output.greetings?.Length > 0 && !output.greetings[0].IsNullOrEmpty())
                content.AppendLine(output.greetings[0].ToString());

            await System.IO.File.WriteAllTextAsync(filename, content.ToString());
            StatusMessage = $"Created snippet: {System.IO.Path.GetFileName(filename)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error creating snippet: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task BrowseBackyard()
    {
        var (success, group, character, wantsLink) = await _dialogService.ShowBackyardBrowserAsync();
        if (!success || group == null)
            return;

        try
        {
            // Get character ID to import
            string characterId = character?.isDefined == true ? character.Value.instanceId : null;
            if (string.IsNullOrEmpty(characterId))
            {
                StatusMessage = "No character selected to import";
                return;
            }

            StatusMessage = $"Importing from Backyard...";

            // Import character from Backyard database
            Integration.Backyard.ImageInstance[] images = null;
            UserData userInfo = null;
            Integration.BackyardLinkCard card = null;

            var importError = await Task.Run(() =>
            {
                return Integration.Backyard.Database.ImportCharacter(characterId, out card, out images, out userInfo);
            });

            if (importError != Integration.Backyard.Error.NoError)
            {
                StatusMessage = $"Import failed: {importError}";
                return;
            }

            if (card == null)
            {
                StatusMessage = "Import failed: No character data found";
                return;
            }

            // Reset current character and load imported data
            Current.NewCharacter();

            // Update card metadata
            CharacterName = card.data.displayName ?? card.data.name ?? "";
            SpokenName = card.data.name ?? "";
            Persona = card.data.persona ?? "";
            Scenario = card.data.scenario ?? "";
            Greeting = card.data.greeting.text ?? "";
            ExampleMessages = card.data.example ?? "";
            SystemPrompt = card.data.system ?? "";

            // Load portrait if available
            if (images != null && images.Length > 0)
            {
                var imageUrl = images[0].imageUrl;
                if (!string.IsNullOrEmpty(imageUrl) && File.Exists(imageUrl))
                {
                    try
                    {
                        var portraitData = await File.ReadAllBytesAsync(imageUrl);
                        using var ms = new MemoryStream(portraitData);
                        PortraitImage = new Bitmap(ms);
                        Current.Card.portraitImage = ImageRef.FromBytes(portraitData);
                    }
                    catch { /* Ignore image load errors */ }
                }
            }

            // Set up link if requested
            if (wantsLink)
            {
                Current.Link = new Integration.Backyard.Link
                {
                    groupId = group.Value.instanceId,
                    isActive = true,
                    actors = new[] {
                        new Integration.Backyard.Link.Actor { remoteId = characterId }
                    }
                };

                if (Integration.Backyard.Database.GetCharacter(characterId, out var charInstance))
                {
                    Current.Link.updateDate = charInstance.updateDate;
                }

                StatusMessage = $"Imported and linked: {CharacterName}";
            }
            else
            {
                StatusMessage = $"Imported: {CharacterName}";
            }

            MarkDirty();
            RegenerateOutput();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ToggleDarkMode()
    {
        IsDarkMode = !IsDarkMode;
        StatusMessage = IsDarkMode ? "Dark mode enabled" : "Dark mode disabled";
    }

    [RelayCommand]
    private void ConnectBackyard()
    {
        try
        {
            var error = Integration.Backyard.EstablishConnection();
            if (error == Integration.Backyard.Error.NoError)
            {
                StatusMessage = $"Connected to Backyard AI ({Integration.Backyard.Characters.Count()} characters)";
            }
            else
            {
                StatusMessage = $"Connection failed: {error}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Connection error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void DisconnectBackyard()
    {
        Integration.Backyard.Disconnect();
        StatusMessage = "Disconnected from Backyard AI";
    }

    [RelayCommand]
    private async Task LinkCharacter()
    {
        if (!Integration.Backyard.IsConnected)
        {
            var error = Integration.Backyard.EstablishConnection();
            if (error != Integration.Backyard.Error.NoError)
            {
                StatusMessage = $"Could not connect to Backyard AI: {error}";
                return;
            }
        }

        var (success, group, character, _) = await _dialogService.ShowBackyardBrowserAsync();
        if (!success || group == null)
            return;

        try
        {
            // Create a link to the selected character/group
            var link = new Integration.Backyard.Link
            {
                groupId = group.Value.instanceId,
                isActive = true,
            };

            // Set up actors array if we have a specific character
            if (character?.isDefined == true)
            {
                link.actors = new[] {
                    new Integration.Backyard.Link.Actor {
                        remoteId = character.Value.instanceId,
                        localId = Current.MainCharacter.uid,
                    }
                };
            }

            Current.Link = link;

            var displayName = character?.isDefined == true
                ? (character.Value.displayName ?? character.Value.name)
                : group.Value.GetDisplayName();
            StatusMessage = $"Linked to: {displayName}";
            MarkDirty();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error linking: {ex.Message}";
        }
    }

    [RelayCommand]
    private void UnlinkCharacter()
    {
        if (Current.Link == null)
        {
            StatusMessage = "Character is not linked to Backyard AI";
            return;
        }

        Current.Link = null;
        StatusMessage = "Unlinked from Backyard AI";
        MarkDirty();
    }

    [RelayCommand]
    private async Task ExportAllBackyard()
    {
        if (!Integration.Backyard.IsConnected)
        {
            var error = Integration.Backyard.EstablishConnection();
            if (error != Integration.Backyard.Error.NoError)
            {
                StatusMessage = $"Could not connect to Backyard AI: {error}";
                return;
            }
        }

        var characters = Integration.Backyard.Characters.ToList();
        if (characters.Count == 0)
        {
            StatusMessage = "No characters found in Backyard AI";
            return;
        }

        // Ask for folder
        var folder = await _dialogService.ShowFolderDialogAsync("Select Export Folder");
        if (string.IsNullOrEmpty(folder))
            return;

        StatusMessage = $"Exporting {characters.Count} characters...";
        int exported = 0;
        int failed = 0;

        foreach (var character in characters)
        {
            try
            {
                // Import character from Backyard
                Integration.Backyard.ImageInstance[] images = null;
                UserData userInfo = null;
                Integration.BackyardLinkCard card = null;

                var importError = await Task.Run(() =>
                {
                    return Integration.Backyard.Database.ImportCharacter(character.instanceId, out card, out images, out userInfo);
                });

                if (importError != Integration.Backyard.Error.NoError || card == null)
                {
                    failed++;
                    continue;
                }

                // Create a filename from the character name
                var name = card.data.displayName ?? card.data.name ?? "Unknown";
                var safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
                var filePath = Path.Combine(folder, $"{safeName}.png");

                // Avoid overwriting
                int counter = 1;
                while (File.Exists(filePath))
                {
                    filePath = Path.Combine(folder, $"{safeName} ({counter}).png");
                    counter++;
                }

                // Create a minimal CharacterCard for export
                var exportCard = new CharacterCard
                {
                    Name = card.data.displayName ?? card.data.name ?? "",
                    Persona = card.data.persona ?? "",
                    Scenario = card.data.scenario ?? "",
                    Greeting = card.data.greeting.text ?? "",
                    Example = card.data.example ?? "",
                    System = card.data.system ?? "",
                };

                // Try to load portrait
                if (images != null && images.Length > 0)
                {
                    var imageUrl = images[0].imageUrl;
                    if (!string.IsNullOrEmpty(imageUrl) && File.Exists(imageUrl))
                    {
                        exportCard.PortraitData = await File.ReadAllBytesAsync(imageUrl);
                    }
                }

                // Save as PNG
                if (await _cardService.SaveAsync(filePath, exportCard))
                {
                    exported++;
                }
                else
                {
                    failed++;
                }

                StatusMessage = $"Exported {exported}/{characters.Count}...";
            }
            catch
            {
                failed++;
            }
        }

        StatusMessage = $"Export complete: {exported} succeeded, {failed} failed";
    }

    [RelayCommand]
    private async Task ImportFolderToBackyard()
    {
        if (!Integration.Backyard.IsConnected)
        {
            var error = Integration.Backyard.EstablishConnection();
            if (error != Integration.Backyard.Error.NoError)
            {
                StatusMessage = $"Could not connect to Backyard AI: {error}";
                return;
            }
        }

        // Ask for folder
        var folder = await _dialogService.ShowFolderDialogAsync("Select Folder to Import to Backyard");
        if (string.IsNullOrEmpty(folder))
            return;

        // Find all character files in folder
        var files = Directory.GetFiles(folder)
            .Where(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".charx", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".byaf", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (files.Length == 0)
        {
            StatusMessage = "No character files found in folder";
            return;
        }

        StatusMessage = $"Importing {files.Length} files to Backyard...";
        int imported = 0;
        int failed = 0;

        // Try to create a folder for imports
        Integration.Backyard.FolderInstance targetFolder = default;
        var folderName = AppSettings.BackyardLink.BulkImportFolderName;
        Integration.Backyard.Database.CreateNewFolder(folderName, out targetFolder);

        foreach (var file in files)
        {
            try
            {
                // Load the character card
                var (result, card) = await _cardService.LoadAsync(file);
                if (result != CharacterCardService.LoadResult.Success || card == null)
                {
                    failed++;
                    continue;
                }

                // Load character into Current for generation
                Current.NewCharacter();
                Current.Character.name = card.Name;
                Current.Character.spokenName = card.SpokenName;
                Current.Character.persona = card.Persona;
                Current.Character.personality = card.Personality;
                Current.Character.scenario = card.Scenario;
                Current.Character.greeting = card.Greeting;
                Current.Character.example = card.Example;
                Current.Character.system = card.System;
                if (card.PortraitData != null)
                    Current.Card.portraitImage = ImageRef.FromBytes(card.PortraitData);

                // Generate output for Backyard
                var output = Generator.Generate(Generator.Option.Export | Generator.Option.Faraday);
                var backyardCard = Integration.BackyardLinkCard.FromOutput(output);
                backyardCard.EnsureSystemPrompt(false);

                // Gather images for the character
                var imageInputs = new List<Integration.Backyard.ImageInput>();
                if (card.PortraitData != null && card.PortraitData.Length > 0)
                {
                    imageInputs.Add(new Integration.Backyard.ImageInput
                    {
                        image = ImageRef.FromBytes(card.PortraitData),
                        fileExt = "png",
                    });
                }

                // Create character in Backyard
                var args = new Integration.Backyard.CreateCharacterArguments
                {
                    card = backyardCard,
                    imageInput = imageInputs.ToArray(),
                    folder = targetFolder,
                };

                Integration.Backyard.CharacterInstance newCharacter;
                Integration.Backyard.Link.Image[] imageLinks;
                var createError = await Task.Run(() =>
                {
                    return Integration.Backyard.Database.CreateNewCharacter(args, out newCharacter, out imageLinks);
                });

                if (createError == Integration.Backyard.Error.NoError)
                    imported++;
                else
                    failed++;

                StatusMessage = $"Imported {imported}/{files.Length} to Backyard...";
            }
            catch
            {
                failed++;
            }
        }

        StatusMessage = $"Import to Backyard complete: {imported} succeeded, {failed} failed";
    }

    [RelayCommand]
    private async Task PushChanges()
    {
        if (Current.Link == null || string.IsNullOrEmpty(Current.Link.groupId))
        {
            StatusMessage = "Character is not linked to Backyard AI";
            return;
        }

        if (!Integration.Backyard.IsConnected)
        {
            var error = Integration.Backyard.EstablishConnection();
            if (error != Integration.Backyard.Error.NoError)
            {
                StatusMessage = $"Could not connect to Backyard AI: {error}";
                return;
            }
        }

        try
        {
            StatusMessage = "Pushing changes to Backyard AI...";

            // Generate output for Faraday format
            var output = Generator.Generate(Generator.Option.Export | Generator.Option.Faraday | Generator.Option.Linked);

            // Create BackyardLinkCard from output
            var card = Integration.BackyardLinkCard.FromOutput(output);

            // Ensure system prompt for solo characters
            if (Current.Link.linkType == Integration.Backyard.Link.LinkType.Solo ||
                Current.Link.linkType == Integration.Backyard.Link.LinkType.Group)
            {
                card.EnsureSystemPrompt(false);
            }

            // Call UpdateCharacter (synchronous, wrapped in Task.Run)
            DateTime updateDate = default;
            Integration.Backyard.Link.Image[] imageLinks = null;
            var updateError = await Task.Run(() =>
            {
                return Integration.Backyard.Database.UpdateCharacter(Current.Link, card, null, out updateDate, out imageLinks);
            });

            if (updateError != Integration.Backyard.Error.NoError)
            {
                StatusMessage = $"Push failed: {updateError}";
                return;
            }

            // Update link with new timestamp
            Current.Link.updateDate = updateDate;
            Current.Link.imageLinks = imageLinks;
            Current.Link.isDirty = false;

            StatusMessage = "Changes pushed to Backyard AI";
            MarkDirty();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error pushing changes: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task PullChanges()
    {
        if (Current.Link == null || string.IsNullOrEmpty(Current.Link.groupId))
        {
            StatusMessage = "Character is not linked to Backyard AI";
            return;
        }

        if (!Integration.Backyard.IsConnected)
        {
            var error = Integration.Backyard.EstablishConnection();
            if (error != Integration.Backyard.Error.NoError)
            {
                StatusMessage = $"Could not connect to Backyard AI: {error}";
                return;
            }
        }

        try
        {
            StatusMessage = "Pulling changes from Backyard AI...";

            // Get the main actor's character ID
            string characterId = Current.Link.mainActorId;
            if (string.IsNullOrEmpty(characterId))
            {
                StatusMessage = "Pull failed: No character ID in link";
                return;
            }

            // Import character from Backyard database
            Integration.Backyard.ImageInstance[] images = null;
            UserData userInfo = null;
            Integration.BackyardLinkCard card = null;

            var importError = await Task.Run(() =>
            {
                return Integration.Backyard.Database.ImportCharacter(characterId, out card, out images, out userInfo);
            });

            if (importError != Integration.Backyard.Error.NoError)
            {
                StatusMessage = $"Pull failed: {importError}";
                return;
            }

            if (card == null)
            {
                StatusMessage = "Pull failed: No character data found";
                return;
            }

            // Update ViewModel properties from the pulled data
            CharacterName = card.data.displayName ?? card.data.name ?? "";
            Persona = card.data.persona ?? "";
            Scenario = card.data.scenario ?? "";
            Greeting = card.data.greeting.text ?? "";
            ExampleMessages = card.data.example ?? "";
            SystemPrompt = card.data.system ?? "";

            // Update Current.Link timestamp
            if (Integration.Backyard.Database.GetCharacter(characterId, out var charInstance))
            {
                Current.Link.updateDate = charInstance.updateDate;
            }
            Current.Link.isDirty = false;

            StatusMessage = "Changes pulled from Backyard AI";
            MarkDirty();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error pulling changes: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SaveNewLinked()
    {
        if (!Integration.Backyard.IsConnected)
        {
            var error = Integration.Backyard.EstablishConnection();
            if (error != Integration.Backyard.Error.NoError)
            {
                StatusMessage = $"Could not connect to Backyard AI: {error}";
                return;
            }
        }

        // Export as a file that can then be imported via Import Folder to Backyard
        var filePath = await _fileService.SaveFileAsync(
            "Save Character for Backyard",
            $"{CharacterName.Replace(" ", "_")}.json",
            new[] { "*.json" });

        if (string.IsNullOrEmpty(filePath))
            return;

        try
        {
            StatusMessage = "Exporting character...";

            // Use the card service to export as JSON
            var card = ToCard();
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(card, Newtonsoft.Json.Formatting.Indented);
            await File.WriteAllTextAsync(filePath, json);

            StatusMessage = $"Character saved. Use 'Import Folder to Backyard' to add to Backyard AI";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error exporting character: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task RevertLinked()
    {
        // Revert is the same as Pull - reload from Backyard
        await PullChanges();
    }

    [RelayCommand]
    private async Task ShowChatHistory()
    {
        if (Current.Link == null || string.IsNullOrEmpty(Current.Link.groupId))
        {
            StatusMessage = "Character is not linked to Backyard AI";
            return;
        }

        if (!Integration.Backyard.ConnectionEstablished)
        {
            StatusMessage = "Not connected to Backyard AI";
            return;
        }

        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;
        var window = desktop.MainWindow;
        if (window == null) return;

        var dialog = new Views.Dialogs.LinkEditChatDialog();
        dialog.SetGroupId(Current.Link.groupId, CharacterName);
        await dialog.ShowDialog(window);
    }

    [RelayCommand]
    private async Task EditModelSettings()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;
        var window = desktop.MainWindow;
        if (window == null) return;

        var dialog = new Views.Dialogs.EditModelSettingsDialog();

        // Load current settings from AppSettings or defaults
        dialog.LoadSettings(
            AppSettings.Settings.DefaultTemperature,
            AppSettings.Settings.DefaultMinP,
            AppSettings.Settings.DefaultTopP,
            AppSettings.Settings.DefaultTopK,
            AppSettings.Settings.DefaultRepeatPenalty,
            AppSettings.Settings.DefaultRepeatLastN);

        await dialog.ShowDialog(window);

        if (dialog.DialogResult)
        {
            // Save the new settings
            AppSettings.Settings.DefaultTemperature = dialog.Temperature;
            AppSettings.Settings.DefaultMinP = dialog.MinP;
            AppSettings.Settings.DefaultTopP = dialog.TopP;
            AppSettings.Settings.DefaultTopK = dialog.TopK;
            AppSettings.Settings.DefaultRepeatPenalty = dialog.RepeatPenalty;
            AppSettings.Settings.DefaultRepeatLastN = dialog.RepeatLastN;
            AppSettings.Save();
            StatusMessage = "Model settings saved";
        }
    }

    [RelayCommand]
    private async Task ReestablishLink()
    {
        if (Current.Link == null)
        {
            StatusMessage = "No link to reestablish";
            return;
        }

        if (!Integration.Backyard.ConnectionEstablished)
        {
            StatusMessage = "Not connected to Backyard AI";
            return;
        }

        // Refresh the character list
        var error = Integration.Backyard.RefreshCharacters();
        if (error != Integration.Backyard.Error.NoError)
        {
            StatusMessage = "Failed to refresh Backyard characters";
            return;
        }

        // Try to find the character by group ID
        bool found = false;
        if (!string.IsNullOrEmpty(Current.Link.groupId))
        {
            // Check if group exists in the database
            found = Integration.Backyard.Groups.Any(g => g.instanceId == Current.Link.groupId);
        }

        // If not found by group, try by actor ID
        if (!found && !string.IsNullOrEmpty(Current.Link.mainActorId))
        {
            var character = Integration.Backyard.Characters.FirstOrDefault(c => c.instanceId == Current.Link.mainActorId);
            if (!string.IsNullOrEmpty(character.instanceId))
            {
                Current.Link.groupId = character.groupId;
                found = true;
            }
        }

        if (found)
        {
            Current.Link.filename = _currentFilePath;
            Current.Link.isActive = true;
            Current.Link.RefreshState();
            Current.IsFileDirty = true;
            UpdateWindowTitle();
            StatusMessage = "Link reestablished successfully";
        }
        else
        {
            var confirm = await _dialogService.ShowConfirmationDialogAsync(
                "Link Not Found",
                "Could not find the linked character in Backyard AI. Remove the link?");
            if (confirm)
            {
                Current.Link = null;
                Current.IsFileDirty = true;
                UpdateWindowTitle();
                StatusMessage = "Link removed";
            }
        }
    }

    [ObservableProperty]
    private bool _allowNsfw = true;

    [RelayCommand]
    private void ToggleNsfw()
    {
        AllowNsfw = !AllowNsfw;
        AppSettings.Settings.AllowNSFW = AllowNsfw;
        AppSettings.Save();
        StatusMessage = AllowNsfw ? "NSFW content allowed" : "NSFW content filtered";
    }

    #region Backyard Options Properties

    [ObservableProperty]
    private bool _backyardAutosave = true;

    [ObservableProperty]
    private bool _backyardAlwaysLink = true;

    [ObservableProperty]
    private bool _backyardUsePortraitAsBackground = false;

    [ObservableProperty]
    private bool _backyardImportAltGreetings = false;

    [ObservableProperty]
    private bool _backyardWriteUserPersona = false;

    [ObservableProperty]
    private bool _backyardWriteAuthorNote = true;

    [RelayCommand]
    private void ToggleBackyardAutosave()
    {
        BackyardAutosave = !BackyardAutosave;
        AppSettings.BackyardLink.Autosave = BackyardAutosave;
        AppSettings.Save();
        StatusMessage = BackyardAutosave ? "Backyard autosave enabled" : "Backyard autosave disabled";
    }

    [RelayCommand]
    private void ToggleBackyardAlwaysLink()
    {
        BackyardAlwaysLink = !BackyardAlwaysLink;
        AppSettings.BackyardLink.AlwaysLinkOnImport = BackyardAlwaysLink;
        AppSettings.Save();
        StatusMessage = BackyardAlwaysLink ? "Always link on import enabled" : "Always link on import disabled";
    }

    [RelayCommand]
    private void ToggleBackyardUsePortraitAsBackground()
    {
        BackyardUsePortraitAsBackground = !BackyardUsePortraitAsBackground;
        AppSettings.BackyardLink.UsePortraitAsBackground = BackyardUsePortraitAsBackground;
        AppSettings.Save();
        StatusMessage = BackyardUsePortraitAsBackground ? "Use portrait as background enabled" : "Use portrait as background disabled";
    }

    [RelayCommand]
    private void ToggleBackyardImportAltGreetings()
    {
        BackyardImportAltGreetings = !BackyardImportAltGreetings;
        AppSettings.BackyardLink.ImportAlternateGreetings = BackyardImportAltGreetings;
        AppSettings.Save();
        StatusMessage = BackyardImportAltGreetings ? "Import alternate greetings enabled" : "Import alternate greetings disabled";
    }

    [RelayCommand]
    private void ToggleBackyardWriteUserPersona()
    {
        BackyardWriteUserPersona = !BackyardWriteUserPersona;
        AppSettings.BackyardLink.WriteUserPersona = BackyardWriteUserPersona;
        AppSettings.Save();
        StatusMessage = BackyardWriteUserPersona ? "Write user persona enabled" : "Write user persona disabled";
    }

    [RelayCommand]
    private void ToggleBackyardWriteAuthorNote()
    {
        BackyardWriteAuthorNote = !BackyardWriteAuthorNote;
        AppSettings.BackyardLink.WriteAuthorNote = BackyardWriteAuthorNote;
        AppSettings.Save();
        StatusMessage = BackyardWriteAuthorNote ? "Write author note enabled" : "Write author note disabled";
    }

    [RelayCommand]
    private void SetBackyardApplyChatSettings(string setting)
    {
        AppSettings.BackyardLink.ApplyChatSettings = setting switch
        {
            "First" => AppSettings.BackyardLink.ActiveChatSetting.First,
            "Last" => AppSettings.BackyardLink.ActiveChatSetting.Last,
            "All" => AppSettings.BackyardLink.ActiveChatSetting.All,
            _ => AppSettings.BackyardLink.ActiveChatSetting.Last
        };
        AppSettings.Save();
        StatusMessage = $"Chat settings will apply to {setting.ToLower()} chat";
    }

    #endregion

    #region Backyard Utilities Commands

    [RelayCommand]
    private async Task CreateBackyardBackup()
    {
        if (!Integration.Backyard.IsConnected)
        {
            StatusMessage = "Not connected to Backyard AI";
            return;
        }

        var filePath = await _fileService.SaveFileAsync(
            "Save Backup",
            "backyard_backup.db",
            new[] { "*.db" });

        if (string.IsNullOrEmpty(filePath))
            return;

        StatusMessage = "Creating backup...";
        try
        {
            // Get database location from Backyard settings
            var dbLocation = AppSettings.BackyardLink.Location;
            if (!string.IsNullOrEmpty(dbLocation) && File.Exists(dbLocation))
            {
                File.Copy(dbLocation, filePath, overwrite: true);
                StatusMessage = $"Backup created: {Path.GetFileName(filePath)}";
            }
            else
            {
                StatusMessage = "Could not locate Backyard database. Connect to Backyard first.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Backup failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task RestoreBackyardBackup()
    {
        var confirm = await _dialogService.ShowConfirmationDialogAsync(
            "Restore Backup",
            "This will replace your current Backyard database. Are you sure?");
        if (!confirm)
            return;

        var filePath = await _fileService.OpenFileAsync(
            "Open Backup",
            new[] { "*.db" });

        if (string.IsNullOrEmpty(filePath))
            return;

        StatusMessage = "Restoring backup...";
        try
        {
            var dbLocation = AppSettings.BackyardLink.Location;
            if (string.IsNullOrEmpty(dbLocation))
            {
                StatusMessage = "No Backyard database location configured";
                return;
            }

            // Disconnect first
            Integration.Backyard.Disconnect();

            // Copy backup over
            File.Copy(filePath, dbLocation, overwrite: true);
            StatusMessage = "Backup restored. Reconnect to Backyard to see changes.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Restore failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private void PurgeUnusedImages()
    {
        if (!Integration.Backyard.IsConnected)
        {
            StatusMessage = "Not connected to Backyard AI";
            return;
        }

        // This feature requires implementation in the Backyard database layer
        StatusMessage = "Purge unused images: Feature not yet fully implemented";
    }

    #endregion

    #region Options Menu Properties and Commands

    // Token Budget options
    public ObservableCollection<string> TokenBudgetOptions { get; } = new()
    {
        "None",
        "1024 tokens",
        "2048 tokens",
        "4096 tokens",
        "6144 tokens",
        "8192 tokens",
        "12288 tokens",
        "16384 tokens",
        "24576 tokens",
        "32768 tokens"
    };

    private static readonly int[] TokenBudgetValues = { 0, 1024, 2048, 4096, 6144, 8192, 12288, 16384, 24576, 32768 };

    [ObservableProperty]
    private string _selectedTokenBudget = "None";

    partial void OnSelectedTokenBudgetChanged(string value)
    {
        int index = TokenBudgetOptions.IndexOf(value);
        if (index >= 0 && index < TokenBudgetValues.Length)
        {
            AppSettings.Settings.TokenBudget = TokenBudgetValues[index];
            AppSettings.Save();
            StatusMessage = value == "None" ? "Token budget disabled" : $"Token budget set to {value}";
        }
    }

    [RelayCommand]
    private void SetTokenBudget(string budget)
    {
        SelectedTokenBudget = budget;
    }

    // Output Preview options
    public ObservableCollection<string> OutputPreviewOptions { get; } = new()
    {
        "Default",
        "SillyTavern",
        "Faraday",
        "Faraday (Party)",
        "Plain Text"
    };

    [ObservableProperty]
    private string _selectedOutputPreview = "Default";

    partial void OnSelectedOutputPreviewChanged(string value)
    {
        AppSettings.Settings.PreviewFormat = value switch
        {
            "SillyTavern" => AppSettings.Settings.OutputPreviewFormat.SillyTavern,
            "Faraday" => AppSettings.Settings.OutputPreviewFormat.Faraday,
            "Faraday (Party)" => AppSettings.Settings.OutputPreviewFormat.FaradayParty,
            "Plain Text" => AppSettings.Settings.OutputPreviewFormat.PlainText,
            _ => AppSettings.Settings.OutputPreviewFormat.Default
        };
        AppSettings.Save();
        RegenerateOutput();
        StatusMessage = $"Output preview set to {value}";
    }

    [RelayCommand]
    private void SetOutputPreview(string preview)
    {
        SelectedOutputPreview = preview;
    }

    // Spell Checking toggle
    [ObservableProperty]
    private bool _spellCheckEnabled = true;

    [RelayCommand]
    private void ToggleSpellCheck()
    {
        SpellCheckEnabled = !SpellCheckEnabled;
        AppSettings.Settings.SpellChecking = SpellCheckEnabled;
        AppSettings.Save();
        StatusMessage = SpellCheckEnabled ? "Spell checking enabled" : "Spell checking disabled";
    }

    // Auto Convert Name toggle
    [ObservableProperty]
    private bool _autoConvertName = true;

    [RelayCommand]
    private void ToggleAutoConvertName()
    {
        AutoConvertName = !AutoConvertName;
        AppSettings.Settings.AutoConvertNames = AutoConvertName;
        AppSettings.Save();
        StatusMessage = AutoConvertName ? "Auto convert name enabled" : "Auto convert name disabled";
    }

    // Auto Break toggle
    [ObservableProperty]
    private bool _autoBreak = true;

    [RelayCommand]
    private void ToggleAutoBreak()
    {
        AutoBreak = !AutoBreak;
        AppSettings.Settings.AutoBreakLine = AutoBreak;
        AppSettings.Save();
        StatusMessage = AutoBreak ? "Auto break lines enabled" : "Auto break lines disabled";
    }

    // Rearrange Lore toggle
    [ObservableProperty]
    private bool _rearrangeLoreEnabled = false;

    [RelayCommand]
    private void ToggleRearrangeLore()
    {
        RearrangeLoreEnabled = !RearrangeLoreEnabled;
        AppSettings.Settings.EnableRearrangeLoreMode = RearrangeLoreEnabled;
        AppSettings.Save();
        StatusMessage = RearrangeLoreEnabled ? "Rearrange lore mode enabled" : "Rearrange lore mode disabled";
    }

    // Show Recipe Category toggle
    [ObservableProperty]
    private bool _showRecipeCategory = false;

    [RelayCommand]
    private void ToggleShowRecipeCategory()
    {
        ShowRecipeCategory = !ShowRecipeCategory;
        StatusMessage = ShowRecipeCategory ? "Recipe categories shown" : "Recipe categories hidden";
    }

    // Sort Recipes
    [RelayCommand]
    private void SortRecipesByName()
    {
        var sorted = Recipes.OrderBy(r => r.Name).ToList();
        Recipes.Clear();
        foreach (var r in sorted)
            Recipes.Add(r);
        MarkDirty();
        StatusMessage = "Recipes sorted by name";
    }

    [RelayCommand]
    private void SortRecipesByCategory()
    {
        var sorted = Recipes.OrderBy(r => r.Category).ThenBy(r => r.Name).ToList();
        Recipes.Clear();
        foreach (var r in sorted)
            Recipes.Add(r);
        MarkDirty();
        StatusMessage = "Recipes sorted by category";
    }

    #endregion

    #region File Menu Commands

    [RelayCommand]
    private void NewWindow()
    {
        // Open a new instance of the application
        try
        {
            var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrEmpty(exePath))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true
                });
                StatusMessage = "Opening new window...";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not open new window: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SaveIncremental()
    {
        if (string.IsNullOrEmpty(_currentFilePath))
        {
            // No file saved yet, do regular Save As
            await SaveAsAsync();
            return;
        }

        // Generate incremental filename
        var dir = Path.GetDirectoryName(_currentFilePath) ?? ".";
        var name = Path.GetFileNameWithoutExtension(_currentFilePath);
        var ext = Path.GetExtension(_currentFilePath);

        // Find next available number
        int number = 1;
        string newPath;
        do
        {
            newPath = Path.Combine(dir, $"{name}_{number:D2}{ext}");
            number++;
        } while (File.Exists(newPath) && number < 100);

        if (File.Exists(newPath))
        {
            StatusMessage = "Too many incremental saves";
            return;
        }

        // Save to new path
        var card = ToCard();
        if (await _cardService.SaveAsync(newPath, card))
        {
            _currentFilePath = newPath;
            _isDirty = false;
            UpdateWindowTitle();
            AppSettings.AddToMRU(newPath, CharacterName);
            AppSettings.Save();
            StatusMessage = $"Saved as: {Path.GetFileName(newPath)}";
        }
        else
        {
            StatusMessage = "Failed to save file";
        }
    }

    [RelayCommand]
    private async Task RevertFile()
    {
        if (string.IsNullOrEmpty(_currentFilePath))
        {
            StatusMessage = "No file to revert to";
            return;
        }

        if (!File.Exists(_currentFilePath))
        {
            StatusMessage = "Original file no longer exists";
            return;
        }

        // Confirm revert if dirty
        if (_isDirty)
        {
            var confirm = await _dialogService.ShowConfirmationDialogAsync(
                "Revert to Saved",
                "Discard all changes and revert to the last saved version?");
            if (!confirm)
                return;
        }

        // Reload the file
        var (result, card) = await _cardService.LoadAsync(_currentFilePath);
        if (result == CharacterCardService.LoadResult.Success && card != null)
        {
            LoadFromCard(card);
            _isDirty = false;
            UpdateWindowTitle();
            StatusMessage = $"Reverted to: {Path.GetFileName(_currentFilePath)}";
        }
        else
        {
            StatusMessage = $"Failed to reload file: {result}";
        }
    }

    #endregion

    #region Help Menu Commands

    [RelayCommand]
    private void ViewHelp()
    {
        try
        {
            var helpUrl = "https://github.com/DominaeDev/ginger/wiki";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = helpUrl,
                UseShellExecute = true
            });
            StatusMessage = "Opening help...";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not open help: {ex.Message}";
        }
    }

    [RelayCommand]
    private void VisitGitHub()
    {
        try
        {
            var githubUrl = "https://github.com/DominaeDev/ginger";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = githubUrl,
                UseShellExecute = true
            });
            StatusMessage = "Opening GitHub page...";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not open GitHub: {ex.Message}";
        }
    }

    #endregion

    #region Tools Menu Commands

    [RelayCommand]
    private void BakeAll()
    {
        // Bake all recipes - flatten to text
        int bakedCount = 0;
        foreach (var recipe in Recipes.Where(r => r.IsEnabled))
        {
            recipe.Bake();
            bakedCount++;
        }
        MarkDirty();
        RegenerateOutput();
        StatusMessage = $"Baked {bakedCount} recipe(s)";
    }

    [RelayCommand]
    private void BakeActor()
    {
        // Bake recipes for current actor only
        int bakedCount = 0;
        foreach (var recipe in Recipes.Where(r => r.IsEnabled))
        {
            recipe.Bake();
            bakedCount++;
        }
        MarkDirty();
        RegenerateOutput();
        StatusMessage = $"Baked {bakedCount} recipe(s) for current actor";
    }

    [RelayCommand]
    private void MergeLore()
    {
        if (LorebookEntries.Count < 2)
        {
            StatusMessage = "At least 2 lore entries required to merge";
            return;
        }

        // Find entries with duplicate keys
        var groups = LorebookEntries
            .Where(e => !string.IsNullOrWhiteSpace(e.Keys))
            .GroupBy(e => e.Keys.Trim().ToLowerInvariant())
            .Where(g => g.Count() > 1)
            .ToList();

        if (groups.Count == 0)
        {
            StatusMessage = "No duplicate lore entries found to merge";
            return;
        }

        int mergedCount = 0;
        foreach (var group in groups)
        {
            var entries = group.ToList();
            var first = entries[0];

            // Merge content from other entries into first
            var combinedContent = first.Content;
            for (int i = 1; i < entries.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(entries[i].Content))
                {
                    combinedContent += "\n\n" + entries[i].Content;
                }
                LorebookEntries.Remove(entries[i]);
                mergedCount++;
            }
            first.Content = combinedContent;
        }

        MarkDirty();
        RegenerateOutput();
        StatusMessage = $"Merged {mergedCount} duplicate lore entries";
    }

    #endregion

    [RelayCommand]
    private async Task EditVariables()
    {
        var currentVariables = Current.Card.customVariables ?? new List<CustomVariable>();
        var (success, variables) = await _dialogService.ShowVariablesDialogAsync(currentVariables);
        if (success && variables != null)
        {
            Current.Card.customVariables = variables;
            MarkDirty();
            RegenerateOutput();
            StatusMessage = $"Updated {variables.Count} custom variable(s)";
        }
    }

    [RelayCommand]
    private async Task EditAssets()
    {
        var currentAssets = Current.Card.assets ?? new AssetCollection();
        var (success, assets, changed) = await _dialogService.ShowAssetViewDialogAsync(currentAssets);
        if (success && changed && assets != null)
        {
            Current.Card.assets = assets;
            MarkDirty();
            StatusMessage = $"Updated embedded assets ({assets.assets.Count} total)";
        }
    }

    [RelayCommand]
    private async Task RearrangeActors()
    {
        if (Current.Characters.Count <= 1)
        {
            StatusMessage = "At least 2 actors required to rearrange";
            return;
        }

        var (success, newOrder, changed) = await _dialogService.ShowRearrangeActorsDialogAsync(Current.Characters);
        if (success && changed && newOrder != null)
        {
            // Reorder the characters based on newOrder
            var reordered = new List<CharacterData>(Current.Characters.Count);
            foreach (var index in newOrder)
            {
                if (index >= 0 && index < Current.Characters.Count)
                    reordered.Add(Current.Characters[index]);
            }

            Current.Characters.Clear();
            Current.Characters.AddRange(reordered);

            MarkDirty();
            RegenerateOutput();
            StatusMessage = "Actors reordered";
        }
    }

    [RelayCommand]
    private async Task ImportFromUrl()
    {
        var (success, url) = await _dialogService.ShowEnterUrlDialogAsync(
            "Import from URL",
            "Enter the URL of a character card (PNG, JSON, or YAML):");

        if (!success || string.IsNullOrEmpty(url))
            return;

        StatusMessage = $"Downloading from {url}...";

        try
        {
            using var client = new System.Net.Http.HttpClient();
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsByteArrayAsync();
            var tempPath = Path.GetTempFileName();

            // Determine extension from URL or content type
            var ext = Path.GetExtension(new Uri(url).AbsolutePath).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext))
            {
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
                ext = contentType switch
                {
                    "image/png" => ".png",
                    "application/json" => ".json",
                    "text/yaml" or "application/x-yaml" => ".yaml",
                    _ => ".png"
                };
            }

            var filePath = Path.ChangeExtension(tempPath, ext);
            await File.WriteAllBytesAsync(filePath, content);

            // Try to load the downloaded file
            var (result, card) = await _cardService.LoadAsync(filePath);
            if (result == CharacterCardService.LoadResult.Success && card != null)
            {
                LoadFromCard(card);
                _currentFilePath = null; // Don't keep temp path
                StatusMessage = $"Imported: {CharacterName}";
            }
            else
            {
                StatusMessage = $"Failed to parse downloaded file: {result}";
            }

            // Clean up temp file
            try { File.Delete(filePath); } catch { }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Download failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ImportFromClipboard()
    {
        var (success, text) = await _dialogService.ShowPasteTextDialogAsync();

        if (!success || string.IsNullOrEmpty(text))
            return;

        StatusMessage = "Parsing pasted content...";

        try
        {
            // Try to parse as JSON first
            CharacterCard? card = null;

            if (text.TrimStart().StartsWith("{"))
            {
                // Looks like JSON
                card = _cardService.ParseFromJson(text);
            }
            else if (text.TrimStart().StartsWith("name:") || text.Contains("\nname:"))
            {
                // Looks like YAML
                card = _cardService.ParseFromYaml(text);
            }

            if (card != null)
            {
                LoadFromCard(card);
                _currentFilePath = null;
                StatusMessage = $"Imported from clipboard: {CharacterName}";
            }
            else
            {
                StatusMessage = "Could not parse pasted content as character data";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Parse failed: {ex.Message}";
        }
    }

    #endregion

    #region Advanced Backyard Commands

    [RelayCommand]
    private async Task SaveLinked()
    {
        // Save Linked = Push to Backyard + Save local file
        if (Current.Link == null)
        {
            StatusMessage = "Character is not linked to Backyard AI";
            return;
        }

        // First push to Backyard
        await PushChanges();

        // Then save local file if we have a path
        if (!string.IsNullOrEmpty(_currentFilePath))
        {
            await SaveAsync();
        }
        else
        {
            await SaveAsAsync();
        }
    }

    [RelayCommand]
    private async Task SaveAsNewParty()
    {
        if (!Integration.Backyard.ConnectionEstablished)
        {
            StatusMessage = "Not connected to Backyard AI";
            return;
        }

        if (Current.Characters.Count < 2)
        {
            StatusMessage = "Need at least 2 characters to create a party";
            return;
        }

        // For now, export as a JSON file that can be imported into Backyard
        var filters = new[]
        {
            new FilePickerFileType("JSON Files") { Patterns = new[] { "*.json" } }
        };

        var path = await _dialogService.ShowSaveFileDialogAsync("Save Party", $"{CharacterName}_party.json", filters);
        if (string.IsNullOrEmpty(path))
            return;

        try
        {
            // Create a multi-character card for the party
            var card = ToCard();
            var json = System.Text.Json.JsonSerializer.Serialize(card, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(path, json);
            StatusMessage = $"Party exported to {Path.GetFileName(path)} - Import via Backyard > Import Folder";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task BulkEditModelSettings()
    {
        if (!Integration.Backyard.ConnectionEstablished)
        {
            StatusMessage = "Not connected to Backyard AI";
            return;
        }

        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;
        var window = desktop.MainWindow;
        if (window == null) return;

        // Show model settings dialog to get new values
        var dialog = new Views.Dialogs.EditModelSettingsDialog();
        dialog.LoadSettings(
            AppSettings.Settings.DefaultTemperature,
            AppSettings.Settings.DefaultMinP,
            AppSettings.Settings.DefaultTopP,
            AppSettings.Settings.DefaultTopK,
            AppSettings.Settings.DefaultRepeatPenalty,
            AppSettings.Settings.DefaultRepeatLastN);

        await dialog.ShowDialog(window);

        if (!dialog.DialogResult)
            return;

        // Apply to all characters in Backyard (simplified - just save as defaults)
        AppSettings.Settings.DefaultTemperature = dialog.Temperature;
        AppSettings.Settings.DefaultMinP = dialog.MinP;
        AppSettings.Settings.DefaultTopP = dialog.TopP;
        AppSettings.Settings.DefaultTopK = dialog.TopK;
        AppSettings.Settings.DefaultRepeatPenalty = dialog.RepeatPenalty;
        AppSettings.Settings.DefaultRepeatLastN = dialog.RepeatLastN;
        AppSettings.Save();

        var characterCount = Integration.Backyard.Characters.Count();
        StatusMessage = $"Model settings saved as defaults (will apply to new characters)";
    }

    [RelayCommand]
    private async Task BulkExportParties()
    {
        if (!Integration.Backyard.ConnectionEstablished)
        {
            StatusMessage = "Not connected to Backyard AI";
            return;
        }

        var folder = await _dialogService.ShowFolderDialogAsync("Select Export Folder");
        if (string.IsNullOrEmpty(folder))
            return;

        int exported = 0;
        int failed = 0;

        foreach (var group in Integration.Backyard.Groups)
        {
            try
            {
                // Get characters in this group
                var characters = Integration.Backyard.Characters.Where(c => c.groupId == group.instanceId).ToList();
                if (characters.Count == 0)
                    continue;

                // Export as JSON
                var fileName = SanitizeFileName(group.displayName ?? $"party_{group.instanceId}") + ".json";
                var filePath = Path.Combine(folder, fileName);

                var partyData = new
                {
                    name = group.displayName,
                    groupId = group.instanceId,
                    characters = characters.Select(c => new { c.instanceId, c.displayName, c.persona }).ToList()
                };

                var json = System.Text.Json.JsonSerializer.Serialize(partyData, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(filePath, json);
                exported++;
            }
            catch
            {
                failed++;
            }
        }

        StatusMessage = $"Exported {exported} parties" + (failed > 0 ? $" ({failed} failed)" : "");
    }

    [RelayCommand]
    private async Task DeleteCharacters()
    {
        if (!Integration.Backyard.ConnectionEstablished)
        {
            StatusMessage = "Not connected to Backyard AI";
            return;
        }

        // Show browser to select characters to delete
        var (success, group, character, _) = await _dialogService.ShowBackyardBrowserAsync();
        if (!success || character == null)
            return;

        var confirm = await _dialogService.ShowConfirmationDialogAsync(
            "Delete Character",
            $"Are you sure you want to delete '{character.Value.displayName}' from Backyard AI?\n\nThis action cannot be undone.");

        if (!confirm)
            return;

        // Note: Direct database deletion would require additional API support
        // For now, inform user to use Backyard AI directly
        StatusMessage = "Character deletion requires using Backyard AI directly for safety";
    }

    [RelayCommand]
    private void RepairBrokenImages()
    {
        if (!Integration.Backyard.ConnectionEstablished)
        {
            StatusMessage = "Not connected to Backyard AI";
            return;
        }

        // This would scan for images with broken references
        StatusMessage = "Repair Broken Images: Use Backyard AI's built-in tools for database maintenance";
    }

    [RelayCommand]
    private void RepairLegacyChats()
    {
        if (!Integration.Backyard.ConnectionEstablished)
        {
            StatusMessage = "Not connected to Backyard AI";
            return;
        }

        // This would migrate old chat formats
        StatusMessage = "Repair Legacy Chats: Use Backyard AI's built-in tools for chat migration";
    }

    [RelayCommand]
    private void ResetModelsLocation()
    {
        if (!Integration.Backyard.ConnectionEstablished)
        {
            StatusMessage = "Not connected to Backyard AI";
            return;
        }

        // Reset model path settings
        StatusMessage = "Reset Models Location: Configure in Backyard AI settings";
    }

    [RelayCommand]
    private void ResetModelSettings()
    {
        // Reset to default values
        AppSettings.Settings.DefaultTemperature = 0.8m;
        AppSettings.Settings.DefaultMinP = 0.05m;
        AppSettings.Settings.DefaultTopP = 0.95m;
        AppSettings.Settings.DefaultTopK = 40;
        AppSettings.Settings.DefaultRepeatPenalty = 1.1m;
        AppSettings.Settings.DefaultRepeatLastN = 64;
        AppSettings.Save();

        StatusMessage = "Model settings reset to defaults";
    }

    #endregion

    #region Language Menu

    [ObservableProperty]
    private string _selectedLanguage = "en";

    public IEnumerable<string> AvailableLanguages => new[] { "en", "es", "fr", "de", "ja", "zh" };

    [RelayCommand]
    private void ChangeLanguage(string? language)
    {
        if (string.IsNullOrEmpty(language))
            return;

        SelectedLanguage = language;
        AppSettings.Settings.Language = language;
        AppSettings.Save();

        // Note: Full localization would require reloading UI strings
        StatusMessage = $"Language set to {language} (restart required for full effect)";
    }

    #endregion
}

public partial class LorebookEntryViewModel : ObservableObject
{
    private readonly MainViewModel _parent;

    [ObservableProperty]
    private string _keys = "";

    [ObservableProperty]
    private string _content = "";

    [ObservableProperty]
    private bool _isEnabled = true;

    [ObservableProperty]
    private bool _isExpanded = false;

    [ObservableProperty]
    private string _name = "";

    public LorebookEntryViewModel()
    {
        _parent = null!;
    }

    public LorebookEntryViewModel(MainViewModel parent)
    {
        _parent = parent;
    }

    partial void OnKeysChanged(string value) => _parent?.OnLorebookChanged();
    partial void OnContentChanged(string value) => _parent?.OnLorebookChanged();
    partial void OnIsEnabledChanged(bool value) => _parent?.OnLorebookChanged();

    [RelayCommand]
    private void Remove()
    {
        _parent?.RemoveLorebookEntry(this);
    }

    [RelayCommand]
    private void MoveUp()
    {
        _parent?.MoveLorebookEntryUp(this);
    }

    [RelayCommand]
    private void MoveDown()
    {
        _parent?.MoveLorebookEntryDown(this);
    }
}

public class RecipeLibraryItem
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
}
