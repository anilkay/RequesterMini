# RequesterMini — Copilot Instructions

## Tech Stack

- .NET 10 / C# 14 (use `field` keyword, modern APIs)
- Avalonia UI 11.3 with Fluent theme and compiled bindings (`x:DataType`)
- ReactiveUI for MVVM
- OneOf for discriminated union result types
- System.Text.Json with source generation (reflection disabled, publish-trimmed)
- xUnit for testing

## Solution Structure

```
RequesterMini.sln
├── RequesterMini.csproj          # Main Avalonia WinExe app
│   ├── Views/                    # Avalonia AXAML views + code-behind
│   ├── ViewModels/               # ReactiveUI ViewModels
│   ├── Models/                   # DTOs and result types (OneOf)
│   ├── Utils/                    # Utilities (MakeRequest, timers)
│   └── Constants/                # Static config values
├── CurlExporter/                 # Class library (reusable, no UI deps)
│   └── CurlCommandBuilder.cs    # Fluent builder for cURL commands
└── CurlExporter.Tests/           # xUnit test project
```

`CurlExporter/` and `CurlExporter.Tests/` are subdirectories of the main project. The main `.csproj` excludes them via `DefaultItemExcludes`. Always add this exclusion when creating new projects as subdirectories:

```xml
<DefaultItemExcludes>$(DefaultItemExcludes);NewProject\**</DefaultItemExcludes>
```

## Coding Conventions

### Properties

Use the C# 14 `field` keyword for semi-auto properties with ReactiveUI:

```csharp
internal string ResponseStatusCode
{
    get;
    set => this.RaiseAndSetIfChanged(ref field, value);
} = "";
```

### Commands

- Use `ReactiveCommand.Create(...)` for synchronous commands.
- Use `ReactiveCommand.CreateFromTask(...)` for async commands.
- Mark constructors containing `ReactiveCommand` creation with `[RequiresUnreferencedCode]`.

### ViewModel-to-View Communication

Use `Interaction<TInput, TOutput>` for platform-specific operations (clipboard, dialogs). Register handlers in View code-behind via `DataContextChanged`, not `WhenActivated`.

```csharp
// ViewModel — declare and fire
internal Interaction<string, Unit> CopyToClipboard { get; } = new();
CopyToClipboard.Handle(text).Subscribe();

// View — register handler
vm.CopyToClipboard.RegisterHandler(async interaction =>
{
    var clipboard = GetTopLevel(this)?.Clipboard;
    if (clipboard is not null)
        await clipboard.SetTextAsync(interaction.Input);
    interaction.SetOutput(Unit.Default);
});
```

### Visibility

- ViewModel properties and commands: `internal`.
- Public classes in class libraries: `public`.
- Utility classes in the main app: `internal static`.

### JSON Serialization

Use source-generated `JsonSerializerContext` (`SourceGenerationContext`). Never use reflection-based serialization.

### Result Types

Use `OneOf<TSuccess, TFailure>` for operation results instead of exceptions for control flow.

## Class Libraries

When extracting reusable logic:

1. Target `net10.0` as an SDK-style `classlib`.
2. Use `public` API with argument validation (`ArgumentException.ThrowIfNullOrWhiteSpace`, etc.).
3. Prefer fluent builder patterns for multi-parameter construction.
4. Map application-specific string concepts into strongly-typed enums inside the library.
5. Eliminate thin wrapper/helper classes — callers use the library API directly.

## Testing

- Framework: xUnit (v2.9+).
- Use `[Fact]` for single-case tests and `[Theory]` with `[InlineData]` for parameterized tests.
- Naming: `MethodName_Scenario_ExpectedBehavior`.
- Cover: happy paths, edge cases (escaping, empty input), validation (throws on invalid state), and enum/mapping completeness.

## Adding Features

1. App-specific features (UI, ViewModel wiring) go in the main project.
2. Reusable, UI-independent logic goes in a class library with tests.
3. Wire ViewModel to View using commands for buttons and interactions for platform services.
4. Add AXAML controls with `Command="{Binding CommandName}"`.
5. Write tests for any library-level logic.
6. Build the full solution and run tests before finishing.
