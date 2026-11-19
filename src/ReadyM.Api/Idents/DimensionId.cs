using System;

namespace ReadyM.Api.Idents;

/// <summary>
/// Identifies a dimension: one copy of the game world on a server. A runtime handle the server hands out when a
/// dimension is created, never saved and never sent to clients. The default value is <see cref="Invalid"/>: a dimension
/// is always named, <see cref="Main"/> for the live world.
/// </summary>
public readonly struct DimensionId : IEquatable<DimensionId>
{
    // Stored shifted, so default(DimensionId) is not a real dimension. Shared is -1, so a shift of one would make the
    // default value Shared.
    private const int Offset = 2;

    private readonly int _stored;

    /// <summary>A dimension id from the server's handle.</summary>
    public DimensionId(int value)
    {
        _stored = value + Offset;
    }

    /// <summary>No dimension: what an id nobody set holds.</summary>
    public static DimensionId Invalid => default;

    /// <summary>The main dimension: the live world, which every chain of parents ends in.</summary>
    public static DimensionId Main => new(0);

    public static DimensionId Shared => new(-1);

    /// <summary>The server's handle.</summary>
    public int Value => _stored - Offset;

    /// <summary>Whether this names a dimension, or marks a shared entity.</summary>
    public bool IsValid => _stored != 0;

    /// <summary>Whether this is the main dimension.</summary>
    public bool IsMain => Value == 0;

    /// <summary>Whether this marks an entity every dimension shares.</summary>
    public bool IsShared => Value == -1;

    public bool Equals(DimensionId other) => _stored == other._stored;

    public override bool Equals(object? obj) => obj is DimensionId other && Equals(other);

    public override int GetHashCode() => _stored;

    public static bool operator ==(DimensionId left, DimensionId right) => left.Equals(right);

    public static bool operator !=(DimensionId left, DimensionId right) => !left.Equals(right);

    public override string ToString()
        => !IsValid ? "Dimension[invalid]" : IsMain ? "Dimension[main]" : IsShared ? "Dimension[shared]" : $"Dimension[{Value}]";
}
