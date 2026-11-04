using System;
using System.Collections.Generic;
using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;

namespace ReadyM.Api.Multiplayer.ECS.Managers;

internal sealed class ArchetypeEventRouter : IDisposable
{
    internal readonly struct ArchetypeEntry(ArchetypeEventRouter owner, ArchetypeId archetypeId)
    {
        public event Action<EntityCreate>? OnEntityCreate
        {
            add => owner._createHandlers[archetypeId] += value;
            remove => owner._createHandlers[archetypeId] -= value;
        }
        
        public event Action<EntityDelete>? OnEntityDelete
        {
            add => owner._deleteHandlers[archetypeId] += value;
            remove => owner._deleteHandlers[archetypeId] -= value;
        }
    }
    
    private readonly Store _store;

    private readonly Dictionary<ArchetypeId, Action<EntityCreate>?> _createHandlers = new();
    private readonly Dictionary<ArchetypeId, Action<EntityDelete>?> _deleteHandlers = new();
    
    public ArchetypeEntry this[ArchetypeId archetypeId]
    {
        get
        {
            if (!_createHandlers.ContainsKey(archetypeId))
            {
                _createHandlers[archetypeId] = null;
                _deleteHandlers[archetypeId] = null;
            }
            return new ArchetypeEntry(this, archetypeId);
        }
    }
    
    public ArchetypeEventRouter(Store store)
    {
        _store = store;
        
        // it's fine to subscribe to events here, since this class is only a proxy
        _store.OnEntityCreate += OnEntityCreate;
        _store.OnEntityDelete += OnEntityDelete;
    }

    public void Dispose()
    {
        _store.OnEntityDelete -= OnEntityDelete;
        _store.OnEntityCreate -= OnEntityCreate;
    }

    private void OnEntityCreate(EntityCreate ev)
    {
        if (!ev.Entity.TryGetComponent<MetadataComponent>(out var meta))
            return;
        
        if (!_createHandlers.TryGetValue(meta.Archetype, out var handler))
            return;
        
        handler?.Invoke(ev);
    }

    private void OnEntityDelete(EntityDelete ev)
    {
        if (!ev.Entity.TryGetComponent<MetadataComponent>(out var meta))
            return;

        if (!_deleteHandlers.TryGetValue(meta.Archetype, out var handler))
            return;
        
        handler?.Invoke(ev);
    }
}