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
/// Runs on a server mod project. Per RPC name, for each <c>ServerRpcHandlersBase</c> class emits the
/// declared legs: request (c-&gt;s) as an On(RpcContext, ...) handler + dispatch, and response (s-&gt;c)
/// as a Send(PlayerId, ...). A one-way RPC only produces the leg it declares.
/// </summary>
[Generator]
internal class ServerRpcHandlerGenerator : IIncrementalGenerator
{
    private const string BaseClassName = "ServerRpcHandlersBase";
    private const string ClientBaseClassName = "ServerRpcClient";
    private const string BaseTypeMetadataName = "ReadyM.Relay.Server.Sdk.Rpc.ServerRpcHandlersBase";
    private const string BaseFqn = "global::ReadyM.Relay.Server.Sdk.Rpc.ServerRpcHandlersBase";
    private const string ManifestClassName = "ServerRpcManifest";
    private const string RpcContextFqn = "global::ReadyM.Relay.Server.Sdk.Rpc.RpcContext";
    private const string PlayerIdFqn = "global::ReadyM.Api.Idents.PlayerId";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var handlerClasses = context.SyntaxProvider
            .CreateSyntaxProvider(Predicate, Transform)
            .Where(x => x.Symbol is not null)
            .Collect();

        context.RegisterSourceOutput(handlerClasses, static (ctx, classes) => GenerateSources(ctx, classes));
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

        if (DerivesFrom(classSymbol, BaseClassName))
            return found;

        // Both sides of an RPC name their contracts the same way and a class need not say which
        // side it is, so the compilation does: only a server mod references the base below. A class
        // that still writes the client's own base is that side's, wherever it is compiled.
        return ServerRpcModel.HasServerRpcFor(classSymbol)
               && !DerivesFrom(classSymbol, ClientBaseClassName)
               && IsServerSide(context.SemanticModel.Compilation)
            ? found
            : none;
    }


    /// <summary>Whether this compilation is a server mod, which is what the server SDK being
    /// reachable means. A client mod never references it.</summary>
    internal static bool IsServerSide(Compilation compilation) =>
        compilation.GetTypeByMetadataName(BaseTypeMetadataName) is not null;

    /// <summary>
    /// Whether the mod wrote a base of its own. When it did not, the generated half supplies one,
    /// which is what lets a class name only the contracts it implements.
    /// </summary>
    private static bool HasOwnBase(INamedTypeSymbol symbol) =>
        symbol.BaseType is not null && symbol.BaseType.SpecialType != SpecialType.System_Object;

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

    private static void GenerateSources(
        SourceProductionContext context,
        ImmutableArray<(INamedTypeSymbol? Symbol, bool ModuleInitializer)> rawClasses)
    {
        // Each class names its own contract set, so a mod can host handlers for several of them.
        foreach (var (symbol, moduleInitializer) in rawClasses.Where(c => c.Symbol is not null))
        {
            var classSymbol = symbol!;

            if (!ServerRpcModel.TryResolveContracts(context, classSymbol, out var contractsType, out var manifest))
                continue;

            var manifestFqn = $"global::{manifest.ContainingNamespace.ToDisplayString()}.{ManifestClassName}";

            // Class-scoped: only the legs the named contracts class declares, even when the manifest
            // covers several contract classes from the same assembly.
            var directions = ServerRpcModel.ResolveDirections([contractsType]);

            var rpcs = ServerRpcModel.ManifestNames(manifest)
                .Where(directions.ContainsKey)
                .Select(name =>
                {
                    directions.TryGetValue(name, out var dir);
                    return (Name: name, Request: dir.ClientToServer, Response: dir.ServerToClient);
                })
                .ToList();

            GenerateHandlerClass(context, classSymbol, rpcs, manifestFqn, moduleInitializer);
        }
    }

    private static void GenerateHandlerClass(
        SourceProductionContext context,
        INamedTypeSymbol classSymbol,
        List<(string Name, IMethodSymbol? Request, IMethodSymbol? Response)> rpcs,
        string manifestFqn,
        bool moduleInitializer)
    {
        var ns = classSymbol.ContainingNamespace.ToDisplayString();
        var className = classSymbol.Name;
        var fullClassName = classSymbol.ToDisplayString();
        var access = classSymbol.DeclaredAccessibility.ToString().ToLower();
        var baseList = HasOwnBase(classSymbol) ? string.Empty : $" : {BaseFqn}";

        var sb = new StringBuilder($$"""
                                     // <auto-generated/>
                                     using System;
                                     using LiteNetLib;
                                     using LiteNetLib.Utils;
                                     using ReadyM.Api.Multiplayer.Protocol;
                                     using ReadyM.Api.Multiplayer.Protocol.Enums;
                                     using ReadyM.Relay;

                                     namespace {{ns}};

                                     {{access}} partial class {{className}}{{baseList}}
                                     {

                                     """);

        var dispatchCases = new StringBuilder();
        var initCalls = new StringBuilder();
        var deinitCalls = new StringBuilder();

        foreach (var (eventName, request, response) in rpcs)
        {
            var codeRef = $"{manifestFqn}.{eventName}Code";
            var codeRefWithOffset = $"{codeRef} + {manifestFqn}.Offset";

            // s->c: emit the sender.
            if (response is not null)
            {
                var responseParams = BuildPayloadParams(response);
                EmitSender(sb, eventName, codeRefWithOffset, responseParams);
            }

            // c->s: server receives, so emit the handler stub, dispatch case and (de)registration.
            if (request is not null)
            {
                var requestParams = BuildPayloadParams(request);
                EmitReceiveHandlerStub(sb, eventName, requestParams);
                EmitDispatchCase(dispatchCases, eventName, codeRef, requestParams);

                if (initCalls.Length > 0) initCalls.AppendLine();
                initCalls.Append($"        Rpc.AddServerRpcMessageHandler({codeRef} + {manifestFqn}.Offset, OnServerRpcEventHandler);");
                if (deinitCalls.Length > 0) deinitCalls.AppendLine();
                deinitCalls.Append($"        Rpc.RemoveServerRpcMessageHandler({codeRef} + {manifestFqn}.Offset, OnServerRpcEventHandler);");
            }
        }

        sb.AppendLine($$"""
                            private void OnServerRpcEventHandler(ServerEventHeader header, NetDataReader reader)
                            {
                                switch ((RelayMessageCode)(header.EventCode - {{manifestFqn}}.Offset))
                                {
                            {{dispatchCases}}
                                    default:
                                        break;
                                }
                            }

                            protected override void InitRpc()
                            {
                        {{initCalls}}
                            }

                            protected override void DeInitRpc()
                            {
                        {{deinitCalls}}
                            }
                        """);

        RpcRegistrationEmitter.Emit(sb, classSymbol, moduleInitializer, "Server");

        sb.AppendLine("}");

        context.AddSource(
            $"{fullClassName.Replace('.', '_')}_RpcHandlers.g.cs",
            sb.ToString());
    }

    private static void EmitSender(StringBuilder sb, string eventName, string codeRef, List<PayloadParam> payloadParams)
    {
        var payloadParamList = FormatParamList(payloadParams);
        var sendParamList = payloadParams.Count > 0
            ? $"{PlayerIdFqn} recipient, {payloadParamList}"
            : $"{PlayerIdFqn} recipient";

        sb.AppendLine($$"""
                            public void Send{{eventName}}({{sendParamList}})
                            {
                                var writer = new NetDataWriter();
                                writer.Put((byte)({{codeRef}}));
                        """);

        foreach (var p in payloadParams)
            sb.AppendLine(SerializeStatement(p));

        sb.AppendLine("""
                              Rpc.SendToOne(recipient, writer, DeliveryMethod.ReliableOrdered);
                          }

                      """);
    }

    private static void EmitReceiveHandlerStub(
        StringBuilder sb, string eventName, List<PayloadParam> payloadParams)
    {
        var payloadParamList = FormatParamList(payloadParams);
        var stubParams = payloadParams.Count > 0
            ? $"{RpcContextFqn} context, {payloadParamList}"
            : $"{RpcContextFqn} context";

        // Unimplemented partial stubs are dropped, so a class may implement any subset.
        sb.AppendLine($"    partial void On{eventName}({stubParams});");
        sb.AppendLine();
    }

    private static void EmitDispatchCase(
        StringBuilder dispatchCases, string eventName, string codeRef, List<PayloadParam> payloadParams)
    {
        dispatchCases.AppendLine($"            case {codeRef}:");
        dispatchCases.AppendLine("            {");

        foreach (var p in payloadParams)
            dispatchCases.AppendLine(DeserializeStatement(p));

        var contextArg = $"new {RpcContextFqn}(header.Sender)";
        var dispatchArgs = payloadParams.Count > 0
            ? $"{contextArg}, {string.Join(", ", payloadParams.Select(p => p.Name))}"
            : contextArg;

        dispatchCases.AppendLine($"                On{eventName}({dispatchArgs});");
        dispatchCases.AppendLine("                break;");
        dispatchCases.AppendLine("            }");
    }

    private static string SerializeStatement(PayloadParam p) =>
        p.IsSerializablePrimitive ? $"        writer.Put({p.Name});"
        : p.IsNetSerializable ? $"        {p.Name}.Serialize(writer);"
        : $"        Serializer.SerializeObject(writer, {p.Name});";

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

    private sealed class PayloadParam(ITypeSymbol type, string name, bool isSerializablePrimitive, bool isNetSerializable)
    {
        public ITypeSymbol Type { get; } = type;
        public string Name { get; } = name;
        public bool IsSerializablePrimitive { get; } = isSerializablePrimitive;
        public bool IsNetSerializable { get; } = isNetSerializable;
    }
}
