using System;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Ginger.Integration;
using Ginger.Services;

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

    // Special preset indices
    private const int PresetIndexCurrent = 0;
    private const int PresetIndexDefault = 1;
    private const int PresetIndexUserStart = 2;

    private static readonly Backyard.ChatParameters DefaultParameters = new()
    {
        temperature = 0.8m,
        minP = 0.05m,
        topP = 0.95m,
        topK = 40,
        repeatPenalty = 1.1m,
        repeatLastN = 64
    };

    public EditModelSettingsDialog()
    {
        InitializeComponent();
        PopulatePresets();

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

        UpdatePresetButtonStates();
    }

    private void PopulatePresets()
    {
        PresetCombo.Items.Clear();
        PresetCombo.Items.Add("Current settings");
        PresetCombo.Items.Add("Default settings");

        foreach (var preset in AppSettings.BackyardSettings.Presets)
        {
            PresetCombo.Items.Add(preset.Name);
        }

        PresetCombo.SelectedIndex = PresetIndexCurrent;
    }

    private void UpdatePresetButtonStates()
    {
        bool isUserPreset = PresetCombo.SelectedIndex >= PresetIndexUserStart;
        SavePresetButton.IsEnabled = isUserPreset;
        DeletePresetButton.IsEnabled = isUserPreset;
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
        UpdatePresetButtonStates();

        if (PresetCombo.SelectedIndex == PresetIndexCurrent)
        {
            // Keep current values - do nothing
            return;
        }
        else if (PresetCombo.SelectedIndex == PresetIndexDefault)
        {
            ApplyParameters(DefaultParameters);
        }
        else if (PresetCombo.SelectedIndex >= PresetIndexUserStart)
        {
            int userPresetIndex = PresetCombo.SelectedIndex - PresetIndexUserStart;
            if (userPresetIndex < AppSettings.BackyardSettings.Presets.Count)
            {
                var preset = AppSettings.BackyardSettings.Presets[userPresetIndex];
                ApplyParameters(preset.Parameters);
            }
        }
    }

    private void ApplyParameters(Backyard.ChatParameters parameters)
    {
        TemperatureSlider.Value = (double)parameters.temperature;
        MinPSlider.Value = (double)parameters.minP;
        TopPSlider.Value = (double)parameters.topP;
        TopKSlider.Value = parameters.topK;
        RepeatPenaltySlider.Value = (double)parameters.repeatPenalty;
        RepeatLastNSlider.Value = parameters.repeatLastN;
    }

    private Backyard.ChatParameters GetCurrentParameters()
    {
        return new Backyard.ChatParameters
        {
            temperature = (decimal)TemperatureSlider.Value,
            minP = (decimal)MinPSlider.Value,
            topP = (decimal)TopPSlider.Value,
            topK = (int)TopKSlider.Value,
            repeatPenalty = (decimal)RepeatPenaltySlider.Value,
            repeatLastN = (int)RepeatLastNSlider.Value
        };
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

    private async void SavePreset_Click(object? sender, RoutedEventArgs e)
    {
        if (PresetCombo.SelectedIndex < PresetIndexUserStart)
            return;

        int userPresetIndex = PresetCombo.SelectedIndex - PresetIndexUserStart;
        if (userPresetIndex < AppSettings.BackyardSettings.Presets.Count)
        {
            var existingPreset = AppSettings.BackyardSettings.Presets[userPresetIndex];
            AppSettings.BackyardSettings.Presets[userPresetIndex] = new AppSettings.BackyardSettings.Preset(
                existingPreset.Name,
                GetCurrentParameters());
            AppSettings.Save();

            var messageBox = new MessageBoxDialog
            {
                Title = "Preset Saved",
                Message = $"Preset \"{existingPreset.Name}\" has been updated.",
                Buttons = MessageBoxButtons.Ok
            };
            await messageBox.ShowDialog(this);
        }
    }

    private async void NewPreset_Click(object? sender, RoutedEventArgs e)
    {
        var nameDialog = new EnterNameDialog("New Preset", "Enter a name for the new preset:");

        await nameDialog.ShowDialog(this);

        if (nameDialog.DialogResult && !string.IsNullOrWhiteSpace(nameDialog.EnteredName))
        {
            string presetName = nameDialog.EnteredName.Trim();

            // Check for duplicate names
            if (AppSettings.BackyardSettings.Presets.Any(p => p.Name.Equals(presetName, StringComparison.OrdinalIgnoreCase)))
            {
                var messageBox = new MessageBoxDialog
                {
                    Title = "Duplicate Name",
                    Message = $"A preset named \"{presetName}\" already exists.",
                    Buttons = MessageBoxButtons.Ok
                };
                await messageBox.ShowDialog(this);
                return;
            }

            var newPreset = new AppSettings.BackyardSettings.Preset(presetName, GetCurrentParameters());
            AppSettings.BackyardSettings.Presets.Add(newPreset);
            AppSettings.Save();

            PopulatePresets();
            PresetCombo.SelectedIndex = PresetCombo.Items.Count - 1;
        }
    }

    private async void DeletePreset_Click(object? sender, RoutedEventArgs e)
    {
        if (PresetCombo.SelectedIndex < PresetIndexUserStart)
            return;

        int userPresetIndex = PresetCombo.SelectedIndex - PresetIndexUserStart;
        if (userPresetIndex < AppSettings.BackyardSettings.Presets.Count)
        {
            var preset = AppSettings.BackyardSettings.Presets[userPresetIndex];

            var confirmBox = new MessageBoxDialog
            {
                Title = "Delete Preset",
                Message = $"Are you sure you want to delete the preset \"{preset.Name}\"?",
                Buttons = MessageBoxButtons.YesNo
            };

            var result = await confirmBox.ShowDialog<MessageBoxResult?>(this);
            if (result == MessageBoxResult.Yes)
            {
                AppSettings.BackyardSettings.Presets.RemoveAt(userPresetIndex);
                AppSettings.Save();
                PopulatePresets();
            }
        }
    }

    private async void Copy_Click(object? sender, RoutedEventArgs e)
    {
        var parameters = GetCurrentParameters();
        string clipboardText = $"Temperature={parameters.temperature:F2};" +
                              $"MinP={parameters.minP:F2};" +
                              $"TopP={parameters.topP:F2};" +
                              $"TopK={parameters.topK};" +
                              $"RepeatPenalty={parameters.repeatPenalty:F2};" +
                              $"RepeatLastN={parameters.repeatLastN}";

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard != null)
        {
            await clipboard.SetTextAsync(clipboardText);
        }
    }

    private async void Paste_Click(object? sender, RoutedEventArgs e)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard == null)
            return;

        string? text = await clipboard.GetTextAsync();
        if (string.IsNullOrWhiteSpace(text))
            return;

        try
        {
            var pairs = text.Split(';')
                .Select(s => s.Split('='))
                .Where(a => a.Length == 2)
                .ToDictionary(a => a[0].Trim(), a => a[1].Trim(), StringComparer.OrdinalIgnoreCase);

            if (pairs.TryGetValue("Temperature", out var temp) && decimal.TryParse(temp, out var tempVal))
                TemperatureSlider.Value = (double)tempVal;
            if (pairs.TryGetValue("MinP", out var minP) && decimal.TryParse(minP, out var minPVal))
                MinPSlider.Value = (double)minPVal;
            if (pairs.TryGetValue("TopP", out var topP) && decimal.TryParse(topP, out var topPVal))
                TopPSlider.Value = (double)topPVal;
            if (pairs.TryGetValue("TopK", out var topK) && int.TryParse(topK, out var topKVal))
                TopKSlider.Value = topKVal;
            if (pairs.TryGetValue("RepeatPenalty", out var repPen) && decimal.TryParse(repPen, out var repPenVal))
                RepeatPenaltySlider.Value = (double)repPenVal;
            if (pairs.TryGetValue("RepeatLastN", out var repLastN) && int.TryParse(repLastN, out var repLastNVal))
                RepeatLastNSlider.Value = repLastNVal;

            PresetCombo.SelectedIndex = PresetIndexCurrent;
        }
        catch
        {
            // Invalid clipboard format - ignore
        }
    }

    private void Reset_Click(object? sender, RoutedEventArgs e)
    {
        PresetCombo.SelectedIndex = PresetIndexDefault;
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
