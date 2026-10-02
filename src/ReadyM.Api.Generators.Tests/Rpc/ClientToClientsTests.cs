using System.Reflection;
using Microsoft.CodeAnalysis;
using ReadyM.Api.Generators;
using Xunit;

namespace ReadyM.Api.Generators.Tests.Rpc;

/// <summary>
/// The third RPC direction: one client sends, the relay decides which other clients hear it, and
/// every one of them runs the same handler. Contracts are declared beside the other two directions,
/// so the split that matters is the code space, which is the client one rather than the server one.
/// </summary>
public class ClientToClientsTests(ITestOutputHelper output)
{
    private static readonly Assembly[] Sdk = [typeof(ReadyM.Api.Multiplayer.ClientToClientsAttribute).Assembly];

    /// What a client mod cannot reach, which is how the generators tell the two sides apart. The
    /// test assembly references it, so a client project has to be described by what it leaves out.
    private static readonly string[] AsAClientMod =
    [
        typeof(ReadyM.Relay.Server.Sdk.Rpc.ServerRpcHandlersBase).Assembly.GetName().Name!,
    ];

    private const string Contracts = """
        using ReadyM.Api.Multiplayer;
        using ReadyM.Api.Multiplayer.Protocol.Enums;

        namespace Mod.Common;

        [ServerRpcContracts]
        public static partial class Contracts
        {
            [ClientToServer] public static partial void ScaleBossHp(int percent);
            [ServerToClient] public static partial void BossHpScaled(int percent);
            [ClientToClients(RelayMode.GlobalOthers)] public static partial void ChatMessage(string text);
        }
        """;

    private const string Client = """
        using ReadyM.Api.Multiplayer;
        using Mod.Common;

        namespace Mod;

        [ServerRpcFor(typeof(Contracts))]
        public partial class ModRpc
        {
        }
        """;

    // -- the manifest ------------------------------------------------------------------------------

    /// The two spaces are numbered from their own ends, so a peer-to-peer name never takes a code a
    /// server RPC would have had.
    [Fact]
    public void A_peer_to_peer_name_is_assigned_out_of_the_client_code_space()
    {
        var manifest = Manifest();

        Assert.Contains(
            "public const RelayMessageCode ChatMessageCode =\r\n"
                .Replace("\r\n", Environment.NewLine)
            + "        (RelayMessageCode)(RelayMessageCode.MinClientRpcEvent + 0);",
            manifest);
    }

    [Fact]
    public void The_server_names_keep_the_server_code_space()
    {
        var manifest = Manifest();

        Assert.Contains("BossHpScaledCode", manifest);
        Assert.Contains("ScaleBossHpCode", manifest);
        Assert.DoesNotContain("ChatMessageCode =\n        (RelayMessageCode)(RelayMessageCode.MinServerRpcEvent", manifest);
    }

    /// Each space is handed out by its own provider, so each carries its own size and offset.
    [Fact]
    public void Each_space_carries_its_own_count_and_offset()
    {
        var manifest = Manifest();

        Assert.Contains("public const byte TotalEventCount = 2;", manifest);
        Assert.Contains("public static byte Offset { get; set; }", manifest);
        Assert.Contains("public const byte TotalClientEventCount = 1;", manifest);
        Assert.Contains("public static byte ClientOffset { get; set; }", manifest);
    }

    // -- the client class --------------------------------------------------------------------------

    /// Both ends of a peer-to-peer RPC are the same class, since every client runs it.
    [Fact]
    public void A_peer_to_peer_rpc_gets_a_sender_and_a_handler_on_the_client()
    {
        var generated = ClientCode();

        Assert.Contains("public void SendChatMessage(string text)", generated);
        Assert.Contains("partial void OnChatMessage(global::ReadyM.Api.Idents.PlayerId sender, string text);", generated);
    }

    /// It is relayed rather than routed, and the mode the contract named is what the relay is told.
    [Fact]
    public void It_is_sent_by_relay_mode_out_of_the_client_offset()
    {
        var generated = ClientCode();

        Assert.Contains("RelayMessage.ByRelayMode(", generated);
        Assert.Contains("(RelayMode)2", generated);
        Assert.Contains("ServerRpcManifest.ChatMessageCode + global::Mod.Common.ServerRpcManifest.ClientOffset", generated);
    }

    /// The local player is the sender the message carries, so there is nothing to send without one.
    [Fact]
    public void It_sends_nothing_before_this_client_has_a_player_id()
        => Assert.Contains("if (!RelayClient.PlayerId.HasValue)", ClientCode());

    /// The receiver is told who sent it, which is the one thing it cannot work out for itself.
    [Fact]
    public void The_handler_is_handed_the_player_it_came_from()
    {
        var generated = ClientCode();

        Assert.Contains("var sender = header.Sender;", generated);
        Assert.Contains("RunOnGameThread(() => OnChatMessage(sender, text));", generated);
    }

    /// A separate dispatcher, because the two spaces arrive through different relay callbacks.
    [Fact]
    public void It_is_dispatched_and_registered_through_the_client_rpc_handlers()
    {
        var generated = ClientCode();

        Assert.Contains("protected void OnClientEvent(CustomRelayEventHeader header, NetDataReader reader)", generated);
        Assert.Contains("RelayClient.AddClientRpcMessageHandler(", generated);
        Assert.Contains("RelayClient.RemoveClientRpcMessageHandler(", generated);
    }

    /// The other two directions are untouched by it, and still go through the server's own offset.
    [Fact]
    public void The_server_directions_are_unchanged()
    {
        var generated = ClientCode();

        Assert.Contains("public void SendScaleBossHp(int percent)", generated);
        Assert.Contains("partial void OnBossHpScaled(int percent);", generated);
        Assert.Contains("RelayClient.AddServerRpcMessageHandler(", generated);
    }

    /// The class only names its contracts, so the base it needs is written for it.
    [Fact]
    public void A_class_without_a_base_of_its_own_is_given_one()
        => Assert.Contains(
            "public partial class ModRpc : global::ReadyM.Api.Multiplayer.RPC.RpcBase",
            ClientCode());

    /// And one that still writes the 0.x base keeps it, or the two halves would disagree.
    [Fact]
    public void A_class_that_names_its_own_base_is_left_with_it()
    {
        var generated = ClientCode("""
            using ReadyM.Api.Multiplayer;
            using ReadyM.Api.Multiplayer.RPC;
            using Mod.Common;

            namespace Mod;

            [ServerRpcFor(typeof(Contracts))]
            public partial class ModRpc : ServerRpcClient
            {
            }
            """);

        Assert.Contains("public partial class ModRpc", generated);
        Assert.DoesNotContain("ModRpc :", generated);
    }

    // -- what it refuses ---------------------------------------------------------------------------

    /// Overloads of a name share one wire code, and the two spaces number theirs separately, so a
    /// name has to sit in one of them.
    [Fact]
    public void A_name_that_mixes_the_two_spaces_is_refused()
        => Assert.Contains(
            Reported("""
                using ReadyM.Api.Multiplayer;
                using ReadyM.Api.Multiplayer.Protocol.Enums;

                namespace Mod.Common;

                [ServerRpcContracts]
                public static partial class Contracts
                {
                    [ClientToServer]
                    [ClientToClients(RelayMode.GlobalOthers)]
                    public static partial void Muddle(int value);
                }
                """),
            diagnostic => diagnostic.Id == "SRPC006");

    [Fact]
    public void A_relay_mode_that_is_not_public_api_is_refused()
        => Assert.Contains(
            Reported("""
                using ReadyM.Api.Multiplayer;
                using ReadyM.Api.Multiplayer.Protocol.Enums;

                namespace Mod.Common;

                [ServerRpcContracts]
                public static partial class Contracts
                {
                    [ClientToClients((RelayMode)5)] public static partial void Sneaky(int value);
                }
                """),
            diagnostic => diagnostic.Id == "SRPC007");

    /// Saying nothing at all is still refused, and the message now offers all three.
    [Fact]
    public void A_contract_method_with_no_direction_is_still_refused()
        => Assert.Contains(
            Reported("""
                using ReadyM.Api.Multiplayer;

                namespace Mod.Common;

                [ServerRpcContracts]
                public static partial class Contracts
                {
                    public static partial void Silent(int value);
                }
                """),
            diagnostic => diagnostic.Id == "SRPC001");

    private string Manifest() => Generated(ContractsRun(), "ServerRpcManifest");

    private string ClientCode(string client = Client)
    {
        var result = SourceGeneratorTestHelper.RunGenerators(
            [("Client.cs", client)],
            [new ServerRpcEventGenerator()],
            output,
            Sdk,
            alsoReferenceMetadata: [ContractsRun().OutputCompilation.ToMetadataReference()],
            withoutReferences: AsAClientMod);

        return Generated(result, "RpcEvents");
    }

    private SourceGeneratorTestHelper.GeneratorRunResult ContractsRun() =>
        SourceGeneratorTestHelper.RunGenerators(
            [("Contracts.cs", Contracts)], [new ServerRpcContractGenerator()], output, Sdk,
            assemblyName: "Mod.Common");

    private Diagnostic[] Reported(string contracts) =>
        [.. SourceGeneratorTestHelper.RunGenerators(
            [("Contracts.cs", contracts)], [new ServerRpcContractGenerator()], output, Sdk)
            .CompilationDiagnostics];

    private static string Generated(SourceGeneratorTestHelper.GeneratorRunResult result, string hint)
        => string.Concat(result.GeneratedSyntaxTrees
            .Where(tree => tree.FilePath.Contains(hint, StringComparison.Ordinal))
            .Select(tree => tree.ToString()));
}
