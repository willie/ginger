using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Ginger.Views.Dialogs;

public partial class PasteTextDialog : Window
{
    public bool DialogResult { get; private set; }
    public string PastedText { get; private set; } = "";

    private TextBox? _textContent;

    public PasteTextDialog()
    {
        InitializeComponent();
        _textContent = this.FindControl<TextBox>("TextContent");
    }

    public void SetContent(string text)
    {
        if (_textContent != null)
            _textContent.Text = text;
    }

    private async void PasteButton_Click(object? sender, RoutedEventArgs e)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard != null)
        {
            var text = await clipboard.GetTextAsync();
            if (!string.IsNullOrEmpty(text) && _textContent != null)
            {
                _textContent.Text = text;
            }
        }
    }

    private void OkButton_Click(object? sender, RoutedEventArgs e)
    {
        var text = _textContent?.Text?.Trim() ?? "";

        if (string.IsNullOrEmpty(text))
        {
            // Could show error, but for simplicity just close without result
            DialogResult = false;
            Close();
            return;
        }

        PastedText = text;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
