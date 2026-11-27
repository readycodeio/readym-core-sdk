using ReadyM.Api.Idents;
using ReadyM.Api.Saves;

namespace ReadyM.Api.Multiplayer.ECS.Components;

/// <summary>
/// The dimension an entity was created in. Server-only, not networked. Every server archetype carries it from
/// registration, with no origin. The server's membership observer is its only writer.
/// </summary>
internal struct OriginComponent : ISavableComponent
{
    private const string KindKey = "kind";
    private const string NameKey = "name";

    /// <summary>How a save names the origin; the component itself holds only the id.</summary>
    private enum SavedKind : byte
    {
        None = 0,
        Main = 1,
        Persistent = 2,
        Temporary = 3,
    }

    /// <summary>
    /// The origin. <see cref="DimensionId.Invalid"/> while the entity has not been in a dimension;
    /// <see cref="DimensionId.Shared"/> for a temporary dimension of an earlier run, which no dimension matches.
    /// </summary>
    public DimensionId Dimension;

    /// <summary>
    /// The last dimension the entity was in that is not shared, so a move through a scope with no dimension counts as
    /// one move. Not saved: a loaded entity is not moving.
    /// </summary>
    public DimensionId LastDimension;

    public readonly string SaveName => "Origin";
    public readonly ushort SaveVersion => 1;

    public readonly void WriteSave(ISaveWriter writer, ISaveWriteContext context)
    {
        string? name = null;
        var kind = !Dimension.IsValid ? SavedKind.None
            : Dimension.IsMain ? SavedKind.Main
            : context.DimensionNames?.TryGetPersistentName(Dimension, out name) == true ? SavedKind.Persistent
            : SavedKind.Temporary;

        writer.BeginObject();
        writer.Name(KindKey);
        writer.Write((byte)kind);
        if (kind == SavedKind.Persistent)
        {
            writer.Name(NameKey);
            writer.Write(name!);
        }

        writer.EndObject();
    }

    public void ReadSave(ISaveReader reader, ISaveLoadContext context)
    {
        reader.BeginObject();
        reader.Name(KindKey);
        var kind = (SavedKind)reader.ReadByte();
        var name = kind == SavedKind.Persistent && reader.TryName(NameKey) ? reader.ReadString() : null;
        reader.EndObject();

        Dimension = kind switch
        {
            SavedKind.Main => DimensionId.Main,
            SavedKind.Persistent when !string.IsNullOrEmpty(name) && context.DimensionNames is { } names
                => names.GetPersistentDimensionId(name!),
            SavedKind.Persistent or SavedKind.Temporary => DimensionId.Shared,
            _ => DimensionId.Invalid,
        };
        LastDimension = DimensionId.Invalid;
    }
}
