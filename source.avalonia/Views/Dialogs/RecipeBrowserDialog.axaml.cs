using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Ginger.Views.Dialogs;

public partial class RecipeBrowserDialog : Window
{
    public Recipe? SelectedRecipe { get; private set; }
    public bool DialogResult { get; private set; }

    private List<RecipeCategory> _categories = new();
    private List<RecipeItem> _allRecipes = new();
    private List<RecipeItem> _filteredRecipes = new();

    public RecipeBrowserDialog()
    {
        InitializeComponent();

        SearchBox.TextChanged += (s, e) => FilterRecipes();
        CategoryList.SelectionChanged += (s, e) => FilterRecipes();
    }

    public void LoadRecipes(IEnumerable<Recipe> recipes)
    {
        _allRecipes.Clear();
        _categories.Clear();

        var categoryGroups = recipes
            .GroupBy(r => r.category.ToString())
            .OrderBy(g => g.Key);

        foreach (var group in categoryGroups)
        {
            var category = new RecipeCategory { Name = group.Key };
            _categories.Add(category);

            foreach (var recipe in group.OrderBy(r => r.name ?? r.id.ToString()))
            {
                _allRecipes.Add(new RecipeItem
                {
                    Recipe = recipe,
                    Category = group.Key,
                    Name = recipe.name ?? recipe.id.ToString(),
                    Description = GetRecipeDescription(recipe)
                });
            }
        }

        // Add "All" category at start
        _categories.Insert(0, new RecipeCategory { Name = "All Categories" });

        CategoryList.ItemsSource = _categories.Select(c => c.Name).ToList();
        CategoryList.SelectedIndex = 0;

        CategoryFilter.ItemsSource = _categories.Select(c => c.Name).ToList();
        CategoryFilter.SelectedIndex = 0;

        UpdateStatus();
    }

    private static string GetRecipeDescription(Recipe recipe)
    {
        // Try to get description from first text parameter or recipe content
        var textParam = recipe.parameters?.OfType<TextParameter>().FirstOrDefault();
        if (textParam != null && !string.IsNullOrEmpty(textParam.description))
            return textParam.description;

        return recipe.description ?? "";
    }

    private void FilterRecipes()
    {
        string searchText = SearchBox.Text?.ToLowerInvariant() ?? "";
        string selectedCategory = CategoryList.SelectedItem as string ?? "All Categories";

        _filteredRecipes = _allRecipes
            .Where(r =>
            {
                // Category filter
                if (selectedCategory != "All Categories" && r.Category != selectedCategory)
                    return false;

                // Search filter
                if (!string.IsNullOrEmpty(searchText))
                {
                    return r.Name.ToLowerInvariant().Contains(searchText) ||
                           r.Description.ToLowerInvariant().Contains(searchText);
                }

                return true;
            })
            .ToList();

        RecipeList.ItemsSource = _filteredRecipes;
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        StatusText.Text = $"{_filteredRecipes.Count} of {_allRecipes.Count} recipes";
        AddButton.IsEnabled = RecipeList.SelectedItem != null;
    }

    private void RecipeList_DoubleTapped(object? sender, TappedEventArgs e)
    {
        SelectRecipe();
    }

    private void AddButton_Click(object? sender, RoutedEventArgs e)
    {
        SelectRecipe();
    }

    private void SelectRecipe()
    {
        if (RecipeList.SelectedItem is RecipeItem item)
        {
            SelectedRecipe = item.Recipe;
            DialogResult = true;
            Close();
        }
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private class RecipeCategory
    {
        public string Name { get; set; } = "";
    }

    private class RecipeItem
    {
        public Recipe Recipe { get; set; } = null!;
        public string Category { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
    }
}
