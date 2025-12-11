using System;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
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
