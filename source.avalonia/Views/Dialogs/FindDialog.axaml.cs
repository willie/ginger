using System;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Ginger.Views.Dialogs;

public partial class FindDialog : Window
{
    private bool _isReverse;

    /// <summary>
    /// Callback for Find Next: (searchText, matchCase, wholeWord)
    /// Used for modeless operation.
    /// </summary>
    public Action<string, bool, bool>? OnFindNext { get; set; }

    /// <summary>
    /// Callback for Find Previous: (searchText, matchCase, wholeWord)
    /// Used for modeless operation.
    /// </summary>
    public Action<string, bool, bool>? OnFindPrevious { get; set; }

    /// <summary>
    /// True if user clicked Find (for modal usage).
    /// </summary>
    public bool DialogResult { get; private set; }

    public string Match
    {
        get => FindTextBox.Text ?? "";
        set => FindTextBox.Text = value;
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

    public FindDialog()
    {
        InitializeComponent();

        // Load previous search settings
        if (!string.IsNullOrEmpty(AppSettings.User.FindMatch))
            FindTextBox.Text = AppSettings.User.FindMatch;
        MatchCaseCheckBox.IsChecked = AppSettings.User.FindMatchCase;
        WholeWordCheckBox.IsChecked = AppSettings.User.FindWholeWords;
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        FindTextBox.Focus();
        FindTextBox.SelectAll();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        var isMac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        var cmdOrCtrl = isMac ? KeyModifiers.Meta : KeyModifiers.Control;

        // Cmd+G / F3: Find Next
        if ((isMac && e.KeyModifiers == cmdOrCtrl && e.Key == Key.G) ||
            (!isMac && e.Key == Key.F3 && e.KeyModifiers == KeyModifiers.None))
        {
            FindNext_Click(this, new RoutedEventArgs());
            e.Handled = true;
        }
        // Cmd+Shift+G / Shift+F3: Find Previous
        else if ((isMac && e.KeyModifiers == (cmdOrCtrl | KeyModifiers.Shift) && e.Key == Key.G) ||
                 (!isMac && e.Key == Key.F3 && e.KeyModifiers == KeyModifiers.Shift))
        {
            FindPrevious_Click(this, new RoutedEventArgs());
            e.Handled = true;
        }
        // Cmd+W / Ctrl+W: Close
        else if (e.KeyModifiers == cmdOrCtrl && e.Key == Key.W)
        {
            Close();
            e.Handled = true;
        }
    }

    public void SetStatus(string message)
    {
        StatusText.Text = message;
    }

    private void DoFind(bool reverse)
    {
        if (string.IsNullOrWhiteSpace(Match))
        {
            SetStatus("Enter text to find.");
            FindTextBox.Focus();
            return;
        }

        // Save settings
        AppSettings.User.FindMatch = Match;
        AppSettings.User.FindMatchCase = MatchCase;
        AppSettings.User.FindWholeWords = WholeWord;

        _isReverse = reverse;
        DialogResult = true;

        // For modeless operation, invoke appropriate callback
        if (reverse)
            OnFindPrevious?.Invoke(Match, MatchCase, WholeWord);
        else
            OnFindNext?.Invoke(Match, MatchCase, WholeWord);
    }

    private void FindNext_Click(object? sender, RoutedEventArgs e)
    {
        DoFind(reverse: false);
    }

    private void FindPrevious_Click(object? sender, RoutedEventArgs e)
    {
        DoFind(reverse: true);
    }

    private void Close_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
