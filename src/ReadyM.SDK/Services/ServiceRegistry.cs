using System.Collections.Concurrent;
using System.ComponentModel;
using ReadyM.Api.DI;

namespace ReadyM.SDK.Services;

/// Holds service registrations.
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ServiceRegistry
{
    private static readonly ConcurrentDictionary<Type, Declaration> Declared = new();

    /// Called by generated code for a [Service] that declared an update.
    public static void Register<TService>() where TService : class, IUpdatingService
        => Declared[typeof(TService)] = new Updating<TService>();

    /// Called by generated code for a [Service] that declared no update.
    public static void Hold<TService>() where TService : class
        => Declared[typeof(TService)] = new Held<TService>();

    /// Registers every declared service as a singleton in DI.
    public static void RegisterAll(IDependencyContainer container)
    {
        foreach (var declaration in Declared.Values)
            declaration.Register(container);
    }

    /// The services a game has to update. The rest are resolved lazily.
    public static List<IUpdatingService> Resolve(IDependencyContainer container)
        => Declared.Values
            .Select(declaration => declaration.Resolve(container))
            .OfType<IUpdatingService>()
            .ToList();

    private abstract class Declaration
    {
        public abstract void Register(IDependencyContainer container);

        // ReSharper disable once MemberHidesStaticFromOuterClass
        public abstract IUpdatingService? Resolve(IDependencyContainer container);
    }

    private sealed class Updating<TService> : Declaration
        where TService : class, IUpdatingService
    {
        public override void Register(IDependencyContainer container)
            => container.RegisterSingleton<TService>();

        // ReSharper disable once MemberHidesStaticFromOuterClass
        public override IUpdatingService Resolve(IDependencyContainer container)
            => container.Resolve<TService>();
    }

    private sealed class Held<TService> : Declaration
        where TService : class
    {
        public override void Register(IDependencyContainer container)
            => container.RegisterSingleton<TService>();

        // ReSharper disable once MemberHidesStaticFromOuterClass
        public override IUpdatingService? Resolve(IDependencyContainer container)
            => null;
    }
}