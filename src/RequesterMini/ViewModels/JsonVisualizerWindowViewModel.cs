using System;
using System.IO;
using System.Text;
using System.Text.Json;
using ReactiveUI;
using RequesterMini.Constants;

namespace RequesterMini.ViewModels;

public class JsonVisualizerWindowViewModel : ViewModelBase
{
    internal string PrettyJsonValue
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = "";

    public JsonVisualizerWindowViewModel()
    {
        MessageBus.Current.Listen<string>(MessageBusConstants.NewJsonGenerated)
            .Subscribe(value =>
            {
                if (value is null) return;
                try
                {
                    using var doc = JsonDocument.Parse(value);
                    using var ms = new MemoryStream();
                    using var writer = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = true });
                    doc.WriteTo(writer);
                    writer.Flush();
                    PrettyJsonValue = Encoding.UTF8.GetString(ms.ToArray());
                }
                catch (JsonException)
                {
                    PrettyJsonValue = value;
                }
            });
    }
}