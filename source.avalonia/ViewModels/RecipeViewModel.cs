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

        // Build content from templates
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
    }

    partial void OnContentChanged(string value)
    {
        _parent.OnRecipeChanged();
    }

    partial void OnIsEnabledChanged(bool value)
    {
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
    private void Remove()
    {
        _parent.RemoveRecipe(this);
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

    public Recipe? GetSourceRecipe() => _sourceRecipe;

    public void NotifyParameterChanged()
    {
        _parent.OnRecipeChanged();
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
    public bool IsNumberParameter => _parameter is NumberParameter or MeasurementParameter or RangeParameter;
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

    [ObservableProperty]
    private string _value = "";

    [ObservableProperty]
    private bool _isEnabled = true;

    [ObservableProperty]
    private bool _boolValue;

    [ObservableProperty]
    private decimal _numericValue;

    [ObservableProperty]
    private string? _selectedOption;

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
            _value = _numericValue.ToString();
        }
        else
            _value = parameter.defaultValue ?? "";

        // Initialize choice selection
        if (parameter is ChoiceParameter cp && cp.selectedIndex >= 0 && cp.selectedIndex < cp.items.Count)
            _selectedOption = cp.items[cp.selectedIndex].label;
        else
            _selectedOption = _value;
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
}
