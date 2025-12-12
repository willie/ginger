using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Ginger.Views.Dialogs;

public partial class CreateRecipeDialog : Window
{
    public bool DialogResult { get; private set; }
    public string RecipeName => NameBox.Text ?? "";
    public string RecipeCategory => (CategoryCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Custom";
    public string RecipeComponent => (ComponentCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Persona";
    public string RecipeDescription => DescriptionBox.Text ?? "";
    public string RecipeContent => ContentBox.Text ?? "";

    // Parsed values
    public Recipe.Category Category { get; private set; } = Recipe.Category.Custom;
    public Recipe.Component Component { get; private set; } = Recipe.Component.Persona;

    public CreateRecipeDialog()
    {
        InitializeComponent();
    }

    public CreateRecipeDialog(string defaultContent) : this()
    {
        ContentBox.Text = defaultContent;
    }

    private void CreateButton_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(RecipeName))
        {
            NameBox.Focus();
            return;
        }

        Category = ParseCategory(RecipeCategory);
        Component = ParseComponent(RecipeComponent);
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private static Recipe.Category ParseCategory(string category)
    {
        return category switch
        {
            "Character" => Recipe.Category.Character,
            "Personality" => Recipe.Category.Personality,
            "Model" => Recipe.Category.Model,
            "Story" => Recipe.Category.Story,
            "Traits" => Recipe.Category.Trait,
            "NSFW" => Recipe.Category.Sexual,
            _ => Recipe.Category.Custom,
        };
    }

    private static Recipe.Component ParseComponent(string component)
    {
        return component switch
        {
            "Persona" => Recipe.Component.Persona,
            "Personality" => Recipe.Component.Persona, // Personality maps to Persona
            "Scenario" => Recipe.Component.Scenario,
            "Greeting" => Recipe.Component.Greeting,
            "Example" => Recipe.Component.Example,
            "System Prompt" => Recipe.Component.System,
            "User Persona" => Recipe.Component.UserPersona,
            "Grammar" => Recipe.Component.Grammar,
            _ => Recipe.Component.Persona,
        };
    }
}
