using System.Collections.ObjectModel;
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
}

public partial class RecipeParameterViewModel : ObservableObject
{
    private readonly RecipeViewModel _parent;
    private readonly IParameter _parameter;

    public string Id => _parameter.id.ToString();
    public string Label => _parameter.label ?? "";
    public string Description => _parameter.description ?? "";
    public bool IsOptional => _parameter.isOptional;

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
        _value = parameter.defaultValue ?? "";
        _isEnabled = parameter.isEnabled;

        // Initialize type-specific values based on value
        if (bool.TryParse(_value, out var boolVal))
            _boolValue = boolVal;
        if (decimal.TryParse(_value, out var numVal))
            _numericValue = numVal;
        _selectedOption = _value;
    }

    partial void OnValueChanged(string value)
    {
        // Update parameter value if possible
    }

    partial void OnBoolValueChanged(bool value)
    {
        _value = value.ToString().ToLower();
    }

    partial void OnNumericValueChanged(decimal value)
    {
        _value = value.ToString();
    }

    partial void OnSelectedOptionChanged(string? value)
    {
        if (value != null)
            _value = value;
    }
}
