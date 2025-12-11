using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace Ginger.Services;

public class FileService : IFileService
{
    private Window? GetMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }
        return null;
    }

    public async Task<string?> OpenFileAsync(string title, string[] filters)
    {
        var window = GetMainWindow();
        if (window == null) return null;

        var storageProvider = window.StorageProvider;

        // Combine all patterns into a single file type for the Open dialog
        var combinedFileType = new FilePickerFileType("Character Cards")
        {
            Patterns = filters
        };

        var options = new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = new[] { combinedFileType }
        };

        var result = await storageProvider.OpenFilePickerAsync(options);
        return result.FirstOrDefault()?.Path.LocalPath;
    }

    public async Task<string?> SaveFileAsync(string title, string defaultName, string[] filters)
    {
        var window = GetMainWindow();
        if (window == null) return null;

        var storageProvider = window.StorageProvider;

        var fileTypes = filters.Select(f => new FilePickerFileType(f)
        {
            Patterns = new[] { f }
        }).ToList();

        var options = new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = defaultName,
            FileTypeChoices = fileTypes
        };

        var result = await storageProvider.SaveFilePickerAsync(options);
        return result?.Path.LocalPath;
    }

    public async Task<string?> SelectFolderAsync(string title)
    {
        var window = GetMainWindow();
        if (window == null) return null;

        var storageProvider = window.StorageProvider;

        var options = new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        };

        var result = await storageProvider.OpenFolderPickerAsync(options);
        return result.FirstOrDefault()?.Path.LocalPath;
    }
}
