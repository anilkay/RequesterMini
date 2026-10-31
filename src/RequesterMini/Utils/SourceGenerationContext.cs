
using System.Collections.Generic;
using System.Text.Json.Serialization;
using RequesterMini.ViewModels;

namespace RequesterMini.Utils;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(OldRequestDto))]
[JsonSerializable(typeof(List<OldRequestDto>))]
[JsonSerializable(typeof(RequestCollectionDto))]
[JsonSerializable(typeof(List<RequestCollectionDto>))]
internal partial class SourceGenerationContext : JsonSerializerContext
{
}
