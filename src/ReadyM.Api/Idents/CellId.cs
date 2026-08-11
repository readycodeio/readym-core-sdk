using LiteNetLib.Utils;
using ReadyM.Api.Serialization;
using System;
using Yooni.Native.Container;
using Yooni.Native.Serialization;

namespace ReadyM.Api.Idents;

/// <summary>
/// Identifies a cell within an area.
/// <see cref="CellId"/> has to be unique within its parent area (identified by <see cref="AreaId"/>).
/// The main difference between a cell and an area is that a player can be only in one area at once, but can have many cells active within that area.
/// <remarks>Cells are only used in OblivionMP for now. WukongMP does not use cells.</remarks>
/// </summary>
[DeriveJsonSerializable]
public partial struct CellId : INetSerializable, IEquatable<CellId>
{
    /// <summary>
    /// The underlying ID value.
    /// This is not guaranteed to be stable across game versions, and should not be used for anything other than debugging or logging purposes.
    /// </summary>
    private NativeString256 _id;

    public CellId(NativeString256 id) => _id = id;

    public CellId(string id) : this(new NativeString256(id, true)) { }

    public CellId(int id) : this(new NativeString256(id.ToString(), true)) { }

    public static CellId Invalid => default;

    public void Serialize(NetDataWriter writer) => _id.Serialize(writer);

    public void Deserialize(NetDataReader reader) => _id.Deserialize(reader);

    public bool Equals(CellId other) => _id == other._id;

    public override bool Equals(object? obj) => obj is CellId other && Equals(other);

    public override int GetHashCode() => _id.GetHashCode();

    public static bool operator ==(CellId left, CellId right) => left._id == right._id;

    public static bool operator !=(CellId left, CellId right) => left._id != right._id;

    public override string ToString() => _id == Invalid._id ? $"{nameof(CellId)}.{nameof(Invalid)}" : $"{nameof(CellId)}[{_id}]";
}
