using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Ginger.Services;

namespace Ginger.Views.Dialogs;

public partial class WriteDialog : Window
{
    private bool _hasChanges;
    private string _originalText = "";
    private SpellCheckService? _spellCheckService;
    private string? _lastCheckedWord;

    public bool DialogResult { get; private set; }

    public string Value
    {
        get => TextEditor.Text ?? "";
        set
        {
            _originalText = value ?? "";
            TextEditor.Text = _originalText;
            _hasChanges = false;
        }
    }

    public WriteDialog()
    {
        InitializeComponent();

        TextEditor.TextChanged += TextEditor_TextChanged;
        TextEditor.PropertyChanged += TextEditor_PropertyChanged;

        // Initialize word wrap checkbox state
        WordWrapCheck.IsChecked = true;
        UpdateCharCount();

        // Initialize spell check
        InitializeSpellCheck();

        // Set up context menu for spell check
        SetupSpellCheckContextMenu();
    }

    private async void InitializeSpellCheck()
    {
        _spellCheckService = new SpellCheckService();

        // Try to find dictionary files
        var dicPaths = new[]
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Dictionaries"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "Dictionaries"),
            "Dictionaries"
        };

        foreach (var path in dicPaths)
        {
            if (Directory.Exists(path))
            {
                var loaded = await _spellCheckService.LoadDictionaryFromDirectoryAsync(path, "en_US");
                if (loaded)
                {
                    StatusText.Text = "Spell check enabled";
                    return;
                }
            }
        }
    }

    private void SetupSpellCheckContextMenu()
    {
        var contextMenu = new ContextMenu();
        TextEditor.ContextMenu = contextMenu;
        TextEditor.ContextRequested += TextEditor_ContextRequested;
    }

    private void TextEditor_ContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        if (TextEditor.ContextMenu is not ContextMenu contextMenu)
            return;

        contextMenu.Items.Clear();

        // Get word at cursor
        var word = GetWordAtCursor();
        _lastCheckedWord = word;

        if (!string.IsNullOrEmpty(word) && _spellCheckService?.IsLoaded == true)
        {
            if (!_spellCheckService.CheckWord(word))
            {
                // Word is misspelled - show suggestions
                var suggestions = _spellCheckService.GetSuggestions(word).Take(5).ToList();

                if (suggestions.Count > 0)
                {
                    foreach (var suggestion in suggestions)
                    {
                        var suggestionItem = new MenuItem { Header = suggestion };
                        suggestionItem.Click += (s, args) => ReplaceWordAtCursor(suggestion);
                        contextMenu.Items.Add(suggestionItem);
                    }
                    contextMenu.Items.Add(new Separator());
                }

                // Add "Ignore" option
                var ignoreItem = new MenuItem { Header = $"Ignore \"{word}\"" };
                ignoreItem.Click += (s, args) =>
                {
                    _spellCheckService.IgnoreWord(word);
                    StatusText.Text = $"Ignoring \"{word}\"";
                };
                contextMenu.Items.Add(ignoreItem);

                // Add "Add to Dictionary" option
                var addItem = new MenuItem { Header = $"Add \"{word}\" to Dictionary" };
                addItem.Click += (s, args) =>
                {
                    _spellCheckService.AddToCustomDictionary(word);
                    StatusText.Text = $"Added \"{word}\" to dictionary";
                };
                contextMenu.Items.Add(addItem);

                contextMenu.Items.Add(new Separator());
            }
        }

        // Standard edit items
        var undoItem = new MenuItem { Header = "Undo", InputGesture = KeyGesture.Parse("Ctrl+Z") };
        undoItem.Click += (s, args) => TextEditor.Undo();
        contextMenu.Items.Add(undoItem);

        var redoItem = new MenuItem { Header = "Redo", InputGesture = KeyGesture.Parse("Ctrl+Y") };
        redoItem.Click += (s, args) => TextEditor.Redo();
        contextMenu.Items.Add(redoItem);

        contextMenu.Items.Add(new Separator());

        var cutItem = new MenuItem { Header = "Cut", InputGesture = KeyGesture.Parse("Ctrl+X") };
        cutItem.Click += (s, args) => TextEditor.Cut();
        contextMenu.Items.Add(cutItem);

        var copyItem = new MenuItem { Header = "Copy", InputGesture = KeyGesture.Parse("Ctrl+C") };
        copyItem.Click += (s, args) => TextEditor.Copy();
        contextMenu.Items.Add(copyItem);

        var pasteItem = new MenuItem { Header = "Paste", InputGesture = KeyGesture.Parse("Ctrl+V") };
        pasteItem.Click += (s, args) => TextEditor.Paste();
        contextMenu.Items.Add(pasteItem);

        contextMenu.Items.Add(new Separator());

        var selectAllItem = new MenuItem { Header = "Select All", InputGesture = KeyGesture.Parse("Ctrl+A") };
        selectAllItem.Click += (s, args) => TextEditor.SelectAll();
        contextMenu.Items.Add(selectAllItem);
    }

    private string? GetWordAtCursor()
    {
        var text = TextEditor.Text;
        var caretIndex = TextEditor.CaretIndex;

        if (string.IsNullOrEmpty(text) || caretIndex < 0 || caretIndex > text.Length)
            return null;

        // Find word boundaries
        int start = caretIndex;
        int end = caretIndex;

        // Move start back to beginning of word
        while (start > 0 && char.IsLetter(text[start - 1]))
            start--;

        // Move end forward to end of word
        while (end < text.Length && char.IsLetter(text[end]))
            end++;

        if (start == end)
            return null;

        return text.Substring(start, end - start);
    }

    private void ReplaceWordAtCursor(string replacement)
    {
        var text = TextEditor.Text;
        var caretIndex = TextEditor.CaretIndex;

        if (string.IsNullOrEmpty(text) || caretIndex < 0 || caretIndex > text.Length)
            return;

        // Find word boundaries
        int start = caretIndex;
        int end = caretIndex;

        while (start > 0 && char.IsLetter(text[start - 1]))
            start--;

        while (end < text.Length && char.IsLetter(text[end]))
            end++;

        if (start == end)
            return;

        // Replace the word
        TextEditor.Text = text.Substring(0, start) + replacement + text.Substring(end);
        TextEditor.CaretIndex = start + replacement.Length;
    }

    private void TextEditor_TextChanged(object? sender, TextChangedEventArgs e)
    {
        _hasChanges = TextEditor.Text != _originalText;
        UpdateCharCount();
    }

    private void TextEditor_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property.Name == nameof(TextBox.Text))
        {
            UpdateCharCount();
        }
    }

    private void UpdateCharCount()
    {
        var text = TextEditor.Text ?? "";
        var charCount = text.Length;
        var wordCount = string.IsNullOrWhiteSpace(text) ? 0 :
            text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;

        CharCount.Text = $"{charCount:N0} chars, {wordCount:N0} words";
    }

    private void Undo_Click(object? sender, RoutedEventArgs e)
    {
        TextEditor.Undo();
    }

    private void Redo_Click(object? sender, RoutedEventArgs e)
    {
        TextEditor.Redo();
    }

    private void Cut_Click(object? sender, RoutedEventArgs e)
    {
        TextEditor.Cut();
    }

    private void Copy_Click(object? sender, RoutedEventArgs e)
    {
        TextEditor.Copy();
    }

    private void Paste_Click(object? sender, RoutedEventArgs e)
    {
        TextEditor.Paste();
    }

    private void SelectAll_Click(object? sender, RoutedEventArgs e)
    {
        TextEditor.SelectAll();
    }

    private void WordWrap_Click(object? sender, RoutedEventArgs e)
    {
        var isWrapped = WordWrapCheck.IsChecked ?? false;
        WordWrapCheck.IsChecked = !isWrapped;
        TextEditor.TextWrapping = !isWrapped ? Avalonia.Media.TextWrapping.Wrap : Avalonia.Media.TextWrapping.NoWrap;
    }

    private void Find_Click(object? sender, RoutedEventArgs e)
    {
        StatusText.Text = "Find: Use Ctrl+F in main window";
    }

    private void FindReplace_Click(object? sender, RoutedEventArgs e)
    {
        StatusText.Text = "Find/Replace: Use Ctrl+H in main window";
    }

    private async void SwapPronouns_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = new GenderSwapDialog();
        await dialog.ShowDialog(this);

        if (dialog.DialogResult)
        {
            var text = TextEditor.Text ?? "";
            var replacements = GenderSwap.SwapGenders(
                ref text,
                dialog.CharacterFrom,
                dialog.CharacterTo,
                dialog.UserFrom,
                dialog.UserTo,
                dialog.SwapCharacter,
                dialog.SwapUser);

            if (replacements > 0)
            {
                TextEditor.Text = text;
                StatusText.Text = $"Replaced {replacements} occurrence(s)";
            }
            else
            {
                StatusText.Text = "No replacements made";
            }
        }
    }

    private void Ok_Click(object? sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private async void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        if (_hasChanges)
        {
            var messageBox = new MessageBoxDialog
            {
                Title = "Confirm",
                Message = "You have unsaved changes. Apply changes before closing?",
                Buttons = MessageBoxButtons.YesNoCancel
            };

            var result = await messageBox.ShowDialog<MessageBoxResult>(this);

            if (result == MessageBoxResult.Yes)
            {
                DialogResult = true;
                Close();
            }
            else if (result == MessageBoxResult.No)
            {
                DialogResult = false;
                Close();
            }
            // Cancel - do nothing, stay open
        }
        else
        {
            DialogResult = false;
            Close();
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.Escape)
        {
            Cancel_Click(this, new RoutedEventArgs());
            e.Handled = true;
        }
        else if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.Enter)
        {
            Ok_Click(this, new RoutedEventArgs());
            e.Handled = true;
        }
    }
}
