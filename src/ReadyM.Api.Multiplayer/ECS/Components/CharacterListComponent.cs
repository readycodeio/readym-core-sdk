using System;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Values;
using ReadyM.Api.Saves;

namespace ReadyM.Api.Multiplayer.ECS.Components;

/// <summary>
/// A player's characters and the last selected one. Server-only.
/// </summary>
internal struct CharacterListComponent : ISavableComponent
{
    private const string LastSelectedKey = "lastSelected";
    private const string EntriesKey = "entries";
    private const string IdKey = "id";
    private const string CreatedKey = "created";
    private const string LastPlayedKey = "lastPlayed";

    private CharacterEntry[]? _entries;

    /// <summary>The characters in creation order.</summary>
    public CharacterEntry[] Entries
    {
        readonly get => _entries ?? [];
        set => _entries = value;
    }

    /// <summary>The character selected last.</summary>
    public CharacterId LastSelected { get; set; }

    public readonly string SaveName => "CharacterList";
    public readonly ushort SaveVersion => 1;

    public readonly void WriteSave(ISaveWriter writer, ISaveWriteContext context)
    {
        writer.BeginObject();
        writer.Name(LastSelectedKey);
        writer.Write(LastSelected == default ? string.Empty : LastSelected.ToString());
        writer.Name(EntriesKey);
        var entries = Entries;
        writer.BeginArray(entries.Length);
        foreach (var entry in entries)
        {
            writer.BeginObject();
            writer.Name(IdKey);
            writer.Write(entry.Id.ToString());
            writer.Name(CreatedKey);
            writer.Write(entry.CreatedUtc.ToUniversalTime().Ticks);
            writer.Name(LastPlayedKey);
            writer.Write(entry.LastPlayedUtc.ToUniversalTime().Ticks);
            writer.EndObject();
        }

        writer.EndArray();
        writer.EndObject();
    }

    public void ReadSave(ISaveReader reader, ISaveLoadContext context)
    {
        reader.BeginObject();
        reader.Name(LastSelectedKey);
        var lastSelected = reader.ReadString();
        reader.Name(EntriesKey);
        var entries = new System.Collections.Generic.List<CharacterEntry>();
        reader.BeginArray();
        while (reader.HasMoreElements())
        {
            reader.BeginObject();
            reader.Name(IdKey);
            var id = ParseId(reader.ReadString());
            reader.Name(CreatedKey);
            var created = new DateTime(reader.ReadLong(), DateTimeKind.Utc);
            reader.Name(LastPlayedKey);
            var lastPlayed = new DateTime(reader.ReadLong(), DateTimeKind.Utc);
            reader.EndObject();
            entries.Add(new CharacterEntry(id, created, lastPlayed));
        }

        reader.EndArray();
        reader.EndObject();

        LastSelected = ParseId(lastSelected);
        Entries = entries.ToArray();
    }

    private static CharacterId ParseId(string value)
        => value.Length == 0 ? default : new CharacterId(Guid.ParseExact(value, "N"));
}
