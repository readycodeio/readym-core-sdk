using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using ReadyM.Api.Generators.Archetypes;

namespace ReadyM.Api.Generators.Analyzers;

/// Reports a client writing a replicated value straight through its setter.
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ReplicatedWriteAnalyzer : DiagnosticAnalyzer
{
    private const string Id = "READYM017";

    private static readonly DiagnosticDescriptor Rule = new(
        Id,
        "Replicated value is written directly",
        "'{0}.{1}' is replicated - declare a mapping and use \"Pull\", or override the value",
        "ReadyM",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Who may write a replicated value, and whether the write reports the game or "
            + "overrules it, is what its propagation decides. A setter carries neither, so on a client "
            + "it is refused everywhere the value can be reached.");

    private static readonly DiagnosticDescriptor Collection = new(
        "READYM019",
        "Replicated collection is changed directly",
        "'{0}' changes {1}, which is replicated - declare a mapping for {1} and use \"Pull\", or override the value",
        "ReadyM",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A collection changes through its own members rather than by assignment, and "
            + "the rule it keeps is the one a value keeps. A handler declared for it is handed the "
            + "collection to work, which is where a client changes one.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule, Collection];

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

        context.RegisterOperationAction(Changed, OperationKind.Invocation);

        context.RegisterOperationAction(
            operation => Inspect(operation, replicates),
            OperationKind.SimpleAssignment,
            OperationKind.CompoundAssignment,
            OperationKind.CoalesceAssignment,
            OperationKind.Increment,
            OperationKind.Decrement);
    }

    private static void Changed(OperationAnalysisContext context)
    {
        if (context.Operation is not IInvocationOperation invocation
            || Changes(invocation.TargetMethod) is not { } collection
            || Authoring(context.ContainingSymbol))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            Collection, invocation.Syntax.GetLocation(), invocation.TargetMethod.Name, collection));
    }

    private static string? Changes(IMethodSymbol method)
    {
        foreach (var attribute in method.GetAttributes())
            if (attribute.AttributeClass?.Name == "ChangesAttribute"
                && attribute.ConstructorArguments.Length == 1
                && attribute.ConstructorArguments[0].Value is string collection)
                return collection;

        return null;
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
            || !replicates.GetOrAdd(shape, Replicates)
            || Authoring(context.ContainingSymbol))
            return;

        if (target.Syntax is { } syntax
            && InsideMapping(syntax, context.Operation.SemanticModel, context.CancellationToken))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            Rule, target.Syntax.GetLocation(), shape.Name, property.Name));
    }

    private static bool Replicates(INamedTypeSymbol shape)
        => shape.IsValueType && DeclarationModel.For(shape).IsReplicated;

    /// Whether the write sits in a create handler, where the shape is authoring its own entity.
    private static bool Authoring(ISymbol? containing)
    {
        for (var symbol = containing; symbol is IMethodSymbol method; symbol = symbol.ContainingSymbol)
            if (method.GetAttributes().Any(attribute
                    => attribute.AttributeClass?.Name == "CreateHandlerAttribute"))
                return true;

        return false;
    }

    /// Whether the write sits in a handler handed to a mapping.
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
