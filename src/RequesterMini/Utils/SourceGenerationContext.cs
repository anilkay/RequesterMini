
using System.Collections.Generic;
using System.Text.Json.Serialization;
using RequesterMini.ViewModels;
using Templating;

namespace RequesterMini.Utils;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(OldRequestDto))]
[JsonSerializable(typeof(List<OldRequestDto>))]
[JsonSerializable(typeof(RequestCollectionDto))]
[JsonSerializable(typeof(List<RequestCollectionDto>))]
[JsonSerializable(typeof(VariableEnvironment))]
[JsonSerializable(typeof(EnvironmentState))]
internal partial class SourceGenerationContext : JsonSerializerContext
{
}
