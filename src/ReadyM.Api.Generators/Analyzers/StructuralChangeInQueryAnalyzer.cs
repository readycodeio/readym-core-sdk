using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ReadyM.Api.Generators.Analyzers;

/// <summary>
/// Reports growing the world from inside a query, which the SDK refuses at run time.
/// </summary>
/// <remarks>
/// A query holds the component storage it was handed when the loop started. Growing an archetype
/// allocates a new array and copies, so a loop that carries on reading and writing through the chunk
/// addresses whatever nothing points at any more. That went unnoticed for a while because it loses
/// writes silently rather than failing, which is why it is worth catching at the call site instead of
/// on the tick it first matters.
///
/// It reads the loop as written, so it sees the direct case and not one reached through a method or a
/// delegate. The run-time refusal is what covers the rest; this only moves the common case earlier.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class StructuralChangeInQueryAnalyzer : DiagnosticAnalyzer
{
    private const string Id = "READYM002";

    private static readonly DiagnosticDescriptor Rule = new(
        Id,
        "Structural change inside a query",
        "'{0}' cannot run inside a query. Collect what the loop needs and do it after the loop.",
        "ReadyM",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A query walks storage handed to it when the loop started. Growing the world "
            + "replaces that storage and the loop's reads and writes are lost. Deleting is allowed: "
            + "it is held and applied once the loop ends.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Inspect, SyntaxKind.InvocationExpression);
    }

    /// The calls that grow the world. Deleting is absent because a query may ask for it, and an
    /// entity's components are fixed at creation, so creating is the only way left to grow one.
    private static bool Grows(IMethodSymbol method) => method.Name is "Create";

    private static void Inspect(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol
            is not IMethodSymbol method || !Grows(method) || !IsSdk(method.ContainingType))
            return;

        if (EnclosingQuery(invocation, context.SemanticModel, context.CancellationToken) is null)
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), method.Name));
    }

    /// <summary>
    /// The loop this call sits in, if it walks a query.
    /// </summary>
    /// <remarks>
    /// A lambda or a local function ends the search: its body may well run somewhere else, and
    /// guessing would turn a lint into a compile error someone cannot argue with.
    /// </remarks>
    private static SyntaxNode? EnclosingQuery(SyntaxNode node, SemanticModel model, System.Threading.CancellationToken ct)
    {
        for (var current = node.Parent; current is not null; current = current.Parent)
        {
            switch (current)
            {
                case LambdaExpressionSyntax:
                case AnonymousMethodExpressionSyntax:
                case LocalFunctionStatementSyntax:
                case MemberDeclarationSyntax:
                    return null;

                case ForEachStatementSyntax loop when Walks(loop.Expression, model, ct):
                case ForEachVariableStatementSyntax variable when Walks(variable.Expression, model, ct):
                    return current;
            }
        }

        return null;
    }

    private static bool Walks(ExpressionSyntax expression, SemanticModel model, System.Threading.CancellationToken ct)
        => model.GetTypeInfo(expression, ct).Type is INamedTypeSymbol type
        && type.Name is "EntityQuery" or "QueryBuilder" or "ChunkQuery"
        && IsSdk(type);

    private static bool IsSdk(INamedTypeSymbol? type)
        => type?.ContainingAssembly?.Name.StartsWith("ReadyM.SDK", System.StringComparison.Ordinal) == true;
}
