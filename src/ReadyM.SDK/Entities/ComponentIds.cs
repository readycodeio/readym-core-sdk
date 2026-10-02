using System.Runtime.CompilerServices;
using IComponent = Friflo.Engine.ECS.IComponent;

namespace ReadyM.SDK.Entities;

/// Static cache for resolving component IDs.
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
