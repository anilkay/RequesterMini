# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

RequesterMini is a cross-platform desktop HTTP request client (a mini Postman/Insomnia) built with Avalonia + ReactiveUI on .NET 10.

## Conventions

`.github/copilot-instructions.md` is the authoritative style guide (ReactiveUI property/command patterns, `field`-keyword usage, visibility rules, source-gen JSON, `OneOf` result types, AXAML rules, testing conventions). Read it before writing code — the rules there are enforced and non-obvious. Do not duplicate it here.

`.github/agents/*.agent.md` contain role-specific checklists (`view`, `viewmodel`, `library`, `test-writer`) that mirror those conventions for narrower tasks.

## Commands

Everything operates on the `RequesterMini.slnx` solution (an XML-based `.slnx`, not a `.sln`).

```bash
dotnet build RequesterMini.slnx          # build all projects
dotnet run --project src/RequesterMini    # run the desktop app
dotnet test RequesterMini.slnx            # run all test projects

# run one test project
dotnet test tests/CurlExporter.Tests/CurlExporter.Tests.csproj

# run a single test by name
dotnet test RequesterMini.slnx --filter "FullyQualifiedName~Build_GetMethod_OmitsXFlag"
dotnet test RequesterMini.slnx --filter "DisplayName~SomeScenario"
```

Publishing (single-file, trimmed, self-contained — configured in the csproj):

```bash
dotnet publish -r win-x64  --self-contained true -c Release
dotnet publish -r osx-arm64 --self-contained true -c Release
```

The published exe requires these native files shipped alongside it: `av_libglesv2.dll`, `libHarfBuzzSharp.dll`, `libSkiaSharp.dll`.

## Architecture

### Solution layout
- `src/RequesterMini/` — the Avalonia `WinExe`. Contains `Views/` (AXAML + code-behind), `ViewModels/`, `Models/`, `Utils/`, `Constants/`.
- `src/AppLogger/`, `src/CurlExporter/`, `src/JsonFileStore/`, `src/BrunoImporter/` — standalone `net10.0` class libraries with zero UI dependencies, each with a paired `tests/*.Tests` project. **Reusable, UI-free logic belongs in a library so it can be unit-tested in isolation**; only UI/ViewModel glue stays in the main app.

### Trimming & reflection are hard constraints
The app publishes trimmed + single-file, and reflection-based JSON is disabled (`JsonSerializerIsReflectionEnabledByDefault=false`). Consequences that shape the whole codebase:
- All JSON must go through `Utils/SourceGenerationContext.cs` (source-generated `JsonTypeInfo`). Any new serialized type must be registered there, or it fails at runtime. Pretty-printing without a type uses `Utf8JsonWriter`/`JsonDocument` (trimming-safe).
- ReactiveUI relies on reflection, so its assemblies are pinned via `TrimmerRootAssembly` in the csproj, and constructors that build `ReactiveCommand`s carry `[RequiresUnreferencedCode(...)]`.

### Request flow
`App.axaml.cs` wires DI (`ServiceCollection` → `AddHttpClient` + `MainWindowViewModel`) and sets the main window's `DataContext`. The user configures a request in `MainWindowViewModel`; sending it constructs a `Utils/MakeRequest` (given the shared `HttpClient`, method, body, body-type, url, headers, and an `IElapsedTimerFactory` for timing). `MakeRequest.Execute` returns `OneOf<RequestSuccess, RequestFailure>` — **no exceptions for expected failures** (network errors, cancellation). Requests are cancellable via a `CancellationTokenSource` held by the ViewModel.

### ViewModel ↔ View communication (three distinct channels)
- **Bindings** for data, **`ReactiveCommand`** for actions.
- **`Interaction<TIn,TOut>`** for platform services the ViewModel can't do itself (clipboard, file-open dialog). Handlers are registered in the View's code-behind `DataContextChanged`, never in the ViewModel.
- **`MessageBus.Current`** for cross-ViewModel/cross-window events, keyed by the string constants in `Constants/MessageBusConstants.cs` (`newjson`, `newrequest`, `loadrequest`). This is how the main window, the old-requests window, and the JSON visualizer window talk to each other without direct references.

### Persistence
`JsonFileStore` is a generic `JsonStore<T>` over an `IStoreBackend` (`FileBackend` for disk, `MemoryBackend` for tests). It is **degradation-tolerant**: if a load or save throws, it flips `IsPersistenceAvailable` to false and silently continues in memory-only mode rather than crashing the app. Request history is persisted through this.

### Libraries at a glance
- **CurlExporter** — `CurlCommandBuilder`, a fluent builder that turns the current request into a `curl` command string.
- **BrunoImporter** — `BruYamlParser` / `BrunoFileImporter` import Bruno collection files (both `.bru` and OpenCollection `.yml`) into `BruRequest` objects.
- **AppLogger** — static `Logger` (initialized once in `Program.Main`) writing through pluggable `ILogSink`s (`FileSink`, `ConsoleSink`); log level is `Debug` in DEBUG builds.
