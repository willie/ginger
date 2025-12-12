using Avalonia.Controls;
using Avalonia.Interactivity;
using Ginger.Services;

namespace Ginger.Views.Dialogs;

public partial class MessageBoxDialog : Window
{
    private MessageBoxButtons _buttons = MessageBoxButtons.Ok;

    public MessageBoxDialog()
    {
        InitializeComponent();
    }

    public string Message
    {
        get => MessageText.Text ?? "";
        set => MessageText.Text = value;
    }

    public MessageBoxButtons Buttons
    {
        get => _buttons;
        set
        {
            _buttons = value;
            UpdateButtons();
        }
    }

    private void UpdateButtons()
    {
        switch (_buttons)
        {
            case MessageBoxButtons.Ok:
                OkButton.IsVisible = true;
                CancelButton.IsVisible = false;
                YesButton.IsVisible = false;
                NoButton.IsVisible = false;
                break;
            case MessageBoxButtons.OkCancel:
                OkButton.IsVisible = true;
                CancelButton.IsVisible = true;
                YesButton.IsVisible = false;
                NoButton.IsVisible = false;
                break;
            case MessageBoxButtons.YesNo:
                OkButton.IsVisible = false;
                CancelButton.IsVisible = false;
                YesButton.IsVisible = true;
                NoButton.IsVisible = true;
                break;
            case MessageBoxButtons.YesNoCancel:
                OkButton.IsVisible = false;
                CancelButton.IsVisible = true;
                YesButton.IsVisible = true;
                NoButton.IsVisible = true;
                break;
        }
    }

    private void OkButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(MessageBoxResult.Ok);
    }

    private void YesButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(MessageBoxResult.Yes);
    }

    private void NoButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(MessageBoxResult.No);
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(MessageBoxResult.Cancel);
    }
}
