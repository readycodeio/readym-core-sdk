using System.Runtime.CompilerServices;
using IComponent = Friflo.Engine.ECS.IComponent;

namespace ReadyM.SDK.Entity;

/// <summary>
/// What one side calls a component type, remembered so the question is asked once.
/// </summary>
/// <remarks>
/// The two halves number components differently, and a test host runs both, so the answer belongs
/// to a side rather than to the process. One entry rather than a table: a running game has exactly
/// one side, so only a host holding both ever replaces it. The entry is a single reference, which
/// is what makes a plain read safe: a reader either sees the old one or a fully built new one.
/// </remarks>
internal static class ComponentIds<T> where T : struct, IComponent
{
    private static Entry? _last;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int For(IEntityApi api)
    {
        var last = _last;

        return last is not null && ReferenceEquals(last.Api, api) ? last.Id : Resolve(api);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Resolve(IEntityApi api)
    {
        var id = api.ComponentIdOf(typeof(T));

        _last = new Entry(api, id);

        return id;
    }

    private sealed class Entry(IEntityApi api, int id)
    {
        internal readonly IEntityApi Api = api;
        internal readonly int Id = id;
    }
}
