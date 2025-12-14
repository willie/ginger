using System;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Ginger.Views.Dialogs;

public partial class FontPickerDialog : Window
{
    private const string DefaultFontFamily = "Consolas, Monaco, Menlo, monospace";
    private const double DefaultFontSize = 13;

    public bool DialogResult { get; private set; }
    public string SelectedFontFamily { get; set; } = DefaultFontFamily;
    public double SelectedFontSize { get; set; } = DefaultFontSize;

    public FontPickerDialog()
    {
        InitializeComponent();

        PopulateFontFamilies();

        FontFamilyCombo.SelectionChanged += FontFamilyCombo_SelectionChanged;
        FontSizeSlider.PropertyChanged += (s, e) =>
        {
            if (e.Property.Name == nameof(Slider.Value))
            {
                FontSizeText.Text = ((int)FontSizeSlider.Value).ToString();
                UpdatePreview();
            }
        };
        FontSizeText.LostFocus += FontSizeText_LostFocus;
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        // Set initial values
        SelectFontFamily(SelectedFontFamily);
        FontSizeSlider.Value = SelectedFontSize;
        FontSizeText.Text = ((int)SelectedFontSize).ToString();
        UpdatePreview();
    }

    private void PopulateFontFamilies()
    {
        var fonts = FontManager.Current.SystemFonts
            .Select(f => f.Name)
            .OrderBy(f => f)
            .ToList();

        // Add common monospace fonts at the top if available
        var monospaceFonts = new[] { "Consolas", "Monaco", "Menlo", "Source Code Pro", "JetBrains Mono", "Fira Code", "Courier New" };
        var availableMono = monospaceFonts.Where(m => fonts.Contains(m)).ToList();

        FontFamilyCombo.Items.Clear();

        // Add monospace section
        if (availableMono.Count > 0)
        {
            FontFamilyCombo.Items.Add("--- Monospace ---");
            foreach (var font in availableMono)
            {
                FontFamilyCombo.Items.Add(font);
            }
            FontFamilyCombo.Items.Add("--- All Fonts ---");
        }

        // Add all fonts
        foreach (var font in fonts)
        {
            FontFamilyCombo.Items.Add(font);
        }
    }

    private void SelectFontFamily(string fontFamily)
    {
        // Try to find the font in the list
        var primaryFont = fontFamily.Split(',')[0].Trim();
        for (int i = 0; i < FontFamilyCombo.Items.Count; i++)
        {
            if (FontFamilyCombo.Items[i] is string item &&
                item.Equals(primaryFont, StringComparison.OrdinalIgnoreCase))
            {
                FontFamilyCombo.SelectedIndex = i;
                return;
            }
        }

        // Default to first non-separator item
        for (int i = 0; i < FontFamilyCombo.Items.Count; i++)
        {
            if (FontFamilyCombo.Items[i] is string item && !item.StartsWith("---"))
            {
                FontFamilyCombo.SelectedIndex = i;
                return;
            }
        }
    }

    private void FontFamilyCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (FontFamilyCombo.SelectedItem is string selected && selected.StartsWith("---"))
        {
            // Skip separator items
            if (FontFamilyCombo.SelectedIndex > 0)
                FontFamilyCombo.SelectedIndex--;
            return;
        }
        UpdatePreview();
    }

    private void FontSizeText_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (double.TryParse(FontSizeText.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var size))
        {
            size = Math.Max(8, Math.Min(24, size));
            FontSizeSlider.Value = size;
        }
        else
        {
            FontSizeText.Text = ((int)FontSizeSlider.Value).ToString();
        }
    }

    private void UpdatePreview()
    {
        if (FontFamilyCombo.SelectedItem is string fontName && !fontName.StartsWith("---"))
        {
            PreviewText.FontFamily = new FontFamily(fontName);
        }
        PreviewText.FontSize = FontSizeSlider.Value;
    }

    private void ResetToDefaults_Click(object? sender, RoutedEventArgs e)
    {
        SelectFontFamily(DefaultFontFamily);
        FontSizeSlider.Value = DefaultFontSize;
    }

    private void Ok_Click(object? sender, RoutedEventArgs e)
    {
        if (FontFamilyCombo.SelectedItem is string fontName && !fontName.StartsWith("---"))
        {
            SelectedFontFamily = fontName;
        }
        SelectedFontSize = FontSizeSlider.Value;

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
