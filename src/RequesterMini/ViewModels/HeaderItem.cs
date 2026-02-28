using System;
using System.Reactive;
using ReactiveUI;

namespace RequesterMini.ViewModels;

public class HeaderItem : ViewModelBase
{
    public string Key
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = "";

    public string Value
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = "";

    public bool IsEnabled
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = true;

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
