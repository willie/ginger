using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace Ginger.Services;

/// <summary>
/// Flags for controlling which syntax elements are highlighted.
/// </summary>
[Flags]
public enum SyntaxFlags
{
    None = 0,
    Names = 1 << 0,
    Dialogue = 1 << 1,
    Actions = 1 << 2,
    Commands = 1 << 3,
    Pronouns = 1 << 4,
    Numbers = 1 << 5,
    CodeBlock = 1 << 6,
    Comments = 1 << 7,
    Variables = 1 << 8,
    HTML = 1 << 9,
    Markdown = 1 << 10,

    Default = Names | Dialogue | Actions | Commands | Numbers | CodeBlock | Comments | Variables | HTML | Markdown,
    Limited = Names | Commands | Variables | Numbers | Comments | Markdown,
}

/// <summary>
/// Custom document coloring transformer for AvaloniaEdit that provides syntax highlighting
/// for character cards (names, pronouns, numbers, commands, etc.)
/// </summary>
public class GingerSyntaxColorizer : DocumentColorizingTransformer
{
    private SyntaxFlags _flags = SyntaxFlags.Default;
    private string[] _characterNames = Array.Empty<string>();
    private string[] _variableNames = Array.Empty<string>();

    // Colors matching the original WinForms theme
    public IBrush DialogueBrush { get; set; } = new SolidColorBrush(Color.FromRgb(0, 100, 0));      // Dark green
    public IBrush NarrationBrush { get; set; } = new SolidColorBrush(Color.FromRgb(128, 128, 128)); // Gray
    public IBrush NumberBrush { get; set; } = new SolidColorBrush(Color.FromRgb(255, 140, 0));      // Orange
    public IBrush NameBrush { get; set; } = new SolidColorBrush(Color.FromRgb(0, 0, 200));          // Blue
    public IBrush CommandBrush { get; set; } = new SolidColorBrush(Color.FromRgb(128, 0, 128));     // Purple
    public IBrush PronounBrush { get; set; } = new SolidColorBrush(Color.FromRgb(200, 0, 200));     // Magenta
    public IBrush CommentBrush { get; set; } = new SolidColorBrush(Color.FromRgb(0, 128, 0));       // Green
    public IBrush CodeBrush { get; set; } = new SolidColorBrush(Color.FromRgb(100, 100, 100));      // Dark gray
    public IBrush ErrorBrush { get; set; } = new SolidColorBrush(Color.FromRgb(255, 0, 0));         // Red
    public IBrush VariableBrush { get; set; } = new SolidColorBrush(Color.FromRgb(0, 128, 128));    // Teal
    public IBrush HTMLBrush { get; set; } = new SolidColorBrush(Color.FromRgb(128, 128, 128));      // Gray

    public SyntaxFlags Flags
    {
        get => _flags;
        set => _flags = value;
    }

    public void SetCharacterNames(string[] names)
    {
        _characterNames = names ?? Array.Empty<string>();
    }

    public void SetVariableNames(string[] names)
    {
        _variableNames = names ?? Array.Empty<string>();
    }

    protected override void ColorizeLine(DocumentLine line)
    {
        if (line.Length == 0)
            return;

        var lineText = CurrentContext.Document.GetText(line);
        var lineOffset = line.Offset;

        // Process in order of priority (higher priority = later, overwrites earlier)

        // HTML tags
        if (_flags.HasFlag(SyntaxFlags.HTML))
        {
            HighlightPattern(lineText, lineOffset, @"(<\/?([a-zA-Z]+)(?![^>]*\/>)[^>]*>|<([a-zA-Z ]+)\/>)", HTMLBrush);
        }

        // Comments /* ... */
        if (_flags.HasFlag(SyntaxFlags.Comments))
        {
            HighlightPattern(lineText, lineOffset, @"\/\*[\s\S]*?\*\/", CommentBrush, italic: true);
            HighlightPattern(lineText, lineOffset, @"<!--[\s\S]*?-->", CommentBrush, italic: true);
        }

        // Markdown images ![...](...) - mark as error/warning
        if (_flags.HasFlag(SyntaxFlags.Markdown))
        {
            HighlightPattern(lineText, lineOffset, @"!\[(.*?)\]\((.+?)\)", ErrorBrush);
        }

        // Code blocks `...`
        if (_flags.HasFlag(SyntaxFlags.CodeBlock))
        {
            HighlightPattern(lineText, lineOffset, "`[^`]*`", CodeBrush, monospace: true);
        }

        // Dialogue "..."
        if (_flags.HasFlag(SyntaxFlags.Dialogue))
        {
            HighlightPattern(lineText, lineOffset, @"(?<!\d)""(?:[^""\\]|\\.)*""", DialogueBrush);
            HighlightPattern(lineText, lineOffset, @"\u201C[^\x22]*\u201D", DialogueBrush);
        }

        // Actions/narration *...*
        if (_flags.HasFlag(SyntaxFlags.Actions))
        {
            HighlightPattern(lineText, lineOffset, @"\*+(?:[^*\\]|\\.)*\*+", NarrationBrush, italic: true);
        }

        // Numbers
        if (_flags.HasFlag(SyntaxFlags.Numbers))
        {
            // Feet/inches
            HighlightPattern(lineText, lineOffset, @"\d+(\'|\x22)\d*\x22?", NumberBrush);
            // Ordinal numbers (1st, 2nd, 3rd, etc.)
            HighlightPattern(lineText, lineOffset, @"\d+(?:st|nd|rd|th)", NumberBrush);
            // Regular numbers
            HighlightPattern(lineText, lineOffset, @"[-+#]?\b\d+(?:[.,]\d+)?\b", NumberBrush);
        }

        // Variables {$...}
        if (_flags.HasFlag(SyntaxFlags.Variables))
        {
            // Unknown variables - underlined
            HighlightPattern(lineText, lineOffset, @"\{\$[\w\-_]*\}", VariableBrush, underline: true);

            // Known variables
            foreach (var varName in _variableNames)
            {
                if (!string.IsNullOrEmpty(varName))
                {
                    var escaped = Regex.Escape(varName);
                    HighlightPattern(lineText, lineOffset, escaped, VariableBrush);
                }
            }
        }

        // Commands {char}, {user}, {they}, etc.
        if (_flags.HasFlag(SyntaxFlags.Commands))
        {
            // Invalid patterns like {{char}} - error
            HighlightPattern(lineText, lineOffset, @"\{\{\w+\}\}", ErrorBrush, underline: true);
            HighlightPattern(lineText, lineOffset, @"\{\bcharacter\b\}", ErrorBrush, underline: true);

            // Valid command patterns
            var commandPattern = @"\{(?i)\b(char|user|card|name|gender|names|actors|others|everyone|original|unknown|" +
                "they'll|they're|they've|they'd|they|them|theirs|their|themselves|" +
                "he'll|he's|he'd|he|him|his|himself|" +
                "she'll|she's|she'd|she|her|hers|herself|" +
                @"is|are|isn't|aren't|has|have|hasn't|haven't|was|were|wasn't|weren't|does|do|doesn't|don't|s|y|ies|es)\b\}";
            HighlightPattern(lineText, lineOffset, commandPattern, CommandBrush);

            // User commands {#...}
            var userCommandPattern = @"\{\#(?i)\b(gender|name|they'll|they're|they've|they'd|they|them|theirs|their|themselves|" +
                "he'll|he's|he'd|he|him|his|himself|" +
                "she'll|she's|she'd|she|her|hers|herself|" +
                @"is|are|isn't|aren't|has|have|hasn't|haven't|was|were|wasn't|weren't|does|do|doesn't|don't|s|y|ies|es)\b\}";
            HighlightPattern(lineText, lineOffset, userCommandPattern, CommandBrush);
        }

        // Pronouns (standalone)
        if (_flags.HasFlag(SyntaxFlags.Pronouns))
        {
            var pronounPattern = @"(?i)\b(he/she|him/her|his/hers|his/her|himself/herself|" +
                "he's|he'll|he'd|he|him|his|himself|" +
                "she'll|she's|she'd|she|her|hers|herself|" +
                @"they'll|they're|they've|they'd|they|them|theirs|their|themselves)\b";
            HighlightPattern(lineText, lineOffset, pronounPattern, PronounBrush);
        }

        // Character names
        if (_flags.HasFlag(SyntaxFlags.Names))
        {
            foreach (var name in _characterNames)
            {
                if (!string.IsNullOrEmpty(name))
                {
                    var escaped = Regex.Escape(name);
                    HighlightPattern(lineText, lineOffset, @"\b" + escaped + @"\b", NameBrush, bold: true);
                }
            }
        }
    }

    private void HighlightPattern(string lineText, int lineOffset, string pattern, IBrush brush,
        bool bold = false, bool italic = false, bool underline = false, bool monospace = false)
    {
        try
        {
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            var matches = regex.Matches(lineText);

            foreach (Match match in matches)
            {
                var startOffset = lineOffset + match.Index;
                var endOffset = startOffset + match.Length;

                ChangeLinePart(startOffset, endOffset, element =>
                {
                    element.TextRunProperties.SetForegroundBrush(brush);

                    if (bold)
                    {
                        element.TextRunProperties.SetTypeface(new Typeface(
                            element.TextRunProperties.Typeface.FontFamily,
                            FontStyle.Normal,
                            FontWeight.Bold));
                    }

                    if (italic)
                    {
                        var weight = bold ? FontWeight.Bold : FontWeight.Normal;
                        element.TextRunProperties.SetTypeface(new Typeface(
                            element.TextRunProperties.Typeface.FontFamily,
                            FontStyle.Italic,
                            weight));
                    }

                    if (underline)
                    {
                        element.TextRunProperties.SetTextDecorations(TextDecorations.Underline);
                    }

                    if (monospace)
                    {
                        element.TextRunProperties.SetTypeface(new Typeface("Consolas, Monaco, monospace"));
                    }
                });
            }
        }
        catch (ArgumentException)
        {
            // Invalid regex pattern - ignore
        }
    }
}

public static class SyntaxHighlightExtensions
{
    public static bool Contains(this SyntaxFlags flags, SyntaxFlags flag) => (flags & flag) == flag;
}
