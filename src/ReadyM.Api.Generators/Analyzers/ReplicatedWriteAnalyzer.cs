using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using ReadyM.Api.Generators.Archetypes;

namespace ReadyM.Api.Generators.Analyzers;

/// <summary>
/// Reports a client writing a replicated value straight through its setter.
/// </summary>
/// <remarks>
/// A value projected onto an archetype has no setter on a client, so writing it is already a compile
/// error. The shape that declares the value keeps its own setter whatever the generator wants, since
/// C# makes the implementing part implement every accessor the declaration has, and that leaves one
/// spelling of the same write that compiles. This closes it, so both read the same.
///
/// Inside a mapping's handler the write is the pull, which is the one place a client is meant to put
/// the game's answer into the ECS, so it is left alone there.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ReplicatedWriteAnalyzer : DiagnosticAnalyzer
{
    private const string Id = "READYM017";

    private static readonly DiagnosticDescriptor Rule = new(
        Id,
        "Replicated value is written directly",
        "'{0}.{1}' is replicated - declare a mapping and use \"Pull\", or override the value.",
        "ReadyM",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Who may write a replicated value, and whether the write reports the game or "
            + "overrules it, is what its propagation decides. A setter carries neither, so on a client "
            + "it is refused everywhere the value can be reached.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(Start);
    }

    private static void Start(CompilationStartAnalysisContext context)
    {
        // A server writes what it likes: it is the one deciding, and the API saying otherwise is
        // not even compiled for it.
        if (!SdkRuntime.IsClient(context.Options.AnalyzerConfigOptionsProvider))
            return;

        var replicates = new ConcurrentDictionary<INamedTypeSymbol, bool>(SymbolEqualityComparer.Default);

        context.RegisterOperationAction(
            operation => Inspect(operation, replicates),
            OperationKind.SimpleAssignment,
            OperationKind.CompoundAssignment,
            OperationKind.CoalesceAssignment,
            OperationKind.Increment,
            OperationKind.Decrement);
    }

    private static void Inspect(
        OperationAnalysisContext context,
        ConcurrentDictionary<INamedTypeSymbol, bool> replicates)
    {
        var target = context.Operation switch
        {
            IAssignmentOperation assignment => assignment.Target,
            IIncrementOrDecrementOperation step => step.Target,
            _ => null
        };

        if (target is not IPropertyReferenceOperation { Property: { SetMethod: not null } property }
            || property.ContainingType is not { } shape
            || !replicates.GetOrAdd(shape, Replicates))
            return;

        if (target.Syntax is { } syntax
            && InsideMapping(syntax, context.Operation.SemanticModel, context.CancellationToken))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            Rule, target.Syntax.GetLocation(), shape.Name, property.Name));
    }

    /// The same answer the generator reaches, so the two cannot drift apart.
    private static bool Replicates(INamedTypeSymbol shape)
        => shape.IsValueType && DeclarationModel.For(shape).IsReplicated;

    /// Whether the write sits in a handler handed to a mapping, where it is the pull.
    private static bool InsideMapping(SyntaxNode node, SemanticModel? model, CancellationToken ct)
    {
        if (model is null)
            return false;

        for (var current = node.Parent; current is not null; current = current.Parent)
        {
            if (current is MemberDeclarationSyntax)
                return false;

            if (current is not (LambdaExpressionSyntax or AnonymousMethodExpressionSyntax))
                continue;

            if (current.Parent is ArgumentSyntax { Parent.Parent: InvocationExpressionSyntax invocation }
                && model.GetSymbolInfo(invocation, ct).Symbol is IMethodSymbol { Name: "Map" } map
                && map.ContainingType?.Name == "IShapeMappingScope")
                return true;
        }

        return false;
    }
}
