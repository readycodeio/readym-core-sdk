using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.Interop;

namespace ReadyM.SDK.Entities;

/// <summary>
/// An entity api that keeps dimensions: the server's. A lookup there names the dimensions it sees; a unique one is
/// refused when holders of the key are in more than one.
/// </summary>
internal interface IDimensionLookup
{
    bool TryFindByIndex<TComponent, TKey>(TKey key, DimensionFilter filter, bool unique, out RawEntity entity)
        where TComponent : struct, IIndexedComponent<TKey> where TKey : notnull;
}
