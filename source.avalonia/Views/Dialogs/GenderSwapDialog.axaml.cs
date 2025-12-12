using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Ginger.Views.Dialogs;

public partial class GenderSwapDialog : Window
{
    public GenderSwap.Pronouns CharacterFrom { get; private set; }
    public GenderSwap.Pronouns CharacterTo { get; private set; } = GenderSwap.Pronouns.VariableNeutral;
    public GenderSwap.Pronouns UserFrom { get; private set; }
    public GenderSwap.Pronouns UserTo { get; private set; } = GenderSwap.Pronouns.VariableUserNeutral;
    public bool SwapCharacter { get; private set; } = true;
    public bool SwapUser { get; private set; } = false;
    public bool DialogResult { get; private set; } = false;

    private static readonly GenderSwap.Pronouns[] PronounList = new[]
    {
        GenderSwap.Pronouns.Masculine,
        GenderSwap.Pronouns.Feminine,
        GenderSwap.Pronouns.Neutral,
        GenderSwap.Pronouns.Mixed,
        GenderSwap.Pronouns.VariableNeutral,
        GenderSwap.Pronouns.VariableUserNeutral,
        GenderSwap.Pronouns.Objective,
        GenderSwap.Pronouns.NeopronounsShiHir,
        GenderSwap.Pronouns.NeopronounsEyEm,
        GenderSwap.Pronouns.NeopronounsZeZir,
        GenderSwap.Pronouns.NeopronounsXeXem,
        GenderSwap.Pronouns.NeopronounsFaeFaer,
    };

    private static readonly string[] PronounNames = new[]
    {
        "Masculine (he/him)",
        "Feminine (she/her)",
        "Neutral (they/them)",
        "Mixed (he/she)",
        "Variable {they}",
        "Variable {#they}",
        "Objective (it/its)",
        "Neopronouns (shi/hir)",
        "Neopronouns (ey/em)",
        "Neopronouns (ze/zir)",
        "Neopronouns (xe/xem)",
        "Neopronouns (fae/faer)",
    };

    public GenderSwapDialog()
    {
        InitializeComponent();

        // Populate combo boxes
        CharacterFromCombo.ItemsSource = PronounNames;
        CharacterToCombo.ItemsSource = PronounNames;
        UserFromCombo.ItemsSource = PronounNames;
        UserToCombo.ItemsSource = PronounNames;

        // Set initial values
        CharacterFrom = GenderSwap.PronounsFromGender(Current.Character);
        UserFrom = GenderSwap.PronounsFromGender(Current.Card.userGender);

        CharacterFromCombo.SelectedIndex = Array.IndexOf(PronounList, CharacterFrom);
        CharacterToCombo.SelectedIndex = Array.IndexOf(PronounList, CharacterTo);
        UserFromCombo.SelectedIndex = Array.IndexOf(PronounList, UserFrom);
        UserToCombo.SelectedIndex = Array.IndexOf(PronounList, UserTo);

        // Default selections if not found
        if (CharacterFromCombo.SelectedIndex < 0) CharacterFromCombo.SelectedIndex = 0;
        if (CharacterToCombo.SelectedIndex < 0) CharacterToCombo.SelectedIndex = 4; // Variable {they}
        if (UserFromCombo.SelectedIndex < 0) UserFromCombo.SelectedIndex = 0;
        if (UserToCombo.SelectedIndex < 0) UserToCombo.SelectedIndex = 5; // Variable {#they}

        CharacterCheckBox.IsChecked = true;
        UserCheckBox.IsChecked = false;

        // Wire up events
        CharacterCheckBox.IsCheckedChanged += (s, e) => UpdateState();
        UserCheckBox.IsCheckedChanged += (s, e) => UpdateState();
        CharacterFromCombo.SelectionChanged += (s, e) => UpdateState();
        CharacterToCombo.SelectionChanged += (s, e) => UpdateState();
        UserFromCombo.SelectionChanged += (s, e) => UpdateState();
        UserToCombo.SelectionChanged += (s, e) => UpdateState();

        UpdateState();
    }

    private void UpdateState()
    {
        SwapCharacter = CharacterCheckBox.IsChecked ?? false;
        SwapUser = UserCheckBox.IsChecked ?? false;

        CharacterFromCombo.IsEnabled = SwapCharacter;
        CharacterToCombo.IsEnabled = SwapCharacter;
        UserFromCombo.IsEnabled = SwapUser;
        UserToCombo.IsEnabled = SwapUser;

        if (CharacterFromCombo.SelectedIndex >= 0)
            CharacterFrom = PronounList[CharacterFromCombo.SelectedIndex];
        if (CharacterToCombo.SelectedIndex >= 0)
            CharacterTo = PronounList[CharacterToCombo.SelectedIndex];
        if (UserFromCombo.SelectedIndex >= 0)
            UserFrom = PronounList[UserFromCombo.SelectedIndex];
        if (UserToCombo.SelectedIndex >= 0)
            UserTo = PronounList[UserToCombo.SelectedIndex];

        // Update status
        if (!SwapCharacter && !SwapUser)
        {
            StatusText.Text = "Select at least one option to swap.";
            SwapButton.IsEnabled = false;
        }
        else
        {
            StatusText.Text = "";
            SwapButton.IsEnabled = true;
        }
    }

    private void SwapButton_Click(object? sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
