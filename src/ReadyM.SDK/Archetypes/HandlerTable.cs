using System.Collections.Concurrent;
using ReadyM.SDK.Entities;

namespace ReadyM.SDK.Archetypes;

/// Collection of callbacks applied to entities, either on creation or deletion.
internal sealed class HandlerTable
{
    private readonly ConcurrentDictionary<ComponentSet, Declaration> _declarations = new();

    public bool Any => !_declarations.IsEmpty;

    /// What the shape asked to run for itself. A shape has at most one.
    public void Register(ComponentSet shape, EntityHandler handler)
        => Declared(shape).Own = handler;

    /// What something else asked to run when it sees the shape.
    public void Observe(ComponentSet shape, EntityHandler handler) 
        => Declared(shape).Watch(handler);

    /// <param name="ownFirst">
    /// The shape's own handler runs first on creation (true), last on deletion (false).
    /// </param>
    public void Run(in EntityHandle handle, bool ownFirst)
    {
        // Both passes are over the whole entity rather than over one shape, so the order holds
        // between shapes as well as within one.
        Pass(handle, ownFirst);
        Pass(handle, !ownFirst);
    }

    private void Pass(in EntityHandle handle, bool own)
    {
        foreach (var declaration in _declarations.Values)
        {
            if (!handle.Has(declaration.Shape))
                continue;

            if (own)
                declaration.RunOwn(handle);
            else
                declaration.RunWatching(handle);
        }
    }

    private Declaration Declared(ComponentSet shape)
        => _declarations.GetOrAdd(shape, static set => new Declaration(set));

    private sealed class Declaration(ComponentSet shape)
    {
#if NET
        private readonly Lock _gate = new();
#else
        private readonly object _gate = new();
#endif

        private EntityHandler[] _watching = [];

        public ComponentSet Shape { get; } = shape;

        public EntityHandler? Own { get; set; }

        /// Module initializers of different assemblies can land here at once.
        public void Watch(EntityHandler handler)
        {
            lock (_gate)
                _watching = [.. _watching, handler];
        }

        public void RunOwn(in EntityHandle handle) => Own?.Invoke(handle);

        public void RunWatching(in EntityHandle handle)
        {
            foreach (var handler in _watching)
                handler(handle);
        }
    }
}
