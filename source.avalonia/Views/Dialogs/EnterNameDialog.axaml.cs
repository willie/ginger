using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Ginger.Views.Dialogs;

public partial class EnterNameDialog : Window
{
    public string EnteredName { get; private set; } = "";
    public bool DialogResult { get; private set; }

    public EnterNameDialog()
    {
        InitializeComponent();
    }

    public EnterNameDialog(string title, string prompt, string defaultValue = "") : this()
    {
        Title = title;
        PromptText.Text = prompt;
        InputBox.Text = defaultValue;
    }

    private void OkButton_Click(object? sender, RoutedEventArgs e)
    {
        EnteredName = InputBox.Text ?? "";
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
