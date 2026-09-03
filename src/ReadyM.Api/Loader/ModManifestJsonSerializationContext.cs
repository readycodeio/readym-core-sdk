using System.Text.Json.Serialization;

namespace ReadyM.Api.Loader;

[JsonSerializable(typeof(ModManifest))]
[JsonSerializable(typeof(ModManifest.ModDependency))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, WriteIndented = true, AllowTrailingCommas = true)]
internal partial class ModManifestSerializerContext : JsonSerializerContext;