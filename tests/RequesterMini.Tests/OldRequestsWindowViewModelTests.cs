using System.Reactive.Linq;
using System.Text.Json;
using ReactiveUI;
using ReactiveUI.Reactive;
using RequesterMini.Constants;
using RequesterMini.Utils;
using RequesterMini.ViewModels;
using Templating;

namespace RequesterMini.Tests;

public class OldRequestsWindowViewModelTests : ReactiveTestBase, IDisposable
{
    private readonly string _directory = NewDataDirectory();

    public void Dispose() => DeleteDirectory(_directory);

    private OldRequestsWindowViewModel NewViewModel() => new(_directory);

    private static OldRequestDto Request(string url = "https://example.test/", string responseBody = "") =>
        new("GET", url, "", responseBody.Length == 0 ? "" : "OK", responseBody, []);

    private static void PublishRequest(OldRequestDto dto, string channel) =>
        MessageBus.Current.SendMessage(
            JsonSerializer.Serialize(dto, SourceGenerationContext.Default.OldRequestDto), channel);

    // History ---------------------------------------------------------------

    [Fact]
    public void NewRequestMessage_IsAddedToHistory()
    {
        var vm = NewViewModel();

        PublishRequest(Request("https://added.test/"), MessageBusConstants.NewRequest);

        Assert.Equal("https://added.test/", vm.OldRequests.Single().Url);
    }

    [Fact]
    public void History_PastFiftyEntries_DropsTheOldest()
    {
        var vm = NewViewModel();

        for (var i = 0; i < 55; i++)
        {
            PublishRequest(Request($"https://example.test/{i}"), MessageBusConstants.NewRequest);
        }

        Assert.Equal(50, vm.OldRequests.Count);
        Assert.Equal("https://example.test/5", vm.OldRequests[0].Url);
    }

    [Fact]
    public void History_SurvivesReload()
    {
        var vm = NewViewModel();
        PublishRequest(Request("https://persisted.test/"), MessageBusConstants.NewRequest);

        Assert.Equal("https://persisted.test/", NewViewModel().OldRequests.Single().Url);
    }

    [Fact]
    public void RemoveCommand_DropsTheEntryAndPersists()
    {
        var vm = NewViewModel();
        PublishRequest(Request(), MessageBusConstants.NewRequest);

        vm.RemoveCommand.Execute(vm.OldRequests.Single()).Subscribe();

        Assert.Empty(vm.OldRequests);
        Assert.Empty(NewViewModel().OldRequests);
    }

    [Fact]
    public void LoadCommand_PublishesTheRequestForTheMainWindow()
    {
        var vm = NewViewModel();
        string? published = null;
        using var subscription = MessageBus.Current
            .Listen<string>(MessageBusConstants.LoadRequest)
            .Subscribe(value => published = value);

        vm.LoadCommand.Execute(Request("https://loaded.test/")).Subscribe();

        Assert.NotNull(published);
        Assert.Contains("https://loaded.test/", published);
    }

    // Collections -------------------------------------------------------------

    [Fact]
    public void AddCollectionCommand_CreatesAndSelectsIt()
    {
        var vm = NewViewModel();
        vm.NewCollectionName = "Api";

        vm.AddCollectionCommand.Execute().Subscribe();

        Assert.Equal("Api", vm.Collections.Single().Name);
        Assert.Equal("Api", vm.SelectedCollection?.Name);
        Assert.Equal("", vm.NewCollectionName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AddCollectionCommand_BlankName_IsRejected(string name)
    {
        var vm = NewViewModel();
        vm.NewCollectionName = name;

        vm.AddCollectionCommand.Execute().Subscribe();

        Assert.Empty(vm.Collections);
    }

    [Fact]
    public void AddCollectionCommand_DuplicateIgnoringCase_IsRejected()
    {
        var vm = NewViewModel();
        vm.NewCollectionName = "Api";
        vm.AddCollectionCommand.Execute().Subscribe();
        vm.NewCollectionName = "api";

        vm.AddCollectionCommand.Execute().Subscribe();

        Assert.Single(vm.Collections);
    }

    [Fact]
    public void SaveToCollectionMessage_WithNoCollections_CreatesADefaultOne()
    {
        var vm = NewViewModel();

        PublishRequest(Request("https://saved.test/"), MessageBusConstants.SaveToCollection);

        var collection = vm.Collections.Single();
        Assert.Equal("Default", collection.Name);
        Assert.Equal("https://saved.test/", collection.Requests.Single().Url);
    }

    [Fact]
    public void SaveHistoryToCollectionCommand_StripsTheResponse()
    {
        var vm = NewViewModel();
        vm.NewCollectionName = "Api";
        vm.AddCollectionCommand.Execute().Subscribe();

        vm.SaveHistoryToCollectionCommand.Execute(Request(responseBody: "{\"id\":1}")).Subscribe();

        var saved = vm.Collections.Single().Requests.Single();
        Assert.Equal("", saved.ResponseBody);
        Assert.Equal("", saved.ResponseStatusCode);
    }

    [Fact]
    public void RemoveCollectionRequestCommand_DropsThatRequest()
    {
        var vm = NewViewModel();
        PublishRequest(Request("https://first.test/"), MessageBusConstants.SaveToCollection);
        PublishRequest(Request("https://second.test/"), MessageBusConstants.SaveToCollection);

        vm.RemoveCollectionRequestCommand.Execute(vm.Collections.Single().Requests[0]).Subscribe();

        Assert.Equal("https://second.test/", vm.Collections.Single().Requests.Single().Url);
    }

    [Fact]
    public void Collections_SurviveReload()
    {
        var vm = NewViewModel();
        PublishRequest(Request("https://persisted.test/"), MessageBusConstants.SaveToCollection);

        var reloaded = NewViewModel().Collections.Single();

        Assert.Equal("Default", reloaded.Name);
        Assert.Equal("https://persisted.test/", reloaded.Requests.Single().Url);
    }

    // Environments --------------------------------------------------------------

    [Fact]
    public void AddEnvironmentCommand_FirstOne_BecomesActive()
    {
        var vm = NewViewModel();
        vm.NewEnvironmentName = "dev";

        vm.AddEnvironmentCommand.Execute().Subscribe();

        Assert.Equal("dev", vm.ActiveEnvironment?.Name);
        Assert.True(vm.Environments.Single().IsActive);
        Assert.Equal("", vm.NewEnvironmentName);
    }

    [Fact]
    public void AddEnvironmentCommand_SecondOne_DoesNotStealTheActiveSlot()
    {
        var vm = NewViewModel();
        vm.NewEnvironmentName = "dev";
        vm.AddEnvironmentCommand.Execute().Subscribe();
        vm.NewEnvironmentName = "prod";

        vm.AddEnvironmentCommand.Execute().Subscribe();

        Assert.Equal("dev", vm.ActiveEnvironment?.Name);
    }

    [Fact]
    public void AddEnvironmentCommand_DuplicateIgnoringCase_IsRejected()
    {
        var vm = NewViewModel();
        vm.NewEnvironmentName = "dev";
        vm.AddEnvironmentCommand.Execute().Subscribe();
        vm.NewEnvironmentName = "DEV";

        vm.AddEnvironmentCommand.Execute().Subscribe();

        Assert.Single(vm.Environments);
    }

    [Fact]
    public void ActivateEnvironmentCommand_MovesTheActiveMarker()
    {
        var vm = NewViewModel();
        vm.NewEnvironmentName = "dev";
        vm.AddEnvironmentCommand.Execute().Subscribe();
        vm.NewEnvironmentName = "prod";
        vm.AddEnvironmentCommand.Execute().Subscribe();

        vm.ActivateEnvironmentCommand.Execute(vm.Environments[1]).Subscribe();

        Assert.False(vm.Environments[0].IsActive);
        Assert.True(vm.Environments[1].IsActive);
    }

    [Fact]
    public void RemoveEnvironmentCommand_OnTheActiveOne_FallsBackToAnother()
    {
        var vm = NewViewModel();
        vm.NewEnvironmentName = "dev";
        vm.AddEnvironmentCommand.Execute().Subscribe();
        vm.NewEnvironmentName = "prod";
        vm.AddEnvironmentCommand.Execute().Subscribe();

        vm.RemoveEnvironmentCommand.Execute(vm.Environments[0]).Subscribe();

        Assert.Equal("prod", vm.ActiveEnvironment?.Name);
    }

    [Fact]
    public void Environments_AndTheirVariables_SurviveReload()
    {
        var vm = NewViewModel();
        vm.NewEnvironmentName = "dev";
        vm.AddEnvironmentCommand.Execute().Subscribe();
        vm.AddVariableCommand.Execute(vm.Environments[0]).Subscribe();
        vm.Environments[0].Variables[0].Key = "baseUrl";
        vm.Environments[0].Variables[0].Value = "https://api.test";

        var reloaded = NewViewModel();

        Assert.Equal("dev", reloaded.ActiveEnvironment?.Name);
        Assert.Equal("https://api.test", reloaded.Environments.Single().ToVariableMap()["baseUrl"]);
    }

    [Fact]
    public void ToVariableMap_DropsBlankKeys()
    {
        var environment = new EnvironmentItem("dev");
        environment.Variables.Add(new VariableItem { Key = "  ", Value = "ignored" });
        environment.Variables.Add(new VariableItem { Key = " kept ", Value = "x" });

        var map = environment.ToVariableMap();

        Assert.Equal(["kept"], map.Keys);
    }

    [Fact]
    public void Constructing_PublishesTheActiveEnvironment()
    {
        // The main window is already listening when this control is built, and needs the restored
        // selection before the first request goes out.
        var seed = NewViewModel();
        seed.NewEnvironmentName = "dev";
        seed.AddEnvironmentCommand.Execute().Subscribe();
        seed.AddVariableCommand.Execute(seed.Environments[0]).Subscribe();
        seed.Environments[0].Variables[0].Key = "baseUrl";
        seed.Environments[0].Variables[0].Value = "https://api.test";

        string? published = null;
        using var subscription = MessageBus.Current
            .Listen<string>(MessageBusConstants.ActiveEnvironmentChanged)
            .Subscribe(value => published = value);

        NewViewModel();

        Assert.NotNull(published);
        var environment = JsonSerializer.Deserialize(published, SourceGenerationContext.Default.VariableEnvironment);
        Assert.Equal("dev", environment!.Name);
        Assert.Equal("https://api.test", environment.Variables["baseUrl"]);
    }

    [Fact]
    public void Constructing_WithNoEnvironments_PublishesAnEmptyOne()
    {
        string? published = null;
        using var subscription = MessageBus.Current
            .Listen<string>(MessageBusConstants.ActiveEnvironmentChanged)
            .Subscribe(value => published = value);

        NewViewModel();

        var environment = JsonSerializer.Deserialize(published!, SourceGenerationContext.Default.VariableEnvironment);
        Assert.Equal("", environment!.Name);
        Assert.Empty(environment.Variables);
    }
}
