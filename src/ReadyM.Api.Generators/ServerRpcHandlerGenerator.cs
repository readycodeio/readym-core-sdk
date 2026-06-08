using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReadyM.Api.Generators;

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

    private static INamedTypeSymbol? FindContractClass(INamespaceSymbol ns)
    {
        foreach (var type in ns.GetTypeMembers())
        {
            if (type.GetAttributes().Any(a =>
                    a.AttributeClass?.Name is "ServerRpcContractsAttribute" or "ServerRpcContracts"))
                return type;
        }

        foreach (var child in ns.GetNamespaceMembers())
        {
            var found = FindContractClass(child);
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
                    "SRPC002", "Missing manifest",
                    $"No {ManifestClassName} found. Add a reference to the Common project containing [ServerRpcContracts].",
                    "ServerRpc", DiagnosticSeverity.Error, true),
                classes[0].Locations.FirstOrDefault()));
            return;
        }

        var manifestFqn = $"global::{manifest.ContainingNamespace.ToDisplayString()}.{ManifestClassName}";
        var contractClass = FindContractClass(manifest.ContainingNamespace);

        // Contract methods in the order the manifest defines them (alphabetical).
        // The contract class is in a compiled referenced assembly - compiled methods
        // carry no IsPartialDefinition metadata, so no filter is needed.
        var contractMethods = manifest.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.IsStatic && p.Name.EndsWith("Code"))
            .Select(p => p.Name.Substring(0, p.Name.Length - 4))
            .Select(name => (
                Name: name,
                Symbol: contractClass?.GetMembers(name).OfType<IMethodSymbol>().FirstOrDefault()
            ))
            .ToList();

        foreach (var classSymbol in classes)
        {
            GenerateHandlerClass(context, classSymbol, contractMethods, manifestFqn);
        }
    }

    private static void GenerateHandlerClass(
        SourceProductionContext context,
        INamedTypeSymbol classSymbol,
        List<(string Name, IMethodSymbol? Symbol)> contractMethods,
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

        var dispatchBranches = new StringBuilder();
        var initCalls = new StringBuilder();
        var deinitCalls = new StringBuilder();

        var isFirst = true;
        foreach (var (eventName, contractMethodSymbol) in contractMethods)
        {
            var codeRef = $"{manifestFqn}.{eventName}Code";

            var payloadParams = contractMethodSymbol?.Parameters
                                    .Select(p => new PayloadParam(
                                        p.Type,
                                        p.Name,
                                        SerializationHelper.IsSerializablePrimitive(p.Type.SpecialType),
                                        p.Type.AllInterfaces.Any(i => i.Name == "INetSerializable")))
                                    .ToList()
                                ?? new List<PayloadParam>();

            var payloadParamList = string.Join(", ", payloadParams.Select(p =>
                $"{p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {p.Name}"));

            // Partial declaration stub: RpcContext always first, then payload params from contract.
            // The user provides the implementing half; unimplemented stubs are silently dropped
            // by the compiler so a class may implement any subset of contract methods.
            var stubParams = payloadParams.Count > 0
                ? $"{RpcContextFqn} context, {payloadParamList}"
                : $"{RpcContextFqn} context";
            sb.AppendLine($"    partial void On{eventName}({stubParams});");
            sb.AppendLine();

            // SendX: server → one client
            var sendParamList = payloadParams.Count > 0
                ? $"{PlayerIdFqn} recipient, {payloadParamList}"
                : $"{PlayerIdFqn} recipient";

            sb.AppendLine($$"""
                                public void Send{{eventName}}({{sendParamList}})
                                {
                                    var writer = new NetDataWriter();
                                    writer.Put((byte){{codeRef}});
                            """);

            for (var i = 0; i < payloadParams.Count; i++)
            {
                var p = payloadParams[i];
                if (p.IsSerializablePrimitive)
                    sb.AppendLine($"        writer.Put({p.Name});");
                else if (p.IsNetSerializable)
                    sb.AppendLine($"        {p.Name}.Serialize(writer);");
                else
                    sb.AppendLine($"        Serializer.SerializeObject(writer, {p.Name});");
            }

            sb.AppendLine("""
                                  RpcApi.SendToOne(recipient, writer, DeliveryMethod.ReliableOrdered);
                              }

                          """);

            // Dispatch branch
            var branch = isFirst ? "if" : "else if";
            dispatchBranches.AppendLine($"            {branch} (header.EventCode == {codeRef})");
            dispatchBranches.AppendLine("            {");

            for (var i = 0; i < payloadParams.Count; i++)
            {
                var p = payloadParams[i];
                var typeFqn = p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                if (p.IsSerializablePrimitive)
                {
                    var getter = SerializationHelper.GetDeserializationMethod(p.Type.SpecialType);
                    dispatchBranches.AppendLine($"                var {p.Name} = reader.{getter}();");
                }
                else if (p.IsNetSerializable)
                {
                    dispatchBranches.AppendLine($"                var {p.Name} = new {typeFqn}();");
                    dispatchBranches.AppendLine($"                {p.Name}.Deserialize(reader);");
                }
                else
                {
                    dispatchBranches.AppendLine($"                var {p.Name} = Serializer.DeserializeObject<{typeFqn}>(reader);");
                }
            }

            var contextArg = $"new {RpcContextFqn}(header.Sender)";
            var dispatchArgs = payloadParams.Count > 0
                ? $"{contextArg}, {string.Join(", ", payloadParams.Select(p => p.Name))}"
                : contextArg;

            dispatchBranches.AppendLine($"                On{eventName}({dispatchArgs});");
            dispatchBranches.AppendLine("            }");

            if (initCalls.Length > 0) initCalls.AppendLine();
            initCalls.Append($"        RpcApi.AddServerRpcMessageHandler({codeRef}, OnServerRpcEventHandler);");
            if (deinitCalls.Length > 0) deinitCalls.AppendLine();
            deinitCalls.Append($"        RpcApi.RemoveServerRpcMessageHandler({codeRef}, OnServerRpcEventHandler);");

            isFirst = false;
        }

        sb.AppendLine($$"""
                            private void OnServerRpcEventHandler(ServerEventHeader header, NetDataReader reader)
                            {
                        {{dispatchBranches}}
                                else
                                {
                                    throw new InvalidOperationException($"Unknown event code: {header.EventCode}");
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

    private sealed class PayloadParam(ITypeSymbol type, string name, bool isSerializablePrimitive, bool isNetSerializable)
    {
        public ITypeSymbol Type { get; } = type;
        public string Name { get; } = name;
        public bool IsSerializablePrimitive { get; } = isSerializablePrimitive;
        public bool IsNetSerializable { get; } = isNetSerializable;
    }
}