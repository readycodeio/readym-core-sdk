using System.ComponentModel;
using ReadyM.SDK.Entities;

namespace ReadyM.SDK.Archetypes;

/// One scope, named by whichever shape declared itself one. A shape converts to this on its own, so
/// a query reads <c>InScope(area)</c> rather than naming the handle behind it.
[EditorBrowsable(EditorBrowsableState.Never)]
public readonly struct Scope
{
    internal readonly EntityHandle Handle;

    private Scope(EntityHandle handle) => Handle = handle;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Scope Of<T>(in T shape) where T : struct, IArchetypeQueryable, IScope => new(shape.Handle);
}
