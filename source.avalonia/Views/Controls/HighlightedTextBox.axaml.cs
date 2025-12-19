using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Rendering;
using Ginger.Services;

namespace Ginger.Views.Controls;

/// <summary>
/// A TextBox-like control that wraps AvaloniaEdit TextEditor with syntax highlighting.
/// Supports two-way binding on the Text property.
/// </summary>
public partial class HighlightedTextBox : UserControl
{
    private bool _isUpdatingText;
    private GingerSyntaxColorizer? _colorizer;
    private TextEditor? _textEditor;
    private Border? _highlightRect;
    private bool _colorizerInitialized;

    #region Styled Properties

    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<HighlightedTextBox, string>(
            nameof(Text),
            defaultValue: string.Empty,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> IsReadOnlyProperty =
        AvaloniaProperty.Register<HighlightedTextBox, bool>(
            nameof(IsReadOnly),
            defaultValue: false);

    public static readonly StyledProperty<bool> AcceptsReturnProperty =
        AvaloniaProperty.Register<HighlightedTextBox, bool>(
            nameof(AcceptsReturn),
            defaultValue: true);

    public static readonly StyledProperty<TextWrapping> TextWrappingProperty =
        AvaloniaProperty.Register<HighlightedTextBox, TextWrapping>(
            nameof(TextWrapping),
            defaultValue: TextWrapping.Wrap);

    public static readonly StyledProperty<string?> WatermarkProperty =
        AvaloniaProperty.Register<HighlightedTextBox, string?>(
            nameof(Watermark),
            defaultValue: null);

    public static readonly StyledProperty<bool> EnableHighlightingProperty =
        AvaloniaProperty.Register<HighlightedTextBox, bool>(
            nameof(EnableHighlighting),
            defaultValue: true);

    #endregion

    #region Properties

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public bool IsReadOnly
    {
        get => GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    public bool AcceptsReturn
    {
        get => GetValue(AcceptsReturnProperty);
        set => SetValue(AcceptsReturnProperty, value);
    }

    public TextWrapping TextWrapping
    {
        get => GetValue(TextWrappingProperty);
        set => SetValue(TextWrappingProperty, value);
    }

    public string? Watermark
    {
        get => GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }

    public bool EnableHighlighting
    {
        get => GetValue(EnableHighlightingProperty);
        set => SetValue(EnableHighlightingProperty, value);
    }

    #endregion

    public HighlightedTextBox()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnLoaded(e);

        _textEditor = this.FindControl<TextEditor>("TextEditor");
        _highlightRect = this.FindControl<Border>("HighlightRect");

        if (_textEditor == null)
            return;

        // Wire up TextEditor events
        _textEditor.TextChanged += OnTextEditorTextChanged;
        _textEditor.AttachedToVisualTree += OnEditorAttachedToVisualTree;

        // Apply initial properties
        ApplyPropertiesToEditor();

        // Set initial text
        if (!string.IsNullOrEmpty(Text) && _textEditor.Text != Text)
        {
            _isUpdatingText = true;
            _textEditor.Text = Text;
            _isUpdatingText = false;
        }

        // Subscribe to name changes
        SyntaxHighlightBroadcaster.NamesChanged += OnNamesChanged;

        // Subscribe to visual lines changes to detect when rendering starts
        _textEditor.TextArea.TextView.VisualLinesChanged += OnVisualLinesChanged;

        // Initialize syntax highlighting if control is visible
        // The colorizer only works when visual lines can be constructed
        TryInitializeColorizer();
    }

    protected override void OnUnloaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        // Unsubscribe from events
        SyntaxHighlightBroadcaster.NamesChanged -= OnNamesChanged;

        if (_textEditor != null)
        {
            _textEditor.TextChanged -= OnTextEditorTextChanged;
            _textEditor.AttachedToVisualTree -= OnEditorAttachedToVisualTree;
            _textEditor.TextArea.TextView.VisualLinesChanged -= OnVisualLinesChanged;
        }
    }

    private void OnVisualLinesChanged(object? sender, EventArgs e)
    {
        var valid = _textEditor?.TextArea.TextView.VisualLinesValid ?? false;

        // If visual lines just became valid and we haven't colorized yet, do it now
        if (valid && !_colorizerInitialized)
        {
            TryInitializeColorizer();
        }
    }

    private void OnEditorAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        TryInitializeColorizer();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TextProperty)
        {
            OnTextPropertyChanged(change.GetNewValue<string>() ?? "");
        }
        else if (change.Property == IsReadOnlyProperty ||
                 change.Property == TextWrappingProperty ||
                 change.Property == Layoutable.MinHeightProperty ||
                 change.Property == TemplatedControl.FontFamilyProperty ||
                 change.Property == TemplatedControl.FontSizeProperty)
        {
            ApplyPropertiesToEditor();
        }
        else if (change.Property == EnableHighlightingProperty)
        {
            UpdateHighlightingState();
        }
        else if (change.Property == IsVisibleProperty)
        {
            // When control becomes visible, try to initialize colorizer
            if (change.GetNewValue<bool>())
            {
                TryInitializeColorizer();
            }
        }
    }

    private void TryInitializeColorizer()
    {
        if (_colorizerInitialized || _textEditor == null || !EnableHighlighting || !IsVisible)
            return;

        _colorizer = new GingerSyntaxColorizer();
        UpdateCharacterNames();

        _textEditor.TextArea.TextView.LineTransformers.Add(_colorizer);
        _colorizerInitialized = true;

        // Force a redraw to apply highlighting - defer to ensure layout is complete
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            _textEditor?.TextArea.TextView.Redraw();
            _textEditor?.TextArea.TextView.InvalidateVisual();
        }, Avalonia.Threading.DispatcherPriority.Background);
    }

    private void UpdateHighlightingState()
    {
        if (_textEditor == null)
            return;

        if (EnableHighlighting && _colorizer == null)
        {
            TryInitializeColorizer();
        }
        else if (!EnableHighlighting && _colorizer != null)
        {
            _textEditor.TextArea.TextView.LineTransformers.Remove(_colorizer);
            _colorizer = null;
            _colorizerInitialized = false;
        }

        _textEditor.TextArea.TextView.Redraw();
    }

    private void UpdateCharacterNames()
    {
        if (_colorizer == null)
            return;

        var (names, variables) = SyntaxHighlightBroadcaster.GetCurrentNames();
        _colorizer.SetCharacterNames(names);
        _colorizer.SetVariableNames(variables);
    }

    private void OnNamesChanged()
    {
        // Must dispatch to UI thread since this can be called from anywhere
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            UpdateCharacterNames();
            _textEditor?.TextArea.TextView.Redraw();
        });
    }

    private void OnTextEditorTextChanged(object? sender, EventArgs e)
    {
        if (_isUpdatingText || _textEditor == null)
            return;

        _isUpdatingText = true;
        SetCurrentValue(TextProperty, _textEditor.Text ?? "");
        _isUpdatingText = false;

        // Try to initialize colorizer when text changes (control might now be visible)
        if (!_colorizerInitialized && EnableHighlighting)
        {
            TryInitializeColorizer();
        }
    }

    private void OnTextPropertyChanged(string newValue)
    {
        if (_isUpdatingText || _textEditor == null)
            return;

        if (_textEditor.Text != newValue)
        {
            _isUpdatingText = true;
            _textEditor.Text = newValue;
            _isUpdatingText = false;
        }
    }

    private void ApplyPropertiesToEditor()
    {
        if (_textEditor == null)
            return;

        _textEditor.IsReadOnly = IsReadOnly;
        _textEditor.WordWrap = TextWrapping == TextWrapping.Wrap;
        _textEditor.MinHeight = MinHeight;
        _textEditor.FontFamily = FontFamily;
        _textEditor.FontSize = FontSize;
    }

    /// <summary>
    /// Scroll to and highlight a range of text using Canvas overlay (works without focus).
    /// </summary>
    public void FocusAndSelect(int start = 0, int length = 0)
    {
        if (_textEditor == null || _highlightRect == null)
            return;

        var text = _textEditor.Text ?? "";

        if (length > 0 && start >= 0 && start + length <= text.Length)
        {
            // Scroll to make the highlight visible
            var location = _textEditor.Document.GetLocation(start);
            _textEditor.ScrollTo(location.Line, location.Column);

            // Get visual position of the text range using Canvas overlay
            var textView = _textEditor.TextArea.TextView;
            var document = _textEditor.Document;

            var startPos = textView.GetVisualPosition(
                new AvaloniaEdit.TextViewPosition(document.GetLocation(start)),
                VisualYPosition.LineTop);

            var endPos = textView.GetVisualPosition(
                new AvaloniaEdit.TextViewPosition(document.GetLocation(start + length)),
                VisualYPosition.LineBottom);

            // Position the highlight rectangle
            Canvas.SetLeft(_highlightRect, startPos.X);
            Canvas.SetTop(_highlightRect, startPos.Y);
            _highlightRect.Width = endPos.X - startPos.X;
            _highlightRect.Height = endPos.Y - startPos.Y;
            _highlightRect.IsVisible = true;
        }
    }

    /// <summary>
    /// Clear any search highlight.
    /// </summary>
    public void ClearSearchHighlight()
    {
        if (_highlightRect != null)
        {
            _highlightRect.IsVisible = false;
        }
    }

    /// <summary>
    /// Focus the text editor.
    /// </summary>
    public void FocusEditor()
    {
        _textEditor?.Focus();
    }
}
