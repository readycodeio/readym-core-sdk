using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ReadyM.Api.Generators.Mods;

/// Explains what a <c>[ModEntry]</c> class got wrong before the generator silently writes nothing.
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ModEntryAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [.. ModEntryShape.All];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(Inspect, SymbolKind.NamedType);
        context.RegisterCompilationStartAction(OnlyOne);
    }

    private static void Inspect(SymbolAnalysisContext context)
    {
        if (context.Symbol is not INamedTypeSymbol { TypeKind: TypeKind.Class } entry
            || !ModEntryShape.IsModEntry(entry))
            return;

        foreach (var problem in ModEntryShape.Read(entry).Problems)
            context.ReportDiagnostic(problem);
    }

    /// <summary>A mod is entered through one class, which is a thing only the whole compilation knows.</summary>
    private static void OnlyOne(CompilationStartAnalysisContext context)
    {
        var found = new List<INamedTypeSymbol>();

        context.RegisterSymbolAction(symbol =>
        {
            if (symbol.Symbol is INamedTypeSymbol { TypeKind: TypeKind.Class } entry
                && ModEntryShape.IsModEntry(entry))
                lock (found)
                    found.Add(entry);
        }, SymbolKind.NamedType);

        context.RegisterCompilationEndAction(end =>
        {
            // By name, so the one that is reported stays the same whatever order the symbols arrived in.
            var ordered = found.OrderBy(entry => entry.ToDisplayString(), StringComparer.Ordinal).ToList();

            foreach (var extra in ordered.Skip(1))
                end.ReportDiagnostic(Diagnostic.Create(
                    ModEntryShape.NotAlone,
                    extra.Locations.FirstOrDefault() ?? Location.None,
                    extra.Name,
                    ordered[0].Name));
        });
    }
}
