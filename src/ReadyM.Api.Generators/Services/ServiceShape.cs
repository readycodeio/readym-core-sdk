using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReadyM.Api.Generators.Archetypes;

namespace ReadyM.Api.Generators.Services;

/// What a class has to look like to be a service, for the generator and the analyzer alike.
internal static class ServiceShape
{
    public const string UpdateName = "Update";

    public const string StartName = "Start";

    public const string StopName = "Stop";

    public const string OnEnabledName = "OnEnabled";

    public const string OnDisabledName = "OnDisabled";

    public static readonly DiagnosticDescriptor NotPartial = new(
        "READYM022",
        "Service is not partial",
        "'{0}' is a service, so the generator has a half of it to write. Declare it partial.",
        "ReadyM",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NotSealed = new(
        "READYM023",
        "Service is not sealed",
        "'{0}' is a service, and the SDK builds exactly one of it under its own name. Deriving from "
        + "it would not give you a second one, so declare it sealed.",
        "ReadyM",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NotAHook = new(
        "READYM024",
        "Service method cannot be run",
        "'{0}.{1}' cannot be called by the SDK: it has to be a private instance method returning "
        + "void and taking nothing. A service asks for everything else in its constructor, and "
        + "reads the clock through its own Time.",
        "ReadyM",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NotAHandler = new(
        "READYM025",
        "Handler cannot be run",
        "'{0}.{1}' watches {2}, so it has to be a private instance method returning void and taking "
        + "that shape and nothing else.",
        "ReadyM",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor WatchesNothing = new(
        "READYM026",
        "Handler names no shape",
        "'{0}.{1}' is a handler in a service, so it has to name a shape to watch, as in "
        + "[CreateHandler(typeof(Shape))], where Shape is an [Archetype] or an [ArchetypeMixin]. "
        + "Only a shape's own handler may leave it out.",
        "ReadyM",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor OrdersNothing = new(
        "READYM033",
        "Update order names something that does not update",
        "'{0}' orders its update against '{1}', which is not a [Service] that declares an Update. "
        + "Only a service that ticks can be ordered against.",
        "ReadyM",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor OrdersWithoutUpdate = new(
        "READYM034",
        "Update order on a service that does not update",
        "'{0}' carries [UpdateOrder] somewhere other than on an Update the SDK can run, so there is "
        + "nothing for it to order. Put it on the service's Update.",
        "ReadyM",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor OrdersInACycle = new(
        "READYM035",
        "Update order is a cycle",
        "'{0}' orders its update in a cycle: {1}. There is no order that satisfies every constraint, "
        + "so remove one of them.",
        "ReadyM",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor[] All =
        [NotPartial, NotSealed, NotAHook, NotAHandler, WatchesNothing,
         OrdersNothing, OrdersWithoutUpdate, OrdersInACycle];

    public static Service Read(INamedTypeSymbol service)
    {
        var at = service.Locations.FirstOrDefault() ?? Location.None;
        var problems = new List<Diagnostic>();

        if (!IsPartial(service))
            return Service.Refused(Diagnostic.Create(NotPartial, at, service.Name));

        if (!service.IsSealed)
            problems.Add(Diagnostic.Create(NotSealed, at, service.Name));

        var update = ReadHook(service, UpdateName, at, problems);
        var start = ReadHook(service, StartName, at, problems);
        var stop = ReadHook(service, StopName, at, problems);
        var onEnabled = ReadHook(service, OnEnabledName, at, problems);
        var onDisabled = ReadHook(service, OnDisabledName, at, problems);
        var watching = ReadWatchers(service, ArchetypeNames.CreateHandlerAttribute, problems);
        var leaving = ReadWatchers(service, ArchetypeNames.DeleteHandlerAttribute, problems);
        var order = ReadOrder(service, update, at, problems);

        return new Service(
            update, start, stop, onEnabled, onDisabled, watching, leaving, order, [.. problems]);
    }

    public static bool IsService(ISymbol symbol)
        => symbol.GetAttributes().Any(attribute
            => attribute.AttributeClass?.ToDisplayString() == ArchetypeNames.ServiceAttribute);

    /// <summary>What [UpdateOrder] on the update said, having checked that it can mean anything.</summary>
    private static Ordering ReadOrder(
        INamedTypeSymbol service,
        IMethodSymbol? update,
        Location at,
        List<Diagnostic> problems)
    {
        // Looked for anywhere rather than on Update alone, so putting it on the wrong method is
        // refused instead of quietly doing nothing.
        var carrier = service.GetMembers().OfType<IMethodSymbol>()
            .FirstOrDefault(method => Order(method) is not null);

        if (carrier is null)
            return Ordering.Default;

        var attribute = Order(carrier)!;
        var where = carrier.Locations.FirstOrDefault() ?? at;

        // Ordering something the SDK never runs says nothing, and reads as though it does.
        if (update is null || !SymbolEqualityComparer.Default.Equals(carrier, update))
        {
            problems.Add(Diagnostic.Create(OrdersWithoutUpdate, where, service.Name));
            return Ordering.Default;
        }

        var priority = attribute.ConstructorArguments.Length > 0
                       && attribute.ConstructorArguments[0].Value is int given
            ? given
            : Ordering.DefaultPriority;

        return new Ordering(
            priority,
            Targets(attribute, "Before", service, where, problems),
            Targets(attribute, "After", service, where, problems));
    }

    private static AttributeData? Order(ISymbol method)
        => method.GetAttributes().FirstOrDefault(attribute
            => attribute.AttributeClass?.ToDisplayString() == ArchetypeNames.UpdateOrderAttribute);

    /// The services one end of the constraint names, refusing anything that does not tick.
    private static IReadOnlyList<INamedTypeSymbol> Targets(
        AttributeData attribute,
        string name,
        INamedTypeSymbol service,
        Location at,
        List<Diagnostic> problems)
    {
        var named = attribute.NamedArguments.FirstOrDefault(pair => pair.Key == name).Value;

        if (named.Kind != TypedConstantKind.Array)
            return [];

        var found = new List<INamedTypeSymbol>();

        foreach (var value in named.Values)
        {
            if (value.Value is not INamedTypeSymbol target)
                continue;

            if (!Updates(target))
            {
                problems.Add(Diagnostic.Create(OrdersNothing, at, service.Name, target.Name));
                continue;
            }

            found.Add(target);
        }

        return found;
    }

    /// Whether the type is a service the SDK ticks, which is all that can be ordered against.
    public static bool Updates(INamedTypeSymbol type)
        => IsService(type)
           && type.GetMembers(UpdateName).OfType<IMethodSymbol>().Any(Callable);

    /// One duck-typed method the SDK calls, or null when the service declared none it can call.
    private static IMethodSymbol? ReadHook(
        INamedTypeSymbol service,
        string name,
        Location at,
        List<Diagnostic> problems)
    {
        var named = service.GetMembers(name).OfType<IMethodSymbol>().ToList();

        if (named.Count == 0)
            return null;

        // Every one of them takes nothing, so C# already refuses a second callable overload.
        if (named.FirstOrDefault(Callable) is { } callable)
            return callable;

        problems.Add(Diagnostic.Create(
            NotAHook, named[0].Locations.FirstOrDefault() ?? at, service.Name, name));

        return null;
    }

    private static IReadOnlyList<(IMethodSymbol Method, INamedTypeSymbol Shape)> ReadWatchers(
        INamedTypeSymbol service,
        string attributeName,
        List<Diagnostic> problems)
    {
        var found = new List<(IMethodSymbol, INamedTypeSymbol)>();

        foreach (var method in service.GetMembers().OfType<IMethodSymbol>())
        {
            if (Handler(method, attributeName) is not { } attribute)
                continue;

            var at = method.Locations.FirstOrDefault() ?? Location.None;

            if (attribute.ConstructorArguments.Length != 1
                || attribute.ConstructorArguments[0].Value is not INamedTypeSymbol shape
                || !IsShape(shape))
            {
                problems.Add(Diagnostic.Create(WatchesNothing, at, service.Name, method.Name));
                continue;
            }

            if (!Hidden(method)
                || method.Parameters.Length != 1
                || !SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, shape))
            {
                problems.Add(Diagnostic.Create(NotAHandler, at, service.Name, method.Name, shape.Name));
                continue;
            }

            found.Add((method, shape));
        }

        return found;
    }

    /// The named handler attribute on a method, however it was written, or null for anything else.
    private static AttributeData? Handler(IMethodSymbol method, string attributeName)
    {
        foreach (var attribute in method.GetAttributes())
            if (attribute.AttributeClass?.ToDisplayString() == attributeName)
                return attribute;

        return null;
    }

    /// Only a shape carries the watch point a service hands its handler in to.
    private static bool IsShape(INamedTypeSymbol type)
        => type.GetAttributes().Any(attribute
            => attribute.AttributeClass?.ToDisplayString() is ArchetypeNames.ArchetypeAttribute
                or ArchetypeNames.MixinAttribute);

    private static bool Callable(IMethodSymbol method) => Hidden(method) && method.Parameters.Length == 0;

    /// Everything the SDK calls on a service is the service's own business, so it stays private:
    /// the generated half sits inside the class and reaches it anyway.
    private static bool Hidden(IMethodSymbol method)
        => !method.IsStatic
           && method.ReturnsVoid
           && method.TypeParameters.Length == 0
           && method.DeclaredAccessibility == Accessibility.Private;

    private static bool IsPartial(INamedTypeSymbol service)
        => service.DeclaringSyntaxReferences
            .Select(reference => reference.GetSyntax())
            .OfType<ClassDeclarationSyntax>()
            .Any(declaration => declaration.Modifiers.Any(modifier => modifier.ValueText == "partial"));

    internal sealed class Service(
        IMethodSymbol? update,
        IMethodSymbol? start,
        IMethodSymbol? stop,
        IMethodSymbol? onEnabled,
        IMethodSymbol? onDisabled,
        IReadOnlyList<(IMethodSymbol Method, INamedTypeSymbol Shape)> watching,
        IReadOnlyList<(IMethodSymbol Method, INamedTypeSymbol Shape)> leaving,
        Ordering order,
        ImmutableArray<Diagnostic> problems)
    {
        public IMethodSymbol? Update { get; } = update;

        public IMethodSymbol? Start { get; } = start;

        public IMethodSymbol? Stop { get; } = stop;

        /// Run as its update starts ticking, which is at startup unless something disabled it.
        public IMethodSymbol? OnEnabled { get; } = onEnabled;

        /// Run once its update has stopped ticking.
        public IMethodSymbol? OnDisabled { get; } = onDisabled;

        /// Shapes it is told about as they are created.
        public IReadOnlyList<(IMethodSymbol Method, INamedTypeSymbol Shape)> Watching { get; } = watching;

        /// Shapes it is told about just before they go.
        public IReadOnlyList<(IMethodSymbol Method, INamedTypeSymbol Shape)> Leaving { get; } = leaving;

        /// Where its update runs relative to the others.
        public Ordering Order { get; } = order;

        public ImmutableArray<Diagnostic> Problems { get; } = problems;

        /// Started by the SDK once the host says its game is up. OnEnabled counts, since the first
        /// one fires then and a service declaring only that would otherwise not be built to see it.
        public bool Started => Start is not null || OnEnabled is not null;

        /// Only a Stop needs the container to dispose it, which is what ends a service.
        public bool Disposable => Stop is not null;

        public static Service Refused(Diagnostic problem)
            => new(null, null, null, null, null, [], [], Ordering.Default, [problem]);
    }

    /// What [UpdateOrder] said, or what it means to have left it off.
    internal readonly struct Ordering(int priority, IReadOnlyList<INamedTypeSymbol> before, IReadOnlyList<INamedTypeSymbol> after)
    {
        public const int DefaultPriority = 100;

        public int Priority { get; } = priority;

        public IReadOnlyList<INamedTypeSymbol> Before { get; } = before;

        public IReadOnlyList<INamedTypeSymbol> After { get; } = after;

        public static Ordering Default => new(DefaultPriority, [], []);

        /// Whether anything was said, which is what decides if the registration carries it.
        public bool IsDefault => Priority == DefaultPriority && Before.Count == 0 && After.Count == 0;
    }
}
