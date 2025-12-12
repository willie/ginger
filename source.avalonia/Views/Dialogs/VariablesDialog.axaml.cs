using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Ginger.Views.Dialogs;

public partial class VariablesDialog : Window
{
    public ObservableCollection<VariableItem> Variables { get; } = new();
    public bool DialogResult { get; private set; }

    private ListBox? _variablesList;

    public VariablesDialog()
    {
        InitializeComponent();
        _variablesList = this.FindControl<ListBox>("VariablesList");
        if (_variablesList != null)
            _variablesList.ItemsSource = Variables;
    }

    public void LoadVariables(IEnumerable<CustomVariable> variables)
    {
        Variables.Clear();
        foreach (var v in variables)
        {
            Variables.Add(new VariableItem
            {
                Name = v.Name.ToString(),
                Value = v.Value
            });
        }
    }

    public List<CustomVariable> GetVariables()
    {
        var result = new List<CustomVariable>();
        foreach (var item in Variables)
        {
            if (!string.IsNullOrWhiteSpace(item.Name))
            {
                result.Add(new CustomVariable(new CustomVariableName(item.Name), item.Value ?? ""));
            }
        }
        return result;
    }

    private void AddButton_Click(object? sender, RoutedEventArgs e)
    {
        var newItem = new VariableItem { Name = "new_variable", Value = "" };
        Variables.Add(newItem);
        if (_variablesList != null)
        {
            _variablesList.SelectedItem = newItem;
            _variablesList.ScrollIntoView(newItem);
        }
    }

    private void RemoveButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_variablesList?.SelectedItem is VariableItem item)
        {
            int index = Variables.IndexOf(item);
            Variables.Remove(item);
            if (Variables.Count > 0 && _variablesList != null)
            {
                _variablesList.SelectedIndex = System.Math.Min(index, Variables.Count - 1);
            }
        }
    }

    private void OkButton_Click(object? sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

public partial class VariableItem : ObservableObject
{
    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private string _value = "";
}
