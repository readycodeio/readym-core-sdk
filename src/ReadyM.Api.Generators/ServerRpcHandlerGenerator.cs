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
    private const string ManifestClassName = "ServerRpcManifest";
    private const string RpcContextFqn = "global::ReadyM.Relay.Server.Sdk.Rpc.RpcContext";
    private const string PlayerIdFqn = "global::ReadyM.Api.Idents.PlayerId";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var handlerClasses = context.SyntaxProvider
            .CreateSyntaxProvider(Predicate, Transform)
            .Where(x => x is not null)
            .Collect();

        var manifest = context.CompilationProvider
            .Select(static (compilation, _) => FindManifestType(compilation));

        context.RegisterSourceOutput(
            handlerClasses.Combine(manifest),
            static (ctx, pair) => GenerateSources(ctx, pair.Left, pair.Right));
    }

    private static bool Predicate(SyntaxNode node, CancellationToken _) =>
        node is ClassDeclarationSyntax cls &&
        cls.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)) &&
        cls.BaseList is not null;

    private static INamedTypeSymbol? Transform(GeneratorSyntaxContext context, CancellationToken _)
    {
        if (context.Node is not ClassDeclarationSyntax)
            return null;

        var classSymbol = context.SemanticModel.GetDeclaredSymbol(context.Node) as INamedTypeSymbol;
        if (classSymbol is null || classSymbol.IsAbstract)
            return null;

        return DerivesFrom(classSymbol, BaseClassName) ? classSymbol : null;
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

    private static INamedTypeSymbol? FindManifestType(Compilation compilation)
    {
        var local = FindTypeNamed(compilation.GlobalNamespace, ManifestClassName);
        if (local != null) return local;

        foreach (var reference in compilation.References)
        {
            if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol asm)
            {
                var type = FindTypeNamed(asm.GlobalNamespace, ManifestClassName);
                if (type != null) return type;
            }
        }

        return null;
    }

    private static INamedTypeSymbol? FindTypeNamed(INamespaceSymbol ns, string name)
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

    private static void GenerateSources(
        SourceProductionContext context,
        ImmutableArray<INamedTypeSymbol?> rawClasses,
        INamedTypeSymbol? manifest)
    {
        var classes = rawClasses.Where(c => c is not null).Select(c => c!).ToList();
        if (classes.Count == 0)
            return;

        if (manifest is null)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "SRPC003", "Missing manifest",
                    $"No {ManifestClassName} found. Add a reference to the Common project containing [ServerRpcContracts].",
                    "ServerRpc", DiagnosticSeverity.Error, true),
                classes[0].Locations.FirstOrDefault()));
            return;
        }

        var manifestFqn = $"global::{manifest.ContainingNamespace.ToDisplayString()}.{ManifestClassName}";

        // Contract classes live in the manifest's assembly. Resolve each name's request/response.
        var contractClasses = ServerRpcModel.CollectContractClasses(manifest.ContainingAssembly.GlobalNamespace);
        var directions = ServerRpcModel.ResolveDirections(contractClasses);

        // Names in the order the manifest defines them (alphabetical).
        var names = manifest.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(p => p.IsStatic && p.Name.EndsWith("Code"))
            .Select(p => p.Name.Substring(0, p.Name.Length - 4))
            .ToList();

        var rpcs = names
            .Select(name =>
            {
                directions.TryGetValue(name, out var dir);
                return (Name: name, Request: dir.ClientToServer, Response: dir.ServerToClient);
            })
            .ToList();

        foreach (var classSymbol in classes)
            GenerateHandlerClass(context, classSymbol, rpcs, manifestFqn);
    }

    private static void GenerateHandlerClass(
        SourceProductionContext context,
        INamedTypeSymbol classSymbol,
        List<(string Name, IMethodSymbol? Request, IMethodSymbol? Response)> rpcs,
        string manifestFqn)
    {
        var ns = classSymbol.ContainingNamespace.ToDisplayString();
        var className = classSymbol.Name;
        var fullClassName = classSymbol.ToDisplayString();
        var access = classSymbol.DeclaredAccessibility.ToString().ToLower();

        var sb = new StringBuilder($$"""
                                     // <auto-generated/>
                                     using System;
                                     using LiteNetLib;
                                     using LiteNetLib.Utils;
                                     using ReadyM.Api.Multiplayer.Protocol;
                                     using ReadyM.Api.Multiplayer.Protocol.Enums;
                                     using ReadyM.Relay;

                                     namespace {{ns}};

                                     {{access}} partial class {{className}}
                                     {

                                     """);

        var dispatchCases = new StringBuilder();
        var initCalls = new StringBuilder();
        var deinitCalls = new StringBuilder();

        foreach (var (eventName, request, response) in rpcs)
        {
            var codeRef = $"{manifestFqn}.{eventName}Code";

            // s->c: emit the sender.
            if (response is not null)
            {
                var responseParams = BuildPayloadParams(response);
                EmitSender(sb, eventName, codeRef, responseParams);
            }

            // c->s: server receives, so emit the handler stub, dispatch case and (de)registration.
            if (request is not null)
            {
                var requestParams = BuildPayloadParams(request);
                EmitReceiveHandlerStub(sb, eventName, requestParams);
                EmitDispatchCase(dispatchCases, eventName, codeRef, requestParams);

                if (initCalls.Length > 0) initCalls.AppendLine();
                initCalls.Append($"        Rpc.AddServerRpcMessageHandler({codeRef}, OnServerRpcEventHandler);");
                if (deinitCalls.Length > 0) deinitCalls.AppendLine();
                deinitCalls.Append($"        Rpc.RemoveServerRpcMessageHandler({codeRef}, OnServerRpcEventHandler);");
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

        sb.AppendLine("}");

        context.AddSource(
            $"{fullClassName.Replace('.', '_')}_RpcHandlers.g.cs",
            sb.ToString());
    }

    private static void EmitSender(
        StringBuilder sb, string eventName, string codeRef, List<PayloadParam> payloadParams)
    {
        var payloadParamList = FormatParamList(payloadParams);
        var sendParamList = payloadParams.Count > 0
            ? $"{PlayerIdFqn} recipient, {payloadParamList}"
            : $"{PlayerIdFqn} recipient";

        sb.AppendLine($$"""
                            public void Send{{eventName}}({{sendParamList}})
                            {
                                var writer = new NetDataWriter();
                                writer.Put((byte){{codeRef}});
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
