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
using Ginger.Integration;
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
    private readonly TokenizerService _tokenizerService;
    private CharacterCard? _currentCard;
    private string? _currentFilePath;
    private bool _isDirty;
    private string? _contentPath;
    private Generator.Output? _currentOutput;
    private int _outputHash;

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

    [ObservableProperty]
    private Bitmap? _backgroundImage;

    private byte[]? _backgroundData;

    [ObservableProperty]
    private bool _hasBackgroundImage;

    [ObservableProperty]
    private bool _isPortraitFallback;

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
    private ObservableCollection<string> _alternateGreetings = new();

    [ObservableProperty]
    private string _exampleMessages = "";

    [ObservableProperty]
    private string _systemPrompt = "";

    [ObservableProperty]
    private string _postHistoryInstructions = "";

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
    private bool _useStyleGrammar;

    // Include/Omit toggles (true = include, false = omit)
    [ObservableProperty]
    private bool _includeSystemPrompt = true;

    [ObservableProperty]
    private bool _includePersonality = true;

    [ObservableProperty]
    private bool _includeUserPersona = true;

    [ObservableProperty]
    private bool _includeScenario = true;

    [ObservableProperty]
    private bool _includeExample = true;

    [ObservableProperty]
    private bool _includeGreeting = true;

    [ObservableProperty]
    private bool _includeGrammar = true;

    [ObservableProperty]
    private bool _includeLore = true;

    // Legacy filter properties for compatibility
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
    private int _permanentTokensFaraday;

    [ObservableProperty]
    private int _permanentTokensSillyTavern;

    [ObservableProperty]
    private int _loreCount;

    [ObservableProperty]
    private int _recipeCount;

    [ObservableProperty]
    private int _actorCount;

    [ObservableProperty]
    private bool _hasMultipleActors;

    [ObservableProperty]
    private int _embeddedAssetCount;

    [ObservableProperty]
    private bool _hasEmbeddedAssets;

    [ObservableProperty]
    private bool _isBackyardConnected;

    [ObservableProperty]
    private string _backyardStatusText = "";

    [ObservableProperty]
    private Avalonia.Media.IBrush _backyardStatusColor = Avalonia.Media.Brushes.Gray;

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

    // Dictionary selection
    public ObservableCollection<DictionaryItem> AvailableDictionaries { get; } = new();

    [ObservableProperty]
    private DictionaryItem? _selectedDictionary;

    [ObservableProperty]
    private bool _spellCheckEnabled = true;

    public ObservableCollection<RecentFileItem> RecentFiles { get; } = new();

    public ObservableCollection<ActorItem> Actors { get; } = new();

    public ObservableCollection<RecipeViewModel> Recipes { get; } = new();

    public ObservableCollection<LorebookEntryViewModel> LorebookEntries { get; } = new();

    // Recipe Library
    public ObservableCollection<RecipeLibraryItem> ModelRecipes { get; } = new();
    public ObservableCollection<RecipeLibraryItem> CharacterRecipes { get; } = new();
    public ObservableCollection<RecipeLibraryItem> PersonalityRecipes { get; } = new();
    public ObservableCollection<RecipeLibraryItem> TraitRecipes { get; } = new();
    public ObservableCollection<RecipeLibraryItem> StoryRecipes { get; } = new();
    public ObservableCollection<RecipeLibraryItem> OtherRecipes { get; } = new();
    public ObservableCollection<RecipeLibraryItem> ComponentRecipes { get; } = new();
    public ObservableCollection<RecipeLibraryItem> SnippetRecipes { get; } = new();
    public ObservableCollection<RecipeLibraryItem> LoreRecipes { get; } = new();

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
        _tokenizerService = new TokenizerService();
        _selectedDetailLevel = "Normal detail";

        // Subscribe to undo state changes
        _undoService.StateChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
        };

        // Subscribe to token count updates
        _tokenizerService.TokenCountCompleted += OnTokenCountCompleted;

        // Try to find content path
        FindContentPath();
        LoadRecipeLibrary();

        // Load available dictionaries
        LoadAvailableDictionaries();

        // Load recent files
        LoadRecentFiles();

        NewCommand.Execute(null);
    }

    private void LoadRecentFiles()
    {
        RecentFiles.Clear();
        foreach (var entry in AppSettings.MRUList)
        {
            if (!string.IsNullOrEmpty(entry.Filename))
            {
                RecentFiles.Add(new RecentFileItem
                {
                    Filename = entry.Filename,
                    CharacterName = entry.CharacterName ?? ""
                });
            }
        }
    }

    private void RefreshActors()
    {
        Actors.Clear();
        for (int i = 0; i < Current.Characters.Count; i++)
        {
            var character = Current.Characters[i];
            Actors.Add(new ActorItem
            {
                Index = i,
                Name = character.spokenName ?? character.name ?? $"Actor {i + 1}"
            });
        }

        // Update actor count
        ActorCount = Current.Characters.Count;
        HasMultipleActors = ActorCount > 1;
        OnPropertyChanged(nameof(CanRemoveActor));
        OnPropertyChanged(nameof(IsMultiCharacter));

        // Update selected actor
        SelectedActor = Actors.FirstOrDefault(a => a.Index == Current.SelectedCharacter);
    }

    private void LoadAvailableDictionaries()
    {
        DictionaryService.Load();

        foreach (var dict in DictionaryService.Available)
        {
            AvailableDictionaries.Add(new DictionaryItem
            {
                Locale = dict.Key,
                DisplayName = dict.Value
            });
        }

        // Set selected dictionary from settings
        var currentLocale = AppSettings.Settings.Dictionary;
        SelectedDictionary = AvailableDictionaries.FirstOrDefault(d => d.Locale == currentLocale)
            ?? AvailableDictionaries.FirstOrDefault();

        SpellCheckEnabled = AppSettings.Settings.SpellChecking;
    }

    private void OnTokenCountCompleted(TokenizerService.Result result)
    {
        if (result.hash != _outputHash)
            return;

        // Update token counts on UI thread
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            TokenCount = result.tokens_total;

            // Use format-specific permanent token count
            PermanentTokens = AppSettings.Settings.PreviewFormat == AppSettings.Settings.OutputPreviewFormat.SillyTavern
                ? result.tokens_permanent_silly
                : result.tokens_permanent_faraday;

            // Update lorebook entry token counts if available
            if (result.loreTokens != null)
            {
                foreach (var entry in LorebookEntries)
                {
                    if (result.loreTokens.TryGetValue(entry.Id, out var tokenCount))
                    {
                        entry.TokenCount = tokenCount;
                    }
                }
            }
        });
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
        PopulateRecipeLibrary("Other", OtherRecipes);
        PopulateRecipeLibrary("Component", ComponentRecipes);
        PopulateRecipeLibrary("Snippet", SnippetRecipes);
        PopulateRecipeLibrary("Lore", LoreRecipes);
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
        Current.Character.recipes.Add(cloned);
        var vm = new RecipeViewModel(this, cloned);
        Recipes.Add(vm);

        // Record undo action
        _undoService.RecordAction("Add recipe",
            () =>
            {
                // Undo: remove the added recipe
                Current.Character.recipes.Remove(cloned);
                Recipes.Remove(vm);
                MarkDirty();
                RegenerateOutput();
            },
            () =>
            {
                // Redo: add again
                Current.Character.recipes.Add(cloned);
                Recipes.Add(vm);
                MarkDirty();
                RegenerateOutput();
            });

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
                var imageData = await File.ReadAllBytesAsync(file);

                if (Current.SelectedCharacter <= 0)
                {
                    // Main character: update main portrait
                    _portraitData = imageData;
                    using var stream = new MemoryStream(_portraitData);
                    PortraitImage = new Bitmap(stream);
                    IsPortraitFallback = false;
                }
                else
                {
                    // Secondary actor: create/update actor-specific portrait asset
                    SetActorPortrait(Current.SelectedCharacter, imageData);
                    using var stream = new MemoryStream(imageData);
                    PortraitImage = new Bitmap(stream);
                    IsPortraitFallback = false;
                }
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
        if (Current.SelectedCharacter <= 0)
        {
            // Main character: clear main portrait
            _portraitData = null;
            PortraitImage = null;
            IsPortraitFallback = false;
        }
        else
        {
            // Secondary actor: remove actor-specific portrait asset
            ClearActorPortrait(Current.SelectedCharacter);
            // Show fallback to main portrait
            ShowFallbackPortrait();
        }
        MarkDirty();
        StatusMessage = "Portrait cleared";
    }

    public void LoadPortraitFromData(byte[] data)
    {
        try
        {
            if (Current.SelectedCharacter <= 0)
            {
                _portraitData = data;
                using var stream = new MemoryStream(data);
                PortraitImage = new Bitmap(stream);
                IsPortraitFallback = false;
            }
            else
            {
                SetActorPortrait(Current.SelectedCharacter, data);
                using var stream = new MemoryStream(data);
                PortraitImage = new Bitmap(stream);
                IsPortraitFallback = false;
            }
            MarkDirty();
            StatusMessage = "Portrait loaded";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading portrait: {ex.Message}";
        }
    }

    /// <summary>
    /// Sets or updates the portrait asset for a secondary actor.
    /// </summary>
    private void SetActorPortrait(int actorIndex, byte[] imageData)
    {
        // Remove any existing portrait for this actor
        var existingAsset = Current.Card.assets.FirstOrDefault(a =>
            a.actorIndex == actorIndex &&
            (a.type == AssetFile.AssetType.Icon || a.type == AssetFile.AssetType.Portrait));

        if (existingAsset != null)
        {
            Current.Card.assets.Remove(existingAsset);
        }

        // Create new portrait asset
        var actorName = actorIndex < Current.Characters.Count
            ? Current.Characters[actorIndex].spokenName ?? Current.Characters[actorIndex].name ?? $"Actor {actorIndex + 1}"
            : $"Actor {actorIndex + 1}";

        var portraitAsset = new AssetFile
        {
            name = $"Portrait ({actorName})",
            actorIndex = actorIndex,
            type = AssetFile.AssetType.Icon,
            isEmbeddedAsset = true,
            data = new AssetData { data = imageData }
        };
        Current.Card.assets.Add(portraitAsset);
    }

    /// <summary>
    /// Removes the portrait asset for a secondary actor.
    /// </summary>
    private void ClearActorPortrait(int actorIndex)
    {
        var existingAsset = Current.Card.assets.FirstOrDefault(a =>
            a.actorIndex == actorIndex &&
            (a.type == AssetFile.AssetType.Icon || a.type == AssetFile.AssetType.Portrait));

        if (existingAsset != null)
        {
            Current.Card.assets.Remove(existingAsset);
        }
    }

    /// <summary>
    /// Returns true if the current portrait is larger than MaxImageDimension and can be resized.
    /// </summary>
    public bool CanResizePortrait
    {
        get
        {
            if (PortraitImage == null)
                return false;
            return PortraitImage.PixelSize.Width > Constants.MaxImageDimension ||
                   PortraitImage.PixelSize.Height > Constants.MaxImageDimension;
        }
    }

    [RelayCommand]
    private void ResizePortrait()
    {
        if (PortraitImage == null)
            return;

        int srcWidth = PortraitImage.PixelSize.Width;
        int srcHeight = PortraitImage.PixelSize.Height;

        if (srcWidth <= Constants.MaxImageDimension && srcHeight <= Constants.MaxImageDimension)
        {
            StatusMessage = "Portrait is already within size limits";
            return;
        }

        // Calculate new dimensions
        float scale = Math.Min((float)Constants.MaxImageDimension / srcWidth, (float)Constants.MaxImageDimension / srcHeight);
        int newWidth = Math.Max((int)Math.Round(srcWidth * scale), 1);
        int newHeight = Math.Max((int)Math.Round(srcHeight * scale), 1);

        try
        {
            // Resize using SkiaSharp
            byte[]? currentData = Current.SelectedCharacter <= 0
                ? _portraitData
                : Current.Card.assets.FirstOrDefault(a =>
                    a.actorIndex == Current.SelectedCharacter &&
                    (a.type == AssetFile.AssetType.Icon || a.type == AssetFile.AssetType.Portrait))?.data.data;

            if (currentData == null)
            {
                StatusMessage = "No portrait data to resize";
                return;
            }

            using var originalBitmap = SkiaSharp.SKBitmap.Decode(currentData);
            if (originalBitmap == null)
            {
                StatusMessage = "Failed to decode portrait image";
                return;
            }

            using var resizedBitmap = originalBitmap.Resize(new SkiaSharp.SKImageInfo(newWidth, newHeight), SkiaSharp.SKFilterQuality.High);
            if (resizedBitmap == null)
            {
                StatusMessage = "Failed to resize portrait image";
                return;
            }

            using var image = SkiaSharp.SKImage.FromBitmap(resizedBitmap);
            using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
            var resizedData = data.ToArray();

            // Update the portrait
            if (Current.SelectedCharacter <= 0)
            {
                _portraitData = resizedData;
                using var stream = new MemoryStream(resizedData);
                PortraitImage = new Bitmap(stream);
            }
            else
            {
                SetActorPortrait(Current.SelectedCharacter, resizedData);
                using var stream = new MemoryStream(resizedData);
                PortraitImage = new Bitmap(stream);
            }

            MarkDirty();
            OnPropertyChanged(nameof(CanResizePortrait));
            StatusMessage = $"Resized portrait from {srcWidth}x{srcHeight} to {newWidth}x{newHeight}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error resizing portrait: {ex.Message}";
        }
    }

    #region Background Image Commands

    [RelayCommand]
    private async Task LoadBackgroundAsync()
    {
        var file = await _fileService.OpenFileAsync(
            "Open Background Image",
            new[] { "*.png", "*.jpg", "*.jpeg", "*.gif", "*.bmp", "*.webp" });

        if (file != null)
        {
            try
            {
                _backgroundData = await File.ReadAllBytesAsync(file);
                using var stream = new MemoryStream(_backgroundData);
                BackgroundImage = new Bitmap(stream);
                HasBackgroundImage = true;
                MarkDirty();
                StatusMessage = "Background loaded";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading background: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    private void ClearBackground()
    {
        _backgroundData = null;
        BackgroundImage = null;
        HasBackgroundImage = false;
        MarkDirty();
        StatusMessage = "Background cleared";
    }

    [RelayCommand]
    private async Task PasteBackgroundAsync()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        var topLevel = desktop.MainWindow;
        if (topLevel?.Clipboard == null)
            return;

        try
        {
            var formats = await topLevel.Clipboard.GetFormatsAsync();
            if (formats.Contains("image/png") || formats.Contains("PNG"))
            {
                var data = await topLevel.Clipboard.GetDataAsync("image/png");
                if (data is byte[] imageBytes)
                {
                    _backgroundData = imageBytes;
                    using var stream = new MemoryStream(imageBytes);
                    BackgroundImage = new Bitmap(stream);
                    HasBackgroundImage = true;
                    MarkDirty();
                    StatusMessage = "Background pasted from clipboard";
                    return;
                }
            }

            // Try getting as file paths
            var text = await topLevel.Clipboard.GetTextAsync();
            if (!string.IsNullOrEmpty(text) && File.Exists(text))
            {
                _backgroundData = await File.ReadAllBytesAsync(text);
                using var stream = new MemoryStream(_backgroundData);
                BackgroundImage = new Bitmap(stream);
                HasBackgroundImage = true;
                MarkDirty();
                StatusMessage = "Background loaded from clipboard path";
                return;
            }

            StatusMessage = "No image found in clipboard";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error pasting background: {ex.Message}";
        }
    }

    [RelayCommand]
    private void UsePortraitAsBackground()
    {
        if (_portraitData == null || _portraitData.Length == 0)
        {
            StatusMessage = "No portrait to use as background";
            return;
        }

        try
        {
            _backgroundData = (byte[])_portraitData.Clone();
            using var stream = new MemoryStream(_backgroundData);
            BackgroundImage = new Bitmap(stream);
            HasBackgroundImage = true;
            MarkDirty();
            StatusMessage = "Portrait copied to background";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error copying portrait to background: {ex.Message}";
        }
    }

    [RelayCommand]
    private void BlurBackground()
    {
        if (_backgroundData == null || _backgroundData.Length == 0)
        {
            StatusMessage = "No background to blur";
            return;
        }

        try
        {
            _backgroundData = ImageService.BlurImage(_backgroundData, 15);
            using var stream = new MemoryStream(_backgroundData);
            BackgroundImage = new Bitmap(stream);
            MarkDirty();
            StatusMessage = "Background blurred";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error blurring background: {ex.Message}";
        }
    }

    [RelayCommand]
    private void DarkenBackground()
    {
        if (_backgroundData == null || _backgroundData.Length == 0)
        {
            StatusMessage = "No background to darken";
            return;
        }

        try
        {
            _backgroundData = ImageService.DarkenImage(_backgroundData, 0.5f);
            using var stream = new MemoryStream(_backgroundData);
            BackgroundImage = new Bitmap(stream);
            MarkDirty();
            StatusMessage = "Background darkened";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error darkening background: {ex.Message}";
        }
    }

    [RelayCommand]
    private void DesaturateBackground()
    {
        if (_backgroundData == null || _backgroundData.Length == 0)
        {
            StatusMessage = "No background to desaturate";
            return;
        }

        try
        {
            _backgroundData = ImageService.DesaturateImage(_backgroundData);
            using var stream = new MemoryStream(_backgroundData);
            BackgroundImage = new Bitmap(stream);
            MarkDirty();
            StatusMessage = "Background desaturated";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error desaturating background: {ex.Message}";
        }
    }

    public void LoadBackgroundFromData(byte[] data)
    {
        try
        {
            _backgroundData = data;
            using var stream = new MemoryStream(data);
            BackgroundImage = new Bitmap(stream);
            HasBackgroundImage = true;
        }
        catch
        {
            _backgroundData = null;
            BackgroundImage = null;
            HasBackgroundImage = false;
        }
    }

    #endregion

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
    partial void OnPortraitImageChanged(Bitmap? value) => OnPropertyChanged(nameof(CanResizePortrait));

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

    partial void OnSelectedDictionaryChanged(DictionaryItem? value)
    {
        if (value != null)
        {
            AppSettings.Settings.Dictionary = value.Locale;
            AppSettings.Save();
            StatusMessage = $"Spell check language: {value.DisplayName}";
        }
    }

    partial void OnSpellCheckEnabledChanged(bool value)
    {
        AppSettings.Settings.SpellChecking = value;
        AppSettings.Save();
    }

    // Output Settings change handlers
    partial void OnUserPersonaInScenarioChanged(bool value)
    {
        if (value)
            Current.Card.extraFlags.Add(CardData.Flag.UserPersonaInScenario);
        else
            Current.Card.extraFlags.Remove(CardData.Flag.UserPersonaInScenario);
        MarkDirty();
        RegenerateOutput();
    }

    partial void OnPruneScenarioChanged(bool value)
    {
        if (value)
            Current.Card.extraFlags.Add(CardData.Flag.PruneScenario);
        else
            Current.Card.extraFlags.Remove(CardData.Flag.PruneScenario);
        MarkDirty();
        RegenerateOutput();
    }

    partial void OnUseStyleGrammarChanged(bool value)
    {
        Current.Card.useStyleGrammar = value;
        MarkDirty();
        RegenerateOutput();
    }

    partial void OnIncludeSystemPromptChanged(bool value)
    {
        if (!value)
            Current.Card.extraFlags.Add(CardData.Flag.OmitSystemPrompt);
        else
            Current.Card.extraFlags.Remove(CardData.Flag.OmitSystemPrompt);
        MarkDirty();
        RegenerateOutput();
    }

    partial void OnIncludePersonalityChanged(bool value)
    {
        if (!value)
            Current.Card.extraFlags.Add(CardData.Flag.OmitPersonality);
        else
            Current.Card.extraFlags.Remove(CardData.Flag.OmitPersonality);
        MarkDirty();
        RegenerateOutput();
    }

    partial void OnIncludeUserPersonaChanged(bool value)
    {
        if (!value)
            Current.Card.extraFlags.Add(CardData.Flag.OmitUserPersona);
        else
            Current.Card.extraFlags.Remove(CardData.Flag.OmitUserPersona);
        MarkDirty();
        RegenerateOutput();
    }

    partial void OnIncludeScenarioChanged(bool value)
    {
        if (!value)
            Current.Card.extraFlags.Add(CardData.Flag.OmitScenario);
        else
            Current.Card.extraFlags.Remove(CardData.Flag.OmitScenario);
        MarkDirty();
        RegenerateOutput();
    }

    partial void OnIncludeExampleChanged(bool value)
    {
        if (!value)
            Current.Card.extraFlags.Add(CardData.Flag.OmitExample);
        else
            Current.Card.extraFlags.Remove(CardData.Flag.OmitExample);
        MarkDirty();
        RegenerateOutput();
    }

    partial void OnIncludeGreetingChanged(bool value)
    {
        if (!value)
            Current.Card.extraFlags.Add(CardData.Flag.OmitGreeting);
        else
            Current.Card.extraFlags.Remove(CardData.Flag.OmitGreeting);
        MarkDirty();
        RegenerateOutput();
    }

    partial void OnIncludeGrammarChanged(bool value)
    {
        if (!value)
            Current.Card.extraFlags.Add(CardData.Flag.OmitGrammar);
        else
            Current.Card.extraFlags.Remove(CardData.Flag.OmitGrammar);
        MarkDirty();
        RegenerateOutput();
    }

    partial void OnIncludeLoreChanged(bool value)
    {
        if (!value)
            Current.Card.extraFlags.Add(CardData.Flag.OmitLore);
        else
            Current.Card.extraFlags.Remove(CardData.Flag.OmitLore);
        MarkDirty();
        RegenerateOutput();
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
        // Sync UI fields to Current so Generator uses the latest values
        SyncToCurrent();

        // Build Generator options based on preview format
        Generator.Option options = Generator.Option.Preview;

        switch (AppSettings.Settings.PreviewFormat)
        {
            case AppSettings.Settings.OutputPreviewFormat.Faraday:
                options |= Generator.Option.Faraday;
                if (Backyard.ConnectionEstablished)
                    options |= Generator.Option.Linked;
                break;
            case AppSettings.Settings.OutputPreviewFormat.FaradayParty:
                options |= Generator.Option.Faraday;
                if (Backyard.ConnectionEstablished)
                    options |= Generator.Option.Linked;
                break;
            case AppSettings.Settings.OutputPreviewFormat.SillyTavern:
                options |= Generator.Option.SillyTavernV2;
                break;
        }

        // Generate output using the full Generator pipeline
        var output = Generator.Generate(options);
        _currentOutput = output;

        // Format output for display
        OutputPreview = FormatOutputForDisplay(output);

        // Update counts
        RecipeCount = Recipes.Count(r => r.IsEnabled);
        LoreCount = LorebookEntries.Count(e => e.IsEnabled);

        // Update actor list
        RefreshActors();

        // Update embedded assets count (from card's asset collection)
        EmbeddedAssetCount = Current.Card.assets?.assets?.Count ?? 0;
        HasEmbeddedAssets = EmbeddedAssetCount > 0;

        // Update Backyard connection status
        UpdateBackyardStatus();

        // Schedule async token counting
        _outputHash = output.GetHashCode() ^ (int)AppSettings.Settings.PreviewFormat;
        _tokenizerService.Schedule(output, _outputHash);
    }

    private void UpdateBackyardStatus()
    {
        IsBackyardConnected = Integration.Backyard.ConnectionEstablished;

        if (!IsBackyardConnected)
        {
            BackyardStatusText = "";
            BackyardStatusColor = Avalonia.Media.Brushes.Gray;
            return;
        }

        bool isLinked = Current.Link != null;
        bool isDirty = _isDirty;

        if (isLinked && isDirty)
        {
            BackyardStatusText = "Linked*";
            BackyardStatusColor = Avalonia.Media.Brushes.Orange;
        }
        else if (isLinked)
        {
            BackyardStatusText = "Linked";
            BackyardStatusColor = Avalonia.Media.Brushes.LimeGreen;
        }
        else
        {
            BackyardStatusText = "Connected";
            BackyardStatusColor = Avalonia.Media.Brushes.DodgerBlue;
        }
    }

    private static string FormatOutputForDisplay(Generator.Output output)
    {
        var sbOutput = new System.Text.StringBuilder();

        string outputSystem = output.system.ToOutputPreview();
        string outputSystemPostHistory = output.system_post_history.ToOutputPreview();
        string outputPersona = output.persona.ToOutputPreview();
        string outputPersonality = output.personality.ToOutputPreview();
        string outputScenario = output.scenario.ToOutputPreview();
        string outputGreeting = output.greeting.ToOutputPreview(Recipe.Component.Greeting);
        string outputExample = output.example.ToOutputPreview(Recipe.Component.Example);
        string outputGrammar = output.grammar.ToGrammarPreview();
        string outputUserPersona = output.userPersona.ToOutputPreview();

        // Plain Text mode - no headers, just concatenated content
        if (AppSettings.Settings.PreviewFormat == AppSettings.Settings.OutputPreviewFormat.PlainText)
        {
            var parts = new List<string>();

            if (!string.IsNullOrEmpty(outputSystem))
                parts.Add(outputSystem);
            if (!string.IsNullOrEmpty(outputSystemPostHistory))
                parts.Add(outputSystemPostHistory);
            if (!string.IsNullOrEmpty(outputPersona))
                parts.Add(outputPersona);
            if (!string.IsNullOrEmpty(outputPersonality))
                parts.Add(outputPersonality);
            if (!string.IsNullOrEmpty(outputUserPersona))
                parts.Add(outputUserPersona);
            if (!string.IsNullOrEmpty(outputScenario))
                parts.Add(outputScenario);
            if (!string.IsNullOrEmpty(outputExample))
                parts.Add(outputExample);
            if (!string.IsNullOrEmpty(outputGreeting))
                parts.Add(outputGreeting);

            // Alternate greetings
            if (output.greetings != null && output.greetings.Length > 1)
            {
                for (int i = 1; i < output.greetings.Length; ++i)
                    parts.Add(output.greetings[i].ToOutputPreview(Recipe.Component.Greeting));
            }

            // Group greetings
            if (output.group_greetings != null)
            {
                foreach (var greeting in output.group_greetings)
                    parts.Add(greeting.ToOutputPreview(Recipe.Component.Greeting));
            }

            // Lorebook
            if (output.hasLore)
            {
                foreach (var entry in output.lorebook.entries)
                    parts.Add(GingerString.FromString(entry.value).ToOutputPreview(Recipe.Component.Invalid));
            }

            if (!string.IsNullOrEmpty(outputGrammar))
                parts.Add(outputGrammar);

            return parts.Count > 0 ? string.Join("\n\n", parts) : "( NO OUTPUT )";
        }

        bool bSillyTavern = AppSettings.Settings.PreviewFormat == AppSettings.Settings.OutputPreviewFormat.SillyTavern;
        bool bFaraday = AppSettings.Settings.PreviewFormat == AppSettings.Settings.OutputPreviewFormat.Faraday
            || AppSettings.Settings.PreviewFormat == AppSettings.Settings.OutputPreviewFormat.FaradayParty;
        bool bUserPersona = Backyard.ConnectionEstablished && AppSettings.BackyardLink.WriteUserPersona;
        bool bShowGrammar = AppSettings.Settings.PreviewFormat != AppSettings.Settings.OutputPreviewFormat.SillyTavern;

        if (bFaraday && !(Backyard.ConnectionEstablished && AppSettings.BackyardLink.WriteAuthorNote))
        {
            // Combine system prompts
            if (!string.IsNullOrEmpty(outputSystemPostHistory))
                outputSystem = string.Join("\r\n", outputSystem, outputSystemPostHistory).TrimStart();
            outputSystemPostHistory = null;
        }

        if (bFaraday)
        {
            // Replace {original}
            string original = FaradayCardV4.OriginalModelInstructionsByFormat[EnumHelper.ToInt(Current.Card.textStyle)];
            if (!string.IsNullOrWhiteSpace(outputSystem))
            {
                int pos_original = outputSystem.IndexOf(GingerString.OriginalMarker, 0);
                if (pos_original != -1)
                {
                    var sbSystem = new System.Text.StringBuilder(outputSystem);
                    sbSystem.Remove(pos_original, 10);
                    sbSystem.Insert(pos_original, original);
                    sbSystem.Replace(GingerString.OriginalMarker, ""); // Only once
                    outputSystem = sbSystem.ToString();
                }
            }
        }

        if ((bFaraday && !bUserPersona) || bSillyTavern)
        {
            // Combine user persona
            if (!string.IsNullOrEmpty(outputUserPersona))
            {
                if (Current.Card.extraFlags.Contains(CardData.Flag.UserPersonaInScenario)
                    && !Current.Card.extraFlags.Contains(CardData.Flag.OmitScenario)) // -> Scenario
                    outputScenario = string.Concat(outputScenario, "\r\n\r\n", outputUserPersona).Trim();
                else // -> Persona
                    outputPersona = string.Concat(outputPersona, "\r\n\r\n", outputUserPersona).Trim();
                outputUserPersona = null;
            }
        }

        // Build output display
        if (!string.IsNullOrEmpty(outputSystem))
        {
            if (bSillyTavern)
                sbOutput.AppendLine(FormatHeader("SYSTEM INSTRUCTIONS"));
            else
                sbOutput.AppendLine(FormatHeader("MODEL INSTRUCTIONS"));
            sbOutput.AppendLine();
            sbOutput.AppendLine(outputSystem);
            sbOutput.AppendLine();
        }

        if (!string.IsNullOrEmpty(outputSystemPostHistory))
        {
            if (bSillyTavern)
                sbOutput.AppendLine(FormatHeader("POST HISTORY INSTRUCTIONS"));
            else
                sbOutput.AppendLine(FormatHeader("MODEL INSTRUCTIONS (IMPORTANT)"));
            sbOutput.AppendLine();
            sbOutput.AppendLine(outputSystemPostHistory);
            sbOutput.AppendLine();
        }

        if (!string.IsNullOrEmpty(outputPersona))
        {
            sbOutput.AppendLine(FormatHeader("CHARACTER PERSONA"));
            sbOutput.AppendLine();
            sbOutput.AppendLine(outputPersona);
            sbOutput.AppendLine();
        }

        if (!string.IsNullOrEmpty(outputPersonality))
        {
            sbOutput.AppendLine(FormatHeader("PERSONALITY SUMMARY"));
            sbOutput.AppendLine();
            sbOutput.AppendLine(outputPersonality);
            sbOutput.AppendLine();
        }

        if (!string.IsNullOrEmpty(outputUserPersona))
        {
            sbOutput.AppendLine(FormatHeader("USER PERSONA"));
            sbOutput.AppendLine();
            sbOutput.AppendLine(outputUserPersona);
            sbOutput.AppendLine();
        }

        if (!string.IsNullOrEmpty(outputScenario))
        {
            sbOutput.AppendLine(FormatHeader("SCENARIO"));
            sbOutput.AppendLine();
            sbOutput.AppendLine(outputScenario);
            sbOutput.AppendLine();
        }

        if (!string.IsNullOrEmpty(outputExample))
        {
            sbOutput.AppendLine(FormatHeader("EXAMPLE CHAT"));
            sbOutput.AppendLine();
            sbOutput.AppendLine(outputExample);
            sbOutput.AppendLine();
        }

        if (!string.IsNullOrEmpty(outputGreeting))
        {
            if (bFaraday)
                sbOutput.AppendLine(FormatHeader("FIRST MESSAGE"));
            else
                sbOutput.AppendLine(FormatHeader("GREETING"));
            sbOutput.AppendLine();
            sbOutput.AppendLine(outputGreeting);
            sbOutput.AppendLine();
        }

        // Alternate greetings
        if (output.greetings != null && output.greetings.Length > 1)
        {
            for (int i = 1; i < output.greetings.Length; ++i)
            {
                var greeting = output.greetings[i].ToOutputPreview(Recipe.Component.Greeting);
                if (output.greetings.Length > 2)
                    sbOutput.AppendLine(FormatHeader($"ALTERNATE GREETING #{i}"));
                else
                    sbOutput.AppendLine(FormatHeader("ALTERNATE GREETING"));
                sbOutput.AppendLine();
                sbOutput.AppendLine(greeting);
                sbOutput.AppendLine();
            }
        }

        // Group greetings
        if (output.group_greetings != null && output.group_greetings.Length > 0)
        {
            for (int i = 0; i < output.group_greetings.Length; ++i)
            {
                var greeting = output.group_greetings[i].ToOutputPreview(Recipe.Component.Greeting);
                if (output.group_greetings.Length > 1)
                    sbOutput.AppendLine(FormatHeader($"GROUP-ONLY GREETING #{i + 1}"));
                else
                    sbOutput.AppendLine(FormatHeader("GROUP-ONLY GREETING"));
                sbOutput.AppendLine();
                sbOutput.AppendLine(greeting);
                sbOutput.AppendLine();
            }
        }

        // Lorebook
        if (output.hasLore)
        {
            var entryCount = output.lorebook.entries.Count;
            sbOutput.AppendLine(FormatHeader($"LOREBOOK ({entryCount} {(entryCount == 1 ? "ENTRY" : "ENTRIES")})"));

            for (int i = 0; i < output.lorebook.entries.Count; ++i)
            {
                var entry = output.lorebook.entries[i];
                sbOutput.AppendLine();
                sbOutput.AppendLine($"#{i + 1} [{GingerString.FromString(entry.key).ToOutputPreview(Recipe.Component.Invalid)}]");
                sbOutput.AppendLine(GingerString.FromString(entry.value).ToOutputPreview(Recipe.Component.Invalid));
            }
            sbOutput.AppendLine();
        }

        // Grammar
        if (!string.IsNullOrEmpty(outputGrammar) && bShowGrammar)
        {
            sbOutput.AppendLine(FormatHeader("GRAMMAR"));
            sbOutput.AppendLine();
            sbOutput.AppendLine(outputGrammar);
            sbOutput.AppendLine();
        }

        if (sbOutput.Length == 0)
        {
            sbOutput.AppendLine("( NO OUTPUT )");
        }

        return sbOutput.ToString().TrimEnd();
    }

    private static string FormatHeader(string text)
    {
        const string line = "--------------------------------------------------"; // 50 chars
        return $"---- {text} {line.Substring(0, Math.Max(line.Length - text.Length, 0))}";
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
        PostHistoryInstructions = card.PostHistoryInstructions ?? "";

        // Alternate greetings
        AlternateGreetings.Clear();
        if (card.AlternateGreetings != null)
        {
            foreach (var altGreeting in card.AlternateGreetings)
            {
                AlternateGreetings.Add(altGreeting);
            }
        }

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
        IsPortraitFallback = false;

        // Lorebook - load all metadata for round-trip
        LorebookEntries.Clear();
        if (card.Lorebook != null)
        {
            foreach (var entry in card.Lorebook.Entries)
            {
                LorebookEntries.Add(new LorebookEntryViewModel(this)
                {
                    Keys = string.Join(", ", entry.Keys),
                    SecondaryKeys = entry.SecondaryKeys != null ? string.Join(", ", entry.SecondaryKeys) : "",
                    Content = entry.Content,
                    IsEnabled = entry.Enabled,
                    Name = entry.Name ?? "",
                    Comment = entry.Comment ?? "",
                    Constant = entry.Constant,
                    Selective = entry.Selective,
                    CaseSensitive = entry.CaseSensitive,
                    InsertionOrder = entry.InsertionOrder,
                    Priority = entry.Priority,
                    Position = entry.Position ?? "before_char",
                });
            }
        }

        // Clear recipes
        Recipes.Clear();

        // Load recipes based on source format
        if (card.GingerData != null && card.GingerData.characters.Count > 0)
        {
            // Ginger format: Load actual Recipe objects from GingerCardV1
            var mainChar = card.GingerData.characters[0];

            // Initialize Current with Ginger data
            Current.NewCharacter();
            Current.Card.name = card.Name;
            Current.Card.creator = card.Creator;
            Current.Card.comment = card.CreatorNotes;
            Current.Card.versionString = card.Version;
            Current.Card.userGender = card.UserGender;
            Current.Card.tags.Clear();
            foreach (var tag in card.Tags)
                Current.Card.tags.Add(tag);
            Current.MainCharacter.spokenName = mainChar.spokenName;
            Current.MainCharacter.gender = mainChar.gender ?? "";

            // Load recipes from Ginger data
            foreach (var recipe in mainChar.recipes)
            {
                // Add to Current
                Current.MainCharacter.recipes.Add(recipe);

                // Create ViewModel
                var recipeVm = new RecipeViewModel(this, recipe);
                Recipes.Add(recipeVm);
            }

            // Extract text content from recipes for the plain text fields
            // This ensures compatibility with non-Ginger exports
            var output = Generator.Generate(Generator.Option.Preview);
            if (!output.persona.IsNullOrEmpty())
                Persona = output.persona.ToString();
            if (!output.scenario.IsNullOrEmpty())
                Scenario = output.scenario.ToString();
            if (!output.greeting.IsNullOrEmpty())
                Greeting = output.greeting.ToString();
            if (!output.example.IsNullOrEmpty())
                ExampleMessages = output.example.ToString();
            if (!output.system.IsNullOrEmpty())
                SystemPrompt = output.system.ToString();
        }
        else
        {
            // Non-Ginger format: Create pseudo-recipes from plain text fields
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

            // Sync ViewModel state to Current model so Generator and Backyard work correctly
            SyncToCurrent();
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
        card.PostHistoryInstructions = PostHistoryInstructions;

        // Alternate greetings
        card.AlternateGreetings = AlternateGreetings.ToList();

        // Portrait
        card.PortraitData = _portraitData;

        // Lorebook - preserve all metadata
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
                    SecondaryKeys = entry.SecondaryKeys?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? Array.Empty<string>(),
                    Name = entry.Name ?? "",
                    Comment = entry.Comment ?? "",
                    Content = entry.Content,
                    Enabled = entry.IsEnabled,
                    Constant = entry.Constant,
                    Selective = entry.Selective,
                    CaseSensitive = entry.CaseSensitive,
                    InsertionOrder = entry.InsertionOrder,
                    Priority = entry.Priority,
                    Position = entry.Position ?? "before_char",
                });
            }
        }

        // Sync changes back to GingerData for the main character (preserves all other actors)
        if (card.GingerData != null)
        {
            // Update card-level properties
            card.GingerData.name = CharacterName;
            card.GingerData.creator = Creator;
            card.GingerData.comment = Comment;
            card.GingerData.versionString = Version;
            card.GingerData.userGender = UserGender;
            card.GingerData.tags = Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            // Update main character (characters[0])
            if (card.GingerData.characters.Count > 0)
            {
                var mainChar = card.GingerData.characters[0];
                mainChar.spokenName = SpokenName;
                mainChar.gender = SelectedGender ?? "";
                // Note: recipes are managed separately through Current.MainCharacter
                // They're already synced by the recipe editing logic
            }
            // Note: All other characters (characters[1+]) remain unchanged for round-trip
        }

        return card;
    }

    /// <summary>
    /// Sync the current UI state to the static Current model.
    /// This must be called before any operations that read from Current
    /// (e.g., Generator.Generate(), Backyard push/pull, export).
    /// </summary>
    private void SyncToCurrent()
    {
        // Card-level data
        Current.Card.name = CharacterName;
        Current.Card.creator = Creator;
        Current.Card.comment = Comment;
        Current.Card.versionString = Version;
        Current.Card.userPlaceholder = UserPlaceholder;
        Current.Card.userGender = UserGender;

        // Text style
        Current.Card.textStyle = SelectedTextStyle switch
        {
            "Chat (asterisks)" => CardData.TextStyle.Chat,
            "Novel (quotes)" => CardData.TextStyle.Novel,
            "Mixed" => CardData.TextStyle.Mixed,
            "Decorative quotes" => CardData.TextStyle.Decorative,
            "Bold" => CardData.TextStyle.Bold,
            "Parentheses" => CardData.TextStyle.Parentheses,
            _ => CardData.TextStyle.None,
        };

        // Detail level
        Current.Card.detailLevel = SelectedDetailLevel switch
        {
            "Less detail" => CardData.DetailLevel.Low,
            "More detail" => CardData.DetailLevel.High,
            _ => CardData.DetailLevel.Normal,
        };

        // Parse tags
        Current.Card.tags.Clear();
        if (!string.IsNullOrWhiteSpace(Tags))
        {
            foreach (var tag in Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                Current.Card.tags.Add(tag);
            }
        }

        // Portrait
        if (_portraitData != null)
            Current.Card.portraitImage = ImageRef.FromBytes(_portraitData);
        else
            Current.Card.portraitImage = null;

        // Main character data
        var character = Current.MainCharacter;
        character.spokenName = SpokenName;
        character.gender = SelectedGender ?? "";
        character.persona = Persona;
        character.personality = Personality;
        character.scenario = Scenario;
        character.greeting = Greeting;
        character.example = ExampleMessages;
        character.system = SystemPrompt;

        // Sync recipes from ViewModels to Current.Character.recipes
        character.recipes.Clear();
        foreach (var recipeVm in Recipes)
        {
            var sourceRecipe = recipeVm.GetSourceRecipe();
            if (sourceRecipe != null)
            {
                // Use the recipe with updated parameters
                character.recipes.Add(sourceRecipe);
            }
        }

        // Sync lorebook entries
        // Note: Lorebook is handled separately via Generator output
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

        // Record undo action
        _undoService.RecordAction("Add lore entry",
            () =>
            {
                LorebookEntries.Remove(entry);
                MarkDirty();
                RegenerateOutput();
            },
            () =>
            {
                LorebookEntries.Add(entry);
                MarkDirty();
                RegenerateOutput();
            });

        MarkDirty();
        RegenerateOutput();
    }

    public void RemoveLorebookEntry(LorebookEntryViewModel entry)
    {
        var idx = LorebookEntries.IndexOf(entry);
        if (idx < 0) return;

        // Record undo action
        _undoService.RecordAction("Remove lore entry",
            () =>
            {
                LorebookEntries.Insert(Math.Min(idx, LorebookEntries.Count), entry);
                MarkDirty();
                RegenerateOutput();
            },
            () =>
            {
                LorebookEntries.Remove(entry);
                MarkDirty();
                RegenerateOutput();
            });

        LorebookEntries.Remove(entry);
        MarkDirty();
        RegenerateOutput();
    }

    public void MoveLorebookEntryUp(LorebookEntryViewModel entry)
    {
        var index = LorebookEntries.IndexOf(entry);
        if (index > 0)
        {
            _undoService.RecordAction("Move lore entry",
                () => { LorebookEntries.Move(index - 1, index); MarkDirty(); },
                () => { LorebookEntries.Move(index, index - 1); MarkDirty(); });
            LorebookEntries.Move(index, index - 1);
            MarkDirty();
        }
    }

    public void MoveLorebookEntryDown(LorebookEntryViewModel entry)
    {
        var index = LorebookEntries.IndexOf(entry);
        if (index < LorebookEntries.Count - 1)
        {
            _undoService.RecordAction("Move lore entry",
                () => { LorebookEntries.Move(index + 1, index); MarkDirty(); },
                () => { LorebookEntries.Move(index, index + 1); MarkDirty(); });
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
    private void SortLorebookByName()
    {
        var sorted = LorebookEntries.OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase).ToList();
        LorebookEntries.Clear();
        foreach (var entry in sorted)
            LorebookEntries.Add(entry);
        MarkDirty();
        StatusMessage = "Lorebook sorted by name";
    }

    [RelayCommand]
    private void SortLorebookByKeys()
    {
        var sorted = LorebookEntries.OrderBy(e => e.Keys, StringComparer.OrdinalIgnoreCase).ToList();
        LorebookEntries.Clear();
        foreach (var entry in sorted)
            LorebookEntries.Add(entry);
        MarkDirty();
        StatusMessage = "Lorebook sorted by keys";
    }

    [RelayCommand]
    private void SortLorebookByInsertionOrder()
    {
        var sorted = LorebookEntries.OrderBy(e => e.InsertionOrder).ToList();
        LorebookEntries.Clear();
        foreach (var entry in sorted)
            LorebookEntries.Add(entry);
        MarkDirty();
        StatusMessage = "Lorebook sorted by insertion order";
    }

    [RelayCommand]
    private void SortLorebookByPriority()
    {
        var sorted = LorebookEntries.OrderByDescending(e => e.Priority).ToList();
        LorebookEntries.Clear();
        foreach (var entry in sorted)
            LorebookEntries.Add(entry);
        MarkDirty();
        StatusMessage = "Lorebook sorted by priority (highest first)";
    }

    [RelayCommand]
    private void SortLorebookByTokenCount()
    {
        var sorted = LorebookEntries.OrderByDescending(e => e.TokenCount).ToList();
        LorebookEntries.Clear();
        foreach (var entry in sorted)
            LorebookEntries.Add(entry);
        MarkDirty();
        StatusMessage = "Lorebook sorted by token count (largest first)";
    }

    public void DuplicateLorebookEntry(LorebookEntryViewModel entry)
    {
        var index = LorebookEntries.IndexOf(entry);
        var newEntry = new LorebookEntryViewModel(this)
        {
            Keys = entry.Keys,
            SecondaryKeys = entry.SecondaryKeys,
            Content = entry.Content,
            Name = entry.Name + " (copy)",
            Comment = entry.Comment,
            IsEnabled = entry.IsEnabled,
            Constant = entry.Constant,
            Selective = entry.Selective,
            CaseSensitive = entry.CaseSensitive,
            InsertionOrder = entry.InsertionOrder,
            Priority = entry.Priority,
            Position = entry.Position,
            Probability = entry.Probability,
            UseProbability = entry.UseProbability,
            Depth = entry.Depth,
            Group = entry.Group,
            ExcludeRecursion = entry.ExcludeRecursion,
            UseRegex = entry.UseRegex,
            IsExpanded = true
        };

        if (index >= 0 && index < LorebookEntries.Count - 1)
            LorebookEntries.Insert(index + 1, newEntry);
        else
            LorebookEntries.Add(newEntry);

        MarkDirty();
        StatusMessage = "Lorebook entry duplicated";
    }

    public async void PasteLorebookEntryAfter(LorebookEntryViewModel afterEntry)
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
            var doc = System.Text.Json.JsonDocument.Parse(text);
            var root = doc.RootElement;

            if (!root.TryGetProperty("type", out var typeEl) || typeEl.GetString() != "ginger_lore_entry")
            {
                StatusMessage = "Clipboard does not contain a lorebook entry";
                return;
            }

            var newEntry = new LorebookEntryViewModel(this)
            {
                Keys = root.TryGetProperty("keys", out var k) ? k.GetString() ?? "" : "",
                SecondaryKeys = root.TryGetProperty("secondaryKeys", out var sk) ? sk.GetString() ?? "" : "",
                Content = root.TryGetProperty("content", out var c) ? c.GetString() ?? "" : "",
                Name = root.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                Comment = root.TryGetProperty("comment", out var cm) ? cm.GetString() ?? "" : "",
                IsEnabled = root.TryGetProperty("isEnabled", out var ie) && ie.GetBoolean(),
                Constant = root.TryGetProperty("constant", out var co) && co.GetBoolean(),
                Selective = root.TryGetProperty("selective", out var se) && se.GetBoolean(),
                CaseSensitive = root.TryGetProperty("caseSensitive", out var cs) && cs.GetBoolean(),
                InsertionOrder = root.TryGetProperty("insertionOrder", out var io) ? io.GetInt32() : 100,
                Priority = root.TryGetProperty("priority", out var pr) ? pr.GetInt32() : 10,
                Position = root.TryGetProperty("position", out var po) ? po.GetString() ?? "before_char" : "before_char",
                Probability = root.TryGetProperty("probability", out var pb) ? pb.GetInt32() : 100,
                UseProbability = root.TryGetProperty("useProbability", out var up) && up.GetBoolean(),
                Depth = root.TryGetProperty("depth", out var de) ? de.GetInt32() : 4,
                Group = root.TryGetProperty("group", out var gr) ? gr.GetString() ?? "" : "",
                ExcludeRecursion = root.TryGetProperty("excludeRecursion", out var er) && er.GetBoolean(),
                UseRegex = root.TryGetProperty("useRegex", out var ur) && ur.GetBoolean(),
                IsExpanded = true
            };

            var index = LorebookEntries.IndexOf(afterEntry);
            if (index >= 0 && index < LorebookEntries.Count - 1)
                LorebookEntries.Insert(index + 1, newEntry);
            else
                LorebookEntries.Add(newEntry);

            MarkDirty();
            StatusMessage = "Lorebook entry pasted";
        }
        catch
        {
            StatusMessage = "Failed to paste lorebook entry from clipboard";
        }
    }

    [RelayCommand]
    private async Task ExportLorebook()
    {
        await ExportLorebookAsGinger();
    }

    [RelayCommand]
    private async Task ExportLorebookAsGinger()
    {
        if (LorebookEntries.Count == 0)
        {
            StatusMessage = "No lorebook entries to export";
            return;
        }

        var filters = new[]
        {
            new FilePickerFileType("JSON Files") { Patterns = new[] { "*.json" } },
            new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } }
        };

        var path = await _dialogService.ShowSaveFileDialogAsync("Export Lorebook (Ginger)", "lorebook.json", filters);
        if (string.IsNullOrEmpty(path))
            return;

        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(new
            {
                entries = LorebookEntries.Select(e => new
                {
                    keys = e.Keys,
                    content = e.Content,
                    enabled = e.IsEnabled,
                    name = e.Name,
                    secondaryKeys = e.SecondaryKeys,
                    constant = e.Constant,
                    selective = e.Selective,
                    caseSensitive = e.CaseSensitive,
                    priority = e.Priority,
                    insertionOrder = e.InsertionOrder,
                    position = e.Position,
                    depth = e.Depth,
                    probability = e.Probability,
                    useProbability = e.UseProbability,
                    group = e.Group,
                    excludeRecursion = e.ExcludeRecursion,
                    useRegex = e.UseRegex
                }).ToArray()
            }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

            await File.WriteAllTextAsync(path, json);
            StatusMessage = $"Exported {LorebookEntries.Count} lorebook entries (Ginger format)";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ExportLorebookAsTavern()
    {
        if (LorebookEntries.Count == 0)
        {
            StatusMessage = "No lorebook entries to export";
            return;
        }

        var filters = new[]
        {
            new FilePickerFileType("JSON Files") { Patterns = new[] { "*.json" } },
            new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } }
        };

        var path = await _dialogService.ShowSaveFileDialogAsync("Export Lorebook (SillyTavern)", "lorebook.json", filters);
        if (string.IsNullOrEmpty(path))
            return;

        try
        {
            // Build TavernWorldBook format with entries as Dictionary<string, Entry>
            var entriesDict = new Dictionary<string, object>();
            int uid = 0;
            foreach (var e in LorebookEntries)
            {
                var keys = e.Keys.Split(',').Select(k => k.Trim()).Where(k => !string.IsNullOrEmpty(k)).ToArray();
                var secondaryKeys = e.SecondaryKeys?.Split(',').Select(k => k.Trim()).Where(k => !string.IsNullOrEmpty(k)).ToArray() ?? Array.Empty<string>();

                entriesDict[uid.ToString()] = new
                {
                    uid = uid,
                    key = keys,
                    keysecondary = secondaryKeys,
                    comment = e.Name ?? "",
                    content = e.Content,
                    constant = e.Constant,
                    selective = e.Selective,
                    order = e.InsertionOrder,
                    position = GetTavernPosition(e.Position),
                    disable = !e.IsEnabled,
                    excludeRecursion = e.ExcludeRecursion,
                    probability = e.Probability,
                    useProbability = e.UseProbability,
                    depth = e.Depth,
                    group = e.Group ?? "",
                    displayIndex = uid
                };
                uid++;
            }

            var worldBook = new
            {
                name = CharacterName ?? "Lorebook",
                description = "",
                scan_depth = 50,
                token_budget = 500,
                recursive_scanning = false,
                entries = entriesDict
            };

            var json = System.Text.Json.JsonSerializer.Serialize(worldBook, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(path, json);
            StatusMessage = $"Exported {LorebookEntries.Count} lorebook entries (SillyTavern format)";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
        }
    }

    private static int GetTavernPosition(string position)
    {
        return position?.ToLower() switch
        {
            "before_char" => 0,
            "after_char" => 1,
            "before_an" or "before_author_note" => 2,
            "after_an" or "after_author_note" => 3,
            "at_depth" => 4,
            _ => 0
        };
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

            // Try to find entries in common lorebook formats
            System.Text.Json.JsonElement entriesElement = default;
            if (root.TryGetProperty("entries", out entriesElement) ||
                root.TryGetProperty("character_book", out var cb) && cb.TryGetProperty("entries", out entriesElement))
            {
                // Handle both Array format (Ginger/Agnai) and Object format (SillyTavern WorldBook)
                IEnumerable<System.Text.Json.JsonElement> entries;
                if (entriesElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    entries = entriesElement.EnumerateArray();
                }
                else if (entriesElement.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    // Tavern WorldBook format: entries is Dictionary<string, Entry>
                    entries = entriesElement.EnumerateObject().Select(p => p.Value);
                }
                else
                {
                    entries = Enumerable.Empty<System.Text.Json.JsonElement>();
                }

                foreach (var entry in entries)
                {
                    string keys = "";
                    string secondaryKeys = "";
                    string content = "";
                    string name = "";
                    bool enabled = true;
                    bool constant = false;
                    bool selective = false;
                    bool caseSensitive = false;
                    int insertionOrder = 100;
                    int priority = 10;
                    string position = "before_char";
                    int depth = 4;
                    int probability = 100;
                    bool useProbability = true;
                    string group = "";
                    bool excludeRecursion = false;
                    bool useRegex = false;

                    // Try various key formats
                    if (entry.TryGetProperty("keys", out var keysEl))
                    {
                        if (keysEl.ValueKind == System.Text.Json.JsonValueKind.Array)
                            keys = string.Join(", ", keysEl.EnumerateArray().Select(k => k.GetString()));
                        else if (keysEl.ValueKind == System.Text.Json.JsonValueKind.String)
                            keys = keysEl.GetString() ?? "";
                    }
                    else if (entry.TryGetProperty("key", out var keyEl))
                    {
                        if (keyEl.ValueKind == System.Text.Json.JsonValueKind.Array)
                            keys = string.Join(", ", keyEl.EnumerateArray().Select(k => k.GetString()));
                        else
                            keys = keyEl.GetString() ?? "";
                    }

                    // Secondary keys
                    if (entry.TryGetProperty("secondaryKeys", out var secKeysEl) || entry.TryGetProperty("keysecondary", out secKeysEl))
                    {
                        if (secKeysEl.ValueKind == System.Text.Json.JsonValueKind.Array)
                            secondaryKeys = string.Join(", ", secKeysEl.EnumerateArray().Select(k => k.GetString()));
                        else if (secKeysEl.ValueKind == System.Text.Json.JsonValueKind.String)
                            secondaryKeys = secKeysEl.GetString() ?? "";
                    }

                    // Name/comment
                    if (entry.TryGetProperty("name", out var nameEl))
                        name = nameEl.GetString() ?? "";
                    else if (entry.TryGetProperty("comment", out var commentEl))
                        name = commentEl.GetString() ?? "";

                    // Content
                    if (entry.TryGetProperty("content", out var contentEl))
                        content = contentEl.GetString() ?? "";
                    else if (entry.TryGetProperty("value", out var valueEl))
                        content = valueEl.GetString() ?? "";

                    // Enabled/disabled status
                    if (entry.TryGetProperty("enabled", out var enabledEl))
                        enabled = enabledEl.GetBoolean();
                    else if (entry.TryGetProperty("isEnabled", out var isEnabledEl))
                        enabled = isEnabledEl.GetBoolean();
                    else if (entry.TryGetProperty("disable", out var disableEl))
                        enabled = !disableEl.GetBoolean();

                    // Additional properties
                    if (entry.TryGetProperty("constant", out var constEl))
                        constant = constEl.GetBoolean();
                    if (entry.TryGetProperty("selective", out var selEl))
                        selective = selEl.GetBoolean();
                    if (entry.TryGetProperty("caseSensitive", out var caseEl))
                        caseSensitive = caseEl.GetBoolean();
                    if (entry.TryGetProperty("insertionOrder", out var orderEl) || entry.TryGetProperty("order", out orderEl))
                        insertionOrder = orderEl.TryGetInt32(out var o) ? o : 100;
                    if (entry.TryGetProperty("priority", out var prioEl))
                        priority = prioEl.TryGetInt32(out var p) ? p : 10;
                    if (entry.TryGetProperty("position", out var posEl))
                    {
                        if (posEl.ValueKind == System.Text.Json.JsonValueKind.Number)
                            position = GetPositionFromTavern(posEl.GetInt32());
                        else
                            position = posEl.GetString() ?? "before_char";
                    }
                    if (entry.TryGetProperty("depth", out var depthEl))
                        depth = depthEl.TryGetInt32(out var d) ? d : 4;
                    if (entry.TryGetProperty("probability", out var probEl))
                        probability = probEl.TryGetInt32(out var pr) ? pr : 100;
                    if (entry.TryGetProperty("useProbability", out var useProbEl))
                        useProbability = useProbEl.GetBoolean();
                    if (entry.TryGetProperty("group", out var groupEl))
                        group = groupEl.GetString() ?? "";
                    if (entry.TryGetProperty("excludeRecursion", out var exclEl))
                        excludeRecursion = exclEl.GetBoolean();
                    if (entry.TryGetProperty("useRegex", out var regexEl))
                        useRegex = regexEl.GetBoolean();

                    if (!string.IsNullOrEmpty(keys) || !string.IsNullOrEmpty(content))
                    {
                        LorebookEntries.Add(new LorebookEntryViewModel(this)
                        {
                            Keys = keys,
                            SecondaryKeys = secondaryKeys,
                            Content = content,
                            IsEnabled = enabled,
                            Name = !string.IsNullOrEmpty(name) ? name : keys.Split(',').FirstOrDefault()?.Trim() ?? $"Entry {LorebookEntries.Count + 1}",
                            Constant = constant,
                            Selective = selective,
                            CaseSensitive = caseSensitive,
                            InsertionOrder = insertionOrder,
                            Priority = priority,
                            Position = position,
                            Depth = depth,
                            Probability = probability,
                            UseProbability = useProbability,
                            Group = group,
                            ExcludeRecursion = excludeRecursion,
                            UseRegex = useRegex
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

    private static string GetPositionFromTavern(int position)
    {
        return position switch
        {
            0 => "before_char",
            1 => "after_char",
            2 => "before_an",
            3 => "after_an",
            4 => "at_depth",
            _ => "before_char"
        };
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

        // Clear undo history for new character
        _undoService.Clear();

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
        IsPortraitFallback = false;
        Notes = "";

        Persona = "";
        Personality = "";
        Scenario = "";
        Greeting = "";
        ExampleMessages = "";
        SystemPrompt = "";
        PostHistoryInstructions = "";
        AlternateGreetings.Clear();

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

    [RelayCommand]
    private async Task OpenRecentFile(RecentFileItem? item)
    {
        if (item == null || string.IsNullOrEmpty(item.Filename))
            return;

        if (!File.Exists(item.Filename))
        {
            StatusMessage = $"File not found: {item.Filename}";
            // Remove from MRU since file doesn't exist
            AppSettings.MRUList.RemoveAll(e => e.Filename == item.Filename);
            AppSettings.Save();
            LoadRecentFiles();
            return;
        }

        await LoadFileAsync(item.Filename);
    }

    [RelayCommand]
    private void ClearRecentFiles()
    {
        AppSettings.MRUList.Clear();
        AppSettings.Save();
        LoadRecentFiles();
        StatusMessage = "Recent files list cleared";
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
                    // Clear undo history for newly loaded character
                    _undoService.Clear();
                    UpdateWindowTitle();
                    StatusMessage = $"Loaded {card.Name} ({card.SourceFormat})";
                    // Add to MRU
                    AppSettings.AddToMRU(filePath, card.Name);
                    AppSettings.Save();
                    LoadRecentFiles();
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

            // Sync UI state to Current model before saving
            // This is required for Ginger XML export which reads from Current
            SyncToCurrent();

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
            // Sync UI state to Current model before export
            SyncToCurrent();

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
                // Sync UI state to Current model before export
                SyncToCurrent();

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

                // Sync UI state to Current model before export
                SyncToCurrent();

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

    [ObservableProperty]
    private ActorItem? _selectedActor;

    public bool CanRemoveActor => Current.Characters.Count > 1;
    public bool IsMultiCharacter => Current.Characters.Count > 1;

    partial void OnSelectedActorChanged(ActorItem? value)
    {
        if (value != null && value.Index != SelectedActorIndex)
        {
            SelectedActorIndex = value.Index;
        }
    }

    [RelayCommand]
    private void AddActor()
    {
        Current.AddCharacter();
        RefreshActors();
        SelectedActorIndex = Current.SelectedCharacter;
        SelectedActor = Actors.FirstOrDefault(a => a.Index == SelectedActorIndex);
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
        RefreshActors();
        SelectedActorIndex = Current.SelectedCharacter;
        SelectedActor = Actors.FirstOrDefault(a => a.Index == SelectedActorIndex);
        MarkDirty();
        RegenerateOutput();
        StatusMessage = $"Removed actor (remaining: {Current.Characters.Count})";
    }

    [RelayCommand]
    private async Task ImportActor()
    {
        if (Current.Characters.Count >= Constants.MaxActorCount)
        {
            StatusMessage = $"Maximum actors reached ({Constants.MaxActorCount})";
            return;
        }

        var file = await _fileService.OpenFileAsync(
            "Import Actor",
            new[] { "*.png", "*.json", "*.charx", "*.byaf" });

        if (file == null)
            return;

        try
        {
            StatusMessage = $"Importing {Path.GetFileName(file)}...";

            var (result, card) = await _cardService.LoadAsync(file);

            if (result != CharacterCardService.LoadResult.Success || card == null)
            {
                StatusMessage = "Failed to load character file";
                return;
            }

            // Get the imported character data
            var importedCharacter = new CharacterData
            {
                spokenName = card.Name ?? Constants.DefaultCharacterName,
                gender = card.Gender,
                persona = card.Persona,
                personality = card.Personality,
                scenario = card.Scenario,
                example = card.Example,
                greeting = card.Greeting,
                system = card.System,
            };

            // Replace last empty actor if present
            int lastIndex = Current.Characters.Count - 1;
            var lastCharacter = Current.Characters[lastIndex];
            bool lastIsEmpty = string.IsNullOrEmpty(lastCharacter.spokenName) &&
                               string.IsNullOrEmpty(lastCharacter.persona) &&
                               string.IsNullOrEmpty(lastCharacter.personality) &&
                               lastCharacter.recipes.Count == 0;

            if (lastIsEmpty && lastIndex > 0)
            {
                Current.Characters.RemoveAt(lastIndex);
            }

            // Add the imported character
            Current.Characters.Add(importedCharacter);
            Current.SelectedCharacter = Current.Characters.Count - 1;
            SelectedActorIndex = Current.SelectedCharacter;

            // Add portrait as actor portrait if available
            if (card.PortraitData != null && Current.SelectedCharacter > 0)
            {
                var portraitAsset = new AssetFile
                {
                    name = $"Portrait ({importedCharacter.spokenName})",
                    actorIndex = Current.SelectedCharacter,
                    type = AssetFile.AssetType.Icon,
                    isEmbeddedAsset = true,
                    data = new AssetData { data = card.PortraitData }
                };
                Current.Card.assets.Add(portraitAsset);
            }

            LoadCharacterIntoUI();
            OnPropertyChanged(nameof(CanRemoveActor));
            OnPropertyChanged(nameof(IsMultiCharacter));
            MarkDirty();
            RegenerateOutput();
            StatusMessage = $"Imported actor: {importedCharacter.spokenName} (total: {Current.Characters.Count})";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error importing actor: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ExportActor()
    {
        if (Current.Characters.Count <= 1)
        {
            StatusMessage = "No additional actors to export";
            return;
        }

        var character = Current.Character;
        var actorName = character.spokenName ?? character.name ?? "actor";

        var file = await _fileService.SaveFileAsync(
            "Export Actor",
            actorName,
            new[] { "*.png", "*.json" });

        if (file == null)
            return;

        try
        {
            StatusMessage = $"Exporting {actorName}...";

            // Get actor portrait if available
            byte[]? portraitData = null;
            if (Current.SelectedCharacter > 0)
            {
                // Find portrait for this specific actor
                var portraitAsset = Current.Card.assets.FirstOrDefault(a =>
                    a.actorIndex == Current.SelectedCharacter &&
                    (a.type == AssetFile.AssetType.Icon || a.type == AssetFile.AssetType.Portrait));
                if (portraitAsset != null)
                    portraitData = portraitAsset.data.data;
            }
            else
            {
                portraitData = Current.Card.portraitImage?.data;
            }

            // Create a standalone card from this actor
            var card = new CharacterCard
            {
                Name = character.spokenName ?? character.name,
                Gender = character.gender,
                Persona = character.persona,
                Personality = character.personality,
                Scenario = character.scenario,
                Example = character.example,
                Greeting = character.greeting,
                System = character.system,
                PortraitData = portraitData,
            };

            bool success = await _cardService.SaveAsync(file, card);

            if (success)
            {
                StatusMessage = $"Exported actor: {actorName}";
            }
            else
            {
                StatusMessage = "Failed to export actor";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error exporting actor: {ex.Message}";
        }
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

        // Update portrait for this actor
        UpdateActorPortrait();
    }

    /// <summary>
    /// Updates the portrait display based on the currently selected actor.
    /// Actor 0 uses the main portrait; other actors use their per-actor portrait if available.
    /// </summary>
    private void UpdateActorPortrait()
    {
        int actorIndex = Current.SelectedCharacter;

        if (actorIndex <= 0)
        {
            // Main character: show main portrait
            if (_portraitData != null && _portraitData.Length > 0)
            {
                try
                {
                    using var stream = new MemoryStream(_portraitData);
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
            IsPortraitFallback = false;
        }
        else
        {
            // Secondary actor: look for actor-specific portrait
            var portraitAsset = Current.Card.assets.FirstOrDefault(a =>
                a.actorIndex == actorIndex &&
                (a.type == AssetFile.AssetType.Icon || a.type == AssetFile.AssetType.Portrait));

            if (portraitAsset != null && portraitAsset.data.data != null && portraitAsset.data.data.Length > 0)
            {
                try
                {
                    using var stream = new MemoryStream(portraitAsset.data.data);
                    PortraitImage = new Bitmap(stream);
                    IsPortraitFallback = false;
                }
                catch
                {
                    // Fall back to main portrait
                    ShowFallbackPortrait();
                }
            }
            else
            {
                // No actor-specific portrait, fall back to main portrait (grayed)
                ShowFallbackPortrait();
            }
        }
    }

    private void ShowFallbackPortrait()
    {
        if (_portraitData != null && _portraitData.Length > 0)
        {
            try
            {
                using var stream = new MemoryStream(_portraitData);
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
        IsPortraitFallback = true;
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
        var idx = Recipes.IndexOf(recipe);
        if (idx >= 0)
        {
            var source = recipe.GetSourceRecipe();
            var sourceIdx = source != null ? Current.Character.recipes.IndexOf(source) : -1;

            // Record undo action
            _undoService.RecordAction("Remove recipe",
                () =>
                {
                    // Undo: restore the recipe
                    if (source != null && sourceIdx >= 0)
                        Current.Character.recipes.Insert(Math.Min(sourceIdx, Current.Character.recipes.Count), source);
                    Recipes.Insert(Math.Min(idx, Recipes.Count), recipe);
                    MarkDirty();
                    RegenerateOutput();
                },
                () =>
                {
                    // Redo: remove again
                    if (source != null && Current.Character.recipes.Contains(source))
                        Current.Character.recipes.Remove(source);
                    Recipes.Remove(recipe);
                    MarkDirty();
                    RegenerateOutput();
                });

            if (source != null && Current.Character.recipes.Contains(source))
                Current.Character.recipes.Remove(source);

            Recipes.RemoveAt(idx);
            MarkDirty();
            RegenerateOutput();
        }
    }

    public void MoveRecipeUp(RecipeViewModel recipe)
    {
        var index = Recipes.IndexOf(recipe);
        if (index > 0)
            ReorderRecipe(index, index - 1);
    }

    public void MoveRecipeDown(RecipeViewModel recipe)
    {
        var index = Recipes.IndexOf(recipe);
        if (index < Recipes.Count - 1)
            ReorderRecipe(index, index + 1);
    }

    public void MoveRecipeToTop(RecipeViewModel recipe)
    {
        var index = Recipes.IndexOf(recipe);
        if (index > 0)
            ReorderRecipe(index, 0);
    }

    public void MoveRecipeToBottom(RecipeViewModel recipe)
    {
        var index = Recipes.IndexOf(recipe);
        if (index < Recipes.Count - 1)
            ReorderRecipe(index, Recipes.Count - 1);
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

    public void MakePrimaryGreeting(RecipeViewModel recipe)
    {
        if (recipe.GetSourceRecipe()?.isGreeting != true)
            return;

        int currentIndex = Recipes.IndexOf(recipe);
        if (currentIndex < 0)
            return;

        int firstGreeting = Recipes
            .Select((r, idx) => (r, idx))
            .Where(t => t.r.GetSourceRecipe()?.isGreeting == true)
            .Select(t => t.idx)
            .DefaultIfEmpty(currentIndex)
            .Min();

        if (currentIndex != firstGreeting)
            ReorderRecipe(currentIndex, firstGreeting);
    }

    private void ReorderRecipe(int fromIndex, int toIndex)
    {
        if (fromIndex == toIndex
            || fromIndex < 0 || toIndex < 0
            || fromIndex >= Recipes.Count || toIndex >= Recipes.Count)
            return;

        var vm = Recipes[fromIndex];
        var source = vm.GetSourceRecipe();

        // Record undo action
        _undoService.RecordAction("Move recipe",
            () =>
            {
                // Undo: move back
                Recipes.Move(toIndex, fromIndex);
                if (source != null)
                {
                    var list = Current.Character.recipes;
                    int idx = list.IndexOf(source);
                    if (idx >= 0)
                    {
                        list.RemoveAt(idx);
                        list.Insert(Math.Min(fromIndex, list.Count), source);
                    }
                }
                MarkDirty();
                RegenerateOutput();
            },
            () =>
            {
                // Redo: move again
                Recipes.Move(fromIndex, toIndex);
                if (source != null)
                {
                    var list = Current.Character.recipes;
                    int idx = list.IndexOf(source);
                    if (idx >= 0)
                    {
                        list.RemoveAt(idx);
                        list.Insert(Math.Min(toIndex, list.Count), source);
                    }
                }
                MarkDirty();
                RegenerateOutput();
            });

        Recipes.Move(fromIndex, toIndex);

        if (source != null)
        {
            var list = Current.Character.recipes;
            int srcIdx = list.IndexOf(source);
            if (srcIdx >= 0)
            {
                list.RemoveAt(srcIdx);
                var targetIdx = Math.Min(toIndex, list.Count);
                list.Insert(targetIdx, source);
            }
        }

        MarkDirty();
    }

    public void InsertRecipes(IEnumerable<Recipe> recipes, RecipeViewModel? insertAfter = null)
    {
        if (recipes == null)
            return;

        int insertIndex = insertAfter != null ? Recipes.IndexOf(insertAfter) + 1 : Recipes.Count;

        foreach (var recipe in recipes)
        {
            Current.Character.recipes.Insert(Math.Min(insertIndex, Current.Character.recipes.Count), recipe);
            var vm = new RecipeViewModel(this, recipe);
            Recipes.Insert(Math.Min(insertIndex, Recipes.Count), vm);
            insertIndex++;
        }

        MarkDirty();
        RegenerateOutput();
    }

    public async Task SaveSnippetToFileAsync(Recipe recipe, string bakedText)
    {
        var defaultName = $"{SanitizeFilename(recipe.title ?? recipe.name ?? "snippet")}.txt";
        var filename = await _dialogService.ShowSaveFileDialogAsync("Save Snippet", defaultName,
            new FilePickerFileType("Text") { Patterns = new List<string> { "*.txt" } });

        if (string.IsNullOrEmpty(filename))
            return;

        await System.IO.File.WriteAllTextAsync(filename, bakedText);
        StatusMessage = $"Saved snippet: {System.IO.Path.GetFileName(filename)}";
    }

    public async Task SaveRecipeToFileAsync(Recipe recipe)
    {
        var defaultName = $"{SanitizeFilename(recipe.title ?? recipe.name ?? "recipe")}.recipe.xml";
        var filename = await _dialogService.ShowSaveFileDialogAsync("Save Recipe", defaultName,
            new FilePickerFileType("Recipe XML") { Patterns = new List<string> { "*.recipe.xml" } });

        if (string.IsNullOrEmpty(filename))
            return;

        var xmlDoc = new System.Xml.XmlDocument();
        var root = xmlDoc.CreateElement("Recipe");
        xmlDoc.AppendChild(root);
        recipe.SaveToXml(root);

        using (var writer = new System.Xml.XmlTextWriter(filename, System.Text.Encoding.UTF8))
        {
            writer.Formatting = System.Xml.Formatting.Indented;
            xmlDoc.Save(writer);
        }

        StatusMessage = $"Saved recipe: {System.IO.Path.GetFileName(filename)}";
    }

    private static string SanitizeFilename(string name)
    {
        var invalid = System.IO.Path.GetInvalidFileNameChars();
        var parts = name.Split(invalid, StringSplitOptions.RemoveEmptyEntries);
        var safe = string.Join("_", parts).Trim('_');
        return string.IsNullOrWhiteSpace(safe) ? "file" : safe;
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

            // Sync UI state to Current model before generating
            SyncToCurrent();

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

        // Ensure data is synced
        SyncToCurrent();

        try
        {
            StatusMessage = "Creating character in Backyard...";

            // Generate output for Backyard
            var output = Generator.Generate(Generator.Option.Export | Generator.Option.Faraday);
            var backyardCard = Integration.BackyardLinkCard.FromOutput(output);
            backyardCard.EnsureSystemPrompt(false);

            // Gather images for the character
            var imageInputs = new List<Integration.Backyard.ImageInput>();
            if (Current.Card.portraitImage != null)
            {
                imageInputs.Add(new Integration.Backyard.ImageInput()
                {
                    image = Current.Card.portraitImage,
                    fileExt = "png",
                });
            }

            // Create character in Backyard
            var args = new Integration.Backyard.CreateCharacterArguments
            {
                card = backyardCard,
                imageInput = imageInputs.ToArray(),
            };

            Integration.Backyard.CharacterInstance newCharacter;
            Integration.Backyard.Link.Image[] imageLinks;
            var createError = await Task.Run(() =>
            {
                return Integration.Backyard.Database.CreateNewCharacter(args, out newCharacter, out imageLinks);
            });

            if (createError != Integration.Backyard.Error.NoError)
            {
                StatusMessage = $"Failed to create character: {createError}";
                return;
            }

            // Refresh character list
            Integration.Backyard.RefreshCharacters();

            StatusMessage = $"Character '{CharacterName}' created in Backyard AI";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error creating character: {ex.Message}";
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
    private async Task PurgeUnusedImages()
    {
        if (!Integration.Backyard.IsConnected)
        {
            StatusMessage = "Not connected to Backyard AI";
            return;
        }

        var backyardLocation = AppSettings.BackyardLink.Location;
        if (string.IsNullOrEmpty(backyardLocation))
        {
            StatusMessage = "Backyard AI location not configured";
            return;
        }

        var imagesFolder = Path.Combine(backyardLocation, "images");
        if (!Directory.Exists(imagesFolder))
        {
            StatusMessage = "Images folder not found";
            return;
        }

        // Get all image URLs referenced in database
        var error = Integration.Backyard.Database.GetAllImageUrls(out var imageUrls);
        if (error != Integration.Backyard.Error.NoError)
        {
            StatusMessage = $"Failed to get image URLs: {error}";
            return;
        }

        if (imageUrls == null || imageUrls.Length == 0)
        {
            StatusMessage = "No images found in database";
            return;
        }

        // Get referenced image filenames
        var referencedImages = new HashSet<string>(
            imageUrls
                .Select(fn => Path.GetFileName(fn)?.ToLowerInvariant())
                .Where(fn => !string.IsNullOrEmpty(fn))!,
            StringComparer.OrdinalIgnoreCase);

        // Get all image files in folder
        var imageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { ".png", ".jpg", ".jpeg", ".gif", ".webp", ".bmp" };
        var allImageFiles = Directory.GetFiles(imagesFolder)
            .Where(f => imageExtensions.Contains(Path.GetExtension(f)))
            .ToList();

        // Find unreferenced images
        var unreferencedImages = allImageFiles
            .Where(f => !referencedImages.Contains(Path.GetFileName(f).ToLowerInvariant()))
            .ToList();

        if (unreferencedImages.Count == 0)
        {
            StatusMessage = "No unused images found";
            return;
        }

        var confirm = await _dialogService.ShowConfirmationDialogAsync(
            "Purge Unused Images",
            $"Found {unreferencedImages.Count} unused image(s).\n\nDo you want to move them to trash?");

        if (!confirm)
            return;

        int deleted = 0;
        foreach (var imagePath in unreferencedImages)
        {
            try
            {
                File.Delete(imagePath);
                deleted++;
            }
            catch
            {
                // Skip files that can't be deleted
            }
        }

        StatusMessage = $"Purged {deleted} unused image(s)";
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

    [RelayCommand]
    private void ToggleSpellCheck()
    {
        SpellCheckEnabled = !SpellCheckEnabled;
        AppSettings.Settings.SpellChecking = SpellCheckEnabled;
        AppSettings.Save();
        StatusMessage = SpellCheckEnabled ? "Spell checking enabled" : "Spell checking disabled";
    }

    [RelayCommand]
    private void SelectDictionary(DictionaryItem? item)
    {
        if (item == null)
            return;

        SelectedDictionary = item;
        AppSettings.Settings.Dictionary = item.Locale;
        AppSettings.Save();
        StatusMessage = $"Spell check language set to {item.DisplayName}";
    }

    [RelayCommand]
    private void SelectDictionaryByLocale(string locale)
    {
        if (string.IsNullOrEmpty(locale))
            return;

        var item = AvailableDictionaries.FirstOrDefault(d => d.Locale == locale);
        if (item != null)
        {
            SelectDictionary(item);
        }
        else
        {
            // If not found in available list, just set the locale directly
            AppSettings.Settings.Dictionary = locale;
            AppSettings.Save();
            StatusMessage = $"Spell check language set to {locale}";
        }
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

        // Check if feature is supported
        if (!Integration.BackyardValidation.CheckFeature(Integration.BackyardValidation.Feature.GroupChat))
        {
            StatusMessage = "Group chat feature not supported in this version of Backyard";
            return;
        }

        SyncToCurrent();

        try
        {
            var options = Generator.Option.Export | Generator.Option.Faraday | Generator.Option.Linked | Generator.Option.Group;
            var outputs = Generator.GenerateMany(options);

            // User persona
            UserData userInfo = null;
            if (AppSettings.BackyardLink.WriteUserPersona)
            {
                string userPersona = outputs[0].userPersona.ToFaraday();
                if (!string.IsNullOrEmpty(userPersona))
                {
                    userInfo = new UserData()
                    {
                        name = Current.Card.userPlaceholder,
                        persona = userPersona,
                    };
                    outputs[0].userPersona = GingerString.Empty;
                }
            }

            var cards = outputs.Select(o => Integration.BackyardLinkCard.FromOutput(o)).ToArray();
            if (cards == null || cards.Length == 0)
            {
                StatusMessage = "Failed to generate party cards";
                return;
            }

            // Set character names
            for (int i = 0; i < cards.Length && i < Current.Characters.Count; ++i)
                cards[i].data.name = Current.Characters[i].name;
            cards[0].data.isNSFW = cards.Any(c => c.data.isNSFW);
            if (string.IsNullOrEmpty(Current.Card.name))
                cards[0].data.displayName = string.Join(" and ", cards.Select(c => c.data.name));
            cards[0].EnsureSystemPrompt(true);

            var imageInput = Integration.BackyardUtil.GatherImages();

            var args = new Integration.Backyard.CreatePartyArguments()
            {
                cards = cards,
                imageInput = imageInput,
                userInfo = userInfo,
            };

            Integration.Backyard.GroupInstance createdGroup;
            Integration.Backyard.CharacterInstance[] createdCharacters;
            Integration.Backyard.Link.Image[] imageLinks;

            var error = await Task.Run(() =>
            {
                return Integration.Backyard.Database.CreateNewParty(args, out createdGroup, out createdCharacters, out imageLinks);
            });

            if (error != Integration.Backyard.Error.NoError)
            {
                StatusMessage = $"Failed to create party: {error}";
                return;
            }

            Current.IsFileDirty = true;

            // Refresh character list
            Integration.Backyard.RefreshCharacters();

            StatusMessage = $"Party created in Backyard with {cards.Length} characters";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Create party failed: {ex.Message}";
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

        // Save as defaults
        AppSettings.Settings.DefaultTemperature = dialog.Temperature;
        AppSettings.Settings.DefaultMinP = dialog.MinP;
        AppSettings.Settings.DefaultTopP = dialog.TopP;
        AppSettings.Settings.DefaultTopK = dialog.TopK;
        AppSettings.Settings.DefaultRepeatPenalty = dialog.RepeatPenalty;
        AppSettings.Settings.DefaultRepeatLastN = dialog.RepeatLastN;
        AppSettings.Save();

        // Create chat parameters from dialog values
        var chatParameters = new Integration.Backyard.ChatParameters()
        {
            temperature = dialog.Temperature,
            minP = dialog.MinP,
            topP = dialog.TopP,
            topK = dialog.TopK,
            repeatPenalty = dialog.RepeatPenalty,
            repeatLastN = dialog.RepeatLastN,
        };

        // Get all groups and update their chats
        var groups = Integration.Backyard.Groups.ToList();
        int totalGroups = groups.Count;
        int succeeded = 0;
        int failed = 0;

        await _dialogService.RunWithProgressAsync(
            "Updating Model Settings",
            "Updating all character chats...",
            async (cancellationToken, progress) =>
            {
                for (int i = 0; i < groups.Count; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    var group = groups[i];
                    progress.Report(($"Processing {group.GetDisplayName()}...", (double)i / totalGroups));

                    // Get chats for this group
                    Integration.Backyard.ChatInstance[] chats = null;
                    var error = await Task.Run(() =>
                    {
                        Integration.Backyard.ChatInstance[] outChats;
                        var result = Integration.Backyard.Database.GetChats(group.instanceId, out outChats);
                        chats = outChats;
                        return result;
                    });

                    if (error == Integration.Backyard.Error.NoError && chats != null && chats.Length > 0)
                    {
                        // Update chat parameters for all chats in this group
                        var chatIds = chats.Select(c => c.instanceId).ToArray();
                        var updateError = await Task.Run(() =>
                            Integration.Backyard.Database.UpdateChatParameters(chatIds, null, chatParameters));

                        if (updateError == Integration.Backyard.Error.NoError)
                            succeeded++;
                        else
                            failed++;
                    }
                    else
                    {
                        failed++;
                    }
                }
            },
            true);

        StatusMessage = $"Model settings updated for {succeeded} of {totalGroups} characters" +
            (failed > 0 ? $" ({failed} failed)" : "");
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

        // Refresh character list
        if (Integration.Backyard.RefreshCharacters() != Integration.Backyard.Error.NoError)
        {
            StatusMessage = "Failed to refresh character list";
            return;
        }

        // Show browser to select characters to delete (multi-select mode)
        var (success, groups, characters) = await _dialogService.ShowBackyardBrowserMultiSelectAsync("Select characters to delete");
        if (!success || characters.Count == 0)
            return;

        // Get all character IDs
        var characterIds = characters.Select(c => c.instanceId).Distinct().ToArray();

        // Get affected IDs from database
        var error = Integration.Backyard.Database.ConfirmDeleteCharacters(characterIds, out var result);
        if (error != Integration.Backyard.Error.NoError)
        {
            StatusMessage = $"Failed to prepare deletion: {error}";
            return;
        }

        // Confirm deletion
        string message = result.characterIds.Length == result.groupIds.Length
            ? $"Are you sure you want to delete {result.characterIds.Length} character(s) from Backyard AI?\n\nThis action cannot be undone."
            : $"Are you sure you want to delete {result.characterIds.Length} character(s) and their {result.groupIds.Length} associated chat(s) from Backyard AI?\n\nThis action cannot be undone.";

        var confirm = await _dialogService.ShowConfirmationDialogAsync("Delete Characters", message);
        if (!confirm)
            return;

        // Perform deletion
        error = Integration.Backyard.Database.DeleteCharacters(result.characterIds, result.groupIds, result.imageIds);
        if (error != Integration.Backyard.Error.NoError)
        {
            StatusMessage = $"Deletion failed: {error}";
            return;
        }

        // Clean up orphaned users
        Integration.Backyard.Database.DeleteOrphanedUsers(out _);

        // Refresh character list
        Integration.Backyard.RefreshCharacters();

        StatusMessage = $"Deleted {result.characterIds.Length} character(s)";
    }

    [RelayCommand]
    private async Task RepairBrokenImages()
    {
        if (!Integration.Backyard.ConnectionEstablished)
        {
            StatusMessage = "Not connected to Backyard AI";
            return;
        }

        var confirm = await _dialogService.ShowConfirmationDialogAsync(
            "Repair Broken Images",
            "This will scan the database for broken image references and attempt to repair them.\n\nContinue?");

        if (!confirm)
            return;

        StatusMessage = "Repairing broken images...";

        var error = Integration.Backyard.Database.RepairImages(out int modified, out int skipped);

        if (error == Integration.Backyard.Error.NotFound)
        {
            StatusMessage = "Images folder not found";
            return;
        }
        if (error != Integration.Backyard.Error.NoError)
        {
            StatusMessage = $"Repair failed: {error}";
            return;
        }

        if (skipped > 0)
            StatusMessage = $"Repaired {modified} image(s), {skipped} skipped (files not found)";
        else if (modified > 0)
            StatusMessage = $"Repaired {modified} image(s)";
        else
            StatusMessage = "No broken images found";
    }

    [RelayCommand]
    private async Task RepairLegacyChats()
    {
        if (!Integration.Backyard.ConnectionEstablished)
        {
            StatusMessage = "Not connected to Backyard AI";
            return;
        }

        // Refresh character list
        if (Integration.Backyard.RefreshCharacters() != Integration.Backyard.Error.NoError)
        {
            StatusMessage = "Failed to refresh character list";
            return;
        }

        var groups = Integration.Backyard.Groups.ToArray();
        if (groups.Length == 0)
        {
            StatusMessage = "No characters found to repair";
            return;
        }

        var confirm = await _dialogService.ShowConfirmationDialogAsync(
            "Repair Legacy Chats",
            $"This will repair legacy chat formats for all {groups.Length} character(s).\n\nContinue?");

        if (!confirm)
            return;

        StatusMessage = "Repairing legacy chats...";

        int totalModified = 0;
        int charactersModified = 0;

        foreach (var group in groups)
        {
            var error = Integration.Backyard.Database.RepairChats(group.instanceId, out int modified);
            if (error == Integration.Backyard.Error.NoError && modified > 0)
            {
                totalModified += modified;
                charactersModified++;
            }
        }

        if (totalModified > 0)
            StatusMessage = $"Repaired {totalModified} chat(s) across {charactersModified} character(s)";
        else
            StatusMessage = "No legacy chats needed repair";
    }

    [RelayCommand]
    private async Task ResetModelsLocation()
    {
        if (!Integration.Backyard.ConnectionEstablished)
        {
            StatusMessage = "Not connected to Backyard AI";
            return;
        }

        var confirm = await _dialogService.ShowConfirmationDialogAsync(
            "Reset Model Download Location",
            "This will reset the model download location setting in Backyard AI to the default.\n\nContinue?");

        if (!confirm)
            return;

        var error = Integration.Backyard.Database.ResetModelDownloadLocation();
        if (error != Integration.Backyard.Error.NoError)
        {
            StatusMessage = $"Reset failed: {error}";
            return;
        }

        StatusMessage = "Model download location has been reset to default";
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

    public string Id { get; set; } = Guid.NewGuid().ToString();

    [ObservableProperty]
    private int _tokenCount;

    [ObservableProperty]
    private string _keys = "";

    [ObservableProperty]
    private string _secondaryKeys = "";

    [ObservableProperty]
    private string _content = "";

    [ObservableProperty]
    private bool _isEnabled = true;

    [ObservableProperty]
    private bool _isExpanded = false;

    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private string _comment = "";

    [ObservableProperty]
    private bool _constant = false;

    [ObservableProperty]
    private bool _selective = false;

    [ObservableProperty]
    private bool _caseSensitive = false;

    [ObservableProperty]
    private int _insertionOrder = 100;

    [ObservableProperty]
    private int _priority = 10;

    [ObservableProperty]
    private string _position = "before_char";

    [ObservableProperty]
    private int _probability = 100;

    [ObservableProperty]
    private bool _useProbability = false;

    [ObservableProperty]
    private int _depth = 4;

    [ObservableProperty]
    private string _group = "";

    [ObservableProperty]
    private bool _excludeRecursion = false;

    [ObservableProperty]
    private bool _useRegex = false;

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
    partial void OnSecondaryKeysChanged(string value) => _parent?.OnLorebookChanged();
    partial void OnConstantChanged(bool value) => _parent?.OnLorebookChanged();
    partial void OnSelectiveChanged(bool value) => _parent?.OnLorebookChanged();
    partial void OnCaseSensitiveChanged(bool value) => _parent?.OnLorebookChanged();
    partial void OnInsertionOrderChanged(int value) => _parent?.OnLorebookChanged();
    partial void OnPriorityChanged(int value) => _parent?.OnLorebookChanged();
    partial void OnPositionChanged(string value) => _parent?.OnLorebookChanged();
    partial void OnProbabilityChanged(int value) => _parent?.OnLorebookChanged();
    partial void OnUseProbabilityChanged(bool value) => _parent?.OnLorebookChanged();
    partial void OnDepthChanged(int value) => _parent?.OnLorebookChanged();
    partial void OnGroupChanged(string value) => _parent?.OnLorebookChanged();
    partial void OnExcludeRecursionChanged(bool value) => _parent?.OnLorebookChanged();
    partial void OnUseRegexChanged(bool value) => _parent?.OnLorebookChanged();

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

    [RelayCommand]
    private async Task CopyEntryAsync()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        var topLevel = desktop.MainWindow;
        if (topLevel?.Clipboard == null)
            return;

        var entry = new
        {
            type = "ginger_lore_entry",
            keys = Keys,
            secondaryKeys = SecondaryKeys,
            content = Content,
            name = Name,
            comment = Comment,
            isEnabled = IsEnabled,
            constant = Constant,
            selective = Selective,
            caseSensitive = CaseSensitive,
            insertionOrder = InsertionOrder,
            priority = Priority,
            position = Position,
            probability = Probability,
            useProbability = UseProbability,
            depth = Depth,
            group = Group,
            excludeRecursion = ExcludeRecursion,
            useRegex = UseRegex
        };

        var json = System.Text.Json.JsonSerializer.Serialize(entry);
        await topLevel.Clipboard.SetTextAsync(json);
        _parent?.SetStatusMessage("Lorebook entry copied to clipboard");
    }

    [RelayCommand]
    private async Task PasteEntryAsync()
    {
        _parent?.PasteLorebookEntryAfter(this);
    }

    [RelayCommand]
    private void Duplicate()
    {
        _parent?.DuplicateLorebookEntry(this);
    }
}

public class RecipeLibraryItem
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
}

public class DictionaryItem
{
    public string Locale { get; set; } = "";
    public string DisplayName { get; set; } = "";

    public override string ToString() => DisplayName;
}

public class RecentFileItem
{
    public string Filename { get; set; } = "";
    public string CharacterName { get; set; } = "";
    public string DisplayName => string.IsNullOrEmpty(CharacterName)
        ? System.IO.Path.GetFileName(Filename)
        : $"{CharacterName} ({System.IO.Path.GetFileName(Filename)})";

    public override string ToString() => DisplayName;
}

public class ActorItem
{
    public int Index { get; set; }
    public string Name { get; set; } = "";
    public string DisplayName => Index == 0 ? Name : $"{Name} (Actor {Index + 1})";

    public override string ToString() => DisplayName;
}
