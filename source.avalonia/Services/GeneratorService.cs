using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ginger.Models;

namespace Ginger.Services;

/// <summary>
/// Service for generating character card output from recipes.
/// </summary>
public class GeneratorService
{
    /// <summary>
    /// Generated output structure containing all card components.
    /// </summary>
    public class Output
    {
        public string System { get; set; } = "";
        public string Persona { get; set; } = "";
        public string Personality { get; set; } = "";
        public string Scenario { get; set; } = "";
        public string Greeting { get; set; } = "";
        public string Example { get; set; } = "";
        public string UserPersona { get; set; } = "";
        public string Grammar { get; set; } = "";

        /// <summary>
        /// Get total estimated token count.
        /// </summary>
        public int EstimatedTokens =>
            TextProcessing.EstimateTokenCount(System) +
            TextProcessing.EstimateTokenCount(Persona) +
            TextProcessing.EstimateTokenCount(Personality) +
            TextProcessing.EstimateTokenCount(Scenario) +
            TextProcessing.EstimateTokenCount(Greeting) +
            TextProcessing.EstimateTokenCount(Example);

        /// <summary>
        /// Format output for preview display.
        /// </summary>
        public string ToPreview(TextProcessing.OutputFormat format = TextProcessing.OutputFormat.Default,
            string? characterName = null, string? userName = null)
        {
            var sb = new StringBuilder();

            AppendSection(sb, "System", System, format, characterName, userName);
            AppendSection(sb, "Persona", Persona, format, characterName, userName);
            AppendSection(sb, "Personality", Personality, format, characterName, userName);
            AppendSection(sb, "Scenario", Scenario, format, characterName, userName);
            AppendSection(sb, "Greeting", Greeting, format, characterName, userName);
            AppendSection(sb, "Example Dialog", Example, format, characterName, userName);

            return sb.ToString().TrimEnd();
        }

        private void AppendSection(StringBuilder sb, string title, string content,
            TextProcessing.OutputFormat format, string? characterName, string? userName)
        {
            if (string.IsNullOrWhiteSpace(content))
                return;

            string formatted = TextProcessing.ToOutputFormat(content, format, characterName, userName);
            if (string.IsNullOrWhiteSpace(formatted))
                return;

            sb.AppendLine($"=== {title} ===");
            sb.AppendLine(formatted);
            sb.AppendLine();
        }
    }

    private readonly RecipeService _recipeService;

    public GeneratorService(RecipeService recipeService)
    {
        _recipeService = recipeService;
    }

    /// <summary>
    /// Generate output from a character card.
    /// </summary>
    public Output Generate(CharacterCard card)
    {
        var output = new Output
        {
            System = ProcessText(card.System),
            Persona = ProcessText(card.Persona),
            Personality = ProcessText(card.Personality),
            Scenario = ProcessText(card.Scenario),
            Greeting = ProcessText(card.Greeting),
            Example = ProcessText(card.Example),
        };

        return output;
    }

    /// <summary>
    /// Generate output from recipes applied to a character card.
    /// </summary>
    public Output Generate(CharacterCard card, IEnumerable<Recipe> recipes)
    {
        var output = new Output();

        // Start with card values
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["system"] = card.System ?? "",
            ["persona"] = card.Persona ?? "",
            ["personality"] = card.Personality ?? "",
            ["scenario"] = card.Scenario ?? "",
            ["greeting"] = card.Greeting ?? "",
            ["example"] = card.Example ?? "",
            ["user_persona"] = "",
            ["grammar"] = "",
            ["name"] = card.Name ?? "",
            ["char"] = card.SpokenName ?? card.Name ?? "",
        };

        // Apply each recipe in order
        foreach (var recipe in recipes.Where(r => r.isEnabled))
        {
            ApplyRecipe(recipe, values);
        }

        // Build output
        output.System = ProcessText(values["system"]);
        output.Persona = ProcessText(values["persona"]);
        output.Personality = ProcessText(values["personality"]);
        output.Scenario = ProcessText(values["scenario"]);
        output.Greeting = ProcessText(values["greeting"]);
        output.Example = ProcessText(values["example"]);
        output.UserPersona = ProcessText(values["user_persona"]);
        output.Grammar = values["grammar"];

        return output;
    }

    /// <summary>
    /// Apply a recipe's parameters to the values dictionary.
    /// </summary>
    private void ApplyRecipe(Recipe recipe, Dictionary<string, string> values)
    {
        foreach (var param in recipe.parameters)
        {
            if (!param.isEnabled || StringHandle.IsNullOrEmpty(param.id))
                continue;

            string paramId = param.id.ToString();
            string value = param.defaultValue ?? "";

            // Handle different parameter modes
            switch (paramId.ToLowerInvariant())
            {
                case "system":
                case "persona":
                case "personality":
                case "scenario":
                case "greeting":
                case "example":
                case "user_persona":
                case "grammar":
                    // Direct assignment or append based on parameter mode
                    if (values.TryGetValue(paramId, out string? existing) && !string.IsNullOrEmpty(existing))
                    {
                        // Append with separator
                        values[paramId] = existing + "\n\n" + value;
                    }
                    else
                    {
                        values[paramId] = value;
                    }
                    break;

                default:
                    // Store as variable for later substitution
                    values[paramId] = value;
                    break;
            }
        }
    }

    /// <summary>
    /// Process text: clean up, remove comments, normalize.
    /// </summary>
    private string ProcessText(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return "";

        text = TextProcessing.RemoveComments(text);
        text = TextProcessing.CleanText(text);
        return text;
    }

    /// <summary>
    /// Generate a quick preview of just the main components.
    /// </summary>
    public string GenerateQuickPreview(CharacterCard card, TextProcessing.OutputFormat format = TextProcessing.OutputFormat.Default)
    {
        var sb = new StringBuilder();

        string charName = card.SpokenName ?? card.Name ?? "Character";

        if (!string.IsNullOrWhiteSpace(card.Persona))
        {
            sb.AppendLine("=== Persona ===");
            sb.AppendLine(TextProcessing.ToOutputFormat(card.Persona, format, charName, "User"));
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(card.Scenario))
        {
            sb.AppendLine("=== Scenario ===");
            sb.AppendLine(TextProcessing.ToOutputFormat(card.Scenario, format, charName, "User"));
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(card.Greeting))
        {
            sb.AppendLine("=== Greeting ===");
            sb.AppendLine(TextProcessing.ToOutputFormat(card.Greeting, format, charName, "User"));
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }
}
