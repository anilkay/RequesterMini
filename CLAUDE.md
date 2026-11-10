# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

RequesterMini is a cross-platform HTTP request client (a mini Postman/Insomnia) on .NET 11, with two front ends over a shared set of libraries: an Avalonia + ReactiveUI desktop app and a Blazor Server + MudBlazor web app.

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
- `src/AppLogger/`, `src/CurlExporter/`, `src/JsonFileStore/`, `src/BrunoImporter/`, `src/HttpRequesting/`, `src/HttpAuth/`, `src/SyntaxHighlighter/`, `src/UrlQuery/`, `src/Templating/` — standalone `net11.0` class libraries with zero UI dependencies, each with a paired `tests/*.Tests` project. **Reusable, UI-free logic belongs in a library so it can be unit-tested in isolation**; only UI/ViewModel glue stays in the front ends.
  - A new library the web app references also needs a `COPY` line in `src/RequesterMini.Web/Dockerfile`. `dotnet build` and `dotnet test` will not notice it missing — only the container build does. The `CI` workflow builds that image on every push to `main`, which is what catches it; before that workflow existed the first sign was a failed release, and v0.3.0 had to be tagged twice.
- `tests/RequesterMini.Tests/` — ViewModel tests for the desktop app, which reaches them through `InternalsVisibleTo` since ViewModel members are `internal`. See **Testing the desktop ViewModels** below.

### Two front ends, one set of libraries
The desktop app predates the web app and is the reference implementation — leave it alone unless a change is asked for explicitly. The web app deliberately does **not** reference it; the request-sending logic it needs is `src/HttpRequesting` (`HttpRequestRunner`), a library port of the desktop app's `Utils/MakeRequest`. **Those two remain duplicates: a change to one needs the same change in the other** (or the desktop app should finally be pointed at the library). Everything else a feature needs is shared — proxying (`ProxyClientFactory`), auth (`BasicAuth`, `BearerAuth`) and variables (`VariableInterpolator`) all live in libraries both front ends call.

Web-app state lives in four services: `RequestWorkspace` (scoped — the request being composed, one per Blazor circuit) and the singletons `RequestHistoryService`, `CollectionService` and `EnvironmentService`, each a `JsonFileStore` over one file. The web app has no `MessageBus`; pages talk through the scoped workspace and subscribe to the singletons' `Changed` events. The desktop app keeps the same three stores inside `OldRequestsWindowViewModel` and reaches the main window over `MessageBus`.

Browser calls from the web app (clipboard, file download) go through `Utils/BrowserActions`, not `IJSRuntime.InvokeVoidAsync` directly. A JS call in Blazor Server can fail without anything being wrong — the circuit may have dropped, or the browser may refuse the write — and the .NET 11 `BL0016` analyzer flags unguarded calls. The wrappers return `false` instead of throwing, and `CopyTextAsync` reads the browser's real answer, so a page must say "Could not copy" rather than assume success.

### Variables
`{{name}}` placeholders are resolved from the active environment at the moment a request is sent, and for the cURL export. Everything persisted — history, collections — keeps the **template** form, so a reloaded request follows whichever environment is active then. An unknown placeholder is left as written rather than blanked.

### Testing the desktop ViewModels
Two things make these tests work, and both are easy to trip over:
- ReactiveUI 24 refuses to run until it is built. `ReactiveTestBase` calls `RxAppBuilder.CreateReactiveUIBuilder().WithCoreServices().BuildApp()` once, then points `RxSchedulers.MainThreadScheduler` at the current thread — outside the app nothing pumps a UI thread.
- `MessageBus.Current` is a process-wide singleton these ViewModels both publish and listen on, so the assembly sets `DisableTestParallelization`.

`OldRequestsWindowViewModel` takes an optional data directory so tests write to a temp folder rather than the user's real `history.json`, `collections.json` and `environments.json`.

### Trimming & reflection are hard constraints
The app publishes trimmed + single-file, and reflection-based JSON is disabled (`JsonSerializerIsReflectionEnabledByDefault=false`). Consequences that shape the whole codebase:
- All JSON must go through `Utils/SourceGenerationContext.cs` (source-generated `JsonTypeInfo`). Any new serialized type must be registered there, or it fails at runtime. Pretty-printing without a type uses `Utf8JsonWriter`/`JsonDocument` (trimming-safe).
- ReactiveUI relies on reflection, so its assemblies are pinned via `TrimmerRootAssembly` in the csproj, and constructors that build `ReactiveCommand`s carry `[RequiresUnreferencedCode(...)]`.

### Runtime async
Every project under `src/` is compiled with .NET 11's runtime async (`<Features>$(Features);runtime-async=on</Features>` in `src/Directory.Build.props`): the runtime, not a compiler-generated state machine, suspends and resumes `async` methods. Delete that one line to turn it off; nothing else depends on it.

- **The file lives in `src/` on purpose.** The web `Dockerfile` copies only `src/`, so a repo-root `Directory.Build.props` would never reach the container build and the setting would be silently off in the shipped image. It deliberately does not cover `tests/`, whose assemblies hold no production async code (the ViewModel tests drive the `src/` assemblies, which have it).
- **How to tell it is on:** an `async` method has `MethodImplAttributes.Async` (`0x2000`), no `AsyncStateMachineAttribute`, and the assembly has no compiler-generated `<Name>d__N` state-machine type. `dotnet build -p:Features=` (an empty override) builds the same code without it, which makes a handy before/after.
- It is the riskiest .NET 11 feature in use here, because the desktop app publishes trimmed + single-file. If a published build misbehaves, turn it off first and see whether the problem goes away.

### Request flow
`App.axaml.cs` wires DI (`ServiceCollection` → `AddHttpClient` + `MainWindowViewModel`) and sets the main window's `DataContext`. The user configures a request in `MainWindowViewModel`; sending it constructs a `Utils/MakeRequest` (given the shared `HttpClient`, method, body, body-type, url, headers, and an `IElapsedTimerFactory` for timing). `MakeRequest.Execute` returns `OneOf<RequestSuccess, RequestFailure>` — **no exceptions for expected failures** (network errors, cancellation). Requests are cancellable via a `CancellationTokenSource` held by the ViewModel.

### ViewModel ↔ View communication (three distinct channels)
- **Bindings** for data, **`ReactiveCommand`** for actions.
- **`Interaction<TIn,TOut>`** for platform services the ViewModel can't do itself (clipboard, file-open dialog). Handlers are registered in the View's code-behind `DataContextChanged`, never in the ViewModel.
- **`MessageBus.Current`** for cross-ViewModel/cross-window events, keyed by the string constants in `Constants/MessageBusConstants.cs` (`newjson`, `newrequest`, `loadrequest`). This is how the main window, the old-requests window, and the JSON visualizer window talk to each other without direct references.

### Result types: OneOf vs FluentResults
Two idioms, split by who the failure is *for*. `OneOf<TSuccess, TFailure>` carries a **typed** failure the caller branches on (the request path). FluentResults' `Result` carries a **message for the user** (the web app's `CollectionService` / `EnvironmentService`, which refuse blank or taken names and say why). Argument guards still throw and idempotent removals stay `void`. The full rule, with the reasoning, is under "Result Types" in `.github/copilot-instructions.md`.

This was a deliberate partial migration, not an unfinished one: FluentResults was **not** pushed into the request path, because replacing `OneOf`'s typed failure with `List<IError>` there would lose type safety for no gain. Don't "finish" it.

### Persistence
`JsonFileStore` is a generic `JsonStore<T>` over an `IStoreBackend` (`FileBackend` for disk, `MemoryBackend` for tests). It is **degradation-tolerant**: if a load or save throws, it flips `IsPersistenceAvailable` to false and silently continues in memory-only mode rather than crashing the app. History, collections and environments are all persisted through this.

That tolerance has a sharp edge: a type the serializer cannot handle does not throw, it just stops persisting. A record with **two constructors** is the way this has bitten — System.Text.Json cannot tell which to use. Give persisted records one constructor, and cover each store with a round-trip-through-reload test.

### Libraries at a glance
- **CurlExporter** — `CurlCommandBuilder`, a fluent builder that turns the current request into a `curl` command string.
- **BrunoImporter** — `BruYamlParser` / `BrunoFileImporter` import Bruno collection files (both `.bru` and OpenCollection `.yml`) into `BruRequest` objects.
- **AppLogger** — static `Logger` (initialized once at startup) writing through pluggable `ILogSink`s (`FileSink`, `ConsoleSink`); log level is `Debug` in DEBUG builds.
- **HttpRequesting** — `HttpRequestRunner` sends a `HttpRequestSpec` and returns `OneOf<RequestSuccess, RequestFailure>`; `ProxyClientFactory` builds the proxied `HttpClient`.
- **HttpAuth** — `BasicAuth` and `BearerAuth` build Authorization header values; `AuthScheme` is the enum behind the UI's auth dropdown.
- **UrlQuery** — `UrlQueryCodec` parses and rewrites query strings. `ParseQuery` also reads form bodies, which have the same `key=value&…` shape.
- **SyntaxHighlighter** — `JsonHighlighter` / `XmlHighlighter` tokenize into UI-free `HighlightSegment`s; each front end maps `TokenKind` to its own colors.
- **Templating** — `VariableInterpolator` resolves `{{name}}` placeholders; `VariableEnvironment` and `EnvironmentState` are the persisted shapes.
