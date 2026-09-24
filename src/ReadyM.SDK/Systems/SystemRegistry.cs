using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using ReadyM.Api.DI;

namespace ReadyM.SDK.Systems;

/// Holds system registrations.
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SystemRegistry
{
    private static readonly ConcurrentDictionary<Type, Declaration> Declared = new();

    /// Called by generated code for a class declared [System].
    public static void Register<TSystem>() where TSystem : class, IModSystem
        => Declared[typeof(TSystem)] = new Declaration<TSystem>();

    /// Registers every declared system as a singleton.
    /// A game calls this once its mods are loaded, before anything updates.
    public static void RegisterAll(IDependencyContainer container)
    {
        foreach (var declaration in Declared.Values)
            declaration.Register(container);
    }
    
    public static IEnumerable<IModSystem> Resolve(IDependencyContainer container)
        => Declared.Values.Select(declaration => declaration.Resolve(container)).ToList();

    // The type is only known where the system was declared, so what to do with it is captured there.
    private abstract class Declaration
    {
        public abstract void Register(IDependencyContainer container);

        // ReSharper disable once MemberHidesStaticFromOuterClass
        public abstract IModSystem Resolve(IDependencyContainer container);
    }

    private sealed class Declaration<TSystem> : Declaration
        where TSystem : class, IModSystem
    {
        public override void Register(IDependencyContainer container) 
            => container.RegisterSingleton<TSystem>();

        // ReSharper disable once MemberHidesStaticFromOuterClass
        public override IModSystem Resolve(IDependencyContainer container) 
            => container.Resolve<TSystem>();
    }
}