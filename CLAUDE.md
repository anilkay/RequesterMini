# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

RequesterMini is a cross-platform HTTP request client (a mini Postman/Insomnia) on .NET 10, with two front ends over a shared set of libraries: an Avalonia + ReactiveUI desktop app and a Blazor Server + MudBlazor web app.

## Conventions

`.github/copilot-instructions.md` is the authoritative style guide (ReactiveUI property/command patterns, `field`-keyword usage, visibility rules, source-gen JSON, `OneOf` result types, AXAML rules, testing conventions). Read it before writing code — the rules there are enforced and non-obvious. Do not duplicate it here.

`.github/agents/*.agent.md` contain role-specific checklists (`view`, `viewmodel`, `library`, `test-writer`) that mirror those conventions for narrower tasks.

## Commands

Everything operates on the `RequesterMini.slnx` solution (an XML-based `.slnx`, not a `.sln`).

```bash
dotnet build RequesterMini.slnx              # build all projects
dotnet run --project src/RequesterMini       # run the desktop app
dotnet run --project src/RequesterMini.Web   # run the web app (http://localhost:5168)
dotnet test RequesterMini.slnx               # run all test projects

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

The web app ships as a container instead. The build context is the repository root, since the app references sibling libraries:

```bash
docker build -f src/RequesterMini.Web/Dockerfile -t requestermini-web .
docker compose up --build
```

It serves plain HTTP on `8080` behind a TLS-terminating reverse proxy (`X-Forwarded-*` is honoured), writes history and logs to `/data` (override with the `DataDirectory` env var), and exposes `GET /healthz`.

## Architecture

### Solution layout
- `src/RequesterMini/` — the Avalonia `WinExe`. Contains `Views/` (AXAML + code-behind), `ViewModels/`, `Models/`, `Utils/`, `Constants/`.
- `src/RequesterMini.Web/` — the Blazor Server app (MudBlazor UI). Contains `Components/` (`Pages/`, `Layout/`, `Shared/`), `Services/`, `Models/`, `Utils/`, `Constants/`. Ships with a `Dockerfile` (build context is the repo root) and a root `compose.yaml`.
- `src/AppLogger/`, `src/CurlExporter/`, `src/JsonFileStore/`, `src/BrunoImporter/`, `src/HttpRequesting/`, `src/HttpAuth/`, `src/SyntaxHighlighter/`, `src/UrlQuery/` — standalone `net10.0` class libraries with zero UI dependencies, each with a paired `tests/*.Tests` project. **Reusable, UI-free logic belongs in a library so it can be unit-tested in isolation**; only UI/ViewModel glue stays in the front ends.

### Two front ends, one set of libraries
The desktop app predates the web app and is the reference implementation — leave it alone unless a change is asked for explicitly. The web app deliberately does **not** reference it; the request-sending logic it needs is `src/HttpRequesting` (`HttpRequestRunner`), a library port of the desktop app's `Utils/MakeRequest`. The two are duplicates today; if `MakeRequest` changes, `HttpRequestRunner` needs the same change (or the desktop app should be pointed at the library).

Web-app state lives in two services: `RequestWorkspace` (scoped — the request being composed, one per Blazor circuit) and `RequestHistoryService` (singleton — one `JsonFileStore` over one file, capped at 50 entries). The web app has no `MessageBus`; pages talk through the scoped workspace instead.

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
