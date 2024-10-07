using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using RequesterMini.ViewModels;

namespace RequesterMini.Utils;
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(OldRequestDto))]
internal partial class SourceGenerationContext : JsonSerializerContext
{
    
}