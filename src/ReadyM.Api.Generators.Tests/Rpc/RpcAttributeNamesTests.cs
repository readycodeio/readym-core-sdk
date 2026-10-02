using System.Reflection;
using Microsoft.CodeAnalysis;
using ReadyM.Api.Generators;
using Xunit;

namespace ReadyM.Api.Generators.Tests.Rpc;

/// <summary>
/// [RpcContracts] and [RpcHandlersFor] are what the two attributes are called now. The 0.x
/// [ServerRpcContracts] and [ServerRpcFor] mean exactly the same thing and are read the same way, so
/// a mod keeps compiling and can move over a file at a time, including a contracts class and its
/// handlers disagreeing about which spelling they use.
/// </summary>
public class RpcAttributeNamesTests(ITestOutputHelper output)
{
    private static readonly Assembly[] Sdk =
    [
        typeof(ReadyM.SDK.Attributes.RpcContractsAttribute).Assembly,
        typeof(ReadyM.Api.Multiplayer.ServerRpcForAttribute).Assembly,
    ];

    /// A client mod cannot reach the server SDK, which is how a baseless class is placed.
    private static readonly string[] AsAClientMod =
    [
        typeof(ReadyM.Relay.Server.Sdk.Rpc.ServerRpcHandlersBase).Assembly.GetName().Name!,
    ];

    private const string New = "RpcContracts";
    private const string Old = "ServerRpcContracts";
    private const string NewFor = "RpcHandlersFor";
    private const string OldFor = "ServerRpcFor";

    [Theory]
    [InlineData(New)]
    [InlineData(Old)]
    public void Either_spelling_declares_a_contract_set(string contracts)
    {
        var manifest = Manifest(contracts);

        Assert.Contains("public const string Id = \"Mod.Common\";", manifest);
        Assert.Contains("public const byte TotalEventCount = 2;", manifest);
        Assert.Contains("PingCode", manifest);
        Assert.Contains("PongCode", manifest);
    }

    /// Four combinations, because the contracts class and the class implementing it are usually in
    /// different projects and move over at different times.
    [Theory]
    [InlineData(New, NewFor)]
    [InlineData(Old, OldFor)]
    [InlineData(New, OldFor)]
    [InlineData(Old, NewFor)]
    public void Either_spelling_implements_a_contract_set_on_the_client(string contracts, string handlersFor)
    {
        var generated = Client(contracts, handlersFor);

        Assert.Contains("public partial class Rpc : global::ReadyM.Api.Multiplayer.RPC.RpcBase", generated);
        Assert.Contains("public void SendPing(int value)", generated);
        Assert.Contains("partial void OnPong(int value);", generated);
    }

    [Theory]
    [InlineData(New, NewFor)]
    [InlineData(Old, OldFor)]
    public void Either_spelling_implements_a_contract_set_on_the_server(string contracts, string handlersFor)
    {
        var generated = Server(contracts, handlersFor);

        Assert.Contains(
            "public partial class Rpc : global::ReadyM.Relay.Server.Sdk.Rpc.ServerRpcHandlersBase",
            generated);
        Assert.Contains("partial void OnPing(global::ReadyM.Relay.Server.Sdk.Rpc.RpcContext context, int value);", generated);
        Assert.Contains("public void SendPong(global::ReadyM.Api.Idents.PlayerId recipient, int value)", generated);
    }

    /// The two spellings are one attribute as far as the generator is concerned, so what they write
    /// has to be the same text, not merely similar.
    [Fact]
    public void The_two_spellings_generate_the_same_thing()
    {
        Assert.Equal(Manifest(New), Manifest(Old));
        Assert.Equal(Client(New, NewFor), Client(Old, OldFor));
        Assert.Equal(Server(New, NewFor), Server(Old, OldFor));
    }

    /// Naming no contract set at all is still refused, and the message names the new spelling.
    [Fact]
    public void A_class_naming_no_contract_set_is_still_refused()
    {
        var reported = Run(
            new ServerRpcEventGenerator(),
            """
            namespace Mod;

            public partial class Rpc : global::ReadyM.Api.Multiplayer.RPC.ServerRpcClient
            {
            }
            """,
            New,
            AsAClientMod).Diagnostics;

        Assert.Contains(reported, diagnostic => diagnostic.Id == "SRPC004");
        Assert.Contains(reported, diagnostic => diagnostic.GetMessage().Contains("[RpcHandlersFor("));
    }

    private string Manifest(string contracts) =>
        Text(SourceGeneratorTestHelper.RunGenerators(
            [("Contracts.cs", Contracts(contracts))], [new ServerRpcContractGenerator()], output, Sdk,
            assemblyName: "Mod.Common"));

    private string Client(string contracts, string handlersFor) =>
        Run(new ServerRpcEventGenerator(), Handlers(handlersFor), contracts, AsAClientMod).Generated;

    private string Server(string contracts, string handlersFor) =>
        Run(new ServerRpcHandlerGenerator(), Handlers(handlersFor), contracts, []).Generated;

    private (string Generated, Diagnostic[] Diagnostics) Run(
        IIncrementalGenerator generator, string source, string contracts, string[] without)
    {
        var declared = SourceGeneratorTestHelper.RunGenerators(
            [("Contracts.cs", Contracts(contracts))], [new ServerRpcContractGenerator()], output, Sdk,
            assemblyName: "Mod.Common");

        var result = SourceGeneratorTestHelper.RunGenerators(
            [("Rpc.cs", source)], [generator], output, Sdk,
            alsoReferenceMetadata: [declared.OutputCompilation.ToMetadataReference()],
            withoutReferences: without);

        return (Text(result), [.. result.CompilationDiagnostics]);
    }

    private static string Contracts(string attribute) =>
        $$"""
          using ReadyM.Api.Multiplayer;
          using ReadyM.SDK.Attributes;

          namespace Mod.Common;

          [{{attribute}}]
          public static partial class Contracts
          {
              [ClientToServer] public static partial void Ping(int value);
              [ServerToClient] public static partial void Pong(int value);
          }
          """;

    private static string Handlers(string attribute) =>
        $$"""
          using ReadyM.Api.Multiplayer;
          using ReadyM.SDK.Attributes;
          using Mod.Common;

          namespace Mod;

          [{{attribute}}(typeof(Contracts))]
          public partial class Rpc
          {
          }
          """;

    private static string Text(SourceGeneratorTestHelper.GeneratorRunResult result)
        => string.Concat(result.GeneratedSyntaxTrees.Select(tree => tree.ToString()));
}
