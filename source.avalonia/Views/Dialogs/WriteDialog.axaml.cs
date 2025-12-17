using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using Ginger.Services;

namespace Ginger.Views.Dialogs;

public partial class WriteDialog : Window
{
    private bool _hasChanges;
    private string _originalText = "";
    private SpellCheckService? _spellCheckService;
    private string? _lastCheckedWord;
    private TokenizerService? _tokenizerService;
    private System.Timers.Timer? _tokenCountTimer;
    private int _tokenInputHash;
    private GingerSyntaxColorizer? _syntaxColorizer;
    private string _lastFindText = "";
    private bool _lastFindMatchCase;
    private bool _lastFindWholeWord;

    public bool DialogResult { get; private set; }

    public string Value
    {
        get => TextEditor.Text ?? "";
        set
        {
            _originalText = value ?? "";
            TextEditor.Text = _originalText;
            _hasChanges = false;

            // Schedule initial token count
            _tokenCountTimer?.Stop();
            _tokenCountTimer?.Start();
        }
    }

    public WriteDialog()
    {
        InitializeComponent();

        TextEditor.TextChanged += TextEditor_TextChanged;

        // Initialize syntax highlighting
        InitializeSyntaxHighlighting();

        // Initialize word wrap checkbox state
        WordWrapCheck.IsChecked = AppSettings.WriteDialog.WordWrap;
        TextEditor.WordWrap = AppSettings.WriteDialog.WordWrap;

        // Initialize syntax highlighting checkboxes
        HighlightCheck.IsChecked = AppSettings.WriteDialog.Highlight;
        HighlightNamesCheck.IsChecked = AppSettings.WriteDialog.HighlightNames;
        HighlightNumbersCheck.IsChecked = AppSettings.WriteDialog.HighlightNumbers;
        HighlightPronounsCheck.IsChecked = AppSettings.WriteDialog.HighlightPronouns;

        // Update menu item enabled state
        UpdateHighlightMenuState();

        // Apply font settings
        if (!string.IsNullOrEmpty(AppSettings.WriteDialog.FontFamily))
        {
            TextEditor.FontFamily = new Avalonia.Media.FontFamily(AppSettings.WriteDialog.FontFamily);
        }
        if (AppSettings.WriteDialog.FontSize > 0)
        {
            TextEditor.FontSize = AppSettings.WriteDialog.FontSize;
        }

        UpdateCharCount();

        // Initialize spell check
        InitializeSpellCheck();

        // Initialize token counting
        InitializeTokenCounting();

        // Set up context menu for spell check
        SetupSpellCheckContextMenu();

        // Populate spell check language menu
        PopulateSpellCheckLanguageMenu();

        // Restore window size/position
        RestoreWindowState();

        // Save window state on close
        Closing += WriteDialog_Closing;
    }

    private void InitializeSyntaxHighlighting()
    {
        _syntaxColorizer = new GingerSyntaxColorizer();
        UpdateSyntaxFlags();
        UpdateCharacterNames();
        TextEditor.TextArea.TextView.LineTransformers.Add(_syntaxColorizer);
    }

    private void UpdateSyntaxFlags()
    {
        if (_syntaxColorizer == null)
            return;

        var flags = SyntaxFlags.None;

        if (AppSettings.WriteDialog.Highlight)
        {
            flags = SyntaxFlags.Default;

            if (!AppSettings.WriteDialog.HighlightNames)
                flags &= ~SyntaxFlags.Names;
            if (!AppSettings.WriteDialog.HighlightNumbers)
                flags &= ~SyntaxFlags.Numbers;
            if (AppSettings.WriteDialog.HighlightPronouns)
                flags |= SyntaxFlags.Pronouns;
        }

        _syntaxColorizer.Flags = flags;
        TextEditor.TextArea.TextView.Redraw();
    }

    private void UpdateCharacterNames()
    {
        if (_syntaxColorizer == null)
            return;

        var names = new List<string>();

        // Add user placeholder
        if (!string.IsNullOrEmpty(Current.Card?.userPlaceholder))
            names.Add(Current.Card.userPlaceholder);

        // Add character names
        if (Current.Characters != null)
        {
            foreach (var character in Current.Characters)
            {
                if (!string.IsNullOrEmpty(character?.name))
                    names.Add(character.name);
            }
        }

        _syntaxColorizer.SetCharacterNames(names.ToArray());

        // Add custom variables
        if (Current.Card?.customVariables != null)
        {
            var varNames = Current.Card.customVariables
                .Where(v => !string.IsNullOrWhiteSpace(v.Value))
                .Select(v => $"{{{v.Name}}}")
                .ToArray();
            _syntaxColorizer.SetVariableNames(varNames);
        }
    }

    private void UpdateHighlightMenuState()
    {
        var enabled = HighlightCheck.IsChecked ?? false;
        HighlightNamesMenuItem.IsEnabled = enabled;
        HighlightNumbersMenuItem.IsEnabled = enabled;
        HighlightPronounsMenuItem.IsEnabled = enabled;
    }

    private void RestoreWindowState()
    {
        if (AppSettings.WriteDialog.WindowWidth > 0 && AppSettings.WriteDialog.WindowHeight > 0)
        {
            Width = AppSettings.WriteDialog.WindowWidth;
            Height = AppSettings.WriteDialog.WindowHeight;
        }

        if (AppSettings.WriteDialog.WindowX != 0 || AppSettings.WriteDialog.WindowY != 0)
        {
            Position = new PixelPoint(
                (int)AppSettings.WriteDialog.WindowX,
                (int)AppSettings.WriteDialog.WindowY);
        }
    }

    private void WriteDialog_Closing(object? sender, WindowClosingEventArgs e)
    {
        // Save window state
        AppSettings.WriteDialog.WindowWidth = Width;
        AppSettings.WriteDialog.WindowHeight = Height;
        AppSettings.WriteDialog.WindowX = Position.X;
        AppSettings.WriteDialog.WindowY = Position.Y;
        AppSettings.WriteDialog.WordWrap = WordWrapCheck.IsChecked ?? true;
        AppSettings.WriteDialog.Highlight = HighlightCheck.IsChecked ?? true;
        AppSettings.WriteDialog.HighlightNames = HighlightNamesCheck.IsChecked ?? true;
        AppSettings.WriteDialog.HighlightNumbers = HighlightNumbersCheck.IsChecked ?? true;
        AppSettings.WriteDialog.HighlightPronouns = HighlightPronounsCheck.IsChecked ?? false;
        AppSettings.Save();

        _tokenCountTimer?.Dispose();
        _tokenizerService?.Dispose();
    }

    private void InitializeTokenCounting()
    {
        _tokenizerService = new TokenizerService();
        _tokenizerService.TokenCountCompleted += OnTokenCountCompleted;

        _tokenCountTimer = new System.Timers.Timer(300);
        _tokenCountTimer.Elapsed += (s, e) => ScheduleTokenCount();
        _tokenCountTimer.AutoReset = false;
    }

    private void ScheduleTokenCount()
    {
        if (_tokenizerService == null)
            return;

        var text = TextEditor.Text ?? "";
        var hash = text.GetHashCode();

        if (hash == _tokenInputHash)
            return;

        _tokenInputHash = hash;

        // Create a minimal Generator.Output for token counting
        var output = new Generator.Output
        {
            persona = GingerString.FromString(text)
        };

        _tokenizerService.Schedule(output, hash);
    }

    private void OnTokenCountCompleted(TokenizerService.Result result)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            TokenCount.Text = $"Token count: {result.tokens_total:N0}";
        });
    }

    private async void InitializeSpellCheck()
    {
        _spellCheckService = new SpellCheckService();

        // Try to find dictionary files
        var dicPaths = new[]
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Dictionaries"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "Dictionaries"),
            "Dictionaries"
        };

        foreach (var path in dicPaths)
        {
            if (Directory.Exists(path))
            {
                var loaded = await _spellCheckService.LoadDictionaryFromDirectoryAsync(path, "en_US");
                if (loaded)
                {
                    StatusText.Text = "Spell check enabled";
                    return;
                }
            }
        }
    }

    private void SetupSpellCheckContextMenu()
    {
        var contextMenu = new ContextMenu();
        TextEditor.ContextMenu = contextMenu;
        TextEditor.ContextRequested += TextEditor_ContextRequested;
    }

    private void TextEditor_ContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        if (TextEditor.ContextMenu is not ContextMenu contextMenu)
            return;

        contextMenu.Items.Clear();

        // Get word at cursor
        var word = GetWordAtCursor();
        _lastCheckedWord = word;

        if (!string.IsNullOrEmpty(word) && _spellCheckService?.IsLoaded == true)
        {
            if (!_spellCheckService.CheckWord(word))
            {
                // Word is misspelled - show suggestions
                var suggestions = _spellCheckService.GetSuggestions(word).Take(5).ToList();

                if (suggestions.Count > 0)
                {
                    foreach (var suggestion in suggestions)
                    {
                        var suggestionItem = new MenuItem { Header = suggestion };
                        suggestionItem.Click += (s, args) => ReplaceWordAtCursor(suggestion);
                        contextMenu.Items.Add(suggestionItem);
                    }
                    contextMenu.Items.Add(new Separator());
                }

                // Add "Ignore" option
                var ignoreItem = new MenuItem { Header = $"Ignore \"{word}\"" };
                ignoreItem.Click += (s, args) =>
                {
                    _spellCheckService.IgnoreWord(word);
                    StatusText.Text = $"Ignoring \"{word}\"";
                };
                contextMenu.Items.Add(ignoreItem);

                // Add "Add to Dictionary" option
                var addItem = new MenuItem { Header = $"Add \"{word}\" to Dictionary" };
                addItem.Click += (s, args) =>
                {
                    _spellCheckService.AddToCustomDictionary(word);
                    StatusText.Text = $"Added \"{word}\" to dictionary";
                };
                contextMenu.Items.Add(addItem);

                contextMenu.Items.Add(new Separator());
            }
        }

        // Standard edit items - use platform-appropriate shortcuts (Cmd on macOS, Ctrl elsewhere)
        var undoItem = new MenuItem { Header = "Undo", InputGesture = GetPlatformGesture(Key.Z, KeyModifiers.Control) };
        undoItem.Click += (s, args) => TextEditor.Undo();
        contextMenu.Items.Add(undoItem);

        // macOS uses Cmd+Shift+Z for redo, Windows uses Ctrl+Y
        var redoGesture = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
            ? new KeyGesture(Key.Z, KeyModifiers.Meta | KeyModifiers.Shift)
            : new KeyGesture(Key.Y, KeyModifiers.Control);
        var redoItem = new MenuItem { Header = "Redo", InputGesture = redoGesture };
        redoItem.Click += (s, args) => TextEditor.Redo();
        contextMenu.Items.Add(redoItem);

        contextMenu.Items.Add(new Separator());

        var cutItem = new MenuItem { Header = "Cut", InputGesture = GetPlatformGesture(Key.X, KeyModifiers.Control) };
        cutItem.Click += (s, args) => TextEditor.Cut();
        contextMenu.Items.Add(cutItem);

        var copyItem = new MenuItem { Header = "Copy", InputGesture = GetPlatformGesture(Key.C, KeyModifiers.Control) };
        copyItem.Click += (s, args) => TextEditor.Copy();
        contextMenu.Items.Add(copyItem);

        var pasteItem = new MenuItem { Header = "Paste", InputGesture = GetPlatformGesture(Key.V, KeyModifiers.Control) };
        pasteItem.Click += (s, args) => TextEditor.Paste();
        contextMenu.Items.Add(pasteItem);

        contextMenu.Items.Add(new Separator());

        var selectAllItem = new MenuItem { Header = "Select All", InputGesture = GetPlatformGesture(Key.A, KeyModifiers.Control) };
        selectAllItem.Click += (s, args) => TextEditor.SelectAll();
        contextMenu.Items.Add(selectAllItem);
    }

    /// <summary>
    /// Gets a platform-appropriate KeyGesture - uses Cmd on macOS, Ctrl on other platforms.
    /// </summary>
    private static KeyGesture GetPlatformGesture(Key key, KeyModifiers modifiers)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // Convert Ctrl to Meta (Cmd) on macOS
            if (modifiers.HasFlag(KeyModifiers.Control))
            {
                modifiers = (modifiers & ~KeyModifiers.Control) | KeyModifiers.Meta;
            }
        }
        return new KeyGesture(key, modifiers);
    }

    private string? GetWordAtCursor()
    {
        var text = TextEditor.Text;
        var caretIndex = TextEditor.CaretOffset;

        if (string.IsNullOrEmpty(text) || caretIndex < 0 || caretIndex > text.Length)
            return null;

        // Find word boundaries
        int start = caretIndex;
        int end = caretIndex;

        // Move start back to beginning of word
        while (start > 0 && char.IsLetter(text[start - 1]))
            start--;

        // Move end forward to end of word
        while (end < text.Length && char.IsLetter(text[end]))
            end++;

        if (start == end)
            return null;

        return text.Substring(start, end - start);
    }

    private void ReplaceWordAtCursor(string replacement)
    {
        var text = TextEditor.Text;
        var caretIndex = TextEditor.CaretOffset;

        if (string.IsNullOrEmpty(text) || caretIndex < 0 || caretIndex > text.Length)
            return;

        // Find word boundaries
        int start = caretIndex;
        int end = caretIndex;

        while (start > 0 && char.IsLetter(text[start - 1]))
            start--;

        while (end < text.Length && char.IsLetter(text[end]))
            end++;

        if (start == end)
            return;

        // Replace the word using document operations
        TextEditor.Document.Replace(start, end - start, replacement);
        TextEditor.CaretOffset = start + replacement.Length;
    }

    private void TextEditor_TextChanged(object? sender, EventArgs e)
    {
        _hasChanges = TextEditor.Text != _originalText;
        UpdateCharCount();

        // Restart token count timer
        _tokenCountTimer?.Stop();
        _tokenCountTimer?.Start();
    }

    private void UpdateCharCount()
    {
        var text = TextEditor.Text ?? "";
        var charCount = text.Length;
        var wordCount = string.IsNullOrWhiteSpace(text) ? 0 :
            text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;

        CharCount.Text = $"{charCount:N0} chars, {wordCount:N0} words";
    }

    private void Undo_Click(object? sender, RoutedEventArgs e)
    {
        TextEditor.Undo();
    }

    private void Redo_Click(object? sender, RoutedEventArgs e)
    {
        TextEditor.Redo();
    }

    private void Cut_Click(object? sender, RoutedEventArgs e)
    {
        TextEditor.Cut();
    }

    private void Copy_Click(object? sender, RoutedEventArgs e)
    {
        TextEditor.Copy();
    }

    private void Paste_Click(object? sender, RoutedEventArgs e)
    {
        TextEditor.Paste();
    }

    private void SelectAll_Click(object? sender, RoutedEventArgs e)
    {
        TextEditor.SelectAll();
    }

    private void WordWrap_Click(object? sender, RoutedEventArgs e)
    {
        var isWrapped = WordWrapCheck.IsChecked ?? false;
        WordWrapCheck.IsChecked = !isWrapped;
        TextEditor.WordWrap = !isWrapped;
    }

    private void Highlight_Click(object? sender, RoutedEventArgs e)
    {
        var isChecked = HighlightCheck.IsChecked ?? false;
        HighlightCheck.IsChecked = !isChecked;
        AppSettings.WriteDialog.Highlight = !isChecked;
        UpdateHighlightMenuState();
        UpdateSyntaxFlags();
    }

    private void HighlightNames_Click(object? sender, RoutedEventArgs e)
    {
        var isChecked = HighlightNamesCheck.IsChecked ?? false;
        HighlightNamesCheck.IsChecked = !isChecked;
        AppSettings.WriteDialog.HighlightNames = !isChecked;
        UpdateSyntaxFlags();
    }

    private void HighlightNumbers_Click(object? sender, RoutedEventArgs e)
    {
        var isChecked = HighlightNumbersCheck.IsChecked ?? false;
        HighlightNumbersCheck.IsChecked = !isChecked;
        AppSettings.WriteDialog.HighlightNumbers = !isChecked;
        UpdateSyntaxFlags();
    }

    private void HighlightPronouns_Click(object? sender, RoutedEventArgs e)
    {
        var isChecked = HighlightPronounsCheck.IsChecked ?? false;
        HighlightPronounsCheck.IsChecked = !isChecked;
        AppSettings.WriteDialog.HighlightPronouns = !isChecked;
        UpdateSyntaxFlags();
    }

    private void PopulateSpellCheckLanguageMenu()
    {
        SpellCheckLanguageMenu.Items.Clear();

        foreach (var dict in DictionaryService.Available)
        {
            var menuItem = new MenuItem
            {
                Header = dict.Value, // Display name (e.g., "English (US)")
                Tag = dict.Key // Locale code (e.g., "en_US")
            };
            menuItem.Click += async (s, e) =>
            {
                if (s is MenuItem item && item.Tag is string locale)
                {
                    await ChangeSpellCheckLanguage(locale);
                }
            };

            // Mark current language
            if (_spellCheckService?.CurrentLanguage == dict.Key)
            {
                menuItem.Icon = new CheckBox { IsChecked = true, BorderThickness = new Thickness(0) };
            }

            SpellCheckLanguageMenu.Items.Add(menuItem);
        }

        if (SpellCheckLanguageMenu.Items.Count == 0)
        {
            var noDict = new MenuItem { Header = "(No dictionaries available)", IsEnabled = false };
            SpellCheckLanguageMenu.Items.Add(noDict);
        }
    }

    private async Task ChangeSpellCheckLanguage(string locale)
    {
        if (_spellCheckService == null)
            return;

        var path = DictionaryService.GetDictionaryPath(locale);
        if (path != null)
        {
            var loaded = await _spellCheckService.LoadDictionaryFromDirectoryAsync(path, locale);
            if (loaded)
            {
                StatusText.Text = $"Spell check: {locale}";
                AppSettings.Settings.Dictionary = locale;
                PopulateSpellCheckLanguageMenu(); // Refresh checkmarks
            }
        }
    }

    private async void ChangeFont_Click(object? sender, RoutedEventArgs e)
    {
        // Use a simple dialog to pick font family and size
        var fontDialog = new FontPickerDialog();
        fontDialog.SelectedFontFamily = TextEditor.FontFamily.Name;
        fontDialog.SelectedFontSize = TextEditor.FontSize;

        await fontDialog.ShowDialog(this);

        if (fontDialog.DialogResult)
        {
            TextEditor.FontFamily = new Avalonia.Media.FontFamily(fontDialog.SelectedFontFamily);
            TextEditor.FontSize = fontDialog.SelectedFontSize;

            // Save to settings
            AppSettings.WriteDialog.FontFamily = fontDialog.SelectedFontFamily;
            AppSettings.WriteDialog.FontSize = fontDialog.SelectedFontSize;
        }
    }

    private async void Find_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = new FindDialog();
        if (TextEditor.SelectionLength > 0)
            dialog.Match = TextEditor.SelectedText;

        await dialog.ShowDialog(this);

        if (dialog.DialogResult && !string.IsNullOrEmpty(dialog.Match))
        {
            _lastFindText = dialog.Match;
            _lastFindMatchCase = dialog.MatchCase;
            _lastFindWholeWord = dialog.WholeWord;
            AppSettings.User.FindMatch = dialog.Match;
            AppSettings.User.FindMatchCase = dialog.MatchCase;
            AppSettings.User.FindWholeWords = dialog.WholeWord;

            FindText(_lastFindText, _lastFindMatchCase, _lastFindWholeWord, false);
        }
    }

    private void FindNext_Click(object? sender, RoutedEventArgs e)
    {
        var match = AppSettings.User.FindMatch;
        if (string.IsNullOrEmpty(match))
        {
            Find_Click(sender, e);
            return;
        }

        FindText(match, AppSettings.User.FindMatchCase, AppSettings.User.FindWholeWords, false);
    }

    private void FindPrevious_Click(object? sender, RoutedEventArgs e)
    {
        var match = AppSettings.User.FindMatch;
        if (string.IsNullOrEmpty(match))
        {
            Find_Click(sender, e);
            return;
        }

        FindText(match, AppSettings.User.FindMatchCase, AppSettings.User.FindWholeWords, true);
    }

    private void FindText(string match, bool matchCase, bool wholeWord, bool reverse)
    {
        var text = TextEditor.Text ?? "";
        if (string.IsNullOrEmpty(text))
        {
            StatusText.Text = "No match found";
            return;
        }

        var comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        int startPos = TextEditor.CaretOffset;

        if (reverse)
        {
            // Search backward from current position
            startPos = Math.Max(0, startPos - 1);
        }
        else
        {
            // Move past current selection when searching forward
            if (TextEditor.SelectionLength > 0)
                startPos = TextEditor.SelectionStart + TextEditor.SelectionLength;
        }

        int pos = -1;

        if (wholeWord)
        {
            var pattern = @"\b" + Regex.Escape(match) + @"\b";
            var options = matchCase ? RegexOptions.None : RegexOptions.IgnoreCase;

            if (reverse)
            {
                // Find all matches and get the one before startPos
                var matches = Regex.Matches(text, pattern, options);
                foreach (Match m in matches)
                {
                    if (m.Index < startPos)
                        pos = m.Index;
                    else
                        break;
                }

                // Wrap around
                if (pos == -1 && matches.Count > 0)
                {
                    var lastMatch = matches[matches.Count - 1];
                    if (lastMatch.Index != startPos)
                        pos = lastMatch.Index;
                }
            }
            else
            {
                var regex = new Regex(pattern, options);
                var m = regex.Match(text, startPos);
                if (m.Success)
                    pos = m.Index;

                // Wrap around
                if (pos == -1)
                {
                    m = regex.Match(text, 0);
                    if (m.Success)
                        pos = m.Index;
                }
            }
        }
        else
        {
            if (reverse)
            {
                pos = text.LastIndexOf(match, startPos, comparison);

                // Wrap around
                if (pos == -1)
                    pos = text.LastIndexOf(match, comparison);
            }
            else
            {
                pos = text.IndexOf(match, startPos, comparison);

                // Wrap around
                if (pos == -1)
                    pos = text.IndexOf(match, 0, comparison);
            }
        }

        if (pos >= 0)
        {
            TextEditor.CaretOffset = pos;
            TextEditor.Select(pos, match.Length);
            TextEditor.TextArea.Caret.BringCaretToView();
            StatusText.Text = "";
        }
        else
        {
            StatusText.Text = "No match found";
        }
    }

    private async void FindReplace_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = new FindReplaceDialog();
        if (TextEditor.SelectionLength > 0)
            dialog.FindText = TextEditor.SelectedText;

        dialog.OnFindNext = (findText, replaceText, matchCase, wholeWord) =>
        {
            FindText(findText, matchCase, wholeWord, false);
        };

        dialog.OnReplace = (findText, replaceText, matchCase, wholeWord) =>
        {
            // Replace current selection if it matches
            if (TextEditor.SelectionLength > 0)
            {
                var selectedText = TextEditor.SelectedText;
                var comparison = matchCase ? System.StringComparison.Ordinal : System.StringComparison.OrdinalIgnoreCase;
                if (selectedText.Equals(findText, comparison))
                {
                    TextEditor.Document.Replace(TextEditor.SelectionStart, TextEditor.SelectionLength, replaceText);
                    StatusText.Text = "Replaced";
                }
            }
            // Find next
            FindText(findText, matchCase, wholeWord, false);
        };

        dialog.OnReplaceAll = (findText, replaceText, matchCase, wholeWord) =>
        {
            var text = TextEditor.Text ?? "";
            var replacements = FindReplace.Replace(
                ref text,
                findText,
                replaceText,
                wholeWord,
                !matchCase);

            if (replacements > 0)
            {
                TextEditor.Text = text;
                dialog.SetStatus($"Replaced {replacements} occurrence(s)");
            }
            else
            {
                dialog.SetStatus("No replacements made");
            }
        };

        await dialog.ShowDialog(this);
    }

    private async void SwapPronouns_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = new GenderSwapDialog();
        await dialog.ShowDialog(this);

        if (dialog.DialogResult)
        {
            var text = TextEditor.Text ?? "";
            var replacements = GenderSwap.SwapGenders(
                ref text,
                dialog.CharacterFrom,
                dialog.CharacterTo,
                dialog.UserFrom,
                dialog.UserTo,
                dialog.SwapCharacter,
                dialog.SwapUser);

            if (replacements > 0)
            {
                TextEditor.Text = text;
                StatusText.Text = $"Replaced {replacements} occurrence(s)";
            }
            else
            {
                StatusText.Text = "No replacements made";
            }
        }
    }

    private void Ok_Click(object? sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private async void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        if (_hasChanges)
        {
            var messageBox = new MessageBoxDialog
            {
                Title = "Confirm",
                Message = "You have unsaved changes. Apply changes before closing?",
                Buttons = MessageBoxButtons.YesNoCancel
            };

            var result = await messageBox.ShowDialog<MessageBoxResult>(this);

            if (result == MessageBoxResult.Yes)
            {
                DialogResult = true;
                Close();
            }
            else if (result == MessageBoxResult.No)
            {
                DialogResult = false;
                Close();
            }
            // Cancel - do nothing, stay open
        }
        else
        {
            DialogResult = false;
            Close();
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        var isMac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        var cmdOrCtrl = isMac ? KeyModifiers.Meta : KeyModifiers.Control;

        if (e.Key == Key.Escape)
        {
            Cancel_Click(this, new RoutedEventArgs());
            e.Handled = true;
        }
        // Cmd+Enter (Mac) or Ctrl+Enter (Win/Linux): OK
        else if (e.KeyModifiers == cmdOrCtrl && e.Key == Key.Enter)
        {
            Ok_Click(this, new RoutedEventArgs());
            e.Handled = true;
        }
        // Cmd+F (Mac) or Ctrl+F (Win/Linux): Find
        else if (e.KeyModifiers == cmdOrCtrl && e.Key == Key.F)
        {
            Find_Click(this, new RoutedEventArgs());
            e.Handled = true;
        }
        // Cmd+Alt+F (Mac) or Ctrl+H (Win/Linux): Find and Replace
        else if ((isMac && e.KeyModifiers == (KeyModifiers.Meta | KeyModifiers.Alt) && e.Key == Key.F) ||
                 (!isMac && e.KeyModifiers == KeyModifiers.Control && e.Key == Key.H))
        {
            FindReplace_Click(this, new RoutedEventArgs());
            e.Handled = true;
        }
        // Cmd+G / Cmd+Shift+G (Mac) or F3 / Shift+F3 (Win/Linux): Find Next/Previous
        else if (e.Key == Key.F3 ||
                 (isMac && e.Key == Key.G && e.KeyModifiers.HasFlag(KeyModifiers.Meta)))
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                FindPrevious_Click(this, new RoutedEventArgs());
            else
                FindNext_Click(this, new RoutedEventArgs());
            e.Handled = true;
        }
    }
}
