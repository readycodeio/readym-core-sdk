using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Archetypes.Access;
using ReadyM.SDK.Entities;

namespace ReadyM.SDK.Client;

public static class WriteExtensions
{
    public static bool Override<TShape, TOwner, TValue>(this TShape shape, Value<TOwner, TValue> value, TValue set)
        where TShape : struct, IArchetypeQueryable
        where TOwner : struct, IArchetypeQueryable
        => value.Access.Write(EntityHandle.Of(shape), set, WriteKind.Override);
}
