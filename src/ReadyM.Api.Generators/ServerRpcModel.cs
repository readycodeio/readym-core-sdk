using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators;

/// <summary>
/// Shared helpers for the three server-RPC generators (contract manifest, server handlers,
/// client events). Encapsulates how a <c>[ServerRpcContracts]</c> method's DIRECTION is read
/// from its <c>[ClientToServer]</c> / <c>[ServerToClient]</c> attributes, and how the two
/// directions of one logical RPC (identified by method NAME) are paired up.
///
/// A single RPC name maps to a single wire code used in both directions: a client only ever
/// receives the server-to-client shape and a server only ever receives the client-to-server
/// shape, so one code is unambiguous. The request (client-to-server) and response
/// (server-to-client) payloads may nonetheless differ, which is the whole point of the split.
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
    /// Resolves, per RPC name, the client-to-server (request) and server-to-client (response)
    /// contract method symbols. Either may be null for a one-way RPC. A method carrying both
    /// direction attributes fills both slots (symmetric two-way of identical shape).
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

    /// <summary>Per-name pairing of the two directional contract methods.</summary>
    public struct DirectionalRpc
    {
        /// <summary>Request shape (client to server). Null when the RPC has no client-to-server leg.</summary>
        public IMethodSymbol? ClientToServer;

        /// <summary>Response/push shape (server to client). Null when the RPC has no server-to-client leg.</summary>
        public IMethodSymbol? ServerToClient;
    }
}
