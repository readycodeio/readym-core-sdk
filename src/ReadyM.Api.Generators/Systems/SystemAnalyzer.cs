using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ReadyM.Api.Generators.Systems;

/// <summary>
/// Explains what a <c>[System]</c> class is missing before the generator silently writes nothing.
/// </summary>
/// <remarks>
/// It reports rather than the generator so the message lands in the editor, where a generator's
/// diagnostics do not: an IDE has no descriptor to register a severity against for one, and drops it.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SystemAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [.. SystemShape.All];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(Inspect, SymbolKind.NamedType);
    }

    private static void Inspect(SymbolAnalysisContext context)
    {
        if (context.Symbol is not INamedTypeSymbol { TypeKind: TypeKind.Class } system
            || !SystemShape.IsSystem(system))
            return;

        if (SystemShape.Read(system).Problem is { } problem)
            context.ReportDiagnostic(problem);
    }
}
