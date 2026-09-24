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

    public static readonly DiagnosticDescriptor NotACreateHandler = new(
        "READYM025",
        "Create handler cannot be run",
        "'{0}.{1}' watches {2}, so it has to be a private instance method returning void and taking "
        + "that shape and nothing else.",
        "ReadyM",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor WatchesNothing = new(
        "READYM026",
        "Create handler names no shape",
        "'{0}.{1}' is a create handler in a service, so it has to name a shape to watch: "
        + "[CreateHandler(typeof(Shape))], where Shape is an [Archetype] or an [ArchetypeMixin]. "
        + "Only a shape's own handler may leave it out.",
        "ReadyM",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NotWatchable = new(
        "READYM027",
        "Shape cannot be watched",
        "'{0}.{1}' watches {2}, which holds nothing of its own: it is exactly the shapes it "
        + "includes, so nothing on an entity says it appeared. Watch one of those instead.",
        "ReadyM",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor[] All =
        [NotPartial, NotSealed, NotAHook, NotACreateHandler, WatchesNothing, NotWatchable];

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
        var watching = ReadWatchers(service, problems);

        return new Service(update, start, stop, watching, [.. problems]);
    }

    public static bool IsService(ISymbol symbol)
        => symbol.GetAttributes().Any(attribute
            => attribute.AttributeClass?.ToDisplayString() == ArchetypeNames.ServiceAttribute);

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
        List<Diagnostic> problems)
    {
        var found = new List<(IMethodSymbol, INamedTypeSymbol)>();

        foreach (var method in service.GetMembers().OfType<IMethodSymbol>())
        {
            if (Handler(method) is not { } attribute)
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
                problems.Add(Diagnostic.Create(NotACreateHandler, at, service.Name, method.Name, shape.Name));
                continue;
            }

            if (!DeclarationModel.For(shape).Watchable)
            {
                problems.Add(Diagnostic.Create(NotWatchable, at, service.Name, method.Name, shape.Name));
                continue;
            }

            found.Add((method, shape));
        }

        return found;
    }

    /// The [CreateHandler] on a method, however it was written, or null for anything else.
    private static AttributeData? Handler(IMethodSymbol method)
    {
        foreach (var attribute in method.GetAttributes())
            if (attribute.AttributeClass?.ToDisplayString() == ArchetypeNames.CreateHandlerAttribute)
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
        IReadOnlyList<(IMethodSymbol Method, INamedTypeSymbol Shape)> watching,
        ImmutableArray<Diagnostic> problems)
    {
        public IMethodSymbol? Update { get; } = update;

        public IMethodSymbol? Start { get; } = start;

        public IMethodSymbol? Stop { get; } = stop;

        public IReadOnlyList<(IMethodSymbol Method, INamedTypeSymbol Shape)> Watching { get; } = watching;

        public ImmutableArray<Diagnostic> Problems { get; } = problems;

        /// A service with either end of a lifetime is started and stopped with the game.
        public bool Hosted => Start is not null || Stop is not null;

        public static Service Refused(Diagnostic problem) => new(null, null, null, [], [problem]);
    }
}
