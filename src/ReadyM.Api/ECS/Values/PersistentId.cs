using System;
using System.Runtime.InteropServices;
using ReadyM.Api.Saves;

namespace ReadyM.Api.ECS.Values;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct PersistentId(Guid value) : IEquatable<PersistentId>, ISaveSerializable
{
    public Guid Value = value;

    public static PersistentId New() => new(Guid.NewGuid());

    public static PersistentId None => default;

    public readonly void WriteSave(ISaveWriter writer, ISaveWriteContext context) => writer.Write(Value.ToString("N"));

    public void ReadSave(ISaveReader reader, ISaveLoadContext context)
        => Value = Guid.TryParseExact(reader.ReadString(), "N", out var value) ? value : default;

    public bool Equals(PersistentId other) => Value == other.Value;

    public override bool Equals(object? obj) => obj is PersistentId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(PersistentId left, PersistentId right) => left.Equals(right);

    public static bool operator !=(PersistentId left, PersistentId right) => !left.Equals(right);

    public override string ToString() => $"PersistentId[{Value:N}]";
}
