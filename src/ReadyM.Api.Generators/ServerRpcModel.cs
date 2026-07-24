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
