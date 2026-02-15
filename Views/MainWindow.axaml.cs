using System;
using System.Reactive;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using RequesterMini.ViewModels;

namespace RequesterMini.Views;

public partial class MainWindow : Window
{
    private IDisposable? _clipboardHandler;

    public MainWindow()
    {
        InitializeComponent();

        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        _clipboardHandler?.Dispose();

        if (DataContext is MainWindowViewModel vm)
        {
            _clipboardHandler = vm.CopyToClipboard.RegisterHandler(async interaction =>
            {
                IClipboard? clipboard = GetTopLevel(this)?.Clipboard;
                if (clipboard is not null)
                {
                    await clipboard.SetTextAsync(interaction.Input);
                }

                interaction.SetOutput(Unit.Default);
            });
        }
    }
}