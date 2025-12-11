using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Ginger.Services;

/// <summary>
/// Text processing utilities for character card text manipulation.
/// Handles format conversion, placeholder replacement, and text cleanup.
/// </summary>
public static class TextProcessing
{
    // User-facing markers
    public const string CharacterMarker = "{char}";
    public const string UserMarker = "{user}";
    public const string OriginalMarker = "{original}";

    // SillyTavern markers
    public const string TavernCharacterMarker = "{{char}}";
    public const string TavernUserMarker = "{{user}}";
    public const string TavernOriginalMarker = "{{original}}";

    // Backyard/Faraday markers
    public const string BackyardCharacterMarker = "{character}";
    public const string BackyardUserMarker = "{user}";

    /// <summary>
    /// Output format for preview display.
    /// </summary>
    public enum OutputFormat
    {
        Default,        // {char}, {user}
        SillyTavern,    // {{char}}, {{user}}
        Faraday,        // {character}, {user}
        PlainText       // Replace with actual names
    }

    /// <summary>
    /// Convert text from Tavern/Faraday format to internal Ginger format.
    /// </summary>
    public static string FromTavern(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var sb = new StringBuilder(text);

        // Convert various character markers to standard {char}
        sb.Replace("<bot>", CharacterMarker, StringComparison.OrdinalIgnoreCase);
        sb.Replace("<user>", UserMarker, StringComparison.OrdinalIgnoreCase);
        sb.Replace(TavernCharacterMarker, CharacterMarker, StringComparison.OrdinalIgnoreCase);
        sb.Replace(TavernUserMarker, UserMarker, StringComparison.OrdinalIgnoreCase);
        sb.Replace(TavernOriginalMarker, OriginalMarker, StringComparison.OrdinalIgnoreCase);
        sb.Replace(BackyardCharacterMarker, CharacterMarker, StringComparison.OrdinalIgnoreCase);

        // Normalize line breaks
        sb.Replace("\r\n", "\n");
        sb.Replace("\r", "\n");

        return sb.ToString().Trim();
    }

    /// <summary>
    /// Convert text from Faraday format to internal Ginger format.
    /// </summary>
    public static string FromFaraday(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var sb = new StringBuilder(text);

        sb.Replace("<bot>", CharacterMarker, StringComparison.OrdinalIgnoreCase);
        sb.Replace("<user>", UserMarker, StringComparison.OrdinalIgnoreCase);
        sb.Replace("#{character}", CharacterMarker, StringComparison.OrdinalIgnoreCase);
        sb.Replace("#{user}", UserMarker, StringComparison.OrdinalIgnoreCase);
        sb.Replace(TavernCharacterMarker, CharacterMarker, StringComparison.OrdinalIgnoreCase);
        sb.Replace(TavernUserMarker, UserMarker, StringComparison.OrdinalIgnoreCase);
        sb.Replace(BackyardCharacterMarker, CharacterMarker, StringComparison.OrdinalIgnoreCase);
        sb.Replace(BackyardUserMarker, UserMarker, StringComparison.OrdinalIgnoreCase);

        sb.Replace("\r\n", "\n");
        sb.Replace("\r", "\n");

        return sb.ToString().Trim();
    }

    /// <summary>
    /// Convert internal text to Tavern output format.
    /// </summary>
    public static string ToTavern(string text, string? characterName = null)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var sb = new StringBuilder(text);

        sb.Replace(CharacterMarker, TavernCharacterMarker, StringComparison.OrdinalIgnoreCase);
        sb.Replace(UserMarker, TavernUserMarker, StringComparison.OrdinalIgnoreCase);
        sb.Replace(OriginalMarker, TavernOriginalMarker, StringComparison.OrdinalIgnoreCase);

        // Unix line endings for Tavern
        sb.Replace("\r\n", "\n");

        return sb.ToString().Trim();
    }

    /// <summary>
    /// Convert internal text to Faraday/Backyard output format.
    /// </summary>
    public static string ToFaraday(string text, string? characterName = null)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var sb = new StringBuilder(text);

        sb.Replace(CharacterMarker, BackyardCharacterMarker, StringComparison.OrdinalIgnoreCase);
        sb.Replace(UserMarker, BackyardUserMarker, StringComparison.OrdinalIgnoreCase);
        sb.Replace(OriginalMarker, "", StringComparison.OrdinalIgnoreCase);

        sb.Replace("\r\n", "\n");

        return sb.ToString().Trim();
    }

    /// <summary>
    /// Convert text to plain output with names substituted.
    /// </summary>
    public static string ToPlainText(string text, string characterName, string userName)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var sb = new StringBuilder(text);

        sb.Replace(CharacterMarker, characterName ?? "Character", StringComparison.OrdinalIgnoreCase);
        sb.Replace(TavernCharacterMarker, characterName ?? "Character", StringComparison.OrdinalIgnoreCase);
        sb.Replace(BackyardCharacterMarker, characterName ?? "Character", StringComparison.OrdinalIgnoreCase);

        sb.Replace(UserMarker, userName ?? "User", StringComparison.OrdinalIgnoreCase);
        sb.Replace(TavernUserMarker, userName ?? "User", StringComparison.OrdinalIgnoreCase);
        sb.Replace(BackyardUserMarker, userName ?? "User", StringComparison.OrdinalIgnoreCase);

        sb.Replace(OriginalMarker, "", StringComparison.OrdinalIgnoreCase);
        sb.Replace(TavernOriginalMarker, "", StringComparison.OrdinalIgnoreCase);

        return sb.ToString().Trim();
    }

    /// <summary>
    /// Convert text to the specified output format.
    /// </summary>
    public static string ToOutputFormat(string text, OutputFormat format, string? characterName = null, string? userName = null)
    {
        return format switch
        {
            OutputFormat.SillyTavern => ToTavern(text, characterName),
            OutputFormat.Faraday => ToFaraday(text, characterName),
            OutputFormat.PlainText => ToPlainText(text, characterName ?? "Character", userName ?? "User"),
            _ => CleanText(text)
        };
    }

    /// <summary>
    /// Clean and normalize text (remove extra whitespace, normalize line breaks).
    /// </summary>
    public static string CleanText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var sb = new StringBuilder(text);

        // Normalize line breaks
        sb.Replace("\r\n", "\n");
        sb.Replace("\r", "\n");

        // Remove excessive blank lines (more than 2 in a row)
        string result = sb.ToString();
        result = Regex.Replace(result, @"\n{3,}", "\n\n");

        // Trim trailing whitespace from lines
        var lines = result.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            lines[i] = lines[i].TrimEnd();
        }

        return string.Join("\n", lines).Trim();
    }

    /// <summary>
    /// Remove C-style and HTML comments from text.
    /// </summary>
    public static string RemoveComments(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // Remove /* */ comments
        text = Regex.Replace(text, @"/\*.*?\*/", "", RegexOptions.Singleline);

        // Remove <!-- --> comments
        text = Regex.Replace(text, @"<!--.*?-->", "", RegexOptions.Singleline);

        return text;
    }

    /// <summary>
    /// Count tokens in text (rough estimate based on word count).
    /// This is a rough approximation - actual token count depends on the tokenizer.
    /// </summary>
    public static int EstimateTokenCount(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        // Rough estimate: ~0.75 tokens per word, ~4 chars per token
        // This is a very rough approximation
        int charCount = text.Length;
        int wordCount = text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;

        // Use a blend of character and word-based estimates
        return Math.Max(charCount / 4, (int)(wordCount * 1.3));
    }
}

/// <summary>
/// Extension methods for StringBuilder to support case-insensitive replace.
/// </summary>
public static class StringBuilderExtensions
{
    public static StringBuilder Replace(this StringBuilder sb, string oldValue, string newValue, StringComparison comparison)
    {
        if (comparison == StringComparison.Ordinal)
        {
            return sb.Replace(oldValue, newValue);
        }

        int index = 0;
        while (index < sb.Length)
        {
            int pos = sb.ToString().IndexOf(oldValue, index, comparison);
            if (pos < 0)
                break;

            sb.Remove(pos, oldValue.Length);
            sb.Insert(pos, newValue);
            index = pos + newValue.Length;
        }
        return sb;
    }
}
