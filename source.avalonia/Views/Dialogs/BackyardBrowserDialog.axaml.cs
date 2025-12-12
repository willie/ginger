using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Ginger.Integration;

namespace Ginger.Views.Dialogs;

public partial class BackyardBrowserDialog : Window
{
    public Backyard.GroupInstance? SelectedGroup { get; private set; }
    public Backyard.CharacterInstance? SelectedCharacter { get; private set; }
    public bool WantsLink { get; private set; }
    public bool DialogResult { get; private set; }

    private List<FolderItem> _folders = new();
    private List<CharacterItem> _allCharacters = new();
    private List<CharacterItem> _filteredCharacters = new();
    private Dictionary<string, Backyard.ChatCount> _chatCounts = new();

    public BackyardBrowserDialog()
    {
        InitializeComponent();

        SearchBox.TextChanged += (s, e) => FilterCharacters();
        FolderList.SelectionChanged += (s, e) => FilterCharacters();
        CharacterList.SelectionChanged += (s, e) => UpdateButtons();
        ShowPartiesCheck.IsCheckedChanged += (s, e) => FilterCharacters();
    }

    public bool LoadCharacters()
    {
        if (!Backyard.ConnectionEstablished)
        {
            var error = Backyard.EstablishConnection();
            if (error != Backyard.Error.NoError)
                return false;
        }

        _allCharacters.Clear();
        _folders.Clear();

        // Get chat counts
        BackyardUtil.GetChatCounts(out _chatCounts);

        // Load folders
        _folders.Add(new FolderItem { Name = "All Characters", Id = null });
        foreach (var folder in Backyard.Folders.OrderBy(f => f.name))
        {
            _folders.Add(new FolderItem
            {
                Name = folder.name ?? "Unnamed Folder",
                Id = folder.instanceId
            });
        }

        FolderList.ItemsSource = _folders.Select(f => f.Name).ToList();
        FolderList.SelectedIndex = 0;

        // Load groups (which contain characters)
        foreach (var group in Backyard.Groups.OrderByDescending(g => g.updateDate))
        {
            var groupType = group.GetGroupType();
            string displayName = group.GetDisplayName();
            string creator = group.hubAuthorUsername;

            Backyard.ChatCount chatCount = default;
            _chatCounts.TryGetValue(group.instanceId, out chatCount);

            _allCharacters.Add(new CharacterItem
            {
                Group = group,
                DisplayName = displayName,
                FolderId = group.folderId,
                IsParty = groupType == Backyard.GroupInstance.GroupType.Party,
                Creator = creator,
                ChatCount = chatCount.count,
                LastChat = chatCount.lastMessage,
                UpdateDate = group.updateDate
            });
        }

        FilterCharacters();
        UpdateStatus();
        return true;
    }

    private void FilterCharacters()
    {
        string searchText = SearchBox.Text?.ToLowerInvariant() ?? "";
        string selectedFolder = FolderList.SelectedItem as string ?? "All Characters";
        bool showParties = ShowPartiesCheck.IsChecked == true;

        var selectedFolderItem = _folders.FirstOrDefault(f => f.Name == selectedFolder);
        string? folderId = selectedFolderItem?.Id;

        _filteredCharacters = _allCharacters
            .Where(c =>
            {
                // Folder filter
                if (folderId != null && c.FolderId != folderId)
                    return false;

                // Party filter
                if (!showParties && c.IsParty)
                    return false;

                // Search filter
                if (!string.IsNullOrEmpty(searchText))
                {
                    return c.DisplayName.ToLowerInvariant().Contains(searchText) ||
                           (c.Creator?.ToLowerInvariant().Contains(searchText) ?? false);
                }

                return true;
            })
            .ToList();

        CharacterList.ItemsSource = _filteredCharacters;
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        StatusText.Text = $"{_filteredCharacters.Count} of {_allCharacters.Count} characters";
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        bool hasSelection = CharacterList.SelectedItem != null;
        ImportButton.IsEnabled = hasSelection;
        LinkButton.IsEnabled = hasSelection;
    }

    private void CharacterList_DoubleTapped(object? sender, TappedEventArgs e)
    {
        SelectAndImport();
    }

    private void ImportButton_Click(object? sender, RoutedEventArgs e)
    {
        SelectAndImport();
    }

    private void LinkButton_Click(object? sender, RoutedEventArgs e)
    {
        if (CharacterList.SelectedItem is CharacterItem item)
        {
            SelectedGroup = item.Group;

            // Get the first character from the group
            if (item.Group.activeMembers?.Length > 0)
            {
                var firstCharId = item.Group.activeMembers
                    .Select(id => Backyard.Database.GetCharacter(id))
                    .FirstOrDefault(c => c.isCharacter);
                if (firstCharId.isDefined)
                    SelectedCharacter = firstCharId;
            }

            WantsLink = true;
            DialogResult = true;
            Close();
        }
    }

    private void SelectAndImport()
    {
        if (CharacterList.SelectedItem is CharacterItem item)
        {
            SelectedGroup = item.Group;

            // Get the first character from the group
            if (item.Group.activeMembers?.Length > 0)
            {
                var firstCharId = item.Group.activeMembers
                    .Select(id => Backyard.Database.GetCharacter(id))
                    .FirstOrDefault(c => c.isCharacter);
                if (firstCharId.isDefined)
                    SelectedCharacter = firstCharId;
            }

            WantsLink = false;
            DialogResult = true;
            Close();
        }
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private class FolderItem
    {
        public string Name { get; set; } = "";
        public string? Id { get; set; }
    }

    private class CharacterItem
    {
        public Backyard.GroupInstance Group { get; set; }
        public string DisplayName { get; set; } = "";
        public string? FolderId { get; set; }
        public bool IsParty { get; set; }
        public string? Creator { get; set; }
        public int ChatCount { get; set; }
        public DateTime LastChat { get; set; }
        public DateTime UpdateDate { get; set; }

        public string TypeLabel => IsParty ? "Group" : "Character";
        public bool HasChatCount => ChatCount > 0;
        public string ChatCountLabel => ChatCount > 0 ? $"{ChatCount} chat{(ChatCount != 1 ? "s" : "")}" : "";
        public bool HasCreator => !string.IsNullOrEmpty(Creator);
        public string CreatorLabel => HasCreator ? $"by {Creator}" : "";
    }
}
