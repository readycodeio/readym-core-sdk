using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.Interop;

namespace ReadyM.SDK.Entities;

/// <summary>An entity api that keeps dimensions: the server's. A lookup there names the dimensions it sees.</summary>
internal interface IDimensionLookup
{
    /// <summary>The first holder of the key the filter sees.</summary>
    bool TryFindByIndex<TComponent, TKey>(TKey key, DimensionFilter filter, out RawEntity entity)
        where TComponent : struct, IIndexedComponent<TKey> where TKey : notnull;
}
