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
/// Runs on a client mod project. For each class deriving from <c>ServerRpcClient</c>,
/// emits, per RPC name:
///   - the client-to-server (request) leg, if declared: a <c>Send{Name}(...)</c> sender;
///   - the server-to-client (response/push) leg, if declared: an <c>On{Name}(...)</c> handler
///     stub, its receive/dispatch wiring, and handler (de)registration.
///
/// This is the mirror image of the server handler generator: the client sends the request and
/// receives the response, so a one-way RPC only produces the leg it declares.
/// </summary>
[Generator]
internal class ServerRpcEventGenerator : IIncrementalGenerator
{
    private const string BaseClassName = "ServerRpcClient";
    private const string ManifestClassName = "ServerRpcManifest";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var eventClasses = context.SyntaxProvider
            .CreateSyntaxProvider(Predicate, Transform)
            .Where(x => x is not null)
            .Collect();

        var manifest = context.CompilationProvider
            .Select(static (compilation, _) => FindManifestType(compilation));

        context.RegisterSourceOutput(
            eventClasses.Combine(manifest),
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

        var contractClasses = ServerRpcModel.CollectContractClasses(manifest.ContainingAssembly.GlobalNamespace);
        var directions = ServerRpcModel.ResolveDirections(contractClasses);

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
            GenerateEventClass(context, classSymbol, rpcs, manifestFqn);
    }

    private static void GenerateEventClass(
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
                                     using ReadyM.Api.Multiplayer;
                                     using ReadyM.Api.Multiplayer.Protocol;
                                     using ReadyM.Api.Multiplayer.Protocol.Enums;
                                     using ReadyM.Api.Multiplayer.Client;
                                     using ReadyM.Api.Multiplayer.Extensions;

                                     namespace {{ns}};

                                     {{access}} partial class {{className}}
                                     {

                                     """);

        var dispatchBranches = new StringBuilder();
        var initCalls = new StringBuilder();
        var deinitCalls = new StringBuilder();
        var offsetRef = $"{manifestFqn}.Offset";

        foreach (var (eventName, request, response) in rpcs)
        {
            var codeRef = $"{manifestFqn}.{eventName}Code";

            // Client -> server (request): emit the sender using the request shape.
            if (request is not null)
            {
                var requestParams = BuildPayloadParams(request);
                EmitSender(sb, eventName, codeRef, offsetRef, requestParams);
            }

            // Server -> client (response/push): the client RECEIVES this leg, so emit the
            // handler stub, its dispatch branch, and (de)registration on the shared code.
            if (response is not null)
            {
                var responseParams = BuildPayloadParams(response);
                EmitReceiveHandlerStub(sb, eventName, responseParams);
                EmitDispatchBranch(dispatchBranches, eventName, codeRef, responseParams);

                if (initCalls.Length > 0) initCalls.AppendLine();
                initCalls.Append($"        RelayClient.AddServerRpcMessageHandler({codeRef} + {offsetRef}, OnServerEvent);");
                if (deinitCalls.Length > 0) deinitCalls.AppendLine();
                deinitCalls.Append($"        RelayClient.RemoveServerRpcMessageHandler({codeRef} + {offsetRef}, OnServerEvent);");
            }
        }

        sb.AppendLine($$"""
                            protected void OnServerEvent(ServerEventHeader header, NetDataReader reader)
                            {
                                switch ((RelayMessageCode)(header.EventCode - {{offsetRef}}))
                                {
                                {{dispatchBranches}}
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
            $"{fullClassName.Replace('.', '_')}_RpcEvents.g.cs",
            sb.ToString());
    }

    private static void EmitSender(
        StringBuilder sb, string eventName, string codeRef, string offsetRef, List<PayloadParam> payloadParams)
    {
        var payloadParamList = FormatParamList(payloadParams);

        sb.AppendLine($$"""
                            public void Send{{eventName}}({{payloadParamList}})
                            {
                                var message = RelayMessage.ToServer({{codeRef}} + {{offsetRef}}, DeliveryMethod.ReliableOrdered);
                                var writer = message.Writer;
                        """);

        foreach (var p in payloadParams)
            sb.AppendLine(SerializeStatement(p));

        sb.AppendLine("""
                              RelayClient.SendMessage(message);
                          }

                      """);
    }

    private static void EmitReceiveHandlerStub(
        StringBuilder sb, string eventName, List<PayloadParam> payloadParams)
    {
        var payloadParamList = FormatParamList(payloadParams);

        // No RpcContext on the client side - the message always comes from the server, there
        // is no meaningful sender. Unimplemented stubs are silently dropped by the compiler.
        sb.AppendLine($"    partial void On{eventName}({payloadParamList});");
        sb.AppendLine();
    }

    private static void EmitDispatchBranch(
        StringBuilder dispatchBranches, string eventName, string codeRef, List<PayloadParam> payloadParams)
    {
        dispatchBranches.AppendLine($"            case {codeRef}:");
        dispatchBranches.AppendLine("            {");

        foreach (var p in payloadParams)
            dispatchBranches.AppendLine(DeserializeStatement(p));

        var dispatchArgs = string.Join(", ", payloadParams.Select(p => p.Name));
        dispatchBranches.AppendLine($"                RunOnGameThread(() => {{ On{eventName}({dispatchArgs}); }});");
        dispatchBranches.AppendLine("                break;");
        dispatchBranches.AppendLine("            }");
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
