using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Ginger.Views.Dialogs;

public partial class EditModelSettingsDialog : Window
{
    public bool DialogResult { get; private set; }

    // Model settings properties
    public decimal Temperature { get; set; } = 0.8m;
    public decimal MinP { get; set; } = 0.05m;
    public decimal TopP { get; set; } = 0.95m;
    public int TopK { get; set; } = 40;
    public decimal RepeatPenalty { get; set; } = 1.1m;
    public int RepeatLastN { get; set; } = 64;

    private static readonly (string Name, decimal Temp, decimal MinP, decimal TopP, int TopK, decimal RepPen, int RepLastN)[] Presets =
    {
        ("Default", 0.8m, 0.05m, 0.95m, 40, 1.1m, 64),
        ("Creative", 1.2m, 0.02m, 0.98m, 60, 1.05m, 128),
        ("Precise", 0.4m, 0.1m, 0.85m, 20, 1.2m, 64),
        ("Balanced", 0.7m, 0.05m, 0.9m, 40, 1.15m, 80),
        ("Deterministic", 0.1m, 0.2m, 0.7m, 10, 1.3m, 32),
    };

    public EditModelSettingsDialog()
    {
        InitializeComponent();

        // Populate presets
        foreach (var preset in Presets)
        {
            PresetCombo.Items.Add(preset.Name);
        }
        PresetCombo.SelectedIndex = 0;
        PresetCombo.SelectionChanged += PresetCombo_SelectionChanged;

        // Wire up slider/text synchronization
        TemperatureSlider.PropertyChanged += (s, e) =>
        {
            if (e.Property.Name == nameof(Slider.Value))
                TemperatureText.Text = TemperatureSlider.Value.ToString("F2");
        };

        MinPSlider.PropertyChanged += (s, e) =>
        {
            if (e.Property.Name == nameof(Slider.Value))
                MinPText.Text = MinPSlider.Value.ToString("F2");
        };

        TopPSlider.PropertyChanged += (s, e) =>
        {
            if (e.Property.Name == nameof(Slider.Value))
                TopPText.Text = TopPSlider.Value.ToString("F2");
        };

        TopKSlider.PropertyChanged += (s, e) =>
        {
            if (e.Property.Name == nameof(Slider.Value))
                TopKText.Text = ((int)TopKSlider.Value).ToString();
        };

        RepeatPenaltySlider.PropertyChanged += (s, e) =>
        {
            if (e.Property.Name == nameof(Slider.Value))
                RepeatPenaltyText.Text = RepeatPenaltySlider.Value.ToString("F2");
        };

        RepeatLastNSlider.PropertyChanged += (s, e) =>
        {
            if (e.Property.Name == nameof(Slider.Value))
                RepeatLastNText.Text = ((int)RepeatLastNSlider.Value).ToString();
        };

        // Wire up text box changes
        TemperatureText.LostFocus += (s, e) => SyncTextToSlider(TemperatureText, TemperatureSlider, 0, 2);
        MinPText.LostFocus += (s, e) => SyncTextToSlider(MinPText, MinPSlider, 0, 1);
        TopPText.LostFocus += (s, e) => SyncTextToSlider(TopPText, TopPSlider, 0, 1);
        TopKText.LostFocus += (s, e) => SyncTextToSlider(TopKText, TopKSlider, 0, 100);
        RepeatPenaltyText.LostFocus += (s, e) => SyncTextToSlider(RepeatPenaltyText, RepeatPenaltySlider, 1, 2);
        RepeatLastNText.LostFocus += (s, e) => SyncTextToSlider(RepeatLastNText, RepeatLastNSlider, 16, 512);
    }

    private void SyncTextToSlider(TextBox textBox, Slider slider, double min, double max)
    {
        if (double.TryParse(textBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            value = Math.Max(min, Math.Min(max, value));
            slider.Value = value;
        }
        else
        {
            textBox.Text = slider.Value.ToString("F2");
        }
    }

    private void PresetCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (PresetCombo.SelectedIndex >= 0 && PresetCombo.SelectedIndex < Presets.Length)
        {
            var preset = Presets[PresetCombo.SelectedIndex];
            TemperatureSlider.Value = (double)preset.Temp;
            MinPSlider.Value = (double)preset.MinP;
            TopPSlider.Value = (double)preset.TopP;
            TopKSlider.Value = preset.TopK;
            RepeatPenaltySlider.Value = (double)preset.RepPen;
            RepeatLastNSlider.Value = preset.RepLastN;
        }
    }

    public void LoadSettings(decimal temperature, decimal minP, decimal topP, int topK, decimal repeatPenalty, int repeatLastN)
    {
        TemperatureSlider.Value = (double)temperature;
        MinPSlider.Value = (double)minP;
        TopPSlider.Value = (double)topP;
        TopKSlider.Value = topK;
        RepeatPenaltySlider.Value = (double)repeatPenalty;
        RepeatLastNSlider.Value = repeatLastN;
    }

    private void Reset_Click(object? sender, RoutedEventArgs e)
    {
        PresetCombo.SelectedIndex = 0;
    }

    private void Ok_Click(object? sender, RoutedEventArgs e)
    {
        // Save values from sliders
        Temperature = (decimal)TemperatureSlider.Value;
        MinP = (decimal)MinPSlider.Value;
        TopP = (decimal)TopPSlider.Value;
        TopK = (int)TopKSlider.Value;
        RepeatPenalty = (decimal)RepeatPenaltySlider.Value;
        RepeatLastN = (int)RepeatLastNSlider.Value;

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
