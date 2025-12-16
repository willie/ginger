using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Ginger.Models;
using Ginger.ViewModels;

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
        // Use dispatcher to ensure UI is updated before focusing
        Dispatcher.UIThread.Post(() => FocusMatch(match), DispatcherPriority.Background);
    }

    private void FocusMatch(MainViewModel.SearchMatch match)
    {
        var recipeList = this.FindControl<ItemsControl>("RecipeList");
        if (recipeList == null) return;

        // Get recipe container
        var recipeContainer = recipeList.ContainerFromIndex(match.RecipeIndex);
        if (recipeContainer == null) return;

        // Scroll into view
        (recipeContainer as Control)?.BringIntoView();

        // Find the Expander (recipe panel), then parameters ItemsControl, then the specific TextBox
        var expander = FindDescendant<Expander>(recipeContainer);
        if (expander == null) return;

        // Find the parameters ItemsControl within the expander
        var paramsControl = FindDescendant<ItemsControl>(expander);
        if (paramsControl == null) return;

        var paramContainer = paramsControl.ContainerFromIndex(match.ParameterIndex);
        if (paramContainer == null) return;

        var textBox = FindDescendant<TextBox>(paramContainer);
        if (textBox == null) return;

        textBox.BringIntoView();
        textBox.Focus();
        textBox.SelectionStart = match.StartPosition;
        textBox.SelectionEnd = match.StartPosition + match.Length;
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
        // Ctrl+U: Push Changes (Linked Save)
        else if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.U)
        {
            if (vm.PushChangesCommand.CanExecute(null))
                vm.PushChangesCommand.Execute(null);
            e.Handled = true;
        }
        // Ctrl+Shift+U: Pull Changes
        else if (e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift) && e.Key == Key.U)
        {
            if (vm.PullChangesCommand.CanExecute(null))
                vm.PullChangesCommand.Execute(null);
            e.Handled = true;
        }
        // F5 is already mapped via menu InputGesture
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
