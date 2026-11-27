using System;
using LiteNetLib.Utils;
using ReadyM.Api.Serialization;

namespace ReadyM.Api.Idents;

/// <summary>
/// Fully identifies an area: the area and the dimension it is in. The same <see cref="AreaId"/> in two dimensions is
/// two areas, so an area scope is keyed by this.
/// </summary>
[DeriveJsonSerializable]
public partial struct FullAreaId : INetSerializable, IEquatable<FullAreaId>
{
    // The dimension last: the native mirror (Idents/FullAreaId.h) has the same layout
    private AreaId _area;
    private DimensionId _dimension;

    public DimensionId Dimension => _dimension;
    public AreaId Area => _area;

    public FullAreaId(DimensionId dimension, AreaId area)
    {
        _dimension = dimension;
        _area = area;
    }

    // In field order, for the generated JSON reader
    private FullAreaId(AreaId area, DimensionId dimension)
        : this(dimension, area)
    {
    }

    /// <summary>The area in the main dimension.</summary>
    public static FullAreaId InMain(AreaId area) => new(DimensionId.Main, area);

    public void Serialize(NetDataWriter writer)
    {
        _dimension.Serialize(writer);
        _area.Serialize(writer);
    }

    public void Deserialize(NetDataReader reader)
    {
        _dimension.Deserialize(reader);
        _area.Deserialize(reader);
    }

    public bool Equals(FullAreaId other) => _dimension == other._dimension && _area == other._area;

    public override bool Equals(object? obj) => obj is FullAreaId other && Equals(other);

    public override int GetHashCode()
    {
        //Can't use HashCode.Combine because it's not part of netstandard2.0
        unchecked
        {
            return (_area.GetHashCode() * 397) ^ _dimension.GetHashCode();
        }
    }

    public static bool operator ==(FullAreaId left, FullAreaId right) => left.Equals(right);

    public static bool operator !=(FullAreaId left, FullAreaId right) => !left.Equals(right);

    public override string ToString() => _dimension.IsMain ? _area.ToString() : $"{_area} in {_dimension}";
}
