using AppLogger;
using Microsoft.AspNetCore.HttpOverrides;
using MudBlazor.Services;
using RequesterMini.Web.Components;
using RequesterMini.Web.Constants;
using RequesterMini.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Where history and logs live. Overridable with the DataDirectory setting (env var `DataDirectory`)
// so a container can point it at a mounted volume.
var dataDirectory = builder.Configuration["DataDirectory"]
    ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RequesterMini.Web");

#if DEBUG
Logger.Initialize("RequesterMini.Web", AppLogger.LogLevel.Debug);
#else
Logger.Initialize("RequesterMini.Web");
#endif

// Initialize picks its own file location; replace its sinks so logs follow the data directory and
// also reach stdout, which is where a container host looks for them.
Logger.ClearSinks();
Logger.AddSink(new FileSink(Path.Combine(dataDirectory, "logs", "requestermini.web.log")));
Logger.AddSink(new ConsoleSink());

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

// The client used for the user's own requests. Redirects are followed (matching the curl -L behaviour
// people expect from a request client) but cookies are not kept, so requests stay independent.
builder.Services.AddHttpClient(HttpConstants.RequestClientName, client =>
    {
        client.Timeout = TimeSpan.FromSeconds(60);
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AllowAutoRedirect = true,
        UseCookies = false,
    });

builder.Services.AddSingleton(_ => new RequestHistoryService(Path.Combine(dataDirectory, "history.json")));
builder.Services.AddScoped<RequestWorkspace>();

builder.Services.AddHealthChecks();

// Behind a reverse proxy (the container deployment) the scheme and host arrive as headers. The
// known-proxy lists are cleared because the proxy's address on a container network is not fixed —
// safe only as long as the container is reachable through that proxy alone, never directly.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    // TLS is terminated by the reverse proxy in production, so redirecting is a local-only concern.
    app.UseHttpsRedirection();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseAntiforgery();

app.MapStaticAssets();
app.MapHealthChecks("/healthz");
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

Logger.Info($"RequesterMini.Web started. Data directory: {dataDirectory}");

app.Run();
