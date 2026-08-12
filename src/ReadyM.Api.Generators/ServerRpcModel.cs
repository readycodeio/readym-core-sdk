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
    /// Resolves the contract set an RPC class implements, from its required <c>[ServerRpcFor]</c>.
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

        var attribute = rpcClass.GetAttributes().FirstOrDefault(a =>
            a.AttributeClass?.Name is "ServerRpcForAttribute" or "ServerRpcFor");

        if (attribute is null || attribute.ConstructorArguments.Length == 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SRPC004", "Missing [ServerRpcFor]",
                    $"'{rpcClass.Name}' must be annotated with [ServerRpcFor(typeof(SomeContracts))] naming the "
                    + "[ServerRpcContracts] class it implements.",
                    "ServerRpc", DiagnosticSeverity.Error, true),
                location));
            return false;
        }

        if (attribute.ConstructorArguments[0].Value is not INamedTypeSymbol target)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SRPC005", "Invalid contracts type",
                    $"The type passed to [ServerRpcFor] on '{rpcClass.Name}' could not be resolved.",
                    "ServerRpc", DiagnosticSeverity.Error, true),
                location));
            return false;
        }

        if (!HasContractsAttribute(target))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SRPC005", "Invalid contracts type",
                    $"'{target.ToDisplayString()}' is not annotated with [ServerRpcContracts], so "
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
                    + "project that declares the [ServerRpcContracts] class.",
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

    public static bool HasContractsAttribute(INamedTypeSymbol type) =>
        type.GetAttributes().Any(a =>
            a.AttributeClass?.Name is "ServerRpcContractsAttribute" or "ServerRpcContracts");

    public static bool IsClientToServer(IMethodSymbol method) =>
        method.GetAttributes().Any(a =>
            a.AttributeClass?.Name is "ClientToServerAttribute" or "ClientToServer");

    public static bool IsServerToClient(IMethodSymbol method) =>
        method.GetAttributes().Any(a =>
            a.AttributeClass?.Name is "ServerToClientAttribute" or "ServerToClient");

    /// <summary>All <c>[ServerRpcContracts]</c> classes reachable under <paramref name="ns"/>.</summary>
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
                if (!cs && !sc)
                    continue;

                result.TryGetValue(method.Name, out var entry);
                if (cs)
                    entry.ClientToServer = method;
                if (sc)
                    entry.ServerToClient = method;
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
    }
}
