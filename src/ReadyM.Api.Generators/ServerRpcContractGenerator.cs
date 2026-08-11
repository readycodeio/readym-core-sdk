using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReadyM.Api.Generators;

/// <summary>
/// Runs on the Common project. For classes marked [ServerRpcContracts], emits partial method
/// implementations and the ServerRpcManifest (the shared code assignment referenced by both the
/// server handler and client event generators). Overloads of a name collapse to one manifest entry
/// (one wire code). See <see cref="ClientToServerAttribute"/> for the direction rules.
/// </summary>
[Generator]
internal class ServerRpcContractGenerator : IIncrementalGenerator
{
    private const string ManifestClassName = "ServerRpcManifest";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var contractClasses = context.SyntaxProvider
            .CreateSyntaxProvider(Predicate, Transform)
            .Where(x => x is not null)
            .Collect();

        context.RegisterSourceOutput(contractClasses, GenerateSources);
    }

    private static bool Predicate(SyntaxNode node, CancellationToken _) =>
        node is ClassDeclarationSyntax cls && cls.AttributeLists.Count > 0;

    private static ContractClassInfo? Transform(GeneratorSyntaxContext context, CancellationToken _)
    {
        if (context.Node is not ClassDeclarationSyntax classSyntax)
            return null;

        if (context.SemanticModel.GetDeclaredSymbol(classSyntax) is not INamedTypeSymbol classSymbol)
            return null;

        if (!ServerRpcModel.HasContractsAttribute(classSymbol))
            return null;

        // Collect the partial void method stubs. Their names are the RPC event names.
        var methods = classSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.IsPartialDefinition && m.ReturnsVoid && m.IsStatic)
            .OrderBy(m => m.Name)
            .ToList();

        return new ContractClassInfo(classSymbol, methods);
    }

    private static void GenerateSources(
        SourceProductionContext context,
        ImmutableArray<ContractClassInfo?> rawClasses)
    {
        var classes = rawClasses.Where(c => c is not null).Select(c => c!).ToList();
        if (classes.Count == 0)
            return;

        var allMethods = classes
            .SelectMany(c => c.Methods.Select(m => (Class: c, Method: m)))
            .ToList();

        ValidateDirections(context, allMethods);

        // One entry per unique name, sorted for a stable client/server code assignment.
        var names = allMethods
            .Select(x => x.Method.Name)
            .Distinct()
            .OrderBy(n => n)
            .ToList();

        // Manifest goes in the first contracts class's namespace (all should share a root namespace).
        var manifestNs = classes[0].Symbol.ContainingNamespace.ToDisplayString();

        // One manifest per assembly, so the assembly name is its stable identity. Offsets are
        // assigned in Id order at runtime, which keeps them independent of mod load order.
        var manifestId = classes[0].Symbol.ContainingAssembly.Name;

        foreach (var cls in classes)
            EmitPartialImplementations(context, cls);

        EmitManifest(context, manifestNs, manifestId, names);
    }

    /// <summary>
    /// Every method must declare at least one direction, and each direction of a name at most once.
    /// Violations are hard errors.
    /// </summary>
    private static void ValidateDirections(
        SourceProductionContext context,
        List<(ContractClassInfo Class, IMethodSymbol Method)> allMethods)
    {
        foreach (var (_, method) in allMethods)
        {
            if (!ServerRpcModel.IsClientToServer(method) && !ServerRpcModel.IsServerToClient(method))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "SRPC001", "Missing RPC direction",
                        "Server RPC contract method '{0}' must be marked [ClientToServer] and/or [ServerToClient].",
                        "ServerRpc", DiagnosticSeverity.Error, true),
                    method.Locations.FirstOrDefault(),
                    method.Name));
            }
        }

        foreach (var group in allMethods.GroupBy(x => x.Method.Name))
        {
            ReportIfDuplicateDirection(context, group, ServerRpcModel.IsClientToServer, "[ClientToServer]");
            ReportIfDuplicateDirection(context, group, ServerRpcModel.IsServerToClient, "[ServerToClient]");
        }
    }

    private static void ReportIfDuplicateDirection(
        SourceProductionContext context,
        IEnumerable<(ContractClassInfo Class, IMethodSymbol Method)> group,
        System.Func<IMethodSymbol, bool> hasDirection,
        string directionLabel)
    {
        var matches = group.Where(x => hasDirection(x.Method)).ToList();
        if (matches.Count <= 1)
            return;

        foreach (var (_, method) in matches)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SRPC002", "Duplicate RPC direction",
                    "RPC name '{0}' declares the {1} direction more than once.",
                    "ServerRpc", DiagnosticSeverity.Error, true),
                method.Locations.FirstOrDefault(),
                method.Name, directionLabel));
        }
    }

    private static void EmitPartialImplementations(
        SourceProductionContext context,
        ContractClassInfo cls)
    {
        var ns = cls.Symbol.ContainingNamespace.ToDisplayString();
        var className = cls.Symbol.Name;
        var access = cls.Symbol.DeclaredAccessibility.ToString().ToLower();

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");

        // #error in the offending file for any undirected method (alongside the SRPC001 diagnostic).
        foreach (var method in cls.Methods)
        {
            if (!ServerRpcModel.IsClientToServer(method) && !ServerRpcModel.IsServerToClient(method))
            {
                sb.AppendLine(
                    $"#error Server RPC contract method '{method.Name}' must be marked [ClientToServer] and/or [ServerToClient].");
            }
        }

        sb.AppendLine($"namespace {ns};");
        sb.AppendLine();
        sb.AppendLine($"{access} static partial class {className}");
        sb.AppendLine("{");

        foreach (var method in cls.Methods)
        {
            var paramList = string.Join(", ", method.Parameters.Select(
                p => $"{p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {p.Name}"));
            sb.AppendLine($"    public static partial void {method.Name}({paramList}) {{ }}");
        }

        sb.AppendLine("}");

        context.AddSource(
            $"{cls.Symbol.ToDisplayString().Replace('.', '_')}_Contracts.g.cs",
            sb.ToString());
    }

    private static void EmitManifest(
        SourceProductionContext context,
        string ns,
        string manifestId,
        List<string> names)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("using ReadyM.Api.Multiplayer.Protocol;");
        sb.AppendLine("using ReadyM.Api.Multiplayer.Protocol.Enums;");
        sb.AppendLine();
        sb.AppendLine($"namespace {ns};");
        sb.AppendLine();
        sb.AppendLine("/// <exclude/>");
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Single source of truth for server RPC code assignment in this mod.");
        sb.AppendLine("/// Referenced by both the server handler and client event generators.");
        sb.AppendLine("/// Set <see cref=\"Offset\"/> once at mod startup before any InitRpc() runs:");
        sb.AppendLine("/// <code>");
        sb.AppendLine($"///   {ManifestClassName}.Offset = offsetProvider.GetNextOffset({ManifestClassName}.TotalEventCount);");
        sb.AppendLine("/// </code>");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public static class {ManifestClassName}");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Stable identity of this contract set (the declaring assembly's name). Offsets are");
        sb.AppendLine("    /// assigned in Id order, so every process agrees regardless of mod load order.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine($"    public const string Id = \"{manifestId}\";");
        sb.AppendLine();
        sb.AppendLine($"    public const byte TotalEventCount = {names.Count};");
        sb.AppendLine();
        sb.AppendLine("    public static byte Offset { get; set; }");
        sb.AppendLine();

        for (var i = 0; i < names.Count; i++)
        {
            var name = names[i];
            sb.AppendLine($"    public const RelayMessageCode {name}Code =");
            sb.AppendLine($"        (RelayMessageCode)(RelayMessageCode.MinServerRpcEvent + {i});");
            sb.AppendLine();
        }

        sb.AppendLine("}");

        context.AddSource($"{ManifestClassName}.g.cs", sb.ToString());
    }

    private sealed class ContractClassInfo(INamedTypeSymbol symbol, List<IMethodSymbol> methods)
    {
        public INamedTypeSymbol Symbol { get; } = symbol;
        public List<IMethodSymbol> Methods { get; } = methods;
    }
}
