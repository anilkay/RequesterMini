using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using RequesterMini.ViewModels;
using RequesterMini.Views;

namespace RequesterMini;

public class ViewLocator : IDataTemplate
{
    private static readonly Dictionary<Type, Func<Control>> _map = new()
    {
        { typeof(OldRequestsWindowViewModel), () => new OldRequestWindow() },
        { typeof(JsonVisualizerWindowViewModel), () => new JsonVisualizerWindow() },
    };

    public Control? Build(object? data)
    {
        if (data is null)
            return null;

        if (_map.TryGetValue(data.GetType(), out var factory))
        {
            var control = factory();
            control.DataContext = data;
            return control;
        }

        return new TextBlock { Text = "Not Found: " + data.GetType().FullName };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}
