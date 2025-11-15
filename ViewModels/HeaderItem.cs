using System;
using System.Reactive;
using ReactiveUI;

namespace RequesterMini.ViewModels;

public class HeaderItem : ViewModelBase
{
    private string _key = "";
    private string _value = "";
    private bool _isEnabled = true;

    public string Key
    {
        get => _key;
        set => this.RaiseAndSetIfChanged(ref _key, value);
    }

    public string Value
    {
        get => _value;
        set => this.RaiseAndSetIfChanged(ref _value, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => this.RaiseAndSetIfChanged(ref _isEnabled, value);
    }

    public ReactiveCommand<Unit, Unit> RemoveCommand { get; }
    
    public Action<HeaderItem>? OnRemove { get; set; }

    public HeaderItem()
    {
        RemoveCommand = ReactiveCommand.Create(Remove);
    }

    private void Remove()
    {
        OnRemove?.Invoke(this);
    }
}
