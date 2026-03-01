using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using RequesterMini.ViewModels;

namespace RequesterMini.Views;

public partial class MainWindow : Window
{
    private IDisposable? _clipboardHandler;
    private IDisposable? _bruFileHandler;

    public MainWindow()
    {
        InitializeComponent();

        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        _clipboardHandler?.Dispose();
        _bruFileHandler?.Dispose();

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

            _bruFileHandler = vm.OpenBruFile.RegisterHandler(async interaction =>
            {
                var topLevel = GetTopLevel(this);
                string? filePath = null;

                if (topLevel is not null)
                {
                    var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                    {
                        Title = "Import Bruno Request",
                        AllowMultiple = false,
                        FileTypeFilter = new List<FilePickerFileType>
                        {
                            new FilePickerFileType("Bruno Request") { Patterns = ["*.bru"] }
                        }
                    });

                    if (files.Count > 0)
                    {
                        filePath = files[0].TryGetLocalPath();
                    }
                }

                interaction.SetOutput(filePath);
            });
        }
    }
}