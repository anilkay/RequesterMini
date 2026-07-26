using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using ReactiveUI;
using ReactiveUI.Reactive;

namespace RequesterMini.ViewModels;

/// <summary>
/// One editable row in the Params tab. Mirrors <see cref="HeaderItem"/> but stays a separate type so
/// query-param rows can diverge from header rows without disturbing the Headers tab.
/// </summary>
public class QueryParamItem : ViewModelBase
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

    public Action<QueryParamItem>? OnRemove { get; set; }

    [RequiresUnreferencedCode("Uses ReactiveCommand")]
    public QueryParamItem()
    {
        RemoveCommand = ReactiveCommand.Create(Remove);
    }

    private void Remove()
    {
        OnRemove?.Invoke(this);
    }
}
