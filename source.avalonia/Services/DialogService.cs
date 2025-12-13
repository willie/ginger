using System;
using System.Collections.Generic;
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
    /// Show a confirmation dialog with Yes/No buttons.
    /// </summary>
    public async Task<bool> ShowConfirmationDialogAsync(string title, string message)
    {
        var result = await ShowMessageBoxAsync(title, message, MessageBoxButtons.YesNo);
        return result == MessageBoxResult.Yes;
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
    /// Show the gender swap dialog.
    /// </summary>
    public async Task<(bool success, GenderSwap.Pronouns charFrom, GenderSwap.Pronouns charTo,
        GenderSwap.Pronouns userFrom, GenderSwap.Pronouns userTo, bool swapChar, bool swapUser)>
        ShowGenderSwapDialogAsync()
    {
        var window = GetMainWindow();
        if (window == null)
            return (false, default, default, default, default, false, false);

        var dialog = new Views.Dialogs.GenderSwapDialog();
        await dialog.ShowDialog(window);

        return (dialog.DialogResult, dialog.CharacterFrom, dialog.CharacterTo,
            dialog.UserFrom, dialog.UserTo, dialog.SwapCharacter, dialog.SwapUser);
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

    /// <summary>
    /// Show the recipe browser dialog.
    /// </summary>
    public async Task<Recipe?> ShowRecipeBrowserAsync(IEnumerable<Recipe> recipes)
    {
        var window = GetMainWindow();
        if (window == null) return null;

        var dialog = new Views.Dialogs.RecipeBrowserDialog();
        dialog.LoadRecipes(recipes);
        await dialog.ShowDialog(window);

        return dialog.DialogResult ? dialog.SelectedRecipe : null;
    }

    /// <summary>
    /// Show the Backyard character browser dialog.
    /// </summary>
    public async Task<(bool success, Integration.Backyard.GroupInstance? group,
        Integration.Backyard.CharacterInstance? character, bool wantsLink)> ShowBackyardBrowserAsync()
    {
        var window = GetMainWindow();
        if (window == null)
            return (false, null, null, false);

        var dialog = new Views.Dialogs.BackyardBrowserDialog();
        if (!dialog.LoadCharacters())
        {
            await ShowMessageBoxAsync("Connection Error",
                "Could not connect to Backyard AI. Please ensure Backyard AI is installed and has been run at least once.",
                MessageBoxButtons.Ok);
            return (false, null, null, false);
        }

        await dialog.ShowDialog(window);

        return (dialog.DialogResult, dialog.SelectedGroup, dialog.SelectedCharacter, dialog.WantsLink);
    }

    /// <summary>
    /// Show a text input dialog.
    /// </summary>
    public async Task<(bool success, string value)> ShowEnterNameDialogAsync(string title, string prompt, string defaultValue = "")
    {
        var window = GetMainWindow();
        if (window == null)
            return (false, "");

        var dialog = new Views.Dialogs.EnterNameDialog(title, prompt, defaultValue);
        await dialog.ShowDialog(window);

        return (dialog.DialogResult, dialog.EnteredName);
    }

    /// <summary>
    /// Show the create recipe dialog.
    /// </summary>
    public async Task<(bool success, string name, Recipe.Category category, Recipe.Component component, string description, string content)> ShowCreateRecipeDialogAsync(string? defaultContent = null)
    {
        var window = GetMainWindow();
        if (window == null)
            return (false, "", default, default, "", "");

        var dialog = defaultContent != null
            ? new Views.Dialogs.CreateRecipeDialog(defaultContent)
            : new Views.Dialogs.CreateRecipeDialog();
        await dialog.ShowDialog(window);

        return (dialog.DialogResult, dialog.RecipeName, dialog.Category, dialog.Component,
            dialog.RecipeDescription, dialog.RecipeContent);
    }

    /// <summary>
    /// Show the create snippet dialog.
    /// </summary>
    public async Task<(bool success, string? filename, Generator.OutputWithNodes output)> ShowCreateSnippetDialogAsync(Generator.Output? sourceOutput = null)
    {
        var window = GetMainWindow();
        if (window == null)
            return (false, null, default);

        var dialog = sourceOutput != null
            ? new Views.Dialogs.CreateSnippetDialog(sourceOutput.Value)
            : new Views.Dialogs.CreateSnippetDialog();
        await dialog.ShowDialog(window);

        return (dialog.DialogResult, dialog.FileName, dialog.Output);
    }

    /// <summary>
    /// Show the file format dialog for export format selection.
    /// </summary>
    public async Task<(bool success, Views.Dialogs.FileFormatDialog.ExportFormat format)> ShowFileFormatDialogAsync(
        Views.Dialogs.FileFormatDialog.ExportFormat defaultFormat = Views.Dialogs.FileFormatDialog.ExportFormat.Png)
    {
        var window = GetMainWindow();
        if (window == null)
            return (false, default);

        var dialog = new Views.Dialogs.FileFormatDialog(defaultFormat);
        await dialog.ShowDialog(window);

        return (dialog.DialogResult, dialog.SelectedFormat);
    }

    /// <summary>
    /// Show the variables editor dialog.
    /// </summary>
    public async Task<(bool success, List<CustomVariable> variables)> ShowVariablesDialogAsync(IEnumerable<CustomVariable> currentVariables)
    {
        var window = GetMainWindow();
        if (window == null)
            return (false, null);

        var dialog = new Views.Dialogs.VariablesDialog();
        dialog.LoadVariables(currentVariables);
        await dialog.ShowDialog(window);

        if (dialog.DialogResult)
            return (true, dialog.GetVariables());
        return (false, null);
    }

    /// <summary>
    /// Show the rearrange actors dialog.
    /// </summary>
    public async Task<(bool success, int[]? newOrder, bool changed)> ShowRearrangeActorsDialogAsync(IEnumerable<CharacterData> characters)
    {
        var window = GetMainWindow();
        if (window == null)
            return (false, null, false);

        var dialog = new Views.Dialogs.RearrangeActorsDialog();
        dialog.LoadActors(characters);
        await dialog.ShowDialog(window);

        return (dialog.DialogResult, dialog.NewOrder, dialog.Changed);
    }

    /// <summary>
    /// Show the asset view dialog.
    /// </summary>
    public async Task<(bool success, AssetCollection? assets, bool changed)> ShowAssetViewDialogAsync(AssetCollection currentAssets)
    {
        var window = GetMainWindow();
        if (window == null)
            return (false, null, false);

        var dialog = new Views.Dialogs.AssetViewDialog();
        dialog.LoadAssets(currentAssets);
        await dialog.ShowDialog(window);

        if (dialog.DialogResult)
            return (true, dialog.Assets, dialog.Changed);
        return (false, null, false);
    }

    /// <summary>
    /// Show a URL input dialog.
    /// </summary>
    public async Task<(bool success, string url)> ShowEnterUrlDialogAsync(string title = "Enter URL", string prompt = "Enter the URL:", string defaultUrl = "")
    {
        var window = GetMainWindow();
        if (window == null)
            return (false, "");

        var dialog = new Views.Dialogs.EnterUrlDialog(title, prompt);
        if (!string.IsNullOrEmpty(defaultUrl))
            dialog.SetDefaultUrl(defaultUrl);

        await dialog.ShowDialog(window);

        return (dialog.DialogResult, dialog.EnteredUrl);
    }

    /// <summary>
    /// Show a paste text dialog for importing character data.
    /// </summary>
    public async Task<(bool success, string text)> ShowPasteTextDialogAsync(string? initialText = null)
    {
        var window = GetMainWindow();
        if (window == null)
            return (false, "");

        var dialog = new Views.Dialogs.PasteTextDialog();
        if (!string.IsNullOrEmpty(initialText))
            dialog.SetContent(initialText);

        await dialog.ShowDialog(window);

        return (dialog.DialogResult, dialog.PastedText);
    }

    /// <summary>
    /// Show the extended write/edit dialog.
    /// </summary>
    public async Task<(bool success, string text)> ShowWriteDialogAsync(string initialText = "", string? title = null)
    {
        var window = GetMainWindow();
        if (window == null)
            return (false, "");

        var dialog = new Views.Dialogs.WriteDialog();
        if (!string.IsNullOrEmpty(title))
            dialog.Title = title;
        dialog.Value = initialText;

        await dialog.ShowDialog(window);

        return (dialog.DialogResult, dialog.Value);
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
