using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ginger.Models;

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
        _id = recipe.Id;
        _name = recipe.Name;
        _title = recipe.Title;
        _description = recipe.Description;
        _category = recipe.Category;
        _isEnabled = recipe.IsEnabled;
        _isExpanded = !recipe.IsCollapsed;

        // Build content from templates
        foreach (var template in recipe.Templates)
        {
            if (!string.IsNullOrEmpty(template.Text))
            {
                _content += template.Text + "\n\n";
            }
        }
        _content = _content.TrimEnd();

        // Load parameters
        foreach (var param in recipe.Parameters)
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
    private readonly RecipeParameter _parameter;

    public string Id => _parameter.Id;
    public string Label => _parameter.Label;
    public string Description => _parameter.Description;
    public RecipeParameter.ParameterType Type => _parameter.Type;
    public decimal MinValue => _parameter.MinValue;
    public decimal MaxValue => _parameter.MaxValue;
    public string Suffix => _parameter.Suffix;
    public string[] Options => _parameter.Options;

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

    public RecipeParameterViewModel(RecipeViewModel parent, RecipeParameter parameter)
    {
        _parent = parent;
        _parameter = parameter;
        _value = parameter.GetEffectiveValue();
        _isEnabled = parameter.IsEnabled;

        // Initialize type-specific values
        switch (parameter.Type)
        {
            case RecipeParameter.ParameterType.Toggle:
                _boolValue = _value.Equals("true", System.StringComparison.OrdinalIgnoreCase);
                break;
            case RecipeParameter.ParameterType.Number:
            case RecipeParameter.ParameterType.Slider:
                decimal.TryParse(_value, out _numericValue);
                break;
            case RecipeParameter.ParameterType.Choice:
                _selectedOption = _value;
                break;
        }
    }

    partial void OnValueChanged(string value)
    {
        _parameter.Value = value;
    }

    partial void OnBoolValueChanged(bool value)
    {
        _parameter.Value = value.ToString().ToLower();
    }

    partial void OnNumericValueChanged(decimal value)
    {
        _parameter.Value = value.ToString();
    }

    partial void OnSelectedOptionChanged(string? value)
    {
        if (value != null)
            _parameter.Value = value;
    }
}
