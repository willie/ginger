using System;
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
