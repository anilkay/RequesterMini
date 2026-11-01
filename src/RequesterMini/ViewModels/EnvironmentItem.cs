using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using ReactiveUI;
using ReactiveUI.Reactive;
using Templating;

namespace RequesterMini.ViewModels;

/// <summary>One editable <c>{{name}}</c> / value pair inside an environment.</summary>
public class VariableItem : ViewModelBase
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

    public ReactiveCommand<Unit, Unit> RemoveCommand { get; }

    public Action<VariableItem>? OnRemove { get; set; }

    [RequiresUnreferencedCode("Uses ReactiveCommand")]
    public VariableItem()
    {
        RemoveCommand = ReactiveCommand.Create(() => OnRemove?.Invoke(this));
    }
}

/// <summary>A named environment and its variables, in the editable shape the view binds to.</summary>
public class EnvironmentItem : ViewModelBase
{
    public EnvironmentItem(string name, IEnumerable<KeyValuePair<string, string>>? variables = null)
    {
        Name = name;
        Variables = [];

        if (variables is null) return;

        foreach (var (key, value) in variables)
        {
            Variables.Add(new VariableItem { Key = key, Value = value });
        }
    }

    public string Name { get; }

    public ObservableCollection<VariableItem> Variables { get; }

    /// <summary>Drives the "Active" marker in the list; the owning ViewModel keeps it in step.</summary>
    public bool IsActive
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>Blank keys are dropped, and a repeated key keeps its last value.</summary>
    public Dictionary<string, string> ToVariableMap()
    {
        var map = new Dictionary<string, string>();
        foreach (var variable in Variables)
        {
            var key = variable.Key.Trim();
            if (key.Length == 0) continue;

            map[key] = variable.Value;
        }

        return map;
    }

    public VariableEnvironment ToDto() => new(Name, ToVariableMap());
}
