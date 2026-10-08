using System.Text.Json.Serialization;

namespace ReadyM.SDK.Server.Commands;

// The shape of permissions.json
public sealed class AceDocument
{
    // ReadyM id to the groups it belongs to
    [JsonPropertyName("principals")]
    public Dictionary<string, List<string>> Principals { get; set; } = new();

    // Group to the groups it also counts as
    [JsonPropertyName("inherits")]
    public Dictionary<string, List<string>> Inherits { get; set; } = new();

    [JsonPropertyName("aces")]
    public List<AceEntry> Aces { get; set; } = new();
}

public sealed class AceEntry
{
    // A ReadyM id or a group
    [JsonPropertyName("principal")]
    public string Principal { get; set; } = "";

    // A dotted object such as command.kick An ace on command covers all of them
    [JsonPropertyName("object")]
    public string Object { get; set; } = "";

    [JsonPropertyName("allow")]
    public bool Allow { get; set; }
}
