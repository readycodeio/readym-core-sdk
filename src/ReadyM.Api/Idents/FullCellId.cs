using System;

namespace ReadyM.Api.Idents;

/// <summary>
/// Fully identifies a cell across the whole game world by pairing it with its parent area.
/// A <see cref="CellId"/> only has to be unique within its parent <see cref="AreaId"/>, so a cell can only be
/// uniquely identified by the (<see cref="AreaId"/>, <see cref="CellId"/>) pair represented by this struct.
/// </summary>
/// <remarks>
/// This struct doesn't replace <see cref="CellId"/>, because most of the time 
/// <see cref="AreaId"/> can be inferred from the context of which area is a given player in.
/// </remarks>
public readonly struct FullCellId : IEquatable<FullCellId>
{
    public AreaId AreaId { get; }

    public CellId CellId { get; }

    public FullCellId(AreaId areaId, CellId cellId)
    {
        AreaId = areaId;
        CellId = cellId;
    }

    public bool Equals(FullCellId other) => AreaId == other.AreaId && CellId == other.CellId;

    public override bool Equals(object? obj) => obj is FullCellId other && Equals(other);

    public override int GetHashCode()
    {
        //Can't use HashCode.Combine because it's not part of netstandard2.0
        unchecked
        {
            return (AreaId.GetHashCode() * 397) ^ CellId.GetHashCode();
        }
    }

    public static bool operator ==(FullCellId left, FullCellId right) => left.Equals(right);

    public static bool operator !=(FullCellId left, FullCellId right) => !left.Equals(right);

    public override string ToString() => $"{nameof(FullCellId)}[{AreaId}, {CellId}]";
}
