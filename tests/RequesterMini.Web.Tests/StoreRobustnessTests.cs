using RequesterMini.Web.Services;

namespace RequesterMini.Web.Tests;

/// <summary>
/// The stores read files that may not have been written by this exact version of the app, or may
/// have been edited by hand. Valid JSON of the wrong shape deserializes without complaint — the C#
/// nullability annotations are not enforced — and used to fail later, in a singleton constructor,
/// taking every page down until someone fixed the file. These tests write such files on purpose.
/// </summary>
public class StoreRobustnessTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
    }

    private string WriteFile(string name, string json)
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, name);
        File.WriteAllText(path, json);
        return path;
    }

    // Environments ----------------------------------------------------------

    [Fact]
    public void Environments_FileWithoutTheEnvironmentsList_LoadsEmpty()
    {
        var path = WriteFile("environments.json", "{\"ActiveName\":\"x\"}");

        var ex = Record.Exception(() => new EnvironmentService(path));

        Assert.Null(ex);
        Assert.Empty(new EnvironmentService(path).Snapshot());
    }

    [Fact]
    public void Environments_EntryWithoutVariables_LoadsWithAnEmptySet()
    {
        var path = WriteFile("environments.json", "{\"Environments\":[{\"Name\":\"dev\"}],\"ActiveName\":\"dev\"}");

        var service = new EnvironmentService(path);

        Assert.Equal("dev", service.ActiveName);
        Assert.Empty(service.ActiveVariables());
        Assert.True(service.SetVariable("dev", "a", "1").IsSuccess);
    }

    [Fact]
    public void Environments_NullAndNamelessEntries_AreDropped()
    {
        var path = WriteFile("environments.json",
            "{\"Environments\":[null,{\"Name\":\"  \",\"Variables\":{}},{\"Name\":\"dev\",\"Variables\":{\"a\":\"1\"}}],\"ActiveName\":\"dev\"}");

        var service = new EnvironmentService(path);

        Assert.Equal("dev", service.Snapshot().Single().Name);
        Assert.Equal("1", service.ActiveVariables()["a"]);
    }

    [Fact]
    public void Environments_NullActiveName_MeansNoneActive()
    {
        var path = WriteFile("environments.json", "{\"Environments\":[{\"Name\":\"dev\",\"Variables\":{}}],\"ActiveName\":null}");

        var ex = Record.Exception(() => new EnvironmentService(path));

        Assert.Null(ex);
        Assert.Equal("", new EnvironmentService(path).ActiveName);
    }

    // Collections ------------------------------------------------------------

    [Fact]
    public void Collections_EntryWithoutRequests_LoadsEmptyAndAcceptsNewOnes()
    {
        var path = WriteFile("collections.json", "[{\"Name\":\"Api\"}]");

        var service = new CollectionService(path);

        Assert.Empty(service.Snapshot().Single().Requests);
        Assert.True(service.AddRequest("Api", NewRequest()).IsSuccess);
    }

    [Fact]
    public void Collections_NullEntries_AreDropped()
    {
        var path = WriteFile("collections.json",
            "[null,{\"Name\":null,\"Requests\":[]},{\"Name\":\"Api\",\"Requests\":[null,{\"Method\":\"GET\",\"Url\":\"https://x.test/\"}]}]");

        var service = new CollectionService(path);

        var collection = service.Snapshot().Single();
        Assert.Equal("Api", collection.Name);
        Assert.Equal("https://x.test/", collection.Requests.Single().Url);
    }

    [Fact]
    public void Collections_RequestWithoutHeaders_GetsAnEmptySetSoLoadingItCannotThrow()
    {
        var path = WriteFile("collections.json", "[{\"Name\":\"Api\",\"Requests\":[{\"Method\":\"GET\",\"Url\":\"https://x.test/\"}]}]");

        var saved = new CollectionService(path).Snapshot().Single().Requests.Single();

        Assert.NotNull(saved.Headers);
        Assert.NotNull(saved.Body);
        Assert.NotNull(saved.ProxyUrl);
    }

    // History ----------------------------------------------------------------

    [Fact]
    public void History_EntryMissingOptionalFields_GetsSafeDefaults()
    {
        var path = WriteFile("history.json", "[{\"Method\":\"GET\",\"Url\":\"https://x.test/\"}]");

        var entry = new RequestHistoryService(path).Snapshot().Single();

        Assert.NotNull(entry.Headers);
        Assert.Equal("", entry.Body);
        Assert.Equal("", entry.ResponseBody);
        Assert.Equal("", entry.ResponseStatusCode);
        Assert.Equal("", entry.ProxyUrl);
    }

    [Fact]
    public void History_NullEntriesAndEntriesWithoutAMethodOrUrl_AreDropped()
    {
        // Without a method or a URL there is nothing to show or to load back into the editor.
        var path = WriteFile("history.json",
            "[null,{\"Method\":null,\"Url\":\"https://x.test/\"},{\"Method\":\"GET\",\"Url\":\"\"},{\"Method\":\"GET\",\"Url\":\"https://ok.test/\"}]");

        var service = new RequestHistoryService(path);

        Assert.Equal("https://ok.test/", service.Snapshot().Single().Url);
    }

    private static Models.StoredRequest NewRequest() =>
        new("GET", "https://x.test/", "", "Json", "", "", [], DateTime.UtcNow);
}
