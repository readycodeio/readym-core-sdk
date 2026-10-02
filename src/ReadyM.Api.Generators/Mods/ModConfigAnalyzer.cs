using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ReadyM.Api.Generators.Mods;

/// Explains what a <c>[ModConfig]</c> class got wrong before the generator silently writes nothing.
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ModConfigAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [.. ModConfigShape.All];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(Inspect, SymbolKind.NamedType);
    }

    private static void Inspect(SymbolAnalysisContext context)
    {
        if (context.Symbol is not INamedTypeSymbol { TypeKind: TypeKind.Class } config
            || !ModConfigShape.IsModConfig(config))
            return;

        foreach (var problem in ModConfigShape.Read(config).Problems)
            context.ReportDiagnostic(problem);
    }
}
