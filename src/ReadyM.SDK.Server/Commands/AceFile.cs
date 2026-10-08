using System.Text.Json;

namespace ReadyM.SDK.Server.Commands;

public static class AceFile
{
    public const string FileName = "permissions.json";

    public static string DefaultPath => Path.Combine(AppContext.BaseDirectory, FileName);

    private static readonly JsonSerializerOptions Format = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
    };

    public static AceDocument? Read(string path)
        => File.Exists(path) ? JsonSerializer.Deserialize<AceDocument>(File.ReadAllText(path), Format) : null;

    public static void Write(string path, AceDocument document)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

        var temporary = path + ".tmp";

        File.WriteAllText(temporary, JsonSerializer.Serialize(document, Format));
        File.Move(temporary, path, overwrite: true);
    }
}
