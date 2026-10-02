using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReadyM.Api.Generators.Archetypes;

namespace ReadyM.Api.Generators.Mods;

/// What a class has to look like to be a mod's entry point, for the generator and the analyzer alike.
internal static class ModEntryShape
{
    public const string StartName = "Start";

    public static readonly DiagnosticDescriptor NotPartial = new(
        "READYM027",
        "Mod entry point is not partial",
        "'{0}' is a mod entry point, so the generator has a half of it to write. Declare it partial.",
        "ReadyM",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NotSealed = new(
        "READYM028",
        "Mod entry point is not sealed",
        "'{0}' is a mod entry point, and the loader builds exactly one of it. Deriving from it would "
        + "not give you a second one, so declare it sealed.",
        "ReadyM",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NotAHook = new(
        "READYM029",
        "Mod entry point method cannot be run",
        "'{0}.{1}' cannot be called by the loader: it has to be a private instance method returning "
        + "void and taking nothing. An entry point asks for everything else in its constructor.",
        "ReadyM",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NotAlone = new(
        "READYM030",
        "More than one mod entry point",
        "'{0}' is a second [ModEntry] class in this mod, and a mod is entered through exactly one. "
        + "'{1}' declares the other.",
        "ReadyM",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor[] All = [NotPartial, NotSealed, NotAHook, NotAlone];

    public static Entry Read(INamedTypeSymbol entry)
    {
        var at = entry.Locations.FirstOrDefault() ?? Location.None;
        var problems = new List<Diagnostic>();

        if (!IsPartial(entry))
            return Entry.Refused(Diagnostic.Create(NotPartial, at, entry.Name));

        if (!entry.IsSealed)
            problems.Add(Diagnostic.Create(NotSealed, at, entry.Name));

        return new Entry(ReadStart(entry, at, problems), [.. problems]);
    }

    public static bool IsModEntry(ISymbol symbol)
        => symbol.GetAttributes().Any(attribute
            => attribute.AttributeClass?.ToDisplayString() == ArchetypeNames.ModEntryAttribute);

    /// The start the loader calls, or null when the entry point declared none it can call.
    private static IMethodSymbol? ReadStart(INamedTypeSymbol entry, Location at, List<Diagnostic> problems)
    {
        var named = entry.GetMembers(StartName).OfType<IMethodSymbol>().ToList();

        if (named.Count == 0)
            return null;

        // Every one of them takes nothing, so C# already refuses a second callable overload.
        if (named.FirstOrDefault(Callable) is { } callable)
            return callable;

        problems.Add(Diagnostic.Create(
            NotAHook, named[0].Locations.FirstOrDefault() ?? at, entry.Name, StartName));

        return null;
    }

    private static bool Callable(IMethodSymbol method)
        => method is
        {
            IsStatic: false,
            DeclaredAccessibility: Accessibility.Private,
            Parameters.Length: 0,
            TypeParameters.Length: 0,
            ReturnsVoid: true
        };

    private static bool IsPartial(INamedTypeSymbol symbol)
        => symbol.DeclaringSyntaxReferences
            .Select(reference => reference.GetSyntax())
            .OfType<ClassDeclarationSyntax>()
            .Any(declaration => declaration.Modifiers.Any(modifier => modifier.ValueText == "partial"));

    internal readonly struct Entry(IMethodSymbol? start, Diagnostic[] problems)
    {
        public IMethodSymbol? Start { get; } = start;

        public Diagnostic[] Problems { get; } = problems;

        public static Entry Refused(Diagnostic problem) => new(null, [problem]);
    }
}
