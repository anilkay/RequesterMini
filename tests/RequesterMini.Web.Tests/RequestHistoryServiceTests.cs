using RequesterMini.Web.Models;
using RequesterMini.Web.Services;

namespace RequesterMini.Web.Tests;

public class RequestHistoryServiceTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    private string HistoryPath => Path.Combine(_directory, "history.json");

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private static StoredRequest Entry(string url = "https://example.test/") =>
        new("GET", url, "", "Json", "OK", "{}", [], DateTime.UtcNow);

    [Fact]
    public void Snapshot_NewStore_IsEmpty()
    {
        var service = new RequestHistoryService(HistoryPath);

        Assert.Empty(service.Snapshot());
    }

    [Fact]
    public void Add_Entries_AreReturnedNewestFirst()
    {
        var service = new RequestHistoryService(HistoryPath);

        service.Add(Entry("https://first.test/"));
        service.Add(Entry("https://second.test/"));

        var snapshot = service.Snapshot();
        Assert.Equal("https://second.test/", snapshot[0].Url);
        Assert.Equal("https://first.test/", snapshot[1].Url);
    }

    [Fact]
    public void Add_PastCap_DropsOldestEntries()
    {
        var service = new RequestHistoryService(HistoryPath);

        for (var i = 0; i < 55; i++)
        {
            service.Add(Entry($"https://example.test/{i}"));
        }

        var snapshot = service.Snapshot();
        Assert.Equal(50, snapshot.Count);
        Assert.Equal("https://example.test/54", snapshot[0].Url);
        Assert.DoesNotContain(snapshot, entry => entry.Url == "https://example.test/4");
    }

    [Fact]
    public void Add_Entry_SurvivesReload()
    {
        var service = new RequestHistoryService(HistoryPath);
        service.Add(Entry("https://persisted.test/"));

        var reloaded = new RequestHistoryService(HistoryPath);

        Assert.Equal("https://persisted.test/", reloaded.Snapshot().Single().Url);
    }

    [Fact]
    public void Add_RaisesChanged()
    {
        var service = new RequestHistoryService(HistoryPath);
        var raised = 0;
        service.Changed += () => raised++;

        service.Add(Entry());

        Assert.Equal(1, raised);
    }

    [Fact]
    public void Remove_KnownEntry_DropsIt()
    {
        var service = new RequestHistoryService(HistoryPath);
        var entry = Entry();
        service.Add(entry);

        service.Remove(service.Snapshot().Single());

        Assert.Empty(service.Snapshot());
    }

    [Fact]
    public void Remove_UnknownEntry_DoesNotRaiseChanged()
    {
        var service = new RequestHistoryService(HistoryPath);
        service.Add(Entry());
        var raised = 0;
        service.Changed += () => raised++;

        service.Remove(Entry("https://never-added.test/"));

        Assert.Equal(0, raised);
    }

    [Fact]
    public void Clear_EmptiesHistoryAndPersists()
    {
        var service = new RequestHistoryService(HistoryPath);
        service.Add(Entry());

        service.Clear();

        Assert.Empty(service.Snapshot());
        Assert.Empty(new RequestHistoryService(HistoryPath).Snapshot());
    }

    [Fact]
    public void Add_NullEntry_Throws()
    {
        var service = new RequestHistoryService(HistoryPath);

        Assert.Throws<ArgumentNullException>(() => service.Add(null!));
    }

    [Fact]
    public void Constructor_BlankPath_Throws()
    {
        Assert.Throws<ArgumentException>(() => new RequestHistoryService("  "));
    }
}
