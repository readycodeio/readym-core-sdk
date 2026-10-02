using System.Collections.Concurrent;
using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ReadyM.Api.DI;
using ReadyM.SDK.Attributes;

namespace ReadyM.SDK.Services;

/// Holds service registrations.
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ServiceRegistry
{
    private static readonly ConcurrentDictionary<Type, Declaration> Declared = new();

    /// Called by generated code for a [Service] that declared an update.
    public static void Register<TService>(
        int priority = UpdateOrderAttribute.DefaultPriority,
        Type[]? before = null,
        Type[]? after = null)
        where TService : class, IUpdatingService
        => Declared[typeof(TService)] = new Updating<TService>(new ServiceOrder(priority, before, after));

    /// Called by generated code for a [Service] that declared no update.
    public static void Hold<TService>() where TService : class
        => Declared[typeof(TService)] = new Held<TService>();

    /// <summary>Registers every declared service as a singleton in DI.</summary>
    /// <remarks>
    /// The update order is worked out here as well, so services ordered in a cycle are refused while
    /// the mods are loading rather than on the first tick, a long way from the cause.
    /// </remarks>
    /// <exception cref="ServiceOrderException">The constraints contain a cycle.</exception>
    public static void RegisterAll(IDependencyContainer container, ILogger? logger = null)
    {
        foreach (var declaration in Declared.Values)
            declaration.Register(container);

        container.RegisterSingleton<IServices>(new ServiceSwitchboard(container), replace: true);

        InUpdateOrder(logger);
    }

    /// <summary>Starts every service that declared a Start, which is what [Service] calls ready.</summary>
    /// <returns>How many were started, which a game can log.</returns>
    public static int StartAll(IDependencyContainer container)
    {
        var started = 0;

        foreach (var declaration in Declared.Values.OrderBy(
                     declaration => declaration.Service.FullName, StringComparer.Ordinal))
        {
            // Read off the type, so a service that declared no Start is still never built here.
            if (!typeof(IStartedService).IsAssignableFrom(declaration.Service))
                continue;

            ((IStartedService)declaration.Get(container)).Start();
            started++;
        }

        return started;
    }

    /// The services a game has to update, in the order they run. The rest are resolved lazily.
    public static List<IUpdatingService> Resolve(IDependencyContainer container)
        => InUpdateOrder(null)
            .Select(declaration => declaration.Resolve(container))
            .OfType<IUpdatingService>()
            .ToList();

    private static List<Declaration> InUpdateOrder(ILogger? logger)
    {
        var updating = Declared.Values.Where(declaration => declaration.Order is not null).ToList();

        return ServiceOrdering.Sort(
            updating,
            declaration => declaration.Service,
            declaration => declaration.Order!.Value,
            (named, by) => logger?.LogWarning(
                "{Service} orders its update against {Named}, which updates nothing here, so the "
                + "constraint is ignored. The mod declaring it is probably not installed.",
                by.Name, named.Name));
    }

    private abstract class Declaration
    {
        public abstract Type Service { get; }

        /// Null for a service that declares no update, which nothing can be ordered against.
        public virtual ServiceOrder? Order => null;

        public abstract void Register(IDependencyContainer container);

        /// The one instance, whether or not it updates.
        public abstract object Get(IDependencyContainer container);

        // ReSharper disable once MemberHidesStaticFromOuterClass
        public abstract IUpdatingService? Resolve(IDependencyContainer container);
    }

    private sealed class Updating<TService>(ServiceOrder order) : Declaration
        where TService : class, IUpdatingService
    {
        public override Type Service => typeof(TService);

        public override ServiceOrder? Order { get; } = order;

        public override void Register(IDependencyContainer container)
            => container.RegisterSingleton<TService>();

        public override object Get(IDependencyContainer container) => container.Resolve<TService>();

        // ReSharper disable once MemberHidesStaticFromOuterClass
        public override IUpdatingService Resolve(IDependencyContainer container)
            => container.Resolve<TService>();
    }

    private sealed class Held<TService> : Declaration
        where TService : class
    {
        public override Type Service => typeof(TService);

        public override void Register(IDependencyContainer container)
            => container.RegisterSingleton<TService>();

        public override object Get(IDependencyContainer container) => container.Resolve<TService>();

        // ReSharper disable once MemberHidesStaticFromOuterClass
        public override IUpdatingService? Resolve(IDependencyContainer container)
            => null;
    }
}