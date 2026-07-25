using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using ReactiveUI;
using RequesterMini.Utils;
using RequesterMini.ViewModels;
using SyntaxHighlighter;

namespace RequesterMini.Views;

public partial class JsonVisualizerWindow : UserControl
{
    private IDisposable? _subscription;

    public JsonVisualizerWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        DataContext = new JsonVisualizerWindowViewModel();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        _subscription?.Dispose();
        if (DataContext is JsonVisualizerWindowViewModel vm)
        {
            _subscription = vm.WhenAnyValue(x => x.PrettyContentValue, x => x.ContentKind)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(t => UpdateInlines(t.Item1, t.Item2));
        }
    }

    private void UpdateInlines(string content, PreviewContentKind kind)
    {
        JsonTextBlock.Inlines ??= new InlineCollection();
        JsonTextBlock.Inlines.Clear();

        if (string.IsNullOrEmpty(content)) return;

        var segments = kind switch
        {
            PreviewContentKind.Json => JsonHighlighter.Highlight(content),
            PreviewContentKind.Xml => XmlHighlighter.Highlight(content),
            _ => new List<HighlightSegment> { new(content, TokenKind.None) },
        };

        foreach (var (text, tokenKind) in segments)
        {
            var run = new Run(text);
            if (HighlightBrushes.For(tokenKind) is { } brush) run.Foreground = brush;
            JsonTextBlock.Inlines.Add(run);
        }
    }
}
