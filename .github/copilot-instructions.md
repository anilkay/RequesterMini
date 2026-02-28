# RequesterMini — Copilot Instructions

## Tech Stack

- .NET 10 / C# 14 — use `field` keyword, modern APIs, nullable enabled everywhere
- Avalonia UI 11.3 — Fluent theme, compiled bindings (`AvaloniaUseCompiledBindingsByDefault=true`)
- ReactiveUI — MVVM, `ReactiveObject`, `ReactiveCommand`, `MessageBus`, `Interaction`
- OneOf — discriminated union result types (no exceptions for control flow)
- System.Text.Json — source-generated only; reflection is **disabled** (`JsonSerializerIsReflectionEnabledByDefault=false`)
- xUnit — testing framework

## Solution Structure

```
RequesterMini.slnx
├── src/
│   ├── RequesterMini/          # Main Avalonia WinExe app
│   │   ├── Views/              # AXAML views + code-behind pairs
│   │   ├── ViewModels/         # ReactiveUI ViewModels
│   │   ├── Models/             # Immutable DTOs and result types
│   │   ├── Utils/              # App-scoped utilities (MakeRequest, timers, colorizer)
│   │   └── Constants/          # Static string/config constants
│   ├── AppLogger/              # Class library — file-based structured logging
│   ├── CurlExporter/           # Class library — fluent cURL command builder
│   └── JsonFileStore/          # Class library — generic JSON file persistence
└── tests/
    ├── AppLogger.Tests/
    ├── CurlExporter.Tests/
    └── JsonFileStore.Tests/
```

New reusable logic goes in a class library under `src/` with a paired test project under `tests/`. App-specific UI logic stays in the main project.

## Coding Conventions

### ReactiveUI Properties

Use the C# 14 `field` keyword — no explicit backing fields:

```csharp
internal string Url
{
    get;
    set => this.RaiseAndSetIfChanged(ref field, value);
} = HttpConstants.StartUrl;
```

Use `ObservableCollection` for lists:

```csharp
internal ObservableCollection<HeaderItem> Headers { get; } = [];
```

### Commands

```csharp
internal ReactiveCommand<Unit, Unit> ClickCommand { get; }
internal ReactiveCommand<OldRequestDto, Unit> RemoveCommand { get; }
```

- Sync: `ReactiveCommand.Create(...)`
- Async: `ReactiveCommand.CreateFromTask(async () => ...)`
- Always mark constructors that create `ReactiveCommand` with `[RequiresUnreferencedCode("Uses ReactiveCommand")]`

### Visibility

| Location | Modifier |
|---|---|
| ViewModel properties & commands | `internal` |
| ViewModel classes | `public` (required for AXAML `x:DataType`) |
| Library public API | `public` |
| Utility/helper classes in main app | `internal static` |
| `SourceGenerationContext` | `internal partial class` |

### ViewModel ↔ View Communication

**Bindings** for data, **commands** for actions, **`Interaction<TIn, TOut>`** for platform services:

```csharp
// ViewModel
internal Interaction<string, Unit> CopyToClipboard { get; } = new();

// View code-behind (register in DataContextChanged, not WhenActivated)
vm.CopyToClipboard.RegisterHandler(async interaction =>
{
    await GetTopLevel(this)?.Clipboard?.SetTextAsync(interaction.Input);
    interaction.SetOutput(Unit.Default);
});
```

**MessageBus** for cross-ViewModel events:

```csharp
// Send
MessageBus.Current.SendMessage(payload, MessageBusConstants.NewRequest);

// Receive
MessageBus.Current.Listen<string>(MessageBusConstants.NewRequest)
    .Subscribe(value => { ... });
```

Always use `ObserveOn(RxApp.MainThreadScheduler)` before `.Subscribe()` in code-behind when updating UI elements from a `WhenAnyValue` subscription.

### JSON Serialization

Always use `SourceGenerationContext` — never `new JsonSerializerOptions()` with reflection:

```csharp
// Register types
[JsonSerializable(typeof(OldRequestDto))]
[JsonSerializable(typeof(List<OldRequestDto>))]
internal partial class SourceGenerationContext : JsonSerializerContext { }

// Use
JsonSerializer.Serialize(item, SourceGenerationContext.Default.OldRequestDto);
JsonSerializer.Deserialize<OldRequestDto>(json, SourceGenerationContext.Default.OldRequestDto);
```

For pretty-printing only (no type), use `Utf8JsonWriter` directly — it is trimming-safe:

```csharp
using var doc = JsonDocument.Parse(json);
using var ms = new MemoryStream();
using var writer = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = true });
doc.WriteTo(writer);
writer.Flush();
```

### Result Types

Use `OneOf<RequestSuccess, RequestFailure>` — never throw for expected failures:

```csharp
return result.Match(
    success => ...,
    failure => ...
);
```

### Error Handling in Libraries

Use built-in throw helpers:

```csharp
ArgumentException.ThrowIfNullOrWhiteSpace(url);
ArgumentNullException.ThrowIfNull(headers);
```

## AXAML Conventions

- Declare `x:DataType` on the root element for compiled bindings
- Always include a `Design.DataContext` for IDE preview
- Use `Command="{Binding CommandName}"` for button actions
- Use `$parent[UserControl].((vm:SomeViewModel)DataContext).CommandName` to reach parent ViewModel inside `DataTemplate`
- Preferred controls: `Grid`, `StackPanel`, `ScrollViewer`, `TabControl`, `ItemsControl`, `Border`, `ComboBox`, `TextBox` with `Watermark`
- Button styling via `Classes="primary"` / `Classes="danger"` / `Classes="secondary"`
- `SelectableTextBlock` for read-only but copyable text (e.g. response body)

## Class Libraries

1. Target `net10.0` SDK-style `classlib`.
2. `public` API with argument validation at entry points.
3. Fluent builder pattern for multi-step construction.
4. Map string concepts to strongly-typed enums inside the library.
5. No UI dependencies — fully testable in isolation.

## Testing

- Framework: xUnit v2.9+
- Naming: `MethodName_Scenario_ExpectedBehavior`
- Use `[Fact]` for single cases, `[Theory] + [InlineData]` for parameterized
- One test class per production class, named `{ClassName}Tests`
- Isolate file I/O with per-test temp directories: `Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())`
- Implement `IDisposable` to clean up temp state
- Cover: happy path, edge cases, invalid input (throws), enum/mapping completeness

## Build & Publishing

```xml
<PublishTrimmed>true</PublishTrimmed>
<PublishSingleFile>true</PublishSingleFile>
<JsonSerializerIsReflectionEnabledByDefault>false</JsonSerializerIsReflectionEnabledByDefault>
<AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
```

Publish commands:

```bash
# Windows
dotnet publish -r win-x64 --self-contained true -c Release

# macOS
dotnet publish -r osx-arm64 --self-contained true -c Release
```

Required native DLLs to ship alongside the exe: `av_libglesv2.dll`, `libHarfBuzzSharp.dll`, `libSkiaSharp.dll`.

## Adding Features Checklist

1. UI/ViewModel logic → `src/RequesterMini/`
2. Reusable logic → new or existing class library in `src/`, with tests in `tests/`
3. Wire buttons via `Command="{Binding ...}"`, platform services via `Interaction<,>`
4. Add new serializable types to `SourceGenerationContext`
5. Run `dotnet build RequesterMini.slnx` and `dotnet test RequesterMini.slnx` before finishing
