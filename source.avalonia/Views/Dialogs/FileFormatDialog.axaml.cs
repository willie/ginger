using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Ginger.Views.Dialogs;

public partial class FileFormatDialog : Window
{
    public enum ExportFormat
    {
        Png,
        Json,
        Yaml,
        Charx,
        Byaf
    }

    public ExportFormat SelectedFormat { get; private set; } = ExportFormat.Png;
    public bool DialogResult { get; private set; }

    public FileFormatDialog()
    {
        InitializeComponent();
    }

    public FileFormatDialog(ExportFormat defaultFormat) : this()
    {
        SelectFormat(defaultFormat);
    }

    private void SelectFormat(ExportFormat format)
    {
        PngRadio.IsChecked = format == ExportFormat.Png;
        JsonRadio.IsChecked = format == ExportFormat.Json;
        YamlRadio.IsChecked = format == ExportFormat.Yaml;
        CharxRadio.IsChecked = format == ExportFormat.Charx;
        ByafRadio.IsChecked = format == ExportFormat.Byaf;
    }

    private void OkButton_Click(object? sender, RoutedEventArgs e)
    {
        if (PngRadio.IsChecked == true)
            SelectedFormat = ExportFormat.Png;
        else if (JsonRadio.IsChecked == true)
            SelectedFormat = ExportFormat.Json;
        else if (YamlRadio.IsChecked == true)
            SelectedFormat = ExportFormat.Yaml;
        else if (CharxRadio.IsChecked == true)
            SelectedFormat = ExportFormat.Charx;
        else if (ByafRadio.IsChecked == true)
            SelectedFormat = ExportFormat.Byaf;

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    /// <summary>
    /// Get the file extension for the selected format.
    /// </summary>
    public string GetFileExtension()
    {
        return SelectedFormat switch
        {
            ExportFormat.Png => ".png",
            ExportFormat.Json => ".json",
            ExportFormat.Yaml => ".yaml",
            ExportFormat.Charx => ".charx",
            ExportFormat.Byaf => ".byaf",
            _ => ".png"
        };
    }

    /// <summary>
    /// Get the file filter for the save dialog.
    /// </summary>
    public string GetFileFilter()
    {
        return SelectedFormat switch
        {
            ExportFormat.Png => "PNG Image|*.png",
            ExportFormat.Json => "JSON File|*.json",
            ExportFormat.Yaml => "YAML File|*.yaml",
            ExportFormat.Charx => "Character Archive|*.charx",
            ExportFormat.Byaf => "Backyard Archive|*.byaf",
            _ => "PNG Image|*.png"
        };
    }
}
