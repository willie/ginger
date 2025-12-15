using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Ginger.Views.Dialogs;

public partial class CreateSnippetDialog : Window
{
    public bool DialogResult { get; private set; }
    public string SnippetName => NameBox.Text ?? "";
    public bool SwapPronouns => SwapPronounsCheck.IsChecked ?? false;
    public string? FileName { get; private set; }

    private Generator.OutputWithNodes _output = new();
    public Generator.OutputWithNodes Output => _output;

    private ObservableCollection<SnippetChannelItem> _channels = new();

    public CreateSnippetDialog()
    {
        InitializeComponent();
        NameBox.Text = "New snippet";

        var channelsList = this.FindControl<ItemsControl>("ChannelsList");
        if (channelsList != null)
            channelsList.ItemsSource = _channels;
    }

    public CreateSnippetDialog(string defaultContent) : this()
    {
        // Single content mode - add as Persona channel
        _channels.Add(new SnippetChannelItem(Recipe.Component.Persona, defaultContent));
    }

    public CreateSnippetDialog(Generator.Output output) : this()
    {
        // Multi-channel mode - populate from output
        PopulateFromOutput(output);
    }

    private void PopulateFromOutput(Generator.Output output)
    {
        var channels = new Recipe.Component[]
        {
            Recipe.Component.System,
            Recipe.Component.System_PostHistory,
            Recipe.Component.Persona,
            Recipe.Component.UserPersona,
            Recipe.Component.Scenario,
            Recipe.Component.Greeting,
            Recipe.Component.Greeting_Group,
            Recipe.Component.Example,
            Recipe.Component.Grammar,
        };

        bool hasAnyContent = false;

        foreach (var channel in channels)
        {
            if (channel == Recipe.Component.Greeting && output.greetings != null)
            {
                for (int i = 0; i < output.greetings.Length; i++)
                {
                    string text = output.greetings[i].ToString();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        _channels.Add(new SnippetChannelItem(channel, ToSnippet(text, channel)));
                        hasAnyContent = true;
                    }
                }
            }
            else if (channel == Recipe.Component.Greeting_Group && output.group_greetings != null)
            {
                for (int i = 0; i < output.group_greetings.Length; i++)
                {
                    string text = output.group_greetings[i].ToString();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        _channels.Add(new SnippetChannelItem(channel, ToSnippet(text, channel)));
                        hasAnyContent = true;
                    }
                }
            }
            else
            {
                string text = output.GetText(channel).ToString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    _channels.Add(new SnippetChannelItem(channel, ToSnippet(text, channel)));
                    hasAnyContent = true;
                }
            }
        }

        // If no content, add empty Persona panel
        if (!hasAnyContent)
        {
            _channels.Add(new SnippetChannelItem(Recipe.Component.Persona, ""));
        }
    }

    private static string ToSnippet(string text, Recipe.Component channel)
    {
        StringBuilder sb = new StringBuilder(text);
        GingerString.Unescape(sb);
        GingerString.ConvertNamePlaceholders(sb, null, Current.SelectedCharacter);
        sb.Trim();
        sb.ConvertLinebreaks(Linebreak.CRLF);

        // Convert text style for certain channels
        if (channel == Recipe.Component.Greeting ||
            channel == Recipe.Component.Greeting_Group ||
            channel == Recipe.Component.Example)
        {
            return TextStyleConverter.Convert(sb.ToString(), CardData.TextStyle.Mixed);
        }

        return sb.ToString();
    }

    private void CreateButton_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(SnippetName))
        {
            NameBox.Focus();
            return;
        }

        // Collect enabled channels with content
        var outputByChannel = new Dictionary<Recipe.Component, string>();
        var greetings = new List<string>();
        var groupGreetings = new List<string>();

        foreach (var channel in _channels)
        {
            if (!channel.IsEnabled)
                continue;

            string content = channel.Content?.Trim() ?? "";
            if (string.IsNullOrEmpty(content))
                continue;

            // Apply pronoun swap if requested
            if (SwapPronouns)
            {
                GenderSwap.ToNeutralMarkers(ref content);
            }

            if (channel.Channel == Recipe.Component.Greeting)
                greetings.Add(content);
            else if (channel.Channel == Recipe.Component.Greeting_Group)
                groupGreetings.Add(content);
            else
                outputByChannel.TryAdd(channel.Channel, content);
        }

        if (outputByChannel.Count == 0 && greetings.Count == 0 && groupGreetings.Count == 0)
        {
            // No content
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

        // Build output
        _output = new Generator.OutputWithNodes
        {
            system = GingerString.FromString(outputByChannel.GetValueOrDefault(Recipe.Component.System)),
            system_post_history = GingerString.FromString(outputByChannel.GetValueOrDefault(Recipe.Component.System_PostHistory)),
            persona = GingerString.FromString(outputByChannel.GetValueOrDefault(Recipe.Component.Persona)),
            userPersona = GingerString.FromString(outputByChannel.GetValueOrDefault(Recipe.Component.UserPersona)),
            scenario = GingerString.FromString(outputByChannel.GetValueOrDefault(Recipe.Component.Scenario)),
            example = GingerString.FromString(outputByChannel.GetValueOrDefault(Recipe.Component.Example)),
            grammar = GingerString.FromString(outputByChannel.GetValueOrDefault(Recipe.Component.Grammar)),
            greetings = greetings.Select(g => GingerString.FromString(g)).ToArray(),
            group_greetings = groupGreetings.Select(g => GingerString.FromString(g)).ToArray(),
        };

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

/// <summary>
/// Represents a single channel in the multi-channel snippet dialog.
/// </summary>
public partial class SnippetChannelItem : ObservableObject
{
    public Recipe.Component Channel { get; }
    public string ChannelName { get; }
    public IBrush ChannelColor { get; }
    public IBrush HeaderForeground { get; }

    [ObservableProperty]
    private bool _isEnabled = true;

    [ObservableProperty]
    private string _content = "";

    public SnippetChannelItem(Recipe.Component channel, string content)
    {
        Channel = channel;
        _content = content;

        ChannelName = channel switch
        {
            Recipe.Component.System => "Model Instructions",
            Recipe.Component.System_PostHistory => "Model Instructions (Important)",
            Recipe.Component.Persona => "Persona",
            Recipe.Component.UserPersona => "User Persona",
            Recipe.Component.Scenario => "Scenario",
            Recipe.Component.Greeting => "Greeting",
            Recipe.Component.Greeting_Group => "Greeting (Group)",
            Recipe.Component.Example => "Example Messages",
            Recipe.Component.Grammar => "Grammar",
            _ => "Text"
        };

        // Channel colors matching the original
        var color = channel switch
        {
            Recipe.Component.System => Color.FromRgb(200, 220, 255),          // Blue-ish (Model)
            Recipe.Component.System_PostHistory => Color.FromRgb(200, 220, 255),
            Recipe.Component.Persona => Color.FromRgb(255, 220, 200),          // Orange-ish (Character)
            Recipe.Component.UserPersona => Color.FromRgb(240, 255, 255),      // Azure
            Recipe.Component.Scenario => Color.FromRgb(220, 255, 220),         // Green-ish (Story)
            Recipe.Component.Greeting => Color.FromRgb(255, 255, 200),         // Yellow-ish (Chat)
            Recipe.Component.Greeting_Group => Color.FromRgb(255, 255, 200),
            Recipe.Component.Example => Color.FromRgb(255, 255, 200),
            Recipe.Component.Grammar => Color.FromRgb(200, 220, 255),
            _ => Color.FromRgb(230, 230, 230)
        };

        ChannelColor = new SolidColorBrush(color);
        HeaderForeground = new SolidColorBrush(Colors.Black);
    }
}
