using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Ginger.ViewModels;

public partial class RecipeViewModel : ObservableObject
{
    private readonly MainViewModel _parent;
    private Recipe? _sourceRecipe;
    private bool _isRegenerating;

    private static readonly string[] _detailLevelOptions = new[]
    {
        EnumHelper.ToString(Recipe.DetailLevel.Default),
        EnumHelper.ToString(Recipe.DetailLevel.Less),
        EnumHelper.ToString(Recipe.DetailLevel.Normal),
        EnumHelper.ToString(Recipe.DetailLevel.More),
    };

    [ObservableProperty]
    private string _id = "";

    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private string _title = "";

    [ObservableProperty]
    private string _description = "";

    [ObservableProperty]
    private string _category = "";

    [ObservableProperty]
    private string _content = "";

    [ObservableProperty]
    private bool _isExpanded = true;

    [ObservableProperty]
    private bool _isEnabled = true;

    [ObservableProperty]
    private bool _isNsfw;

    [ObservableProperty]
    private bool _isTextFormattingEnabled = true;

    [ObservableProperty]
    private string _selectedDetailLevel = EnumHelper.ToString(Recipe.DetailLevel.Default);

    public IReadOnlyList<string> DetailLevelOptions => _detailLevelOptions;
    public bool IsGreeting => _sourceRecipe?.isGreeting == true;
    public bool CanToggleFormatting => _sourceRecipe?.canToggleTextFormatting == true;

    public ObservableCollection<RecipeParameterViewModel> Parameters { get; } = new();

    public RecipeViewModel(MainViewModel parent)
    {
        _parent = parent;
    }

    public RecipeViewModel(MainViewModel parent, Recipe recipe) : this(parent)
    {
        _sourceRecipe = recipe;
        _id = recipe.id.ToString();
        _name = recipe.name ?? "";
        _title = recipe.title ?? "";
        _description = recipe.description ?? "";
        _category = recipe.categoryTag ?? EnumHelper.ToString(recipe.category);
        _isEnabled = recipe.isEnabled;
        _isExpanded = !recipe.isCollapsed;
        _isNsfw = recipe.isNSFW;
        _isTextFormattingEnabled = recipe.enableTextFormatting;
        _selectedDetailLevel = EnumHelper.ToString(recipe.levelOfDetail);

        // Build initial content from templates (as fallback)
        foreach (var template in recipe.templates)
        {
            if (!string.IsNullOrEmpty(template.text))
            {
                _content += template.text + "\n\n";
            }
        }
        _content = _content.TrimEnd();

        // Load parameters
        foreach (var param in recipe.parameters)
        {
            Parameters.Add(new RecipeParameterViewModel(this, param));
        }

        // Generate content using the recipe engine to properly evaluate templates
        // This replaces the raw template text with actual generated output
        RegenerateContent();
    }

    partial void OnContentChanged(string value)
    {
        // Skip notification when we're regenerating content programmatically
        if (!_isRegenerating)
            _parent.OnRecipeChanged();
    }

    partial void OnIsEnabledChanged(bool value)
    {
        _parent.OnRecipeChanged();
    }

    partial void OnIsNsfwChanged(bool value)
    {
        if (_sourceRecipe == null)
            return;

        if (value)
            _sourceRecipe.flags.Add(Constants.Flag.NSFW);
        else
            _sourceRecipe.flags.Remove(Constants.Flag.NSFW);

        _sourceRecipe.enableNSFWContent = value;
        _parent.OnRecipeChanged();
    }

    partial void OnIsTextFormattingEnabledChanged(bool value)
    {
        if (_sourceRecipe == null || !_sourceRecipe.canToggleTextFormatting)
            return;

        _sourceRecipe.EnableTextFormatting(value);
        _parent.OnRecipeChanged();
    }

    partial void OnSelectedDetailLevelChanged(string value)
    {
        if (_sourceRecipe == null)
            return;

        var level = EnumHelper.FromString(value, Recipe.DetailLevel.Default);
        _sourceRecipe.levelOfDetail = level;
        _parent.OnRecipeChanged();
    }

    [RelayCommand]
    private void MoveUp()
    {
        _parent.MoveRecipeUp(this);
    }

    [RelayCommand]
    private void MoveDown()
    {
        _parent.MoveRecipeDown(this);
    }

    [RelayCommand]
    private void MoveToTop()
    {
        _parent.MoveRecipeToTop(this);
    }

    [RelayCommand]
    private void MoveToBottom()
    {
        _parent.MoveRecipeToBottom(this);
    }

    [RelayCommand]
    private void Remove()
    {
        _parent.RemoveRecipe(this);
    }

    [RelayCommand]
    private void ToggleNsfw()
    {
        IsNsfw = !IsNsfw;
    }

    [RelayCommand]
    private void ToggleFormatting()
    {
        IsTextFormattingEnabled = !IsTextFormattingEnabled;
    }

    [RelayCommand]
    private void SetDetailLevel(string level)
    {
        SelectedDetailLevel = level;
    }

    [RelayCommand]
    private void ToggleExpanded()
    {
        IsExpanded = !IsExpanded;
    }

    [RelayCommand]
    private async Task CopyAsync()
    {
        if (_sourceRecipe == null) return;

        var clipboard = RecipeClipboard.FromRecipes(new[] { _sourceRecipe });
        var json = JsonSerializer.Serialize(clipboard);

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var topLevel = desktop.MainWindow;
            if (topLevel?.Clipboard != null)
            {
                await topLevel.Clipboard.SetTextAsync(json);
                _parent.SetStatusMessage("Recipe copied to clipboard");
            }
        }
    }

    [RelayCommand]
    private async Task PasteAsync()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        var topLevel = desktop.MainWindow;
        if (topLevel?.Clipboard == null)
            return;

        var text = await topLevel.Clipboard.GetTextAsync();
        if (string.IsNullOrWhiteSpace(text))
        {
            _parent.SetStatusMessage("Clipboard is empty");
            return;
        }

        try
        {
            var clipboard = System.Text.Json.JsonSerializer.Deserialize<RecipeClipboard>(text);
            var recipes = clipboard?.ToRecipes();
            if (recipes == null || recipes.Count == 0)
            {
                _parent.SetStatusMessage("No recipes found in clipboard");
                return;
            }

            _parent.InsertRecipes(recipes, this);
            _parent.SetStatusMessage($"Pasted {recipes.Count} recipe(s)");
        }
        catch
        {
            _parent.SetStatusMessage("Failed to paste recipes from clipboard");
        }
    }

    [RelayCommand]
    private async Task SaveAsSnippetAsync()
    {
        if (_sourceRecipe == null)
            return;

        var bakedText = RenderBakedText();
        if (string.IsNullOrWhiteSpace(bakedText))
        {
            _parent.SetStatusMessage("Nothing to save as snippet");
            return;
        }

        await _parent.SaveSnippetToFileAsync(_sourceRecipe, bakedText);
    }

    [RelayCommand]
    private async Task SaveAsRecipeAsync()
    {
        if (_sourceRecipe == null)
            return;

        await _parent.SaveRecipeToFileAsync(_sourceRecipe);
    }

    [RelayCommand]
    private void BakeRecipe()
    {
        Bake();
        _parent.OnRecipeChanged();
    }

    [RelayCommand]
    private void MakePrimaryGreeting()
    {
        if (_sourceRecipe == null || !IsGreeting)
            return;

        _parent.MakePrimaryGreeting(this);
    }

    private string RenderBakedText()
    {
        if (_sourceRecipe == null)
            return string.Empty;

        var output = Generator.Generate(_sourceRecipe, Generator.Option.Bake);

        var sb = new System.Text.StringBuilder();
        if (!output.system.IsNullOrEmpty())
            sb.AppendLine(output.system.ToString());
        if (!output.system_post_history.IsNullOrEmpty())
            sb.AppendLine(output.system_post_history.ToString());
        if (!output.persona.IsNullOrEmpty())
            sb.AppendLine(output.persona.ToString());
        if (!output.personality.IsNullOrEmpty())
            sb.AppendLine(output.personality.ToString());
        if (!output.scenario.IsNullOrEmpty())
            sb.AppendLine(output.scenario.ToString());
        if (!output.greeting.IsNullOrEmpty())
            sb.AppendLine(output.greeting.ToString());
        if (!output.example.IsNullOrEmpty())
            sb.AppendLine(output.example.ToString());
        if (!output.grammar.IsNullOrEmpty())
            sb.AppendLine(output.grammar.ToString());
        if (!output.userPersona.IsNullOrEmpty())
            sb.AppendLine(output.userPersona.ToString());

        return sb.ToString().Trim();
    }

    public Recipe? GetSourceRecipe() => _sourceRecipe;

    public void NotifyParameterChanged()
    {
        // Regenerate content when parameters change
        RegenerateContent();
        _parent.OnRecipeChanged();
    }

    /// <summary>
    /// Regenerate the recipe content from the source recipe and its current parameters.
    /// This uses the Generator to properly evaluate templates with parameter values.
    /// </summary>
    public void RegenerateContent()
    {
        if (_sourceRecipe == null || _isRegenerating)
            return;

        _isRegenerating = true;
        try
        {
            // Use Generator to produce output with current parameters
            var output = Generator.Generate(_sourceRecipe, Generator.Option.Preview);

            // Combine all output components into content
            var sb = new System.Text.StringBuilder();

            if (!output.system.IsNullOrEmpty())
            {
                sb.AppendLine("=== SYSTEM ===");
                sb.AppendLine(output.system.ToString());
                sb.AppendLine();
            }

            if (!output.persona.IsNullOrEmpty())
            {
                sb.AppendLine("=== PERSONA ===");
                sb.AppendLine(output.persona.ToString());
                sb.AppendLine();
            }

            if (!output.scenario.IsNullOrEmpty())
            {
                sb.AppendLine("=== SCENARIO ===");
                sb.AppendLine(output.scenario.ToString());
                sb.AppendLine();
            }

            if (!output.greeting.IsNullOrEmpty())
            {
                sb.AppendLine("=== GREETING ===");
                sb.AppendLine(output.greeting.ToString());
                sb.AppendLine();
            }

            if (!output.example.IsNullOrEmpty())
            {
                sb.AppendLine("=== EXAMPLE ===");
                sb.AppendLine(output.example.ToString());
                sb.AppendLine();
            }

            // Only update if we got meaningful output
            var newContent = sb.ToString().Trim();
            if (!string.IsNullOrEmpty(newContent))
            {
                Content = newContent;
            }
        }
        catch
        {
            // If generation fails, keep existing content
        }
        finally
        {
            _isRegenerating = false;
        }
    }

    /// <summary>
    /// Bake the recipe - flatten parameters into the content text.
    /// </summary>
    public void Bake()
    {
        if (_sourceRecipe == null) return;

        // Generate the baked output using the recipe's generator
        var output = Generator.Generate(_sourceRecipe, Generator.Option.Bake);

        if (!output.isEmpty)
        {
            // Combine all output components into content
            var sb = new System.Text.StringBuilder();
            if (!output.system.IsNullOrEmpty())
                sb.AppendLine(output.system.ToString());
            if (!output.persona.IsNullOrEmpty())
                sb.AppendLine(output.persona.ToString());
            if (!output.scenario.IsNullOrEmpty())
                sb.AppendLine(output.scenario.ToString());
            if (!output.greeting.IsNullOrEmpty())
                sb.AppendLine(output.greeting.ToString());
            if (!output.example.IsNullOrEmpty())
                sb.AppendLine(output.example.ToString());

            // Replace content with baked output
            Content = sb.ToString().Trim();

            // Clear parameters since they're now baked
            Parameters.Clear();
        }
    }
}

public partial class RecipeParameterViewModel : ObservableObject
{
    private readonly RecipeViewModel _parent;
    private readonly IParameter _parameter;

    public string Id => _parameter.id.ToString();
    public string Label => _parameter.label ?? "";
    public string Description => _parameter.description ?? "";
    public bool IsOptional => _parameter.isOptional;

    // Parameter type detection
    public bool IsTextParameter => _parameter is TextParameter;
    public bool IsBoolParameter => _parameter is BooleanParameter;
    public bool IsNumberParameter => _parameter is NumberParameter;
    public bool IsRangeParameter => _parameter is RangeParameter;
    public bool IsMeasurementParameter => _parameter is MeasurementParameter;
    public bool IsChoiceParameter => _parameter is ChoiceParameter;
    public bool IsMultiChoiceParameter => _parameter is MultiChoiceParameter;
    public bool IsListParameter => _parameter is ListParameter;
    public bool IsHiddenParameter => _parameter is SetVarParameter or SetFlagParameter or EraseParameter or HintParameter;

    // Text mode for multiline
    public bool IsMultilineText => _parameter is TextParameter tp &&
        (tp.mode == TextParameter.Mode.Brief || tp.mode == TextParameter.Mode.Flexible ||
         tp.mode == TextParameter.Mode.Code || tp.mode == TextParameter.Mode.Chat);

    // Choice options
    public List<string> ChoiceOptions
    {
        get
        {
            if (_parameter is ChoiceParameter cp)
                return cp.items.Select(i => i.label).ToList();
            if (_parameter is MultiChoiceParameter mcp)
                return mcp.items.Select(i => i.label).ToList();
            return new List<string>();
        }
    }

    // Number bounds
    public decimal MinValue => _parameter is NumberParameter np ? np.minValue : (_parameter is RangeParameter rp ? rp.minValue : 0);
    public decimal MaxValue => _parameter is NumberParameter np ? np.maxValue : (_parameter is RangeParameter rp ? rp.maxValue : 100);
    public decimal StepValue => _parameter is NumberParameter np && np.mode == NumberParameter.Mode.Integer ? 1 :
                                (_parameter is RangeParameter rp && rp.mode == RangeParameter.Mode.Integer ? 1 : 0.1m);

    // Range slider suffix (e.g., "%" for percent mode)
    public string RangeSuffix => _parameter is RangeParameter rp ?
        (rp.mode == RangeParameter.Mode.Percent ? "%" : rp.suffix ?? "") : "";

    // Measurement properties
    public string MeasurementUnit => _parameter is MeasurementParameter mp ? mp.unit ?? "" : "";
    public decimal MeasurementMagnitude => _parameter is MeasurementParameter mp ? mp.magnitude : 0;
    public string MeasurementMode => _parameter is MeasurementParameter mp ? EnumHelper.ToString(mp.mode) : "";

    // Multi-choice items collection (for checkbox list)
    public ObservableCollection<MultiChoiceItemViewModel> MultiChoiceItems { get; } = new();

    [ObservableProperty]
    private string _value = "";

    [ObservableProperty]
    private bool _isEnabled = true;

    [ObservableProperty]
    private bool _boolValue;

    [ObservableProperty]
    private decimal _numericValue;

    [ObservableProperty]
    private double _sliderValue;

    [ObservableProperty]
    private string? _selectedOption;

    [ObservableProperty]
    private string _listValue = "";

    public RecipeParameterViewModel(RecipeViewModel parent, IParameter parameter)
    {
        _parent = parent;
        _parameter = parameter;
        _isEnabled = parameter.isEnabled;

        // Initialize value from parameter
        if (parameter is BaseParameter<string> stringParam)
            _value = stringParam.value ?? stringParam.defaultValue ?? "";
        else if (parameter is BaseParameter<bool> boolParam)
        {
            _boolValue = boolParam.value;
            _value = _boolValue.ToString().ToLower();
        }
        else if (parameter is BaseParameter<decimal> numParam)
        {
            _numericValue = numParam.value;
            _sliderValue = (double)numParam.value;
            _value = _numericValue.ToString();
        }
        else if (parameter is BaseParameter<HashSet<string>> setParam)
        {
            // ListParameter or MultiChoiceParameter
            _listValue = string.Join(", ", setParam.value ?? new HashSet<string>());
            _value = _listValue;
        }
        else
            _value = parameter.defaultValue ?? "";

        // Initialize choice selection
        if (parameter is ChoiceParameter cp && cp.selectedIndex >= 0 && cp.selectedIndex < cp.items.Count)
            _selectedOption = cp.items[cp.selectedIndex].label;
        else
            _selectedOption = _value;

        // Initialize multi-choice items
        if (parameter is MultiChoiceParameter mcp)
        {
            foreach (var item in mcp.items)
            {
                var itemVm = new MultiChoiceItemViewModel(this, item.id.ToString(), item.label);
                itemVm.IsChecked = mcp.value?.Contains(item.id.ToString()) ?? false;
                MultiChoiceItems.Add(itemVm);
            }
        }
    }

    partial void OnValueChanged(string value)
    {
        // Update the underlying parameter
        if (_parameter is TextParameter tp)
            tp.value = value;
        else if (_parameter is BaseParameter<string> sp)
            sp.Set(value);
        _parent.NotifyParameterChanged();
    }

    partial void OnBoolValueChanged(bool value)
    {
        if (_parameter is BooleanParameter bp)
            bp.value = value;
        Value = value.ToString().ToLower();
    }

    partial void OnNumericValueChanged(decimal value)
    {
        if (_parameter is NumberParameter np)
            np.value = value;
        else if (_parameter is RangeParameter rp)
            rp.value = value;
        else if (_parameter is MeasurementParameter mp)
            mp.magnitude = value;
        Value = value.ToString();
    }

    partial void OnSelectedOptionChanged(string? value)
    {
        if (value == null) return;

        if (_parameter is ChoiceParameter cp)
        {
            var idx = cp.items.FindIndex(i => i.label == value);
            if (idx >= 0)
            {
                cp.selectedIndex = idx;
                cp.value = cp.items[idx].id.ToString();
                Value = cp.value;
            }
        }
    }

    partial void OnIsEnabledChanged(bool value)
    {
        if (_parameter is BaseParameter<string> sp)
            sp.isEnabled = value;
        else if (_parameter is BaseParameter<bool> bp)
            bp.isEnabled = value;
        else if (_parameter is BaseParameter<decimal> np)
            np.isEnabled = value;
        _parent.NotifyParameterChanged();
    }

    partial void OnSliderValueChanged(double value)
    {
        if (_parameter is RangeParameter rp)
        {
            rp.value = (decimal)value;
            NumericValue = (decimal)value;
        }
    }

    partial void OnListValueChanged(string value)
    {
        if (_parameter is ListParameter lp)
        {
            var items = value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToHashSet();
            lp.value = items;
            Value = value;
            _parent.NotifyParameterChanged();
        }
    }

    public void NotifyMultiChoiceChanged()
    {
        if (_parameter is MultiChoiceParameter mcp)
        {
            var selected = MultiChoiceItems
                .Where(i => i.IsChecked)
                .Select(i => i.Id)
                .ToHashSet();
            mcp.value = selected;
            Value = string.Join(", ", selected);
            _parent.NotifyParameterChanged();
        }
    }
}

public partial class MultiChoiceItemViewModel : ObservableObject
{
    private readonly RecipeParameterViewModel _parent;

    public string Id { get; }
    public string Label { get; }

    [ObservableProperty]
    private bool _isChecked;

    public MultiChoiceItemViewModel(RecipeParameterViewModel parent, string id, string label)
    {
        _parent = parent;
        Id = id;
        Label = label;
    }

    partial void OnIsCheckedChanged(bool value)
    {
        _parent.NotifyMultiChoiceChanged();
    }
}
