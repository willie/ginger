using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Ginger.Integration;

namespace Ginger.Views.Dialogs;

public partial class LinkEditChatDialog : Window
{
    private string? _groupId;
    private Backyard.ChatInstance[]? _chats;
    private readonly IBrush _userBrush;
    private readonly IBrush _characterBrush;

    public LinkEditChatDialog()
    {
        InitializeComponent();

        // Set up message backgrounds
        _userBrush = new SolidColorBrush(Color.FromArgb(40, 100, 149, 237)); // Cornflower blue
        _characterBrush = new SolidColorBrush(Color.FromArgb(40, 144, 238, 144)); // Light green
    }

    public void SetGroupId(string groupId, string characterName)
    {
        _groupId = groupId;
        CharacterName.Text = characterName;
        LoadChats();
    }

    private void LoadChats()
    {
        if (string.IsNullOrEmpty(_groupId) || !Backyard.ConnectionEstablished)
        {
            ChatCount.Text = "Not connected";
            return;
        }

        var error = Backyard.Database.GetChats(_groupId, out _chats);
        if (error != Backyard.Error.NoError)
        {
            ChatCount.Text = $"Error: {error}";
            return;
        }

        ChatList.Items.Clear();
        if (_chats != null)
        {
            foreach (var chat in _chats)
            {
                var panel = new StackPanel { Margin = new Thickness(4) };
                panel.Children.Add(new TextBlock { Text = chat.name ?? "Untitled", FontWeight = Avalonia.Media.FontWeight.SemiBold });
                panel.Children.Add(new TextBlock { Text = $"{chat.history?.count ?? 0} messages", FontSize = 11, Opacity = 0.7 });
                panel.Children.Add(new TextBlock { Text = chat.updateDate.ToString("g"), FontSize = 10, Opacity = 0.5 });
                panel.Tag = chat.instanceId;
                ChatList.Items.Add(panel);
            }
        }

        ChatCount.Text = $"{_chats?.Length ?? 0} chat(s)";

        if (ChatList.Items.Count > 0)
        {
            ChatList.SelectedIndex = 0;
        }
    }

    private void ChatList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ChatList.SelectedItem is StackPanel panel && panel.Tag is string chatId)
        {
            LoadMessages(chatId);
            ExportButton.IsEnabled = true;
        }
        else
        {
            MessageList.Children.Clear();
            SelectedChatName.Text = "Select a chat";
            ExportButton.IsEnabled = false;
        }
    }

    private void LoadMessages(string? chatId)
    {
        if (string.IsNullOrEmpty(chatId) || _chats == null)
            return;

        var chat = _chats.FirstOrDefault(c => c.instanceId == chatId);
        if (chat == null || chat.history == null)
        {
            SelectedChatName.Text = "No messages";
            MessageList.Children.Clear();
            return;
        }

        SelectedChatName.Text = chat.name ?? "Untitled";

        // Get participant names for speaker display
        var participants = new Dictionary<int, string>();
        if (chat.participants != null)
        {
            for (int i = 0; i < chat.participants.Length; i++)
            {
                var charInfo = Backyard.Database.GetCharacter(chat.participants[i]);
                participants[i] = charInfo.name ?? (i == 0 ? "User" : "Character");
            }
        }

        MessageList.Children.Clear();
        if (chat.history.messages != null)
        {
            foreach (var msg in chat.history.messages)
            {
                var background = msg.speaker == 0 ? _userBrush : _characterBrush;
                var border = new Border
                {
                    Margin = new Thickness(4, 4, 4, 8),
                    Padding = new Thickness(12, 8),
                    CornerRadius = new CornerRadius(8),
                    Background = background
                };

                var content = new StackPanel();

                var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
                header.Children.Add(new TextBlock
                {
                    Text = GetSpeakerName(msg.speaker, participants),
                    FontWeight = Avalonia.Media.FontWeight.SemiBold,
                    FontSize = 12
                });
                var timestamp = new TextBlock
                {
                    Text = msg.creationDate.ToString("g"),
                    FontSize = 10,
                    Opacity = 0.5
                };
                Grid.SetColumn(timestamp, 1);
                header.Children.Add(timestamp);
                content.Children.Add(header);

                content.Children.Add(new TextBlock
                {
                    Text = msg.text ?? "(empty)",
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    Margin = new Thickness(0, 4, 0, 0)
                });

                if (msg.swipes != null && msg.swipes.Length > 1)
                {
                    content.Children.Add(new TextBlock
                    {
                        Text = $"Swipe {msg.activeSwipe + 1} of {msg.swipes.Length}",
                        FontSize = 10,
                        Opacity = 0.5,
                        Margin = new Thickness(0, 4, 0, 0)
                    });
                }

                border.Child = content;
                MessageList.Children.Add(border);
            }
        }
    }

    private static string GetSpeakerName(int speaker, Dictionary<int, string> participants)
    {
        if (participants.TryGetValue(speaker, out var name))
            return name;
        return speaker == 0 ? "User" : $"Character {speaker}";
    }

    private void Refresh_Click(object? sender, RoutedEventArgs e)
    {
        LoadChats();
    }

    private async void ExportChat_Click(object? sender, RoutedEventArgs e)
    {
        if (ChatList.SelectedItem is not StackPanel panel || panel.Tag is not string chatId || _chats == null)
            return;

        var chat = _chats.FirstOrDefault(c => c.instanceId == chatId);
        if (chat?.history == null)
            return;

        var dialog = new SaveFileDialog
        {
            Title = "Export Chat",
            DefaultExtension = "json",
            Filters = new List<FileDialogFilter>
            {
                new() { Name = "Ginger Chat (*.json)", Extensions = { "json" } },
                new() { Name = "Backyard Chat Backup (*.json)", Extensions = { "json" } },
                new() { Name = "Text File (*.txt)", Extensions = { "txt" } }
            }
        };

        var result = await dialog.ShowAsync(this);
        if (string.IsNullOrEmpty(result))
            return;

        try
        {
            // Get participant names
            var participantNames = new List<string>();
            if (chat.participants != null)
            {
                foreach (var participantId in chat.participants)
                {
                    var charInfo = Backyard.Database.GetCharacter(participantId);
                    participantNames.Add(charInfo.name ?? "Unknown");
                }
            }
            if (participantNames.Count == 0)
            {
                participantNames.Add("User");
                participantNames.Add("Character");
            }

            var participants = new Dictionary<int, string>();
            for (int i = 0; i < participantNames.Count; i++)
            {
                participants[i] = participantNames[i];
            }

            string content;
            var ext = Path.GetExtension(result).ToLowerInvariant();

            if (ext == ".json")
            {
                // Determine format based on filter index or filename
                if (result.Contains("backup", StringComparison.OrdinalIgnoreCase))
                {
                    // Export as Backyard Chat Backup V2
                    var backupChat = new Models.Formats.ChatLogs.BackupData.Chat
                    {
                        name = chat.name ?? "Chat",
                        creationDate = chat.creationDate,
                        updateDate = chat.updateDate,
                        participants = participantNames.ToArray(),
                        history = ConvertToHistory(chat.history)
                    };
                    var backup = Models.Formats.ChatLogs.BackyardChatBackupV2.FromBackupChat(backupChat);
                    content = backup.ToJson() ?? "";
                }
                else
                {
                    // Export as Ginger Chat V2
                    var gingerChat = Models.Formats.ChatLogs.GingerChatV2.FromBackyardChat(chat, participantNames.ToArray());
                    content = gingerChat.ToJson() ?? "";
                }
            }
            else
            {
                // Plain text export
                var sb = new StringBuilder();
                sb.AppendLine($"Chat: {chat.name}");
                sb.AppendLine($"Exported: {DateTime.Now:g}");
                sb.AppendLine(new string('-', 40));
                sb.AppendLine();

                if (chat.history.messages != null)
                {
                    foreach (var msg in chat.history.messages)
                    {
                        var speaker = GetSpeakerName(msg.speaker, participants);
                        sb.AppendLine($"[{msg.creationDate:g}] {speaker}:");
                        sb.AppendLine(msg.text);
                        sb.AppendLine();
                    }
                }
                content = sb.ToString();
            }

            await File.WriteAllTextAsync(result, content);
        }
        catch (Exception ex)
        {
            var msgBox = new MessageBoxDialog
            {
                Title = "Export Error",
                Message = $"Failed to export chat: {ex.Message}"
            };
            await msgBox.ShowDialog(this);
        }
    }

    private static Models.Formats.ChatLogs.ChatHistory ConvertToHistory(Ginger.Integration.ChatHistory backyardHistory)
    {
        if (backyardHistory?.messages == null)
            return new Models.Formats.ChatLogs.ChatHistory();

        var messages = backyardHistory.messages.Select(m => new Models.Formats.ChatLogs.ChatHistory.Message
        {
            speaker = m.speaker,
            creationDate = m.creationDate,
            updateDate = m.updateDate,
            activeSwipe = m.activeSwipe,
            swipes = m.swipes ?? Array.Empty<string>()
        }).ToArray();

        return new Models.Formats.ChatLogs.ChatHistory
        {
            name = backyardHistory.name ?? "Chat",
            messages = messages
        };
    }

    private void Close_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
