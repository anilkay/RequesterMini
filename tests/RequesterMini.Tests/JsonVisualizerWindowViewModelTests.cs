using ReactiveUI;
using ReactiveUI.Reactive;
using RequesterMini.Constants;
using RequesterMini.Utils;
using RequesterMini.ViewModels;

namespace RequesterMini.Tests;

/// <summary>
/// The visualizer window subscribes to <c>PrettyContentAndKind</c>, a single stream of content and
/// kind. It moved onto the view model when ReactiveUI 27 began source-generating WhenAnyValue for
/// calls made on <c>this</c>, and nothing covered it, so these tests stand in for looking at the pane.
/// </summary>
public class JsonVisualizerWindowViewModelTests : ReactiveTestBase
{
    /// <summary>Subscribes, publishes one response body, and returns the last (content, kind) the stream produced.</summary>
    private static (string Content, PreviewContentKind Kind) LatestAfterPublishing(
        JsonVisualizerWindowViewModel vm, string responseBody)
    {
        var latest = ("", PreviewContentKind.Plain);
        using var subscription = vm.PrettyContentAndKind.Subscribe(v => latest = (v.Property1, v.Property2));

        MessageBus.Current.SendMessage(responseBody, MessageBusConstants.NewJsonGenerated);

        return latest;
    }

    [Fact]
    public void Subscribing_EmitsTheCurrentValueStraightAway()
    {
        // The window subscribes in its DataContextChanged handler, before any response exists.
        var vm = new JsonVisualizerWindowViewModel();
        var seen = new List<(string, PreviewContentKind)>();

        using var subscription = vm.PrettyContentAndKind.Subscribe(v => seen.Add((v.Property1, v.Property2)));

        Assert.Equal(("", PreviewContentKind.Plain), seen.First());
    }

    [Fact]
    public void Json_IsPrettyPrintedAndKindedAsJson()
    {
        var (content, kind) = LatestAfterPublishing(new JsonVisualizerWindowViewModel(), "{\"a\":1,\"b\":[true,null]}");

        Assert.Equal(PreviewContentKind.Json, kind);
        Assert.Contains("\n", content);           // indented
        Assert.Contains("\"a\": 1", content);
    }

    [Fact]
    public void Xml_IsPrettyPrintedAndKindedAsXml()
    {
        var (content, kind) = LatestAfterPublishing(new JsonVisualizerWindowViewModel(), "<root><child>1</child></root>");

        Assert.Equal(PreviewContentKind.Xml, kind);
        Assert.Contains("\n  <child>", content);
    }

    [Fact]
    public void PlainText_IsShownAsIsAndKindedPlain()
    {
        var (content, kind) = LatestAfterPublishing(new JsonVisualizerWindowViewModel(), "just some text");

        Assert.Equal(PreviewContentKind.Plain, kind);
        Assert.Equal("just some text", content);
    }

    [Fact]
    public void MalformedJson_FallsBackToPlainRatherThanThrowing()
    {
        var (content, kind) = LatestAfterPublishing(new JsonVisualizerWindowViewModel(), "{\"a\":");

        Assert.Equal(PreviewContentKind.Plain, kind);
        Assert.Equal("{\"a\":", content);
    }

    [Fact]
    public void ASecondResponse_ReplacesTheFirst()
    {
        var vm = new JsonVisualizerWindowViewModel();
        LatestAfterPublishing(vm, "{\"first\":1}");

        var (content, kind) = LatestAfterPublishing(vm, "<second/>");

        Assert.Equal(PreviewContentKind.Xml, kind);
        Assert.Contains("second", content);
        Assert.DoesNotContain("first", content);
    }
}
