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

    public MainViewModel() : this(new FileService(), new CharacterCardService(), new RecipeService())
    {
    }

    public MainViewModel(IFileService fileService, CharacterCardService cardService, RecipeService recipeService)
    {
        _fileService = fileService;
        _cardService = cardService;
        _recipeService = recipeService;
        _selectedDetailLevel = "Normal detail";

        // Try to find content path
        FindContentPath();
        LoadRecipeLibrary();

        NewCommand.Execute(null);
    }

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
                Id = recipe.Id,
                Name = recipe.Name,
                Title = recipe.Title,
                Description = recipe.Description,
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
        StatusMessage = $"Added recipe: {recipe.Title}";
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

        // System prompt
        if (!string.IsNullOrEmpty(SystemPrompt) && FilterModelInstructions)
        {
            output.AppendLine("=== SYSTEM PROMPT ===");
            output.AppendLine(SystemPrompt);
            output.AppendLine();
        }

        // Persona / Description
        if (!string.IsNullOrEmpty(Persona))
        {
            output.AppendLine("=== PERSONA ===");
            output.AppendLine(Persona);
            output.AppendLine();
        }

        // Personality
        if (!string.IsNullOrEmpty(Personality) && FilterPersonality)
        {
            output.AppendLine("=== PERSONALITY ===");
            output.AppendLine(Personality);
            output.AppendLine();
        }

        // Scenario
        if (!string.IsNullOrEmpty(Scenario) && FilterScenario)
        {
            output.AppendLine("=== SCENARIO ===");
            output.AppendLine(Scenario);
            output.AppendLine();
        }

        // Example messages
        if (!string.IsNullOrEmpty(ExampleMessages) && FilterExample)
        {
            output.AppendLine("=== EXAMPLE MESSAGES ===");
            output.AppendLine(ExampleMessages);
            output.AppendLine();
        }

        // Greeting
        if (!string.IsNullOrEmpty(Greeting) && FilterGreeting)
        {
            output.AppendLine("=== GREETING ===");
            output.AppendLine(Greeting);
            output.AppendLine();
        }

        // Recipes
        if (Recipes.Count > 0)
        {
            output.AppendLine("=== RECIPES ===");
            foreach (var recipe in Recipes)
            {
                if (!string.IsNullOrEmpty(recipe.Content))
                {
                    output.AppendLine($"[{recipe.Name}]");
                    output.AppendLine(recipe.Content);
                    output.AppendLine();
                }
            }
        }

        OutputPreview = output.ToString();
        RecipeCount = Recipes.Count;
        LoreCount = LorebookEntries.Count;

        // Rough token estimate (words / 0.75)
        var words = OutputPreview.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
        TokenCount = (int)(words / 0.75);
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
            card.Lorebook = new Lorebook();
            int id = 1;
            foreach (var entry in LorebookEntries)
            {
                card.Lorebook.Entries.Add(new Lorebook.LorebookEntry
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
    private void Exit()
    {
        Environment.Exit(0);
    }

    #endregion

    #region Edit Commands

    [RelayCommand]
    private void Undo() => StatusMessage = "Undo not yet implemented";

    [RelayCommand]
    private void Redo() => StatusMessage = "Redo not yet implemented";

    [RelayCommand]
    private void Find() => StatusMessage = "Find not yet implemented";

    [RelayCommand]
    private void FindReplace() => StatusMessage = "Find/Replace not yet implemented";

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
    private void About()
    {
        StatusMessage = "Ginger - Cross-platform character card editor";
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
