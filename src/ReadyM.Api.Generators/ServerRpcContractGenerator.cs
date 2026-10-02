using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReadyM.Api.Generators;

/// <summary>
/// Runs on the Common project. For classes marked [RpcContracts], emits partial method
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

        // Two code spaces: what goes through the server, and what clients send each other. One
        // entry per unique name in each, sorted so every process assigns the same codes.
        var serverNames = Names(allMethods, ServerRpcModel.IsServerDirected);
        var clientNames = Names(allMethods, ServerRpcModel.IsClientToClients);

        // Manifest goes in the first contracts class's namespace (all should share a root namespace).
        var manifestNs = classes[0].Symbol.ContainingNamespace.ToDisplayString();

        // One manifest per assembly, so the assembly name is its stable identity. Offsets are
        // assigned in Id order at runtime, which keeps them independent of mod load order.
        var manifestId = classes[0].Symbol.ContainingAssembly.Name;

        foreach (var cls in classes)
            EmitPartialImplementations(context, cls);

        EmitManifest(context, manifestNs, manifestId, serverNames, clientNames);
    }

    private static List<string> Names(
        List<(ContractClassInfo Class, IMethodSymbol Method)> allMethods,
        System.Func<IMethodSymbol, bool> inSpace) =>
        allMethods
            .Where(x => inSpace(x.Method))
            .Select(x => x.Method.Name)
            .Distinct()
            .OrderBy(n => n, System.StringComparer.Ordinal)
            .ToList();

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
            if (!ServerRpcModel.IsServerDirected(method) && !ServerRpcModel.IsClientToClients(method))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "SRPC001", "Missing RPC direction",
                        "RPC contract method '{0}' must be marked [ClientToServer], [ServerToClient] "
                        + "and/or [ClientToClients].",
                        "ServerRpc", DiagnosticSeverity.Error, true),
                    method.Locations.FirstOrDefault(),
                    method.Name));

                continue;
            }

            if (ServerRpcModel.IsServerDirected(method) && ServerRpcModel.IsClientToClients(method))
                ReportMixedSpace(context, method);

            if (ServerRpcModel.IsClientToClients(method))
                ValidateRelayMode(context, method);
        }

        foreach (var group in allMethods.GroupBy(x => x.Method.Name))
        {
            ReportIfDuplicateDirection(context, group, ServerRpcModel.IsClientToServer, "[ClientToServer]");
            ReportIfDuplicateDirection(context, group, ServerRpcModel.IsServerToClient, "[ServerToClient]");
            ReportIfDuplicateDirection(context, group, ServerRpcModel.IsClientToClients, "[ClientToClients]");

            // Overloads of a name share one wire code, and the two spaces number theirs separately,
            // so a name has to sit in one of them.
            if (group.Any(x => ServerRpcModel.IsServerDirected(x.Method))
                && group.Any(x => ServerRpcModel.IsClientToClients(x.Method)))
                foreach (var (_, method) in group)
                    ReportMixedSpace(context, method);
        }
    }

    /// <summary>A name is either relayed between clients or routed through the server, never both.</summary>
    private static void ReportMixedSpace(SourceProductionContext context, IMethodSymbol method)
        => context.ReportDiagnostic(Diagnostic.Create(
            new DiagnosticDescriptor(
                "SRPC006", "Mixed RPC directions",
                "RPC name '{0}' mixes [ClientToClients] with [ClientToServer] or [ServerToClient]. "
                + "A peer-to-peer RPC is assigned a client event code and cannot share a name with one "
                + "the server routes, so give it a name of its own.",
                "ServerRpc", DiagnosticSeverity.Error, true),
            method.Locations.FirstOrDefault(),
            method.Name));

    /// <summary>The relay modes a mod may ask for, which is not all of them.</summary>
    private static void ValidateRelayMode(SourceProductionContext context, IMethodSymbol method)
    {
        var mode = ServerRpcModel.RelayModeOf(method);

        // 4 is EntityOwner and 5 is Peers, both carried by the enum but not offered to mods.
        if (mode is not (4 or 5))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            new DiagnosticDescriptor(
                "SRPC007", "Relay mode is not part of the public API",
                "'{0}' asks for a relay mode that is not part of the public API. Use one of "
                + "AreaOfInterestOthers, AreaOfInterestAll, GlobalOthers or GlobalAll.",
                "ServerRpc", DiagnosticSeverity.Error, true),
            method.Locations.FirstOrDefault(),
            method.Name));
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
            if (!ServerRpcModel.IsServerDirected(method) && !ServerRpcModel.IsClientToClients(method))
            {
                sb.AppendLine(
                    $"#error RPC contract method '{method.Name}' must be marked [ClientToServer], [ServerToClient] and/or [ClientToClients].");
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
        List<string> serverNames,
        List<string> clientNames)
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
        sb.AppendLine($"///   {ManifestClassName}.Offset = serverOffsets.GetNextOffset({ManifestClassName}.TotalEventCount);");
        sb.AppendLine($"///   {ManifestClassName}.ClientOffset = clientOffsets.GetNextOffset({ManifestClassName}.TotalClientEventCount);");
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
        sb.AppendLine("    /// <summary>How many codes this set takes out of the server RPC space.</summary>");
        sb.AppendLine($"    public const byte TotalEventCount = {serverNames.Count};");
        sb.AppendLine();
        sb.AppendLine("    public static byte Offset { get; set; }");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// How many codes this set takes out of the client RPC space, which is where the");
        sb.AppendLine("    /// peer-to-peer RPCs live. A separate space, so a separate offset.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine($"    public const byte TotalClientEventCount = {clientNames.Count};");
        sb.AppendLine();
        sb.AppendLine("    public static byte ClientOffset { get; set; }");
        sb.AppendLine();

        for (var i = 0; i < serverNames.Count; i++)
        {
            sb.AppendLine($"    public const RelayMessageCode {serverNames[i]}Code =");
            sb.AppendLine($"        (RelayMessageCode)(RelayMessageCode.MinServerRpcEvent + {i});");
            sb.AppendLine();
        }

        for (var i = 0; i < clientNames.Count; i++)
        {
            sb.AppendLine($"    public const RelayMessageCode {clientNames[i]}Code =");
            sb.AppendLine($"        (RelayMessageCode)(RelayMessageCode.MinClientRpcEvent + {i});");
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
