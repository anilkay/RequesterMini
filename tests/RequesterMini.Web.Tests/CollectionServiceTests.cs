using RequesterMini.Web.Models;
using RequesterMini.Web.Services;

namespace RequesterMini.Web.Tests;

public class CollectionServiceTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    private string CollectionsPath => Path.Combine(_directory, "collections.json");

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private static StoredRequest Request(string url = "https://example.test/", string response = "") =>
        new("GET", url, "", "Json", response.Length == 0 ? "" : "OK", response, [], DateTime.UtcNow);

    [Fact]
    public void Snapshot_NewStore_IsEmpty()
    {
        Assert.Empty(new CollectionService(CollectionsPath).Snapshot());
    }

    [Fact]
    public void Create_Name_AddsEmptyCollection()
    {
        var service = new CollectionService(CollectionsPath);

        Assert.True(service.Create("Api"));

        var collection = service.Snapshot().Single();
        Assert.Equal("Api", collection.Name);
        Assert.Empty(collection.Requests);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankName_IsRejected(string name)
    {
        var service = new CollectionService(CollectionsPath);

        Assert.False(service.Create(name));
        Assert.Empty(service.Snapshot());
    }

    [Fact]
    public void Create_Name_IsTrimmed()
    {
        var service = new CollectionService(CollectionsPath);

        service.Create("  Api  ");

        Assert.Equal("Api", service.Snapshot().Single().Name);
    }

    [Fact]
    public void Create_DuplicateNameIgnoringCase_IsRejected()
    {
        var service = new CollectionService(CollectionsPath);
        service.Create("Api");

        Assert.False(service.Create("api"));
        Assert.Single(service.Snapshot());
    }

    [Fact]
    public void AddRequest_UnknownCollection_CreatesIt()
    {
        var service = new CollectionService(CollectionsPath);

        service.AddRequest("Api", Request());

        var collection = service.Snapshot().Single();
        Assert.Equal("Api", collection.Name);
        Assert.Single(collection.Requests);
    }

    [Fact]
    public void AddRequest_StripsTheResponse()
    {
        var service = new CollectionService(CollectionsPath);

        service.AddRequest("Api", Request(response: "{\"id\":1}"));

        var saved = service.Snapshot().Single().Requests.Single();
        Assert.Equal("", saved.ResponseBody);
        Assert.Equal("", saved.ResponseStatusCode);
    }

    [Fact]
    public void AddRequest_ExistingCollectionIgnoringCase_AppendsRatherThanForking()
    {
        var service = new CollectionService(CollectionsPath);
        service.Create("Api");

        service.AddRequest("api", Request());

        Assert.Single(service.Snapshot());
        Assert.Single(service.Snapshot().Single().Requests);
    }

    [Fact]
    public void AddRequest_BlankCollectionName_IsIgnored()
    {
        var service = new CollectionService(CollectionsPath);

        service.AddRequest("  ", Request());

        Assert.Empty(service.Snapshot());
    }

    [Fact]
    public void RemoveRequest_ByIndex_RemovesThatOne()
    {
        var service = new CollectionService(CollectionsPath);
        service.AddRequest("Api", Request("https://first.test/"));
        service.AddRequest("Api", Request("https://second.test/"));

        service.RemoveRequest("Api", 0);

        Assert.Equal("https://second.test/", service.Snapshot().Single().Requests.Single().Url);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    public void RemoveRequest_IndexOutOfRange_IsIgnored(int index)
    {
        var service = new CollectionService(CollectionsPath);
        service.AddRequest("Api", Request());

        service.RemoveRequest("Api", index);

        Assert.Single(service.Snapshot().Single().Requests);
    }

    [Fact]
    public void Delete_Collection_RemovesItAndItsRequests()
    {
        var service = new CollectionService(CollectionsPath);
        service.AddRequest("Api", Request());

        service.Delete("Api");

        Assert.Empty(service.Snapshot());
    }

    [Fact]
    public void Delete_UnknownCollection_DoesNotRaiseChanged()
    {
        var service = new CollectionService(CollectionsPath);
        service.Create("Api");
        var raised = 0;
        service.Changed += () => raised++;

        service.Delete("Nope");

        Assert.Equal(0, raised);
    }

    [Fact]
    public void Create_RaisesChanged()
    {
        var service = new CollectionService(CollectionsPath);
        var raised = 0;
        service.Changed += () => raised++;

        service.Create("Api");

        Assert.Equal(1, raised);
    }

    [Fact]
    public void Collections_SurviveReload()
    {
        var service = new CollectionService(CollectionsPath);
        service.AddRequest("Api", Request("https://persisted.test/"));

        var reloaded = new CollectionService(CollectionsPath).Snapshot().Single();

        Assert.Equal("Api", reloaded.Name);
        Assert.Equal("https://persisted.test/", reloaded.Requests.Single().Url);
    }

    [Fact]
    public void Snapshot_IsACopy_SoCallersCannotMutateTheStore()
    {
        var service = new CollectionService(CollectionsPath);
        service.AddRequest("Api", Request());

        service.Snapshot().Single().Requests.Clear();

        Assert.Single(service.Snapshot().Single().Requests);
    }

    [Fact]
    public void AddRequest_Null_Throws()
    {
        var service = new CollectionService(CollectionsPath);

        Assert.Throws<ArgumentNullException>(() => service.AddRequest("Api", null!));
    }

    [Fact]
    public void Constructor_BlankPath_Throws()
    {
        Assert.Throws<ArgumentException>(() => new CollectionService("  "));
    }
}
