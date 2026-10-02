using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators;

/// <summary>
/// Shared helpers for the three server-RPC generators: reads a contract method's direction
/// attributes and pairs the two directions of one RPC (keyed by method name). One name = one wire
/// code (each side only receives one direction), but request and response payloads may differ.
/// </summary>
internal static class ServerRpcModel
{
    public const string ManifestClassName = "ServerRpcManifest";

    /// <summary>
    /// Resolves the contract set an RPC class implements, from its required <c>[RpcHandlersFor]</c>.
    /// Reports a diagnostic and returns false when the attribute is missing, names a type that is
    /// not a contracts class, or names one whose assembly has no manifest.
    /// </summary>
    public static bool TryResolveContracts(
        SourceProductionContext context,
        INamedTypeSymbol rpcClass,
        out INamedTypeSymbol contractsType,
        out INamedTypeSymbol manifest)
    {
        contractsType = null!;
        manifest = null!;

        var location = rpcClass.Locations.FirstOrDefault();

        var attribute = HandlersFor(rpcClass);

        if (attribute is null || attribute.ConstructorArguments.Length == 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SRPC004", "Missing [RpcHandlersFor]",
                    $"'{rpcClass.Name}' must be annotated with [RpcHandlersFor(typeof(SomeContracts))] naming the "
                    + "[RpcContracts] class it implements.",
                    "ServerRpc", DiagnosticSeverity.Error, true),
                location));
            return false;
        }

        if (attribute.ConstructorArguments[0].Value is not INamedTypeSymbol target)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SRPC005", "Invalid contracts type",
                    $"The type passed to [RpcHandlersFor] on '{rpcClass.Name}' could not be resolved.",
                    "ServerRpc", DiagnosticSeverity.Error, true),
                location));
            return false;
        }

        if (!HasContractsAttribute(target))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SRPC005", "Invalid contracts type",
                    $"'{target.ToDisplayString()}' is not annotated with [RpcContracts], so "
                    + $"'{rpcClass.Name}' cannot implement it.",
                    "ServerRpc", DiagnosticSeverity.Error, true),
                location));
            return false;
        }

        var found = FindTypeNamed(target.ContainingAssembly.GlobalNamespace, ManifestClassName);
        if (found is null)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SRPC003", "Missing manifest",
                    $"No {ManifestClassName} was found in '{target.ContainingAssembly.Name}'. Reference the "
                    + "project that declares the [RpcContracts] class.",
                    "ServerRpc", DiagnosticSeverity.Error, true),
                location));
            return false;
        }

        contractsType = target;
        manifest = found;
        return true;
    }

    /// <summary>The RPC names of a manifest, in its own (authoritative) code order.</summary>
    public static List<string> ManifestNames(INamedTypeSymbol manifest) =>
        manifest.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(f => f.IsStatic && f.Name.EndsWith("Code"))
            .Select(f => f.Name.Substring(0, f.Name.Length - 4))
            .ToList();

    public static INamedTypeSymbol? FindTypeNamed(INamespaceSymbol ns, string name)
    {
        var direct = ns.GetTypeMembers(name).FirstOrDefault();
        if (direct != null) return direct;

        foreach (var child in ns.GetNamespaceMembers())
        {
            var found = FindTypeNamed(child, name);
            if (found != null) return found;
        }

        return null;
    }

    /// <summary>Whether a class names the contract set it implements, which is what makes it one.</summary>
    public static bool HasServerRpcFor(INamedTypeSymbol type) => HandlersFor(type) is not null;

    /// <summary>
    /// Whether a generator will complete this class, which is what says a registration exists to
    /// collect. The same conditions TryResolveContracts reports on, without reporting anything.
    /// </summary>
    public static bool Implements(INamedTypeSymbol rpcClass)
    {
        if (rpcClass.IsAbstract || !IsPartial(rpcClass) || HandlersFor(rpcClass) is not { } attribute)
            return false;

        if (attribute.ConstructorArguments.Length == 0
            || attribute.ConstructorArguments[0].Value is not INamedTypeSymbol target
            || !HasContractsAttribute(target))
            return false;

        return FindTypeNamed(target.ContainingAssembly.GlobalNamespace, ManifestClassName) is not null;
    }

    /// Only half of it is the mod's, so a class that is not partial is not one of these.
    private static bool IsPartial(INamedTypeSymbol type) =>
        type.DeclaringSyntaxReferences
            .Select(reference => reference.GetSyntax())
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
            .Any(declaration => declaration.Modifiers.Any(modifier => modifier.ValueText == "partial"));

    /// <summary>
    /// The attribute binding a class to its contract set. [ServerRpcFor] is the 0.x spelling of
    /// [RpcHandlersFor] and is read the same way, so a mod can move over a class at a time.
    /// </summary>
    public static AttributeData? HandlersFor(INamedTypeSymbol type) =>
        type.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name
            is "RpcHandlersForAttribute" or "RpcHandlersFor"
            or "ServerRpcForAttribute" or "ServerRpcFor");

    /// <summary>[ServerRpcContracts] is the 0.x spelling of [RpcContracts].</summary>
    public static bool HasContractsAttribute(INamedTypeSymbol type) =>
        type.GetAttributes().Any(a => a.AttributeClass?.Name
            is "RpcContractsAttribute" or "RpcContracts"
            or "ServerRpcContractsAttribute" or "ServerRpcContracts");

    public static bool IsClientToServer(IMethodSymbol method) =>
        method.GetAttributes().Any(a =>
            a.AttributeClass?.Name is "ClientToServerAttribute" or "ClientToServer");

    public static bool IsServerToClient(IMethodSymbol method) =>
        method.GetAttributes().Any(a =>
            a.AttributeClass?.Name is "ServerToClientAttribute" or "ServerToClient");

    public static bool IsClientToClients(IMethodSymbol method) => ClientToClients(method) is not null;

    /// <summary>The <c>[ClientToClients]</c> on a method, which carries the relay mode, or null.</summary>
    public static AttributeData? ClientToClients(IMethodSymbol method) =>
        method.GetAttributes().FirstOrDefault(a =>
            a.AttributeClass?.Name is "ClientToClientsAttribute" or "ClientToClients");

    /// <summary>
    /// The RelayMode a <c>[ClientToClients]</c> names, as its underlying byte. Zero
    /// (AreaOfInterestOthers) when the attribute is there but its argument could not be read.
    /// </summary>
    public static byte RelayModeOf(IMethodSymbol method)
    {
        var attribute = ClientToClients(method);

        if (attribute is null || attribute.ConstructorArguments.Length == 0)
            return 0;

        return attribute.ConstructorArguments[0].Value is { } value
            ? System.Convert.ToByte(value)
            : (byte)0;
    }

    /// <summary>
    /// Whether a name travels between clients rather than through the server, which is what decides
    /// the code space it is assigned out of. The two cannot be mixed under one name.
    /// </summary>
    public static bool IsServerDirected(IMethodSymbol method) =>
        IsClientToServer(method) || IsServerToClient(method);

    /// <summary>All <c>[RpcContracts]</c> classes reachable under <paramref name="ns"/>.</summary>
    public static List<INamedTypeSymbol> CollectContractClasses(INamespaceSymbol ns)
    {
        var acc = new List<INamedTypeSymbol>();
        Recurse(ns, acc);
        return acc;

        static void Recurse(INamespaceSymbol current, List<INamedTypeSymbol> acc)
        {
            foreach (var type in current.GetTypeMembers())
            {
                if (HasContractsAttribute(type))
                    acc.Add(type);
            }

            foreach (var child in current.GetNamespaceMembers())
                Recurse(child, acc);
        }
    }

    /// <summary>
    /// Per RPC name, the request (c-&gt;s) and response (s-&gt;c) method symbols; either may be null
    /// (one-way). A method with both attributes fills both slots.
    /// </summary>
    public static Dictionary<string, DirectionalRpc> ResolveDirections(
        IEnumerable<INamedTypeSymbol> contractClasses)
    {
        var result = new Dictionary<string, DirectionalRpc>();

        foreach (var contractClass in contractClasses)
        {
            foreach (var method in contractClass.GetMembers().OfType<IMethodSymbol>())
            {
                var cs = IsClientToServer(method);
                var sc = IsServerToClient(method);
                var cc = IsClientToClients(method);
                if (!cs && !sc && !cc)
                    continue;

                result.TryGetValue(method.Name, out var entry);
                if (cs)
                    entry.ClientToServer = method;
                if (sc)
                    entry.ServerToClient = method;
                if (cc)
                    entry.ClientToClients = method;
                result[method.Name] = entry;
            }
        }

        return result;
    }

    public struct DirectionalRpc
    {
        /// <summary>Request (c-&gt;s) method, or null.</summary>
        public IMethodSymbol? ClientToServer;

        /// <summary>Response/push (s-&gt;c) method, or null.</summary>
        public IMethodSymbol? ServerToClient;

        /// <summary>Peer-to-peer (c-&gt;cs) method, or null. Never set alongside the other two.</summary>
        public IMethodSymbol? ClientToClients;
    }
}
