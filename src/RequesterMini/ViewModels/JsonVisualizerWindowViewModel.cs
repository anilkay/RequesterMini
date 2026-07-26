using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using ReactiveUI;
using ReactiveUI.Reactive;
using RequesterMini.Constants;
using RequesterMini.Utils;

namespace RequesterMini.ViewModels;

public class JsonVisualizerWindowViewModel : ViewModelBase
{
    internal string PrettyContentValue
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = "";

    internal PreviewContentKind ContentKind
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = PreviewContentKind.Plain;

    public JsonVisualizerWindowViewModel()
    {
        MessageBus.Current.Listen<string>(MessageBusConstants.NewJsonGenerated)
            .Subscribe(value =>
            {
                if (value is null) return;

                var trimmed = value.TrimStart();

                // XML content starts with '<' (a tag, declaration, comment, or doctype).
                if (trimmed.StartsWith('<') && TryPrettyXml(value, out var prettyXml))
                {
                    PrettyContentValue = prettyXml;
                    ContentKind = PreviewContentKind.Xml;
                    return;
                }

                if (TryPrettyJson(value, out var prettyJson))
                {
                    PrettyContentValue = prettyJson;
                    ContentKind = PreviewContentKind.Json;
                    return;
                }

                PrettyContentValue = value;
                ContentKind = PreviewContentKind.Plain;
            });
    }

    private static bool TryPrettyJson(string value, out string pretty)
    {
        try
        {
            using var doc = JsonDocument.Parse(value);
            using var ms = new MemoryStream();
            using var writer = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = true });
            doc.WriteTo(writer);
            writer.Flush();
            pretty = Encoding.UTF8.GetString(ms.ToArray());
            return true;
        }
        catch (JsonException)
        {
            pretty = "";
            return false;
        }
    }

    private static bool TryPrettyXml(string value, out string pretty)
    {
        try
        {
            // ToString() indents the document; the declaration is dropped, which is fine for a preview.
            pretty = XDocument.Parse(value).ToString();
            return true;
        }
        catch (XmlException)
        {
            pretty = "";
            return false;
        }
    }
}
