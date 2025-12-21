
using System.Text.Json.Serialization;
using RequesterMini.ViewModels;

namespace RequesterMini.Utils;
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(OldRequestDto))]
internal partial class SourceGenerationContext : JsonSerializerContext
{
    
}