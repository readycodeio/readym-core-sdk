using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ReadyM.Api.Generators.Services;

/// Explains what a <c>[Service]</c> class got wrong before the generator silently writes nothing.
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ServiceAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [.. ServiceShape.All];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(Inspect, SymbolKind.NamedType);
    }

    private static void Inspect(SymbolAnalysisContext context)
    {
        if (context.Symbol is not INamedTypeSymbol { TypeKind: TypeKind.Class } service
            || !ServiceShape.IsService(service))
            return;

        foreach (var problem in ServiceShape.Read(service).Problems)
            context.ReportDiagnostic(problem);
    }
}
