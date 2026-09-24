using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReadyM.Api.Generators.Archetypes;

namespace ReadyM.Api.Generators.Systems;

/// What a class has to look like to be a system, for the generator and the analyzer alike.
internal static class SystemShape
{
    public const string UpdateName = "Update";

    public static readonly DiagnosticDescriptor NotPartial = new(
        "READYM022",
        "System is not partial",
        "'{0}' is a system, so the generator has a half of it to write. Declare it partial.",
        "ReadyM",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NoUpdate = new(
        "READYM023",
        "System has no update",
        "'{0}' is a system and has no update, so nothing would ever run. Declare "
        + "'void Update(Tick tick)', or 'void Update()' if it has no use for the time.",
        "ReadyM",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TooManyUpdates = new(
        "READYM024",
        "System has more than one update",
        "'{0}' has {1} updates the SDK could call, and which one it meant would be nobody's to say. "
        + "Keep one and call the rest from it.",
        "ReadyM",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NotAnUpdate = new(
        "READYM025",
        "Update cannot be run",
        "'{0}.Update' cannot be called as an update: it has to be an instance method returning void, "
        + "taking the tick or nothing at all. A system asks for everything else in its constructor.",
        "ReadyM",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor[] All = [NotPartial, NoUpdate, TooManyUpdates, NotAnUpdate];

    public static (IMethodSymbol? Update, Diagnostic? Problem) Read(INamedTypeSymbol system)
    {
        var at = system.Locations.FirstOrDefault() ?? Location.None;

        if (!IsPartial(system))
            return (null, Diagnostic.Create(NotPartial, at, system.Name));

        var named = system.GetMembers(UpdateName).OfType<IMethodSymbol>().ToList();
        var callable = named.Where(Callable).ToList();

        if (callable.Count == 1)
            return (callable[0], null);

        if (callable.Count > 1)
            return (null, Diagnostic.Create(TooManyUpdates, at, system.Name, callable.Count));

        // Something is called Update and cannot be run, which is worth saying plainly rather than
        // reporting that the class has no update at all.
        return named.Count > 0
            ? (null, Diagnostic.Create(NotAnUpdate, named[0].Locations.FirstOrDefault() ?? at, system.Name))
            : (null, Diagnostic.Create(NoUpdate, at, system.Name));
    }

    public static bool IsSystem(ISymbol symbol)
        => symbol.GetAttributes().Any(attribute
            => attribute.AttributeClass?.ToDisplayString() == ArchetypeNames.SystemAttribute);

    private static bool Callable(IMethodSymbol update)
        => !update.IsStatic
           && update.ReturnsVoid
           && update.TypeParameters.Length == 0
           && (update.Parameters.Length == 0
               || (update.Parameters.Length == 1
                   && update.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                   == ArchetypeNames.Tick));

    private static bool IsPartial(INamedTypeSymbol system)
        => system.DeclaringSyntaxReferences
            .Select(reference => reference.GetSyntax())
            .OfType<ClassDeclarationSyntax>()
            .Any(declaration => declaration.Modifiers.Any(modifier => modifier.ValueText == "partial"));
}
