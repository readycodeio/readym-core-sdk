using System.Diagnostics.CodeAnalysis;
using ReadyM.Api.Idents;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Archetypes.Core;
using ReadyM.SDK.Entities;
using ReadyM.SDK.Exceptions;

namespace ReadyM.SDK.Server.Entities;

internal class ServerEntities(ServerEntityApi api) : IEntities
{
    public EntityQuery<T> Query<T>()
        where T : struct, IArchetypeQueryable
        => new(api);

    public World World
    {
        get
        {
            foreach (var world in Query<World>())
            {
                return world;
            }

            throw new InvalidEntityException("World entity has not yet been created.");
        }
    }

    public T Create<T>()
        where T : struct, IArchetype
        => new() { Handle = new EntityHandle(api.Create(ArchetypeRegistry.SetFor(typeof(T), default(T).Components)), api) };

    public bool Delete<T>(in T shape)
        where T : IEntityShape, allows ref struct
        => api.Delete(shape.Handle.RawEntity);

    public bool TryGetScope<TShape, TScope>(in TShape shape, out TScope scope)
#if NET
        where TShape : struct, IEntityShape, allows ref struct
#else
        where TShape : struct, IEntityShape
#endif
        where TScope : struct, IArchetypeQueryable, IScope
        => ScopeLookup.TryFind(api, shape.Handle, out scope);
    
    public bool TryGetScope<TScope>(in EntityHandle handle, out TScope scope)
        where TScope : struct, IArchetypeQueryable, IScope
        => ScopeLookup.TryFind(api, handle, out scope);

    public EntityQuery<T1, T2> Query<T1, T2>()
        where T1 : struct, IArchetypeQueryable
        where T2 : struct, IArchetypeMixin
        => new(api);

    public EntityQuery<T1, T2, T3> Query<T1, T2, T3>()
        where T1 : struct, IArchetypeQueryable
        where T2 : struct, IArchetypeMixin
        where T3 : struct, IArchetypeMixin
        => new(api);

    public EntityQuery<T1, T2, T3, T4> Query<T1, T2, T3, T4>()
        where T1 : struct, IArchetypeQueryable
        where T2 : struct, IArchetypeMixin
        where T3 : struct, IArchetypeMixin
        where T4 : struct, IArchetypeMixin
        => new(api);

    public EntityQuery<T1, T2, T3, T4, T5> Query<T1, T2, T3, T4, T5>()
        where T1 : struct, IArchetypeQueryable
        where T2 : struct, IArchetypeMixin
        where T3 : struct, IArchetypeMixin
        where T4 : struct, IArchetypeMixin
        where T5 : struct, IArchetypeMixin
        => new(api);

    public EntityQuery<T1, T2, T3, T4, T5, T6> Query<T1, T2, T3, T4, T5, T6>()
        where T1 : struct, IArchetypeQueryable
        where T2 : struct, IArchetypeMixin
        where T3 : struct, IArchetypeMixin
        where T4 : struct, IArchetypeMixin
        where T5 : struct, IArchetypeMixin
        where T6 : struct, IArchetypeMixin
        => new(api);

    public bool TryLookup<T, TKey>(TKey key, out T shape) 
        where T : struct, IArchetypeQueryable, IIndexed<TKey>
    {
        if (IndexRegistry.TryFind<T, TKey>(api, key, out var entity))
        {
            shape = new T { Handle = new EntityHandle(entity, api) };
            return true;
        }

        shape = default;
        return false;
    }

    public T Create<T>(Scope scope) where T : struct, IArchetype
        => new() { Handle = new EntityHandle(api.Create(ArchetypeRegistry.SetFor(typeof(T), default(T).Components), scope.Handle.RawEntity), api) };
    
    public T Create<T>(Scope scope, PlayerId owner) where T : struct, IArchetype
        => new()
        {
            Handle = new EntityHandle(
                api.Create(ArchetypeRegistry.SetFor(typeof(T), default(T).Components), scope.Handle.RawEntity, owner),
                api)
        };
}
