using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Ginger.Views.Dialogs;

public partial class FindDialog : Window
{
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

    private void Find_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Match))
        {
            FindTextBox.Focus();
            return;
        }

        // Save settings
        AppSettings.User.FindMatch = Match;
        AppSettings.User.FindMatchCase = MatchCase;
        AppSettings.User.FindWholeWords = WholeWord;

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
