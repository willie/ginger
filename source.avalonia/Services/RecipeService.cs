using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ginger.Services;

/// <summary>
/// Service for loading and managing recipes.
/// </summary>
public class RecipeService
{
    private readonly Dictionary<string, Recipe> _recipes = new();
    private readonly Dictionary<string, List<Recipe>> _recipesByCategory = new();

    /// <summary>
    /// All loaded recipes.
    /// </summary>
    public IReadOnlyCollection<Recipe> AllRecipes => _recipes.Values;

    /// <summary>
    /// Get recipes by category.
    /// </summary>
    public IReadOnlyList<Recipe> GetRecipesByCategory(string category)
    {
        return _recipesByCategory.TryGetValue(category, out var list) ? list : Array.Empty<Recipe>();
    }

    /// <summary>
    /// Get a recipe by ID.
    /// </summary>
    public Recipe? GetRecipe(string id)
    {
        return _recipes.TryGetValue(id, out var recipe) ? recipe : null;
    }

    /// <summary>
    /// Load all recipes from a content directory.
    /// </summary>
    public int LoadRecipes(string contentPath)
    {
        _recipes.Clear();
        _recipesByCategory.Clear();

        if (!Directory.Exists(contentPath))
            return 0;

        var recipesPath = Path.Combine(contentPath, "Recipes");
        if (!Directory.Exists(recipesPath))
            return 0;

        var files = Directory.GetFiles(recipesPath, "*.recipe.xml", SearchOption.AllDirectories);
        int loaded = 0;

        foreach (var file in files)
        {
            var recipe = Recipe.LoadFromFile(file);
            if (recipe != null && !StringHandle.IsNullOrEmpty(recipe.id))
            {
                var recipeId = recipe.id.ToString();
                _recipes[recipeId] = recipe;

                // Index by category
                var category = !string.IsNullOrEmpty(recipe.categoryTag)
                    ? recipe.categoryTag
                    : EnumHelper.ToString(recipe.category);
                if (string.IsNullOrEmpty(category))
                    category = "Other";

                if (!_recipesByCategory.ContainsKey(category))
                    _recipesByCategory[category] = new List<Recipe>();
                _recipesByCategory[category].Add(recipe);

                loaded++;
            }
        }

        // Sort categories
        foreach (var list in _recipesByCategory.Values)
        {
            list.Sort((a, b) =>
            {
                int orderCompare = (a.order ?? 50).CompareTo(b.order ?? 50);
                return orderCompare != 0 ? orderCompare : string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase);
            });
        }

        return loaded;
    }

    /// <summary>
    /// Get all category names.
    /// </summary>
    public IEnumerable<string> GetCategories()
    {
        // Return in a sensible order
        var order = new[] { "Model", "Character", "Personality", "Nsfw", "Story", "World" };
        var result = new List<string>();

        foreach (var cat in order)
        {
            if (_recipesByCategory.ContainsKey(cat))
                result.Add(cat);
        }

        // Add any remaining categories
        foreach (var cat in _recipesByCategory.Keys.OrderBy(c => c))
        {
            if (!result.Contains(cat))
                result.Add(cat);
        }

        return result;
    }

    /// <summary>
    /// Clone a recipe for use in a character.
    /// </summary>
    public Recipe CloneRecipe(string id)
    {
        var original = GetRecipe(id);
        if (original == null)
            throw new ArgumentException($"Recipe not found: {id}");

        return CloneRecipe(original);
    }

    /// <summary>
    /// Clone a recipe for use in a character.
    /// </summary>
    public Recipe CloneRecipe(Recipe original)
    {
        return (Recipe)original.Clone();
    }
}
