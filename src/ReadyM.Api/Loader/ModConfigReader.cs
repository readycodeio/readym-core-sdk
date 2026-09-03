using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Logging;

namespace ReadyM.Api.Loader;

/// <summary>
/// Reads a mod's JSON config file out of its own package folder.
/// </summary>
internal static class ModConfigReader
{
    public const string DefaultFileName = "config.json";

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        // The host app disables reflection-based serialization by default because it is published AOT,
        // and that switch covers the whole process. Naming the resolver opts this reader back in.
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
    };

    public static T Read<T>(string directory, string fileName, ILogger logger)
        where T : class, new()
    {
        var path = Path.Combine(directory, fileName);

        if (!File.Exists(path))
        {
            logger.LogWarning("No {FileName} in '{Directory}', using defaults for {ConfigType}",
                fileName, directory, typeof(T).Name);
            return new T();
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options);

            if (parsed == null)
                throw new ModConfigException($"Config file '{path}' contains no object.");

            logger.LogInformation("Loaded {ConfigType} from '{Path}'", typeof(T).Name, path);
            return parsed;
        }
        catch (JsonException e)
        {
            throw new ModConfigException($"Config file '{path}' is not valid: {e.Message}", e);
        }
        catch (IOException e)
        {
            throw new ModConfigException($"Config file '{path}' could not be read: {e.Message}", e);
        }
    }
}