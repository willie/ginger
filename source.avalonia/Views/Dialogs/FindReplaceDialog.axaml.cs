using System;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Ginger.Views.Dialogs;

public partial class FindReplaceDialog : Window
{
    public FindReplaceDialog()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        // Focus the find text box and select all text
        FindTextBox.Focus();
        FindTextBox.SelectAll();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        var isMac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        var cmdOrCtrl = isMac ? KeyModifiers.Meta : KeyModifiers.Control;

        // Enter: Find Next
        if (e.Key == Key.Enter)
        {
            FindNextButton_Click(this, new RoutedEventArgs());
            e.Handled = true;
        }
        // Cmd+W (Mac) or Ctrl+W (Win/Linux): Close
        else if (e.KeyModifiers == cmdOrCtrl && e.Key == Key.W)
        {
            Close();
            e.Handled = true;
        }
        // Escape: Close
        else if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }

    /// <summary>
    /// Callback for Find Next: (searchText, replaceText, matchCase, wholeWord)
    /// </summary>
    public Action<string, string, bool, bool>? OnFindNext { get; set; }

    /// <summary>
    /// Callback for Replace: (searchText, replaceText, matchCase, wholeWord)
    /// </summary>
    public Action<string, string, bool, bool>? OnReplace { get; set; }

    /// <summary>
    /// Callback for Replace All: (searchText, replaceText, matchCase, wholeWord)
    /// </summary>
    public Action<string, string, bool, bool>? OnReplaceAll { get; set; }

    public string FindText
    {
        get => FindTextBox.Text ?? "";
        set => FindTextBox.Text = value;
    }

    public string ReplaceText
    {
        get => ReplaceTextBox.Text ?? "";
        set => ReplaceTextBox.Text = value;
    }

    public bool MatchCase
    {
        get => MatchCaseCheckBox.IsChecked ?? false;
        set => MatchCaseCheckBox.IsChecked = value;
    }

    public bool WholeWord
    {
        get => WholeWordCheckBox.IsChecked ?? false;
        set => WholeWordCheckBox.IsChecked = value;
    }

    public void SetStatus(string message)
    {
        StatusText.Text = message;
    }

    private void FindNextButton_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(FindText))
        {
            SetStatus("Enter text to find.");
            return;
        }

        OnFindNext?.Invoke(FindText, ReplaceText, MatchCase, WholeWord);
    }

    private void ReplaceButton_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(FindText))
        {
            SetStatus("Enter text to find.");
            return;
        }

        OnReplace?.Invoke(FindText, ReplaceText, MatchCase, WholeWord);
    }

    private void ReplaceAllButton_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(FindText))
        {
            SetStatus("Enter text to find.");
            return;
        }

        OnReplaceAll?.Invoke(FindText, ReplaceText, MatchCase, WholeWord);
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
