using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
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

    [RelayCommand]
    private void Find() => ShowFindDialog(findOnly: true);

    [RelayCommand]
    private void FindReplace() => ShowFindDialog(findOnly: false);

    private void ShowFindDialog(bool findOnly)
    {
        _dialogService.ShowFindReplaceDialog(
            onFind: (search, replace, matchCase, wholeWord) =>
            {
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

    #endregion

    #region Other Commands

    [RelayCommand]
    private async Task AboutAsync()
    {
        await _dialogService.ShowAboutAsync();
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
            if (wantsLink)
            {
                // Link mode - just record the link, don't import
                StatusMessage = $"Linked to: {group.Value.GetDisplayName()}";
            }
            else
            {
                // Import mode - load the character data
                if (character?.isDefined == true)
                {
                    StatusMessage = $"Importing from Backyard: {character.Value.displayName ?? character.Value.name}...";
                    // TODO: Implement full import from Backyard database
                    StatusMessage = $"Imported: {character.Value.displayName ?? character.Value.name}";
                }
                else
                {
                    StatusMessage = $"Selected group: {group.Value.GetDisplayName()}";
                }
            }
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
            // TODO: Implement full push to Backyard database
            // This would use Integration.Backyard.UpdateCharacter() when implemented
            StatusMessage = "Push to Backyard AI not yet fully implemented";
            await Task.CompletedTask;
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
            // TODO: Implement full pull from Backyard database
            // This would refresh character data from the Backyard database
            StatusMessage = "Pull from Backyard AI not yet fully implemented";
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error pulling changes: {ex.Message}";
        }
    }

    [ObservableProperty]
    private bool _allowNsfw = true;

    [RelayCommand]
    private void ToggleNsfw()
    {
        AllowNsfw = !AllowNsfw;
        StatusMessage = AllowNsfw ? "NSFW content allowed" : "NSFW content filtered";
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
