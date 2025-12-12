using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Text;

namespace Ginger.Views.Dialogs;

public partial class CreateSnippetDialog : Window
{
    public bool DialogResult { get; private set; }
    public string SnippetName => NameBox.Text ?? "";
    public string SnippetComponent => (ComponentCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Persona";
    public string SnippetContent => ContentBox.Text ?? "";
    public bool SwapPronouns => SwapPronounsCheck.IsChecked ?? false;
    public string? FileName { get; private set; }

    private Generator.OutputWithNodes _output;
    public Generator.OutputWithNodes Output => _output;

    public CreateSnippetDialog()
    {
        InitializeComponent();
        NameBox.Text = "New snippet";
    }

    public CreateSnippetDialog(string defaultContent) : this()
    {
        ContentBox.Text = defaultContent;
    }

    public CreateSnippetDialog(Generator.Output output) : this()
    {
        // Populate from output - take the first non-empty channel
        if (!output.persona.IsNullOrEmpty())
        {
            ContentBox.Text = ToSnippet(output.persona.ToString());
            ComponentCombo.SelectedIndex = 0; // Persona
        }
        else if (!output.scenario.IsNullOrEmpty())
        {
            ContentBox.Text = ToSnippet(output.scenario.ToString());
            ComponentCombo.SelectedIndex = 2; // Scenario
        }
        else if (output.greetings != null && output.greetings.Length > 0 && !output.greetings[0].IsNullOrEmpty())
        {
            ContentBox.Text = ToSnippet(output.greetings[0].ToString());
            ComponentCombo.SelectedIndex = 3; // Greeting
        }
        else if (!output.example.IsNullOrEmpty())
        {
            ContentBox.Text = ToSnippet(output.example.ToString());
            ComponentCombo.SelectedIndex = 4; // Example
        }
        else if (!output.system.IsNullOrEmpty())
        {
            ContentBox.Text = ToSnippet(output.system.ToString());
            ComponentCombo.SelectedIndex = 5; // System Prompt
        }
        else if (!output.system_post_history.IsNullOrEmpty())
        {
            ContentBox.Text = ToSnippet(output.system_post_history.ToString());
            ComponentCombo.SelectedIndex = 6; // Post-History
        }
        else if (!output.userPersona.IsNullOrEmpty())
        {
            ContentBox.Text = ToSnippet(output.userPersona.ToString());
            ComponentCombo.SelectedIndex = 7; // User Persona
        }
        else if (!output.grammar.IsNullOrEmpty())
        {
            ContentBox.Text = ToSnippet(output.grammar.ToString());
            ComponentCombo.SelectedIndex = 8; // Grammar
        }
    }

    private static string ToSnippet(string text)
    {
        StringBuilder sb = new StringBuilder(text);
        GingerString.Unescape(sb);
        GingerString.ConvertNamePlaceholders(sb, null, Current.SelectedCharacter);
        sb.Trim();
        sb.ConvertLinebreaks(Linebreak.CRLF);
        return sb.ToString();
    }

    private void CreateButton_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(SnippetName))
        {
            NameBox.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(SnippetContent))
        {
            ContentBox.Focus();
            return;
        }

        // Parse path
        var pathParts = SnippetName.Split(new[] { '/' }, System.StringSplitOptions.RemoveEmptyEntries);
        var fullName = new StringBuilder();
        foreach (var part in pathParts)
        {
            var trimmed = part.Trim();
            if (trimmed.Length > 0)
            {
                if (fullName.Length > 0)
                    fullName.Append('/');
                fullName.Append(trimmed);
            }
        }

        if (fullName.Length == 0)
            return;

        var filename = Utility.ValidFilename(pathParts[pathParts.Length - 1].Trim());
        FileName = Utility.ContentPath("Snippets", filename + ".snippet");

        // Process content - apply pronoun swap if requested
        string content = SnippetContent;
        if (SwapPronouns)
        {
            GenderSwap.ToNeutralMarkers(ref content);
        }

        // Create output with the content in the selected channel
        _output = new Generator.OutputWithNodes();
        var channel = ParseComponent(SnippetComponent);
        var gingerContent = GingerString.FromString(content);

        switch (channel)
        {
            case Recipe.Component.Persona:
                _output.persona = gingerContent;
                break;
            case Recipe.Component.Scenario:
                _output.scenario = gingerContent;
                break;
            case Recipe.Component.Greeting:
                _output.greetings = new[] { gingerContent };
                break;
            case Recipe.Component.Example:
                _output.example = gingerContent;
                break;
            case Recipe.Component.System:
                _output.system = gingerContent;
                break;
            case Recipe.Component.System_PostHistory:
                _output.system_post_history = gingerContent;
                break;
            case Recipe.Component.UserPersona:
                _output.userPersona = gingerContent;
                break;
            case Recipe.Component.Grammar:
                _output.grammar = gingerContent;
                break;
        }

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
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
            "Post-History" => Recipe.Component.System_PostHistory,
            "User Persona" => Recipe.Component.UserPersona,
            "Grammar" => Recipe.Component.Grammar,
            _ => Recipe.Component.Persona,
        };
    }
}
