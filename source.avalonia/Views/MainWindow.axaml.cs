using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Ginger.Models;
using Ginger.ViewModels;
using Ginger.Views.Controls;

namespace Ginger.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Enable drag-drop
        AddHandler(DragDrop.DropEvent, OnDrop);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);

        // Handle additional keyboard shortcuts
        KeyDown += MainWindow_KeyDown;

        // Subscribe to ViewModel events when DataContext is set
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.FocusMatchRequested += OnFocusMatchRequested;
    }

    private void OnFocusMatchRequested(object? sender, MainViewModel.SearchMatch match)
    {
        // Use dispatcher with delay to ensure UI is fully updated (expanders expanded, controls loaded)
        Dispatcher.UIThread.Post(async () =>
        {
            // Small delay to let expanders animate and content load
            await Task.Delay(100);
            FocusMatch(match);
        }, DispatcherPriority.Background);
    }

    private void FocusMatch(MainViewModel.SearchMatch match)
    {
        switch (match.Location)
        {
            case MainViewModel.SearchLocation.RecipeContent:
                FocusRecipeMatch(match);
                break;
            case MainViewModel.SearchLocation.LorebookContent:
                FocusLorebookMatch(match);
                break;
            case MainViewModel.SearchLocation.Notes:
                FocusNotesMatch(match);
                break;
        }
    }

    private void FocusNotesMatch(MainViewModel.SearchMatch match)
    {
        var notesTextBox = this.FindControl<HighlightedTextBox>("NotesTextBox");
        if (notesTextBox != null)
        {
            notesTextBox.BringIntoView();
            notesTextBox.FocusAndSelect(match.StartPosition, match.Length);
        }
    }

    private void FocusRecipeMatch(MainViewModel.SearchMatch match)
    {
        var recipeList = this.FindControl<ItemsControl>("RecipeList");
        if (recipeList == null) return;

        var container = recipeList.ContainerFromIndex(match.Index);
        if (container == null) return;

        (container as Control)?.BringIntoView();

        // Find the Expander, then find the HighlightedTextBox for Content within it
        var expander = FindDescendant<Expander>(container);
        if (expander == null) return;

        // The Content HighlightedTextBox is a direct child of the Expander's content StackPanel
        var highlightedTextBox = FindDescendant<HighlightedTextBox>(expander);
        if (highlightedTextBox != null)
        {
            highlightedTextBox.BringIntoView();
            highlightedTextBox.FocusAndSelect(match.StartPosition, match.Length);
        }
    }

    private void FocusLorebookMatch(MainViewModel.SearchMatch match)
    {
        var lorebookList = this.FindControl<ItemsControl>("LorebookList");
        if (lorebookList == null) return;

        var container = lorebookList.ContainerFromIndex(match.Index);
        if (container == null) return;

        (container as Control)?.BringIntoView();

        // Find the Expander, then find the HighlightedTextBox for Content within it
        var expander = FindDescendant<Expander>(container);
        if (expander == null) return;

        var highlightedTextBox = FindDescendant<HighlightedTextBox>(expander);
        if (highlightedTextBox != null)
        {
            highlightedTextBox.BringIntoView();
            highlightedTextBox.FocusAndSelect(match.StartPosition, match.Length);
        }
    }

    private static T? FindDescendant<T>(object? parent) where T : class
    {
        if (parent is T match) return match;
        if (parent is not Avalonia.Visual visual) return null;

        foreach (var child in visual.GetVisualChildren())
        {
            var result = FindDescendant<T>(child);
            if (result != null) return result;
        }
        return null;
    }

    private void MainWindow_KeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
            return;

        // Alt+Left: Previous Actor
        if (e.KeyModifiers == KeyModifiers.Alt && e.Key == Key.Left)
        {
            if (vm.SelectedActorIndex > 0)
            {
                vm.SelectedActorIndex--;
                vm.StatusMessage = $"Switched to actor {vm.SelectedActorIndex + 1}";
            }
            e.Handled = true;
        }
        // Alt+Right: Next Actor
        else if (e.KeyModifiers == KeyModifiers.Alt && e.Key == Key.Right)
        {
            if (vm.SelectedActorIndex < Current.Characters.Count - 1)
            {
                vm.SelectedActorIndex++;
                vm.StatusMessage = $"Switched to actor {vm.SelectedActorIndex + 1}";
            }
            e.Handled = true;
        }
        // Ctrl+U, Ctrl+Shift+U, F5 are handled via Window.KeyBindings
        // Ctrl+Tab: Switch view tabs
        else if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.Tab)
        {
            // Cycle through tabs: Recipe -> Output -> Notes -> Lorebook -> Recipe
            if (vm.IsRecipeTabActive)
                vm.ShowOutputTabCommand.Execute(null);
            else if (vm.IsOutputTabActive)
                vm.ShowNotesTabCommand.Execute(null);
            else if (vm.IsNotesTabActive)
                vm.ShowLorebookTabCommand.Execute(null);
            else if (vm.IsLorebookTabActive)
                vm.ShowRecipeTabCommand.Execute(null);
            e.Handled = true;
        }
        // Alt+1-4: Quick tab selection
        else if (e.KeyModifiers == KeyModifiers.Alt)
        {
            switch (e.Key)
            {
                case Key.D1:
                    vm.ShowRecipeTabCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.D2:
                    vm.ShowOutputTabCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.D3:
                    vm.ShowNotesTabCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.D4:
                    vm.ShowLorebookTabCommand.Execute(null);
                    e.Handled = true;
                    break;
            }
        }
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        // Check if we can accept the drop
        if (e.Data.Contains(DataFormats.Files))
        {
            e.DragEffects = DragDropEffects.Copy;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        if (!e.Data.Contains(DataFormats.Files))
            return;

        var files = e.Data.GetFiles()?.ToArray();
        if (files == null || files.Length == 0)
            return;

        var file = files[0];
        if (file is not IStorageFile storageFile)
            return;

        var path = storageFile.Path.LocalPath;
        var ext = Path.GetExtension(path).ToLowerInvariant();

        if (DataContext is not MainViewModel vm)
            return;

        // Check if it's an image file (for portrait)
        string[] imageExtensions = { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp" };
        string[] cardExtensions = { ".png", ".json", ".charx", ".byaf" };

        if (imageExtensions.Contains(ext))
        {
            // Check if PNG might be a character card
            if (ext == ".png")
            {
                try
                {
                    // Try to load as character card first
                    var metadata = Ginger.Models.PngMetadata.ReadTextChunks(path);
                    if (metadata.ContainsKey("chara") || metadata.ContainsKey("ccv3"))
                    {
                        // It's a character card, load it
                        await vm.LoadFileAsync(path);
                        e.Handled = true;
                        return;
                    }
                }
                catch
                {
                    // Not a character card, treat as image
                }
            }

            // Load as portrait image
            try
            {
                var data = await File.ReadAllBytesAsync(path);
                vm.LoadPortraitFromData(data);
            }
            catch (Exception ex)
            {
                vm.StatusMessage = $"Error loading image: {ex.Message}";
            }
        }
        else if (ext == ".json" || ext == ".charx" || ext == ".byaf")
        {
            // Load as character card
            await vm.LoadFileAsync(path);
        }

        e.Handled = true;
    }
}
