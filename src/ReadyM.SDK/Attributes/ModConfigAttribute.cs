namespace ReadyM.SDK.Attributes;

/// <summary>
/// Declares a class the SDK fills from a JSON file in the mod's own folder and registers as injectable.
/// </summary>
/// <param name="fileName">
/// The file to read, if not the default. Relative to the mod's folder.
/// </param>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ModConfigAttribute(string fileName = "config.json") : Attribute
{
    public string FileName { get; } = fileName;
}
