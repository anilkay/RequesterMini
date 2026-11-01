using System.Text.Json.Serialization;
using RequesterMini.Web.Models;
using Templating;

namespace RequesterMini.Web.Serialization;

/// <summary>
/// Source-generated JSON metadata. <see cref="JsonFileStore.JsonStore{T}"/> takes a
/// <c>JsonTypeInfo&lt;T&gt;</c>, and the project convention is source generation everywhere.
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(StoredRequest))]
[JsonSerializable(typeof(List<StoredRequest>))]
[JsonSerializable(typeof(StoredCollection))]
[JsonSerializable(typeof(List<StoredCollection>))]
[JsonSerializable(typeof(VariableEnvironment))]
[JsonSerializable(typeof(EnvironmentState))]
internal partial class WebSerializerContext : JsonSerializerContext;
