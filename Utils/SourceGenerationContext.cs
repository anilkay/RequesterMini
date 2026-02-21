
using System.Collections.Generic;
using System.Text.Json.Serialization;
using RequesterMini.ViewModels;

namespace RequesterMini.Utils;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(OldRequestDto))]
[JsonSerializable(typeof(List<OldRequestDto>))]
internal partial class SourceGenerationContext : JsonSerializerContext
{
}
