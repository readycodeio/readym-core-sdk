using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReadyM.Api.Generators;

/// <summary>
/// Runs on the shared (Common) project. Finds classes marked [ServerRpcContracts],
/// reads their partial void method stubs, and emits two files:
///   1. Partial implementations (required by C# partial method rules).
///   2. ServerRpcManifest - the single source of truth for code assignment.
///
/// Each contract method must be marked [ClientToServer] and/or [ServerToClient] to declare
/// which direction(s) it carries. Two overloads sharing a name (one per direction) form an
/// asymmetric two-way RPC; a single method carrying both attributes is a symmetric two-way
/// RPC; a single directional method is a one-way RPC. All overloads of a name collapse to ONE
/// entry in the manifest (one wire code), because each side only ever receives one direction.
///
/// Both the server mod and the client mod reference the compiled Common assembly, so they
/// share this manifest without independently computing anything.
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

        // One manifest entry per unique RPC name, ordered alphabetically for a stable
        // (client- and server-agree) code assignment.
        var names = allMethods
            .Select(x => x.Method.Name)
            .Distinct()
            .OrderBy(n => n)
            .ToList();

        // Use the namespace of the first contracts class as the manifest namespace.
        // All [ServerRpcContracts] classes in a mod should share the same root namespace.
        var manifestNs = classes[0].Symbol.ContainingNamespace.ToDisplayString();

        // Partial implementations (required by C# for public/protected partial methods).
        foreach (var cls in classes)
            EmitPartialImplementations(context, cls);

        // The manifest: single source of truth for TotalEventCount, Offset, and {Name}Code.
        EmitManifest(context, manifestNs, names);
    }

    /// <summary>
    /// Every contract method must declare at least one direction, and each direction of a given
    /// RPC name may be declared at most once. Violations are hard errors: an undirected method
    /// or a duplicated direction cannot be turned into correct generated code.
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

        // Break the build loudly (and locally to the offending file) for any undirected method,
        // in addition to the SRPC001 diagnostic above.
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
        List<string> names)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("using ReadyM.Api.Multiplayer.Protocol;");
        sb.AppendLine("using ReadyM.Api.Multiplayer.Protocol.Enums;");
        sb.AppendLine();
        sb.AppendLine($"namespace {ns};");
        sb.AppendLine();
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
