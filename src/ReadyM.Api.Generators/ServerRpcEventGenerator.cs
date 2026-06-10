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
            .OfType<IFieldSymbol>()
            .Where(p => p.IsStatic && p.Name.EndsWith("Code"))
            .Select(p => p.Name.Substring(0, p.Name.Length - 4))
            .Select(name => (
                Name: name,
                Symbol: contractClass?.GetMembers(name).OfType<IMethodSymbol>().FirstOrDefault()
            ))
            .ToList();

        foreach (var classSymbol in classes)
        {
            GenerateEventClass(context, classSymbol, contractMethods, manifestFqn);
        }
    }

    private static void GenerateEventClass(
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

        foreach (var (eventName, contractMethodSymbol) in contractMethods)
        {
            var codeRef = $"{manifestFqn}.{eventName}Code";

            var payloadParams = contractMethodSymbol?.Parameters
                .Select(p => new PayloadParam(
                    p.Type,
                    p.Name,
                    SerializationHelper.IsSerializablePrimitive(p.Type.SpecialType),
                    SerializationHelper.IsINetSerializable(p.Type)))
                .ToList()
                ?? new List<PayloadParam>();

            var payloadParamList = string.Join(", ", payloadParams.Select(p =>
                $"{p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {p.Name}"));

            // Partial declaration stub. No RpcContext on the client side - the message
            // always comes from the server, there is no meaningful sender.
            // Unimplemented stubs are silently dropped by the compiler.
            sb.AppendLine($"    partial void On{eventName}({payloadParamList});");
            sb.AppendLine();

            // SendX: client → server
            sb.AppendLine($$"""
                                public void Send{{eventName}}({{payloadParamList}})
                                {
                                    var message = RelayMessage.ToServer({{codeRef}}, DeliveryMethod.ReliableOrdered);
                                    var writer = message.Writer;
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
                                  RelayClient.SendMessage(message);
                              }

                          """);

            // Dispatch branch (server → client receive path)
            dispatchBranches.AppendLine($"            case {codeRef}:");
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

            var dispatchArgs = string.Join(", ", payloadParams.Select(p => p.Name));
            dispatchBranches.AppendLine($"                RunOnGameThread(() => {{ On{eventName}({dispatchArgs}); }});");
            dispatchBranches.AppendLine("                break;");
            dispatchBranches.AppendLine("            }");

            if (initCalls.Length > 0) initCalls.AppendLine();
            initCalls.Append($"        RelayClient.AddServerRpcMessageHandler({codeRef}, OnServerEvent);");
            if (deinitCalls.Length > 0) deinitCalls.AppendLine();
            deinitCalls.Append($"        RelayClient.RemoveServerRpcMessageHandler({codeRef}, OnServerEvent);");
        }

        sb.AppendLine($$"""
                            protected void OnServerEvent(ServerEventHeader header, NetDataReader reader)
                            {
                                switch ((RelayMessageCode)(header.EventCode - {{manifestFqn}}.Offset))
                                {
                                {{dispatchBranches}}
                                    default:
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
            $"{fullClassName.Replace('.', '_')}_RpcEvents.g.cs",
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