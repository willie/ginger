using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Ginger.Views.Dialogs;

public partial class RearrangeActorsDialog : Window
{
    public bool DialogResult { get; private set; }
    public int[]? NewOrder { get; private set; }
    public bool Changed { get; private set; }

    private ObservableCollection<ActorItem> _actors = new();
    private ListBox? _actorsList;
    private bool _ignoreEvents;

    public RearrangeActorsDialog()
    {
        InitializeComponent();
        _actorsList = this.FindControl<ListBox>("ActorsList");
    }

    public void LoadActors(IEnumerable<CharacterData> characters)
    {
        _ignoreEvents = true;
        _actors.Clear();

        int index = 0;
        foreach (var character in characters)
        {
            _actors.Add(new ActorItem
            {
                OriginalIndex = index,
                Name = character.name
            });
            index++;
        }

        if (_actorsList != null)
        {
            _actorsList.ItemsSource = _actors;
            _actorsList.SelectedIndex = -1;
        }

        UpdateButtonStates();
        _ignoreEvents = false;
    }

    private void UpdateButtonStates()
    {
        var moveUpButton = this.FindControl<Button>("MoveUpButton");
        var moveDownButton = this.FindControl<Button>("MoveDownButton");

        int index = _actorsList?.SelectedIndex ?? -1;
        if (moveUpButton != null)
            moveUpButton.IsEnabled = index > 0;
        if (moveDownButton != null)
            moveDownButton.IsEnabled = index >= 0 && index < _actors.Count - 1;
    }

    private void ActorsList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_ignoreEvents)
            return;

        UpdateButtonStates();
    }

    private void MoveUpButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_actorsList == null)
            return;

        int index = _actorsList.SelectedIndex;
        if (index <= 0)
            return;

        _ignoreEvents = true;
        var item = _actors[index];
        _actors.RemoveAt(index);
        _actors.Insert(index - 1, item);
        _actorsList.SelectedIndex = index - 1;
        _ignoreEvents = false;

        UpdateButtonStates();
    }

    private void MoveDownButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_actorsList == null)
            return;

        int index = _actorsList.SelectedIndex;
        if (index < 0 || index >= _actors.Count - 1)
            return;

        _ignoreEvents = true;
        var item = _actors[index];
        _actors.RemoveAt(index);
        _actors.Insert(index + 1, item);
        _actorsList.SelectedIndex = index + 1;
        _ignoreEvents = false;

        UpdateButtonStates();
    }

    private void ConfirmButton_Click(object? sender, RoutedEventArgs e)
    {
        NewOrder = new int[_actors.Count];
        Changed = false;

        for (int i = 0; i < _actors.Count; i++)
        {
            NewOrder[i] = _actors[i].OriginalIndex;
            if (NewOrder[i] != i)
                Changed = true;
        }

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private class ActorItem
    {
        public int OriginalIndex { get; set; }
        public string Name { get; set; } = "";

        public override string ToString() => Name;
    }
}
