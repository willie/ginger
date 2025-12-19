using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using Ginger.Services;
using TextMateSharp.Grammars;

namespace Ginger.Views;

public partial class TestWindow : Window
{
    private TextEditor? _editor;
    private GingerSyntaxColorizer? _colorizer;

    public TestWindow()
    {
        InitializeComponent();

        _editor = this.FindControl<TextEditor>("Editor");
        if (_editor != null)
        {
            // Set up like the demo does
            _editor.Background = Brushes.Transparent;
            _editor.ShowLineNumbers = true;

            // Install TextMate like the demo - THIS IS KEY
            var registryOptions = new RegistryOptions(ThemeName.DarkPlus);
            var installation = _editor.InstallTextMate(registryOptions);

            // Set some sample text
            _editor.Text = "Test: {char} says \"Hello\" with numbers 123 and *actions*\nLine 2 with {user} and more \"dialogue\"";

            Console.WriteLine("TextMate installed!");
        }
    }

    private void OnAddColorizer(object? sender, RoutedEventArgs e)
    {
        if (_editor == null || _colorizer != null)
            return;

        Console.WriteLine($"Adding colorizer...");
        Console.WriteLine($"VisualLinesValid: {_editor.TextArea.TextView.VisualLinesValid}");
        Console.WriteLine($"Bounds: {_editor.TextArea.TextView.Bounds}");

        _colorizer = new GingerSyntaxColorizer();
        _editor.TextArea.TextView.LineTransformers.Add(_colorizer);

        Console.WriteLine($"Colorizer added. Count: {_editor.TextArea.TextView.LineTransformers.Count}");

        _editor.TextArea.TextView.Redraw();
        Console.WriteLine("Redraw called");
    }
}
