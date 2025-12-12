using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace Ginger.Services;

/// <summary>
/// Service for showing dialogs and file pickers.
/// </summary>
public class DialogService
{
    private Window? GetMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow;
        return null;
    }

    /// <summary>
    /// Show a message box dialog.
    /// </summary>
    public async Task<MessageBoxResult> ShowMessageBoxAsync(string title, string message, MessageBoxButtons buttons = MessageBoxButtons.Ok)
    {
        var window = GetMainWindow();
        if (window == null) return MessageBoxResult.Cancel;

        var dialog = new Views.Dialogs.MessageBoxDialog
        {
            Title = title,
            Message = message,
            Buttons = buttons
        };

        return await dialog.ShowDialog<MessageBoxResult>(window);
    }

    /// <summary>
    /// Show the About dialog.
    /// </summary>
    public async Task ShowAboutAsync()
    {
        var window = GetMainWindow();
        if (window == null) return;

        var dialog = new Views.Dialogs.AboutDialog();
        await dialog.ShowDialog(window);
    }

    /// <summary>
    /// Show a file open picker.
    /// </summary>
    public async Task<string?> ShowOpenFileDialogAsync(string title, params FilePickerFileType[] filters)
    {
        var window = GetMainWindow();
        if (window?.StorageProvider == null) return null;

        var options = new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = filters
        };

        var result = await window.StorageProvider.OpenFilePickerAsync(options);
        return result.Count > 0 ? result[0].Path.LocalPath : null;
    }

    /// <summary>
    /// Show a file save picker.
    /// </summary>
    public async Task<string?> ShowSaveFileDialogAsync(string title, string? defaultFileName = null, params FilePickerFileType[] filters)
    {
        var window = GetMainWindow();
        if (window?.StorageProvider == null) return null;

        var options = new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = defaultFileName,
            FileTypeChoices = filters
        };

        var result = await window.StorageProvider.SaveFilePickerAsync(options);
        return result?.Path.LocalPath;
    }

    /// <summary>
    /// Show a folder picker.
    /// </summary>
    public async Task<string?> ShowFolderDialogAsync(string title)
    {
        var window = GetMainWindow();
        if (window?.StorageProvider == null) return null;

        var options = new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        };

        var result = await window.StorageProvider.OpenFolderPickerAsync(options);
        return result.Count > 0 ? result[0].Path.LocalPath : null;
    }

    /// <summary>
    /// Show a confirmation dialog.
    /// </summary>
    public async Task<bool> ConfirmAsync(string title, string message)
    {
        var result = await ShowMessageBoxAsync(title, message, MessageBoxButtons.YesNo);
        return result == MessageBoxResult.Yes;
    }

    /// <summary>
    /// Show the find/replace dialog.
    /// </summary>
    public void ShowFindReplaceDialog(Action<string, string, bool, bool>? onFind = null,
        Action<string, string, bool, bool>? onReplace = null,
        Action<string, string, bool, bool>? onReplaceAll = null)
    {
        var window = GetMainWindow();
        if (window == null) return;

        var dialog = new Views.Dialogs.FindReplaceDialog
        {
            OnFindNext = onFind,
            OnReplace = onReplace,
            OnReplaceAll = onReplaceAll
        };
        dialog.Show(window);
    }

    /// <summary>
    /// Run an async task with a progress dialog.
    /// </summary>
    public async Task<T?> RunWithProgressAsync<T>(
        string title,
        string message,
        Func<CancellationToken, IProgress<(string message, double progress)>, Task<T>> task,
        bool canCancel = true)
    {
        var window = GetMainWindow();
        if (window == null) return default;

        var dialog = new Views.Dialogs.ProgressDialog
        {
            Title = title,
            Message = message,
            CanCancel = canCancel
        };

        // Show dialog non-modal and run task
        _ = dialog.ShowDialog<bool>(window);
        return await dialog.RunAsync(task, canCancel);
    }

    /// <summary>
    /// Run an async task (no return value) with a progress dialog.
    /// </summary>
    public async Task<bool> RunWithProgressAsync(
        string title,
        string message,
        Func<CancellationToken, IProgress<(string message, double progress)>, Task> task,
        bool canCancel = true)
    {
        var window = GetMainWindow();
        if (window == null) return false;

        var dialog = new Views.Dialogs.ProgressDialog
        {
            Title = title,
            Message = message,
            CanCancel = canCancel
        };

        // Show dialog non-modal and run task
        _ = dialog.ShowDialog<bool>(window);
        return await dialog.RunAsync(task, canCancel);
    }
}

/// <summary>
/// Message box button options.
/// </summary>
public enum MessageBoxButtons
{
    Ok,
    OkCancel,
    YesNo,
    YesNoCancel
}

/// <summary>
/// Message box result.
/// </summary>
public enum MessageBoxResult
{
    None,
    Ok,
    Cancel,
    Yes,
    No
}
