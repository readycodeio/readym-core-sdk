using System;
using LiteNetLib.Utils;
using ReadyM.Api.Serialization;

namespace ReadyM.Api.Idents;

/// <summary>
/// Fully identifies a cell across the whole server: its dimension, its parent area and the cell.
/// A <see cref="CellId"/> only has to be unique within its parent <see cref="AreaId"/>, and an area within its
/// dimension, so a cell is only uniquely identified by all three.
/// </summary>
/// <remarks>
/// This struct doesn't replace <see cref="CellId"/>, because most of the time 
/// <see cref="AreaId"/> can be inferred from the context of which area is a given player in.
/// Cells are only used in OblivionMP for now. WukongMP does not use cells.
/// </remarks>
[DeriveJsonSerializable]
public partial struct FullCellId : INetSerializable, IEquatable<FullCellId>
{
    // The dimension last: the native mirror (Idents/FullCellId.h) has the same layout
    private AreaId _areaId;
    private CellId _cellId;
    private DimensionId _dimension;

    public DimensionId Dimension => _dimension;
    public AreaId AreaId => _areaId;
    public CellId CellId => _cellId;

    /// <summary>The cell's area, in its dimension.</summary>
    public FullAreaId Area => new(_dimension, _areaId);

    public FullCellId(DimensionId dimension, AreaId areaId, CellId cellId)
    {
        _dimension = dimension;
        _areaId = areaId;
        _cellId = cellId;
    }

    public FullCellId(FullAreaId area, CellId cellId)
        : this(area.Dimension, area.Area, cellId)
    {
    }

    // In field order, for the generated JSON reader
    private FullCellId(AreaId areaId, CellId cellId, DimensionId dimension)
        : this(dimension, areaId, cellId)
    {
    }

    public void Serialize(NetDataWriter writer)
    {
        _dimension.Serialize(writer);
        _areaId.Serialize(writer);
        _cellId.Serialize(writer);
    }

    public void Deserialize(NetDataReader reader)
    {
        _dimension.Deserialize(reader);
        _areaId.Deserialize(reader);
        _cellId.Deserialize(reader);
    }

    public bool Equals(FullCellId other) => _dimension == other._dimension && _areaId == other._areaId && _cellId == other._cellId;

    public override bool Equals(object? obj) => obj is FullCellId other && Equals(other);

    public override int GetHashCode()
    {
        //Can't use HashCode.Combine because it's not part of netstandard2.0
        unchecked
        {
            return (((_areaId.GetHashCode() * 397) ^ _cellId.GetHashCode()) * 397) ^ _dimension.GetHashCode();
        }
    }

    public static bool operator ==(FullCellId left, FullCellId right) => left.Equals(right);

    public static bool operator !=(FullCellId left, FullCellId right) => !left.Equals(right);

    public override string ToString()
        => _dimension.IsMain ? $"{nameof(FullCellId)}[{_areaId}, {_cellId}]" : $"{nameof(FullCellId)}[{_areaId}, {_cellId}] in {_dimension}";
}
