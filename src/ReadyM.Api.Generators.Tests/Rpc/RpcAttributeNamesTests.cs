using System.Reflection;
using Microsoft.CodeAnalysis;
using ReadyM.Relay.Server.Sdk.Rpc;
using ReadyM.SDK.Attributes;
using Xunit;

namespace ReadyM.Api.Generators.Tests.Rpc;

/// <summary>[RpcContracts] declares a contract set and [RpcHandlersFor] implements one, on either side.</summary>
public class RpcAttributeNamesTests(ITestOutputHelper output)
{
    private static readonly Assembly[] Sdk =
    [
        typeof(RpcContractsAttribute).Assembly,
        typeof(ClientToClientsAttribute).Assembly,
    ];

    /// <summary>A client mod cannot reach the server SDK, which is how a baseless class is placed.</summary>
    private static readonly string[] AsAClientMod =
    [
        typeof(ServerRpcHandlersBase).Assembly.GetName().Name!,
    ];

    [Fact]
    public void RpcContracts_declares_a_contract_set()
    {
        var manifest = Manifest();

        Assert.Contains("public const string Id = \"Mod.Common\";", manifest);
        Assert.Contains("public const ushort TotalEventCount = 2;", manifest);
        Assert.Contains("PingCode", manifest);
        Assert.Contains("PongCode", manifest);
    }

    [Fact]
    public void RpcHandlersFor_implements_a_contract_set_on_the_client()
    {
        var generated = Client();

        Assert.Contains("\npartial class Rpc : global::ReadyM.Api.Multiplayer.RPC.RpcBase", generated);
        Assert.Contains("public void SendPing(int value)", generated);
        Assert.Contains("partial void OnPong(int value);", generated);
    }

    [Fact]
    public void RpcHandlersFor_implements_a_contract_set_on_the_server()
    {
        var generated = Server();

        Assert.Contains(
            "\npartial class Rpc : global::ReadyM.Relay.Server.Sdk.Rpc.ServerRpcHandlersBase",
            generated);
        Assert.Contains("partial void OnPing(global::ReadyM.Relay.Server.Sdk.Rpc.RpcContext context, int value);", generated);
        Assert.Contains("public void SendPong(global::ReadyM.Api.Idents.PlayerId recipient, int value)", generated);
    }

    /// <summary>A client class naming no contract set is refused, and the message names the attribute.</summary>
    [Fact]
    public void A_class_naming_no_contract_set_is_refused()
    {
        var reported = Run(
            new ServerRpcEventGenerator(),
            """
            namespace Mod;

            public partial class Rpc : global::ReadyM.Api.Multiplayer.RPC.RpcBase
            {
            }
            """,
            AsAClientMod).Diagnostics;

        Assert.Contains(reported, diagnostic => diagnostic.Id == "SRPC004");
        Assert.Contains(reported, diagnostic => diagnostic.GetMessage().Contains("[RpcHandlersFor("));
    }

    private string Manifest() =>
        Text(SourceGeneratorTestHelper.RunGenerators(
            [("Contracts.cs", Contracts)], [new ServerRpcContractGenerator()], output, Sdk,
            assemblyName: "Mod.Common"));

    private string Client() => Run(new ServerRpcEventGenerator(), Handlers, AsAClientMod).Generated;

    private string Server() => Run(new ServerRpcHandlerGenerator(), Handlers, []).Generated;

    private (string Generated, Diagnostic[] Diagnostics) Run(IIncrementalGenerator generator, string source, string[] without)
    {
        var declared = SourceGeneratorTestHelper.RunGenerators(
            [("Contracts.cs", Contracts)], [new ServerRpcContractGenerator()], output, Sdk,
            assemblyName: "Mod.Common");

        var result = SourceGeneratorTestHelper.RunGenerators(
            [("Rpc.cs", source)], [generator], output, Sdk,
            alsoReferenceMetadata: [declared.OutputCompilation.ToMetadataReference()],
            withoutReferences: without);

        return (Text(result), [.. result.CompilationDiagnostics]);
    }

    private const string Contracts = """
          using ReadyM.Api.Multiplayer;
          using ReadyM.SDK.Attributes;

          namespace Mod.Common;

          [RpcContracts]
          public static partial class Contracts
          {
              [ClientToServer] public static partial void Ping(int value);
              [ServerToClient] public static partial void Pong(int value);
          }
          """;

    private const string Handlers = """
          using ReadyM.Api.Multiplayer;
          using ReadyM.SDK.Attributes;
          using Mod.Common;

          namespace Mod;

          [RpcHandlersFor(typeof(Contracts))]
          public partial class Rpc
          {
          }
          """;

    private static string Text(SourceGeneratorTestHelper.GeneratorRunResult result)
        => string.Concat(result.GeneratedSyntaxTrees.Select(tree => tree.ToString()));
}
