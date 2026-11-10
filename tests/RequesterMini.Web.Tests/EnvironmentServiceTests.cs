using RequesterMini.Web.Services;

namespace RequesterMini.Web.Tests;

public class EnvironmentServiceTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    private string EnvironmentsPath => Path.Combine(_directory, "environments.json");

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public void Snapshot_NewStore_IsEmptyWithNoActive()
    {
        var service = new EnvironmentService(EnvironmentsPath);

        Assert.Empty(service.Snapshot());
        Assert.Equal("", service.ActiveName);
        Assert.Empty(service.ActiveVariables());
    }

    [Fact]
    public void Create_First_BecomesActive()
    {
        var service = new EnvironmentService(EnvironmentsPath);

        Assert.True(service.Create("dev").IsSuccess);
        Assert.Equal("dev", service.ActiveName);
    }

    [Fact]
    public void Create_Second_DoesNotStealTheActiveSlot()
    {
        var service = new EnvironmentService(EnvironmentsPath);
        service.Create("dev");

        service.Create("prod");

        Assert.Equal("dev", service.ActiveName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankName_IsRejected(string name)
    {
        var service = new EnvironmentService(EnvironmentsPath);

        Assert.True(service.Create(name).IsFailed);
        Assert.Empty(service.Snapshot());
    }

    [Fact]
    public void Create_DuplicateIgnoringCase_IsRejected()
    {
        var service = new EnvironmentService(EnvironmentsPath);
        service.Create("dev");

        Assert.True(service.Create("DEV").IsFailed);
        Assert.Single(service.Snapshot());
    }

    [Fact]
    public void SetVariable_ThenActiveVariables_ReturnsIt()
    {
        var service = new EnvironmentService(EnvironmentsPath);
        service.Create("dev");

        service.SetVariable("dev", "baseUrl", "https://api.test");

        Assert.Equal("https://api.test", service.ActiveVariables()["baseUrl"]);
    }

    [Fact]
    public void SetVariable_ExistingKey_Overwrites()
    {
        var service = new EnvironmentService(EnvironmentsPath);
        service.Create("dev");
        service.SetVariable("dev", "a", "1");

        service.SetVariable("dev", "a", "2");

        Assert.Equal("2", service.ActiveVariables()["a"]);
    }

    [Fact]
    public void SetVariable_BlankKey_IsIgnored()
    {
        var service = new EnvironmentService(EnvironmentsPath);
        service.Create("dev");

        service.SetVariable("dev", "   ", "x");

        Assert.Empty(service.ActiveVariables());
    }

    [Fact]
    public void SetVariable_UnknownEnvironment_IsIgnored()
    {
        var service = new EnvironmentService(EnvironmentsPath);
        service.Create("dev");

        service.SetVariable("nope", "a", "1");

        Assert.Empty(service.ActiveVariables());
    }

    [Fact]
    public void ActiveVariables_OnlyTheActiveEnvironment()
    {
        var service = new EnvironmentService(EnvironmentsPath);
        service.Create("dev");
        service.SetVariable("dev", "host", "dev.test");
        service.Create("prod");
        service.SetVariable("prod", "host", "prod.test");

        Assert.Equal("dev.test", service.ActiveVariables()["host"]);

        service.Activate("prod");
        Assert.Equal("prod.test", service.ActiveVariables()["host"]);
    }

    [Fact]
    public void Activate_UnknownName_ClearsTheSelection()
    {
        var service = new EnvironmentService(EnvironmentsPath);
        service.Create("dev");

        service.Activate("nope");

        Assert.Equal("", service.ActiveName);
        Assert.Empty(service.ActiveVariables());
    }

    [Fact]
    public void Activate_Null_ClearsTheSelection()
    {
        var service = new EnvironmentService(EnvironmentsPath);
        service.Create("dev");

        service.Activate(null);

        Assert.Equal("", service.ActiveName);
    }

    [Fact]
    public void RemoveVariable_DropsIt()
    {
        var service = new EnvironmentService(EnvironmentsPath);
        service.Create("dev");
        service.SetVariable("dev", "a", "1");

        service.RemoveVariable("dev", "a");

        Assert.Empty(service.ActiveVariables());
    }

    [Fact]
    public void RenameVariable_KeepsTheValue()
    {
        var service = new EnvironmentService(EnvironmentsPath);
        service.Create("dev");
        service.SetVariable("dev", "old", "x");

        Assert.True(service.RenameVariable("dev", "old", "new").IsSuccess);

        var variables = service.ActiveVariables();
        Assert.Equal("x", variables["new"]);
        Assert.False(variables.ContainsKey("old"));
    }

    [Fact]
    public void RenameVariable_ToAnExistingName_IsRejected()
    {
        var service = new EnvironmentService(EnvironmentsPath);
        service.Create("dev");
        service.SetVariable("dev", "a", "1");
        service.SetVariable("dev", "b", "2");

        Assert.True(service.RenameVariable("dev", "a", "b").IsFailed);
        Assert.Equal("1", service.ActiveVariables()["a"]);
    }

    [Fact]
    public void RenameVariable_ToBlank_IsRejected()
    {
        var service = new EnvironmentService(EnvironmentsPath);
        service.Create("dev");
        service.SetVariable("dev", "a", "1");

        Assert.True(service.RenameVariable("dev", "a", "  ").IsFailed);
        Assert.Equal("1", service.ActiveVariables()["a"]);
    }

    [Fact]
    public void RenameVariable_ToTheSameName_IsANoOp()
    {
        var service = new EnvironmentService(EnvironmentsPath);
        service.Create("dev");
        service.SetVariable("dev", "a", "1");

        Assert.True(service.RenameVariable("dev", "a", "a").IsSuccess);
        Assert.Equal("1", service.ActiveVariables()["a"]);
    }

    [Fact]
    public void Delete_ActiveEnvironment_FallsBackToAnother()
    {
        var service = new EnvironmentService(EnvironmentsPath);
        service.Create("dev");
        service.Create("prod");

        service.Delete("dev");

        Assert.Equal("prod", service.ActiveName);
    }

    [Fact]
    public void Delete_LastEnvironment_LeavesNoneActive()
    {
        var service = new EnvironmentService(EnvironmentsPath);
        service.Create("dev");

        service.Delete("dev");

        Assert.Equal("", service.ActiveName);
        Assert.Empty(service.Snapshot());
    }

    [Fact]
    public void EnvironmentsAndSelection_SurviveReload()
    {
        var service = new EnvironmentService(EnvironmentsPath);
        service.Create("dev");
        service.Create("prod");
        service.SetVariable("prod", "host", "prod.test");
        service.Activate("prod");

        var reloaded = new EnvironmentService(EnvironmentsPath);

        Assert.Equal("prod", reloaded.ActiveName);
        Assert.Equal("prod.test", reloaded.ActiveVariables()["host"]);
        Assert.Equal(2, reloaded.Snapshot().Count);
    }

    [Fact]
    public void Snapshot_IsACopy_SoCallersCannotMutateTheStore()
    {
        var service = new EnvironmentService(EnvironmentsPath);
        service.Create("dev");
        service.SetVariable("dev", "a", "1");

        service.Snapshot().Single().Variables.Clear();

        Assert.Single(service.ActiveVariables());
    }

    [Fact]
    public void Create_RaisesChanged()
    {
        var service = new EnvironmentService(EnvironmentsPath);
        var raised = 0;
        service.Changed += () => raised++;

        service.Create("dev");

        Assert.Equal(1, raised);
    }

    [Fact]
    public void Activate_SameName_DoesNotRaiseChanged()
    {
        var service = new EnvironmentService(EnvironmentsPath);
        service.Create("dev");
        var raised = 0;
        service.Changed += () => raised++;

        service.Activate("dev");

        Assert.Equal(0, raised);
    }

    [Fact]
    public void Create_Duplicate_ReportsWhichNameClashed()
    {
        var service = new EnvironmentService(EnvironmentsPath);
        service.Create("dev");

        var result = service.Create("DEV");

        Assert.True(result.IsFailed);
        Assert.Contains("\"DEV\"", result.Errors.Single().Message);
        Assert.Single(service.Snapshot());
    }

    [Fact]
    public void SetVariable_BlankKey_ReportsWhyInsteadOfSilentlyIgnoring()
    {
        var service = new EnvironmentService(EnvironmentsPath);
        service.Create("dev");

        var result = service.SetVariable("dev", "  ", "x");

        Assert.True(result.IsFailed);
        Assert.Contains("cannot be blank", result.Errors.Single().Message);
    }

    [Fact]
    public void SetVariable_UnknownEnvironment_NamesTheMissingOne()
    {
        var service = new EnvironmentService(EnvironmentsPath);

        var result = service.SetVariable("nope", "a", "1");

        Assert.True(result.IsFailed);
        Assert.Contains("\"nope\"", result.Errors.Single().Message);
    }

    [Fact]
    public void SetVariable_Valid_Succeeds()
    {
        var service = new EnvironmentService(EnvironmentsPath);
        service.Create("dev");

        Assert.True(service.SetVariable("dev", "a", "1").IsSuccess);
    }

    [Fact]
    public void RenameVariable_ToAnExistingName_SaysWhichOneIsTaken()
    {
        var service = new EnvironmentService(EnvironmentsPath);
        service.Create("dev");
        service.SetVariable("dev", "a", "1");
        service.SetVariable("dev", "b", "2");

        var result = service.RenameVariable("dev", "a", "b");

        Assert.True(result.IsFailed);
        Assert.Contains("\"b\"", result.Errors.Single().Message);
    }

    [Fact]
    public void RenameVariable_UnknownVariable_SaysSo()
    {
        var service = new EnvironmentService(EnvironmentsPath);
        service.Create("dev");

        var result = service.RenameVariable("dev", "ghost", "x");

        Assert.True(result.IsFailed);
        Assert.Contains("\"ghost\"", result.Errors.Single().Message);
    }

    [Fact]
    public void RenameVariable_UnknownEnvironment_NamesTheMissingOne()
    {
        var service = new EnvironmentService(EnvironmentsPath);

        var result = service.RenameVariable("nope", "a", "b");

        Assert.True(result.IsFailed);
        Assert.Contains("\"nope\"", result.Errors.Single().Message);
    }

    [Fact]
    public void Constructor_BlankPath_Throws()
    {
        Assert.Throws<ArgumentException>(() => new EnvironmentService("  "));
    }
}
