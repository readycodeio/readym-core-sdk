using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Friflo.Engine.ECS;
using ReadyM.Api.DI;
using ReadyM.Api.ECS.Worlds;

namespace ReadyM.Api.ECS.Managers;

internal class EntityDeleteManager : IHostedService
{
    private readonly Store _world;
    private readonly List<IEntityDeleteImpl> _impls = [];

    public EntityDeleteManager(Store world, IReadOnlyList<IEntityDeleteImpl> impls)
    {
        _world = world;
        _impls.AddRange(impls);
    }

    public void OnScopeStart()
    {
        _world.OnEntityDelete += OnEntityDeleteHandler;
    }

    public void Dispose()
    {
        _world.OnEntityDelete -= OnEntityDeleteHandler;
    }

    public bool TryGetImpl<TImpl>([NotNullWhen(true)] out TImpl? impl)
        where TImpl : class, IEntityDeleteImpl
    {
        impl = _impls.FirstOrDefault(impl => impl is TImpl) as TImpl;
        return impl != null;
    }

    private void OnEntityDeleteHandler(EntityDelete ev)
    {
        var entity = ev.Entity;

        foreach (var impl in _impls)
        {
            impl.HandleDelete(entity);
        }
    }
}