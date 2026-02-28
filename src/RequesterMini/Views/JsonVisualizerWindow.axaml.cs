using System;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using ReactiveUI;
using RequesterMini.Utils;
using RequesterMini.ViewModels;

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
            _subscription = vm.WhenAnyValue(x => x.PrettyJsonValue)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(UpdateInlines);
        }
    }

    private void UpdateInlines(string json)
    {
        JsonTextBlock.Inlines ??= new InlineCollection();
        JsonTextBlock.Inlines.Clear();

        if (string.IsNullOrEmpty(json)) return;

        foreach (var (text, brush) in JsonColorizer.Colorize(json))
        {
            var run = new Run(text);
            if (brush is not null) run.Foreground = brush;
            JsonTextBlock.Inlines.Add(run);
        }
    }
}