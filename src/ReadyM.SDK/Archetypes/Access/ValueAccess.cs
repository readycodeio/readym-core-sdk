using ReadyM.Api.Mapping.Data;
using ReadyM.Api.Mapping.Tags;
using ReadyM.SDK.Entities;

namespace ReadyM.SDK.Archetypes.Access;

internal sealed class ValueAccess<TComponent, TValue>(Field<TComponent, TValue> field) : ValueAccess<TValue>
    where TComponent : struct, IReadyComponent
{
    internal override Type Component { get; } = typeof(TComponent);

    internal override int Field { get; } = field;

    internal override bool Write(in EntityHandle handle, TValue value, WriteKind kind)
        => handle.Write(field, value, kind);

    internal override TValue Read(in EntityHandle handle) => field.Get(handle.GetComponent<TComponent>());

    internal override void Set(in EntityHandle handle, TValue value)
        => field.Set(ref handle.GetComponent<TComponent>(), value);

    internal override bool WasSetFromApi(in EntityHandle handle)
        => field.WasSetFromApi(handle.GetComponent<TComponent>());

    internal override void ClearApiFlag(in EntityHandle handle)
        => handle.GetComponent<TComponent>().ClearApiFlag(field);
}

/// Allows accessing the value without the caller knowing which component it sits in.
internal abstract class ValueAccess<TValue>
{
    internal abstract Type Component { get; }

    internal abstract int Field { get; }

    /// Writes, going through the policy and set-from-API checks.
    internal abstract bool Write(in EntityHandle handle, TValue value, WriteKind kind);

    internal abstract TValue Read(in EntityHandle handle);

    /// Unconditional write without touching the API mask.
    internal abstract void Set(in EntityHandle handle, TValue value);

    /// Is the API mask non-zero?
    internal abstract bool WasSetFromApi(in EntityHandle handle);

    /// Called after applying the value to the game.
    internal abstract void ClearApiFlag(in EntityHandle handle);
}