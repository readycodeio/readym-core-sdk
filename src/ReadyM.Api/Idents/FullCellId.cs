using System;
using LiteNetLib.Utils;
using ReadyM.Api.Serialization;

namespace ReadyM.Api.Idents;

/// <summary>
/// Fully identifies a cell across the whole game world by pairing it with its parent area.
/// A <see cref="CellId"/> only has to be unique within its parent <see cref="AreaId"/>, so a cell can only be
/// uniquely identified by the (<see cref="AreaId"/>, <see cref="CellId"/>) pair represented by this struct.
/// </summary>
/// <remarks>
/// This struct doesn't replace <see cref="CellId"/>, because most of the time 
/// <see cref="AreaId"/> can be inferred from the context of which area is a given player in.
/// Cells are only used in OblivionMP for now. WukongMP does not use cells.
/// </remarks>
[DeriveJsonSerializable]
public partial struct FullCellId : INetSerializable, IEquatable<FullCellId>
{
    private AreaId _areaId;
    private CellId _cellId;

    public AreaId AreaId => _areaId;
    public CellId CellId => _cellId;

    public FullCellId(AreaId areaId, CellId cellId)
    {
        _areaId = areaId;
        _cellId = cellId;
    }

    public void Serialize(NetDataWriter writer)
    {
        _areaId.Serialize(writer);
        _cellId.Serialize(writer);
    }

    public void Deserialize(NetDataReader reader)
    {
        _areaId.Deserialize(reader);
        _cellId.Deserialize(reader);
    }

    public bool Equals(FullCellId other) => _areaId == other._areaId && _cellId == other._cellId;

    public override bool Equals(object? obj) => obj is FullCellId other && Equals(other);

    public override int GetHashCode()
    {
        //Can't use HashCode.Combine because it's not part of netstandard2.0
        unchecked
        {
            return (_areaId.GetHashCode() * 397) ^ _cellId.GetHashCode();
        }
    }

    public static bool operator ==(FullCellId left, FullCellId right) => left.Equals(right);

    public static bool operator !=(FullCellId left, FullCellId right) => !left.Equals(right);

    public override string ToString() => $"{nameof(FullCellId)}[{_areaId}, {_cellId}]";
}
