using System;
using System.Runtime.InteropServices;

namespace ReadyM.Api.Multiplayer.ECS.Values;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal readonly struct PersistentId(Guid value) : IEquatable<PersistentId>
{
    public readonly Guid Value = value;

    public static PersistentId New() => new(Guid.NewGuid());

    public static PersistentId None => default;

    public bool Equals(PersistentId other) => Value == other.Value;

    public override bool Equals(object? obj) => obj is PersistentId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(PersistentId left, PersistentId right) => left.Equals(right);

    public static bool operator !=(PersistentId left, PersistentId right) => !left.Equals(right);

    public override string ToString() => $"PersistentId[{Value:N}]";
}
