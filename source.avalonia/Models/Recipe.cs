using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace Ginger.Models;

/// <summary>
/// Represents a recipe template that can be loaded from XML files.
/// Simplified version for Avalonia port - focuses on core functionality.
/// </summary>
public class Recipe
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Author { get; set; } = "";
    public string Category { get; set; } = "";
    public string[] Flags { get; set; } = Array.Empty<string>();
    public int Order { get; set; } = 100;
    public string FileName { get; set; } = "";

    public List<RecipeParameter> Parameters { get; set; } = new();
    public List<RecipeTemplate> Templates { get; set; } = new();

    public bool IsEnabled { get; set; } = true;
    public bool IsCollapsed { get; set; } = false;

    public enum TemplateChannel
    {
        System,
        Persona,
        UserPersona,
        Scenario,
        Example,
        Grammar,
        Greeting,
    }

    public class RecipeTemplate
    {
        public TemplateChannel Channel { get; set; }
        public string Text { get; set; } = "";
        public string? Condition { get; set; }
        public bool IsRaw { get; set; }
    }

    /// <summary>
    /// Load a recipe from an XML file.
    /// </summary>
    public static Recipe? LoadFromFile(string filePath)
    {
        try
        {
            var doc = new XmlDocument();
            doc.Load(filePath);

            var root = doc.DocumentElement;
            if (root == null || root.Name != "Ginger")
                return null;

            var recipe = new Recipe
            {
                FileName = filePath,
                Id = root.GetAttribute("id"),
            };

            // Basic metadata
            recipe.Name = GetElementValue(root, "Name") ?? recipe.Id;
            recipe.Title = GetElementValue(root, "Title") ?? recipe.Name;
            recipe.Description = GetElementValue(root, "Description") ?? "";
            recipe.Author = GetElementValue(root, "Author") ?? "";
            recipe.Category = GetElementValue(root, "Category") ?? "";

            var flagsStr = GetElementValue(root, "Flags") ?? "";
            recipe.Flags = flagsStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var orderStr = GetElementValue(root, "Order");
            if (int.TryParse(orderStr, out int order))
                recipe.Order = order;

            // Parse parameters
            ParseParameters(root, recipe);

            // Parse templates (System, Persona, Scenario, etc.)
            ParseTemplates(root, recipe);

            return recipe;
        }
        catch
        {
            return null;
        }
    }

    private static void ParseParameters(XmlElement root, Recipe recipe)
    {
        foreach (XmlNode node in root.ChildNodes)
        {
            if (node is not XmlElement element)
                continue;

            RecipeParameter? param = element.Name switch
            {
                "Text" => ParseTextParameter(element),
                "Number" => ParseNumberParameter(element),
                "Toggle" => ParseToggleParameter(element),
                "Choice" => ParseChoiceParameter(element),
                "Slider" => ParseSliderParameter(element),
                _ => null
            };

            if (param != null)
                recipe.Parameters.Add(param);
        }
    }

    private static RecipeParameter ParseTextParameter(XmlElement element)
    {
        return new RecipeParameter
        {
            Id = element.GetAttribute("id"),
            Type = RecipeParameter.ParameterType.Text,
            Label = GetElementValue(element, "Label") ?? element.GetAttribute("id"),
            Description = GetElementValue(element, "Description") ?? "",
            DefaultValue = GetElementValue(element, "Default") ?? element.GetAttribute("default") ?? "",
            IsRequired = element.GetAttribute("required") == "yes",
            IsRaw = element.GetAttribute("raw") == "yes",
        };
    }

    private static RecipeParameter ParseNumberParameter(XmlElement element)
    {
        var param = new RecipeParameter
        {
            Id = element.GetAttribute("id"),
            Type = RecipeParameter.ParameterType.Number,
            Label = GetElementValue(element, "Label") ?? element.GetAttribute("id"),
            Description = GetElementValue(element, "Description") ?? "",
            DefaultValue = GetElementValue(element, "Default") ?? element.GetAttribute("default") ?? "0",
        };

        if (decimal.TryParse(element.GetAttribute("min"), out decimal min))
            param.MinValue = min;
        if (decimal.TryParse(element.GetAttribute("max"), out decimal max))
            param.MaxValue = max;

        param.Suffix = GetElementValue(element, "Suffix") ?? "";

        return param;
    }

    private static RecipeParameter ParseToggleParameter(XmlElement element)
    {
        return new RecipeParameter
        {
            Id = element.GetAttribute("id"),
            Type = RecipeParameter.ParameterType.Toggle,
            Label = GetElementValue(element, "Label") ?? element.GetAttribute("id"),
            Description = GetElementValue(element, "Description") ?? "",
            DefaultValue = element.GetAttribute("default") ?? "false",
        };
    }

    private static RecipeParameter ParseChoiceParameter(XmlElement element)
    {
        var param = new RecipeParameter
        {
            Id = element.GetAttribute("id"),
            Type = RecipeParameter.ParameterType.Choice,
            Label = GetElementValue(element, "Label") ?? element.GetAttribute("id"),
            Description = GetElementValue(element, "Description") ?? "",
            DefaultValue = GetElementValue(element, "Default") ?? "",
        };

        // Parse options
        var options = new List<string>();
        foreach (XmlNode optNode in element.ChildNodes)
        {
            if (optNode is XmlElement optElement && optElement.Name == "Option")
            {
                var value = optElement.GetAttribute("value");
                if (!string.IsNullOrEmpty(value))
                    options.Add(value);
                else if (!string.IsNullOrEmpty(optElement.InnerText))
                    options.Add(optElement.InnerText.Trim());
            }
        }
        param.Options = options.ToArray();

        return param;
    }

    private static RecipeParameter ParseSliderParameter(XmlElement element)
    {
        var param = new RecipeParameter
        {
            Id = element.GetAttribute("id"),
            Type = RecipeParameter.ParameterType.Slider,
            Label = GetElementValue(element, "Label") ?? element.GetAttribute("id"),
            Description = GetElementValue(element, "Description") ?? "",
            DefaultValue = GetElementValue(element, "Default") ?? element.GetAttribute("default") ?? "50",
        };

        if (decimal.TryParse(element.GetAttribute("min"), out decimal min))
            param.MinValue = min;
        else
            param.MinValue = 0;
        if (decimal.TryParse(element.GetAttribute("max"), out decimal max))
            param.MaxValue = max;
        else
            param.MaxValue = 100;

        return param;
    }

    private static void ParseTemplates(XmlElement root, Recipe recipe)
    {
        string[] channelNames = { "System", "Persona", "User", "Scenario", "Example", "Grammar", "Greeting" };
        TemplateChannel[] channels = { TemplateChannel.System, TemplateChannel.Persona, TemplateChannel.UserPersona,
                                       TemplateChannel.Scenario, TemplateChannel.Example, TemplateChannel.Grammar,
                                       TemplateChannel.Greeting };

        for (int i = 0; i < channelNames.Length; i++)
        {
            var elements = root.GetElementsByTagName(channelNames[i]);
            foreach (XmlNode node in elements)
            {
                if (node is XmlElement element && !string.IsNullOrWhiteSpace(element.InnerText))
                {
                    recipe.Templates.Add(new RecipeTemplate
                    {
                        Channel = channels[i],
                        Text = element.InnerText.Trim(),
                        Condition = element.GetAttribute("rule"),
                        IsRaw = element.GetAttribute("raw") == "true",
                    });
                }
            }
        }
    }

    private static string? GetElementValue(XmlElement parent, string elementName)
    {
        var elements = parent.GetElementsByTagName(elementName);
        if (elements.Count > 0)
        {
            var text = elements[0]?.InnerText?.Trim();
            return string.IsNullOrEmpty(text) ? null : text;
        }
        return null;
    }
}

/// <summary>
/// Represents a parameter in a recipe.
/// </summary>
public class RecipeParameter
{
    public enum ParameterType
    {
        Text,
        Number,
        Toggle,
        Choice,
        Slider,
        List,
    }

    public string Id { get; set; } = "";
    public ParameterType Type { get; set; } = ParameterType.Text;
    public string Label { get; set; } = "";
    public string Description { get; set; } = "";
    public string DefaultValue { get; set; } = "";
    public string Value { get; set; } = "";
    public bool IsEnabled { get; set; } = true;
    public bool IsRequired { get; set; }
    public bool IsRaw { get; set; }

    // For Number/Slider
    public decimal MinValue { get; set; }
    public decimal MaxValue { get; set; } = 100;
    public string Suffix { get; set; } = "";

    // For Choice
    public string[] Options { get; set; } = Array.Empty<string>();

    public string GetEffectiveValue()
    {
        return string.IsNullOrEmpty(Value) ? DefaultValue : Value;
    }
}
