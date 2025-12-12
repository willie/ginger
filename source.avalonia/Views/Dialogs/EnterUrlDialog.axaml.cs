using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Ginger.Views.Dialogs;

public partial class EnterUrlDialog : Window
{
    public bool DialogResult { get; private set; }
    public string EnteredUrl { get; private set; } = "";

    private TextBox? _urlTextBox;
    private TextBlock? _errorText;
    private TextBlock? _promptText;

    public EnterUrlDialog() : this("Enter URL", "Enter the URL:") { }

    public EnterUrlDialog(string title, string prompt)
    {
        InitializeComponent();
        Title = title;

        _urlTextBox = this.FindControl<TextBox>("UrlTextBox");
        _errorText = this.FindControl<TextBlock>("ErrorText");
        _promptText = this.FindControl<TextBlock>("PromptText");

        if (_promptText != null)
            _promptText.Text = prompt;
    }

    public void SetDefaultUrl(string url)
    {
        if (_urlTextBox != null)
        {
            _urlTextBox.Text = url;
            _urlTextBox.SelectAll();
        }
    }

    private void OkButton_Click(object? sender, RoutedEventArgs e)
    {
        var url = _urlTextBox?.Text?.Trim() ?? "";

        // Validate URL
        if (string.IsNullOrEmpty(url))
        {
            ShowError("Please enter a URL");
            return;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            ShowError("Invalid URL format");
            return;
        }

        if (uri.Scheme != "http" && uri.Scheme != "https")
        {
            ShowError("URL must start with http:// or https://");
            return;
        }

        EnteredUrl = url;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void ShowError(string message)
    {
        if (_errorText != null)
        {
            _errorText.Text = message;
            _errorText.IsVisible = true;
        }
    }
}
