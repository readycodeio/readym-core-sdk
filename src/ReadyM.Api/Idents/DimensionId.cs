using System;

namespace ReadyM.Api.Idents;

/// <summary>
/// Identifies a dimension: one copy of the game world on a server. A runtime handle the server hands out when a
/// dimension is created, never saved and never sent to clients. The default value is the default dimension: the live
/// world.
/// </summary>
public readonly struct DimensionId : IEquatable<DimensionId>
{
    private readonly int _value;

    /// <summary>A dimension id from the server's handle.</summary>
    public DimensionId(int value)
    {
        _value = value;
    }

    /// <summary>The default dimension: the live world.</summary>
    public static DimensionId Default => default;

    public static DimensionId Shared => new(-1);

    /// <summary>The server's handle.</summary>
    public int Value => _value;

    /// <summary>Whether this is the default dimension.</summary>
    public bool IsDefault => _value == 0;

    /// <summary>Whether this marks an entity every dimension shares.</summary>
    public bool IsShared => _value == -1;

    public bool Equals(DimensionId other) => _value == other._value;

    public override bool Equals(object? obj) => obj is DimensionId other && Equals(other);

    public override int GetHashCode() => _value;

    public static bool operator ==(DimensionId left, DimensionId right) => left.Equals(right);

    public static bool operator !=(DimensionId left, DimensionId right) => !left.Equals(right);

    public override string ToString() => IsDefault ? "Dimension[default]" : IsShared ? "Dimension[shared]" : $"Dimension[{_value}]";
}
