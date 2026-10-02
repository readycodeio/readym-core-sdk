using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReadyM.Api.Generators;

/// <summary>
/// Mirror of the server handler generator, on a client mod project. Per RPC name, for each
/// <c>[RpcHandlersFor]</c> class emits the declared legs: request (c-&gt;s) as a Send(...), response
/// (s-&gt;c) as an On(...) handler + dispatch, and peer-to-peer (c-&gt;cs) as both. A one-way RPC only
/// produces its declared leg.
/// </summary>
[Generator]
internal class ServerRpcEventGenerator : IIncrementalGenerator
{
    private const string LegacyBaseClassName = "ServerRpcClient";
    private const string ServerSideBaseClassName = "ServerRpcHandlersBase";
    private const string ManifestClassName = "ServerRpcManifest";
    private const string RpcBaseFqn = "global::ReadyM.Api.Multiplayer.RPC.RpcBase";
    private const string PlayerIdFqn = "global::ReadyM.Api.Idents.PlayerId";
    private const string SenderParameterName = "sender";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var eventClasses = context.SyntaxProvider
            .CreateSyntaxProvider(Predicate, Transform)
            .Where(x => x.Symbol is not null)
            .Collect();

        context.RegisterSourceOutput(eventClasses, static (ctx, classes) => GenerateSources(ctx, classes));
    }

    private static bool Predicate(SyntaxNode node, CancellationToken _) =>
        node is ClassDeclarationSyntax cls &&
        cls.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));

    /// The class, plus what its target lets the registration carry. A pair rather than a symbol,
    /// so the pipeline is never handed a whole compilation to compare.
    private static (INamedTypeSymbol? Symbol, bool ModuleInitializer) Transform(
        GeneratorSyntaxContext context, CancellationToken _)
    {
        (INamedTypeSymbol? Symbol, bool ModuleInitializer) none = (null, false);

        if (context.Node is not ClassDeclarationSyntax)
            return none;

        var classSymbol = context.SemanticModel.GetDeclaredSymbol(context.Node) as INamedTypeSymbol;
        if (classSymbol is null || classSymbol.IsAbstract)
            return none;

        var found = (
            Symbol: (INamedTypeSymbol?)classSymbol,
            ModuleInitializer: Archetypes.ExtendsEmitter.HasModuleInitializer(context.SemanticModel.Compilation));

        // A class that still writes the 0.x base is this side's wherever it is compiled, and it is
        // picked up without the attribute so that it keeps its diagnostic.
        if (DerivesFrom(classSymbol, LegacyBaseClassName))
            return found;

        if (DerivesFrom(classSymbol, ServerSideBaseClassName))
            return none;

        // Otherwise the attribute is what makes a class an RPC client, since the base is written
        // here. Both sides name their contracts the same way, so the compilation decides which side
        // a class that says nothing else is on: only a server mod can reach the server SDK.
        return ServerRpcModel.HasServerRpcFor(classSymbol)
               && !ServerRpcHandlerGenerator.IsServerSide(context.SemanticModel.Compilation)
            ? found
            : none;
    }


    private static bool DerivesFrom(INamedTypeSymbol symbol, string baseName)
    {
        var current = symbol.BaseType;
        while (current is not null)
        {
            if (current.Name == baseName) return true;
            current = current.BaseType;
        }

        return false;
    }

    /// <summary>
    /// Whether the mod wrote a base of its own. When it did not, the generated half supplies one,
    /// which is what lets a class name only the contracts it implements.
    /// </summary>
    private static bool HasOwnBase(INamedTypeSymbol symbol) =>
        symbol.BaseType is not null && symbol.BaseType.SpecialType != SpecialType.System_Object;

    private static void GenerateSources(
        SourceProductionContext context,
        ImmutableArray<(INamedTypeSymbol? Symbol, bool ModuleInitializer)> rawClasses)
    {
        // Each class names its own contract set, so a mod can host clients for several of them.
        foreach (var (symbol, moduleInitializer) in rawClasses.Where(c => c.Symbol is not null))
        {
            var classSymbol = symbol!;

            if (!ServerRpcModel.TryResolveContracts(context, classSymbol, out var contractsType, out var manifest))
                continue;

            var manifestFqn = $"global::{manifest.ContainingNamespace.ToDisplayString()}.{ManifestClassName}";

            // Class-scoped: only the legs the named contracts class declares, even when the manifest
            // covers several contract classes from the same assembly.
            var directions = ServerRpcModel.ResolveDirections(new[] { contractsType });

            var rpcs = ServerRpcModel.ManifestNames(manifest)
                .Where(directions.ContainsKey)
                .Select(name =>
                {
                    directions.TryGetValue(name, out var dir);
                    return new Rpc(name, dir.ClientToServer, dir.ServerToClient, dir.ClientToClients);
                })
                .ToList();

            GenerateEventClass(context, classSymbol, rpcs, manifestFqn, moduleInitializer);
        }
    }

    private static void GenerateEventClass(
        SourceProductionContext context,
        INamedTypeSymbol classSymbol,
        List<Rpc> rpcs,
        string manifestFqn,
        bool moduleInitializer)
    {
        var ns = classSymbol.ContainingNamespace.ToDisplayString();
        var className = classSymbol.Name;
        var fullClassName = classSymbol.ToDisplayString();
        var access = classSymbol.DeclaredAccessibility.ToString().ToLower();
        var baseList = HasOwnBase(classSymbol) ? string.Empty : $" : {RpcBaseFqn}";

        var sb = new StringBuilder($$"""
                                     // <auto-generated/>
                                     using System;
                                     using LiteNetLib;
                                     using LiteNetLib.Utils;
                                     using ReadyM.Api.Multiplayer;
                                     using ReadyM.Api.Multiplayer.Protocol;
                                     using ReadyM.Api.Multiplayer.Protocol.Enums;
                                     using ReadyM.Api.Multiplayer.Client;
                                     using ReadyM.Api.Multiplayer.Extensions;

                                     namespace {{ns}};

                                     {{access}} partial class {{className}}{{baseList}}
                                     {

                                     """);

        var fromServer = new StringBuilder();
        var fromClients = new StringBuilder();
        var initCalls = new StringBuilder();
        var deinitCalls = new StringBuilder();
        var serverOffset = $"{manifestFqn}.Offset";
        var clientOffset = $"{manifestFqn}.ClientOffset";

        foreach (var rpc in rpcs)
        {
            var codeRef = $"{manifestFqn}.{rpc.Name}Code";

            // c->cs: the client both sends and receives, and the relay decides who else hears it.
            if (rpc.PeerToPeer is not null)
            {
                var peerParams = BuildPayloadParams(rpc.PeerToPeer);

                EmitRelaySender(
                    sb, rpc.Name, codeRef, clientOffset,
                    ServerRpcModel.RelayModeOf(rpc.PeerToPeer), peerParams);

                EmitReceiveHandlerStub(sb, rpc.Name, peerParams, withSender: true);
                EmitDispatchBranch(fromClients, rpc.Name, codeRef, peerParams, withSender: true);

                Register(initCalls, deinitCalls, codeRef, clientOffset, "Client", "OnClientEvent");

                continue;
            }

            // c->s: emit the sender.
            if (rpc.Request is not null)
                EmitServerSender(sb, rpc.Name, codeRef, serverOffset, BuildPayloadParams(rpc.Request));

            // s->c: client receives, so emit the handler stub, dispatch branch and (de)registration.
            if (rpc.Response is not null)
            {
                var responseParams = BuildPayloadParams(rpc.Response);

                EmitReceiveHandlerStub(sb, rpc.Name, responseParams, withSender: false);
                EmitDispatchBranch(fromServer, rpc.Name, codeRef, responseParams, withSender: false);

                Register(initCalls, deinitCalls, codeRef, serverOffset, "Server", "OnServerEvent");
            }
        }

        sb.AppendLine($$"""
                            protected void OnServerEvent(ServerEventHeader header, NetDataReader reader)
                            {
                                switch ((RelayMessageCode)(header.EventCode - {{serverOffset}}))
                                {
                                {{fromServer}}
                                    default:
                                        break;
                                }
                            }
                        """);

        if (fromClients.Length > 0)
        {
            sb.AppendLine($$"""

                                protected void OnClientEvent(CustomRelayEventHeader header, NetDataReader reader)
                                {
                                    switch ((RelayMessageCode)(header.EventCode - {{clientOffset}}))
                                    {
                                    {{fromClients}}
                                        default:
                                            break;
                                    }
                                }
                            """);
        }

        sb.AppendLine($$"""

                            protected override void InitRpc()
                            {
                        {{initCalls}}
                            }

                            protected override void DeInitRpc()
                            {
                        {{deinitCalls}}
                            }
                        """);

        RpcRegistrationEmitter.Emit(sb, classSymbol, moduleInitializer, "Client");

        sb.AppendLine("}");

        context.AddSource(
            $"{fullClassName.Replace('.', '_')}_RpcEvents.g.cs",
            sb.ToString());
    }

    private static void Register(
        StringBuilder initCalls,
        StringBuilder deinitCalls,
        string codeRef,
        string offsetRef,
        string space,
        string handler)
    {
        if (initCalls.Length > 0) initCalls.AppendLine();
        initCalls.Append($"        RelayClient.Add{space}RpcMessageHandler({codeRef} + {offsetRef}, {handler});");

        if (deinitCalls.Length > 0) deinitCalls.AppendLine();
        deinitCalls.Append($"        RelayClient.Remove{space}RpcMessageHandler({codeRef} + {offsetRef}, {handler});");
    }

    private static void EmitServerSender(
        StringBuilder sb, string eventName, string codeRef, string offsetRef, List<PayloadParam> payloadParams)
        => EmitSender(
            sb, eventName, payloadParams,
            $"RelayMessage.ToServer({codeRef} + {offsetRef}, DeliveryMethod.ReliableOrdered)",
            guardOnPlayerId: false);

    /// <summary>
    /// The peer-to-peer sender. The message carries the local player as its sender, so there is
    /// nothing to send before the relay has given this client an id.
    /// </summary>
    private static void EmitRelaySender(
        StringBuilder sb,
        string eventName,
        string codeRef,
        string offsetRef,
        byte relayMode,
        List<PayloadParam> payloadParams)
        => EmitSender(
            sb, eventName, payloadParams,
            $"RelayMessage.ByRelayMode({codeRef} + {offsetRef}, RelayClient.PlayerId.Value, "
            + $"(RelayMode){relayMode}, DeliveryMethod.ReliableOrdered)",
            guardOnPlayerId: true);

    /// <summary>
    /// The locals carry a prefix nobody would write, since every other name in here comes from the
    /// contract and a payload called "writer" would otherwise shadow the one being written to.
    /// </summary>
    private static void EmitSender(
        StringBuilder sb,
        string eventName,
        List<PayloadParam> payloadParams,
        string buildMessage,
        bool guardOnPlayerId)
    {
        sb.AppendLine($"    public void Send{eventName}({FormatParamList(payloadParams)})");
        sb.AppendLine("    {");

        if (guardOnPlayerId)
        {
            sb.AppendLine("        if (!RelayClient.PlayerId.HasValue)");
            sb.AppendLine("            return;");
            sb.AppendLine();
        }

        sb.AppendLine($"        var __rpc = {buildMessage};");
        sb.AppendLine("        var __writer = __rpc.Writer;");

        foreach (var p in payloadParams)
            sb.AppendLine(SerializeStatement(p));

        sb.AppendLine("        RelayClient.SendMessage(__rpc);");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitReceiveHandlerStub(
        StringBuilder sb, string eventName, List<PayloadParam> payloadParams, bool withSender)
    {
        var payloadParamList = FormatParamList(payloadParams);

        // A server RPC needs no sender: it is always the server. A peer-to-peer one is handed the
        // player it came from, which is the one thing the receiver cannot work out for itself.
        var stubParams = withSender
            ? payloadParams.Count > 0
                ? $"{PlayerIdFqn} {SenderParameterName}, {payloadParamList}"
                : $"{PlayerIdFqn} {SenderParameterName}"
            : payloadParamList;

        // Unimplemented stubs drop, so a class may implement any subset.
        sb.AppendLine($"    partial void On{eventName}({stubParams});");
        sb.AppendLine();
    }

    private static void EmitDispatchBranch(
        StringBuilder dispatchBranches,
        string eventName,
        string codeRef,
        List<PayloadParam> payloadParams,
        bool withSender)
    {
        dispatchBranches.AppendLine($"            case {codeRef}:");
        dispatchBranches.AppendLine("            {");

        // Read off the header here rather than inside the callback, so the game thread is handed a
        // value instead of a reference to a struct the reader loop has moved on from.
        if (withSender)
            dispatchBranches.AppendLine($"                var {SenderParameterName} = header.Sender;");

        foreach (var p in payloadParams)
            dispatchBranches.AppendLine(DeserializeStatement(p));

        var names = payloadParams.Select(p => p.Name);
        var dispatchArgs = string.Join(", ", withSender ? new[] { SenderParameterName }.Concat(names) : names);

        dispatchBranches.AppendLine($"                RunOnGameThread(() => On{eventName}({dispatchArgs}));");
        dispatchBranches.AppendLine("                break;");
        dispatchBranches.AppendLine("            }");
    }

    private static string SerializeStatement(PayloadParam p) =>
        p.IsSerializablePrimitive ? $"        __writer.Put({p.Name});"
        : p.IsNetSerializable ? $"        {p.Name}.Serialize(__writer);"
        : $"        Serializer.SerializeObject(__writer, {p.Name});";

    private static string DeserializeStatement(PayloadParam p)
    {
        var typeFqn = p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (p.IsSerializablePrimitive)
        {
            var getter = SerializationHelper.GetDeserializationMethod(p.Type.SpecialType);
            return $"                var {p.Name} = reader.{getter}();";
        }

        if (p.IsNetSerializable)
        {
            return $"                var {p.Name} = new {typeFqn}();\n"
                   + $"                {p.Name}.Deserialize(reader);";
        }

        return $"                var {p.Name} = Serializer.DeserializeObject<{typeFqn}>(reader);";
    }

    private static string FormatParamList(List<PayloadParam> payloadParams) =>
        string.Join(", ", payloadParams.Select(p =>
            $"{p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {p.Name}"));

    private static List<PayloadParam> BuildPayloadParams(IMethodSymbol method) =>
        method.Parameters
            .Select(p => new PayloadParam(
                p.Type,
                p.Name,
                SerializationHelper.IsSerializablePrimitive(p.Type.SpecialType),
                SerializationHelper.IsINetSerializable(p.Type)))
            .ToList();

    private sealed class Rpc(string name, IMethodSymbol? request, IMethodSymbol? response, IMethodSymbol? peerToPeer)
    {
        public string Name { get; } = name;
        public IMethodSymbol? Request { get; } = request;
        public IMethodSymbol? Response { get; } = response;
        public IMethodSymbol? PeerToPeer { get; } = peerToPeer;
    }

    private sealed class PayloadParam(ITypeSymbol type, string name, bool isSerializablePrimitive, bool isNetSerializable)
    {
        public ITypeSymbol Type { get; } = type;
        public string Name { get; } = name;
        public bool IsSerializablePrimitive { get; } = isSerializablePrimitive;
        public bool IsNetSerializable { get; } = isNetSerializable;
    }
}
