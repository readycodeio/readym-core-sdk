using System.Collections.Generic;
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
        context.RegisterCompilationStartAction(NoCycles);
    }

    /// <summary>Refuses update constraints that come back on themselves.</summary>
    /// <remarks>
    /// Every cycle is caught here, not only the ones inside this assembly: an edge is a typeof, so
    /// a cycle spanning assemblies would need those assemblies to reference each other, which C#
    /// does not allow. The check at load is a guard for services registered by hand.
    /// </remarks>
    private static void NoCycles(CompilationStartAnalysisContext context)
    {
        var services = new List<INamedTypeSymbol>();

        context.RegisterSymbolAction(symbol =>
        {
            if (symbol.Symbol is INamedTypeSymbol { TypeKind: TypeKind.Class } service
                && ServiceShape.Updates(service))
                lock (services)
                    services.Add(service);
        }, SymbolKind.NamedType);

        context.RegisterCompilationEndAction(end =>
        {
            foreach (var reported in ServiceCycles.Find(services))
                end.ReportDiagnostic(reported);
        });
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
