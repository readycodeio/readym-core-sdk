namespace ReadyM.Api.Saves;

/// <summary>
/// Represents a file with a name and binary content, used for saving game data.
/// </summary>
public readonly struct FileInfo(string name, byte[] content)
{
    /// <summary>
    /// The name of the file.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// The binary content of the file.
    /// </summary>
    public byte[] Content { get; } = content;
}