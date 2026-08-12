using System;
using System.Collections.Generic;
using Friflo.Engine.ECS;

namespace ReadyM.Api.ECS.Managers;

internal class DisposableEntityDeleteImpl : IEntityDeleteImpl
{
    private readonly Dictionary<ComponentType, DisposableEntryBase?> _entryByComponentType = [];

    private abstract class DisposableEntryBase
    {
        public abstract void Dispose(Entity entity);
    }

    private class DisposableEntry<T> : DisposableEntryBase
        where T : struct, IComponent, IDisposable
    {
        public override void Dispose(Entity entity)
        {
            if (!entity.TryGetComponent<T>(out var comp))
                return;
            
            comp.Dispose();
        }
    }

    private DisposableEntryBase? GetComponentEntry(ComponentType componentType)
    {
        if (!_entryByComponentType.TryGetValue(componentType, out var entry))
        {
            if (!typeof(IDisposable).IsAssignableFrom(componentType.Type))
            {
                entry = null;
            }
            else
            {
                var entryType = typeof(DisposableEntry<>).MakeGenericType(componentType.Type);
                entry = (DisposableEntryBase)Activator.CreateInstance(entryType)!;
            }

            _entryByComponentType.Add(componentType, entry);
        }
        
        return entry;
    }
    
    public void HandleDelete(Entity entity)
    {
        foreach (var comp in entity.Components)
        {
            var componentType = comp.Type;
            var entry = GetComponentEntry(componentType);
            entry?.Dispose(entity);
        }
    }
}