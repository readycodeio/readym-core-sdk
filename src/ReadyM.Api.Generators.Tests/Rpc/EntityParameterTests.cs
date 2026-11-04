using System.Reflection;
using Microsoft.CodeAnalysis;
using ReadyM.Api.Generators;
using ReadyM.Api.Generators.Archetypes;
using Xunit;

namespace ReadyM.Api.Generators.Tests.Rpc;

/// <summary>
/// A shape is an RPC parameter like any other to the mod, and travels as its entity's network id: written by every
/// sender, read where the message arrives and looked up before the handler runs, which is skipped when the lookup
/// fails. Only a shape whose entities can have a network id may be one.
/// </summary>
public class EntityParameterTests(ITestOutputHelper output)
{
    private static readonly Assembly[] Common =
    [
        typeof(SDK.Attributes.ClientToClientsAttribute).Assembly,
        typeof(SDK.Attributes.ArchetypeAttribute).Assembly
    ];

    private static readonly Assembly[] Server = [.. Common, typeof(SDK.Server.Entities.IEntities).Assembly];

    private static readonly Assembly[] Client = [.. Common, typeof(SDK.Client.Entities.IEntities).Assembly];

    private static readonly string[] AsAClientMod =
    [
        typeof(ReadyM.Relay.Server.Sdk.Rpc.ServerRpcHandlersBase).Assembly.GetName().Name!,
    ];

    private const string Shapes = """
        using ReadyM.SDK.Attributes;

        namespace Other.Common;

        [Archetype]
        public readonly partial struct Hero { }

        [ArchetypeMixin(Replication.Both)]
        public readonly partial struct Vitals
        {
            public partial int Hp { get; set; }
        }

        [Archetype(Networked = false)]
        public readonly partial struct Prop { }

        [ArchetypeMixin(Replication.None)]
        public readonly partial struct Marker
        {
            public partial int Hue { get; set; }
        }
        """;

    private const string Contracts = """
        using ReadyM.Api.Multiplayer;
        using ReadyM.SDK.Attributes;

        namespace Other.Common;

        [RpcContracts]
        public static partial class Contracts
        {
            [ClientToServer] public static partial void Ask(Hero hero, Vitals? vitals, int power);
            [ServerToClient] public static partial void Tell(Hero hero, Hero? other);
            [ClientToClients(RelayMode.GlobalOthers)] public static partial void Hit(Hero attacker, Hero? target, int damage);
        }
        """;

    private const string ServerHandlers = """
        using ReadyM.Api.Multiplayer;
        using ReadyM.SDK.Attributes;
        using ReadyM.Relay.Server.Sdk.Rpc;
        using Other.Common;

        namespace Mod;

        [RpcHandlersFor(typeof(Contracts))]
        public partial class Referee
        {
            partial void OnAsk(RpcContext context, Hero hero, Vitals? vitals, int power) => SendTell(context.Sender, hero, null);
        }
        """;

    private const string ClientHandlers = """
        using ReadyM.Api.Idents;
        using ReadyM.Api.Multiplayer;
        using ReadyM.SDK.Attributes;
        using Other.Common;

        namespace Mod;

        [RpcHandlersFor(typeof(Contracts))]
        public partial class Events
        {
            partial void OnTell(Hero hero, Hero? other) => SendAsk(hero, null, 1);

            partial void OnHit(PlayerId sender, Hero attacker, Hero? target, int damage) => SendHit(attacker, target, damage);
        }
        """;

    private const string Codec = "global::ReadyM.SDK.Entities.RpcEntityCodec";

    private const string ServerCodec = "global::ReadyM.SDK.Server.Entities.ServerRpcEntityCodec";

    // -- the mod's side of it --------------------------------------------------------------------------

    [Fact]
    public void A_server_with_entity_handlers_compiles() => AssertCompiles(RunServer());

    [Fact]
    public void A_client_with_entity_handlers_compiles() => AssertCompiles(RunClient());

    [Fact]
    public void The_handlers_take_the_shapes_themselves()
    {
        var server = Generated(RunServer());
        var client = Generated(RunClient());

        Assert.Contains("partial void OnAsk(global::ReadyM.Relay.Server.Sdk.Rpc.RpcContext context, global::Other.Common.Hero hero, global::Other.Common.Vitals? vitals, int power);", server);
        Assert.Contains("public void SendTell(global::ReadyM.Api.Idents.PlayerId recipient, global::Other.Common.Hero hero, global::Other.Common.Hero? other)", server);
        Assert.Contains("partial void OnTell(global::Other.Common.Hero hero, global::Other.Common.Hero? other);", client);
        Assert.Contains("public void SendAsk(global::Other.Common.Hero hero, global::Other.Common.Vitals? vitals, int power)", client);
    }

    // -- the wire ------------------------------------------------------------------------------------

    [Fact]
    public void Every_sender_writes_the_entity_and_a_nullable_one_may_be_absent()
    {
        var server = Generated(RunServer());
        var client = Generated(RunClient());

        Assert.Contains($"{Codec}.Write(writer, hero, \"hero\");", server);
        Assert.Contains($"{Codec}.WriteOptional(writer, other, \"other\");", server);
        Assert.Contains($"{Codec}.Write(__writer, hero, \"hero\");", client);
        Assert.Contains($"{Codec}.WriteOptional(__writer, vitals, \"vitals\");", client);
        Assert.Contains($"{Codec}.Write(__writer, attacker, \"attacker\");", client);
    }

    /// <summary>The server handles a message on the thread its world changes on, so it looks up as it reads.</summary>
    [Fact]
    public void The_server_skips_the_handler_when_a_lookup_fails()
    {
        var server = Generated(RunServer());

        Assert.Contains(
            $"if (!{ServerCodec}.TryRead(this, reader, out global::Other.Common.Hero hero, \"Referee.OnAsk\", \"hero\"))\n                    break;",
            server);
        Assert.Contains(
            $"if (!{ServerCodec}.TryReadOptional(this, reader, out global::Other.Common.Vitals? vitals, \"Referee.OnAsk\", \"vitals\"))\n                    break;",
            server);
    }

    /// <summary>
    /// A client hands the game thread a copy of the message, and reads and looks the entities up there, where the
    /// entities they name are created and deleted.
    /// </summary>
    [Fact]
    public void A_client_reads_and_looks_up_entities_on_the_game_thread()
    {
        var client = Generated(RunClient());
        var hit = client[client.IndexOf("case global::Other.Common.ServerRpcManifest.HitCode:", StringComparison.Ordinal)..];
        hit = hit[..hit.IndexOf("break;\n            }", StringComparison.Ordinal)];

        var copy = hit.IndexOf("var __payload = reader.GetRemainingBytes();", StringComparison.Ordinal);
        var gameThread = hit.IndexOf("RunOnGameThread(() =>", StringComparison.Ordinal);
        var read = hit.IndexOf(
            $"if (!{Codec}.TryRead(this, __reader, out global::Other.Common.Hero attacker, \"Events.OnHit\", \"attacker\")) return;",
            StringComparison.Ordinal);
        var handler = hit.IndexOf("OnHit(sender, attacker, target, damage);", StringComparison.Ordinal);

        Assert.True(copy >= 0 && copy < gameThread, "the message is copied before the game thread");
        Assert.True(read > gameThread && read < handler, "the entity is read and looked up on the game thread, before the handler");
        Assert.Contains(
            $"if (!{Codec}.TryReadOptional(this, __reader, out global::Other.Common.Hero? target, \"Events.OnHit\", \"target\")) return;",
            hit);
        Assert.Contains("var damage = __reader.GetInt();", hit);
    }

    [Fact]
    public void A_message_without_entities_is_dispatched_as_before()
    {
        var client = Generated(RunClient(contracts: """
            using ReadyM.Api.Multiplayer;
            using ReadyM.SDK.Attributes;

            namespace Other.Common;

            [RpcContracts]
            public static partial class Contracts
            {
                [ServerToClient] public static partial void Tell(int value);
            }
            """, handlers: """
            using ReadyM.Api.Multiplayer;
            using ReadyM.SDK.Attributes;
            using Other.Common;

            namespace Mod;

            [RpcHandlersFor(typeof(Contracts))]
            public partial class Events { }
            """));

        Assert.Contains("RunOnGameThread(() => OnTell(value));", client);
        Assert.DoesNotContain("RpcEntityCodec", client);
    }

    // -- what cannot be sent -------------------------------------------------------------------------

    [Fact]
    public void A_shape_that_is_never_networked_is_an_error()
    {
        var diagnostics = ContractDiagnostics("""
            using ReadyM.Api.Multiplayer;
            using ReadyM.SDK.Attributes;

            namespace Other.Common;

            [RpcContracts]
            public static partial class Contracts
            {
                [ClientToServer] public static partial void Place(Prop prop, Marker? marker, Hero hero);
            }
            """);

        var errors = diagnostics.Where(d => d.Id == "SRPC008").ToList();

        Assert.Equal(2, errors.Count);
        Assert.All(errors, d => Assert.Equal(DiagnosticSeverity.Error, d.Severity));
        Assert.Contains(errors, d => d.GetMessage().Contains("'prop'") && d.GetMessage().Contains("Prop"));
        Assert.Contains(errors, d => d.GetMessage().Contains("'marker'") && d.GetMessage().Contains("Marker"));
    }

    [Fact]
    public void Networked_shapes_are_not_an_error()
        => Assert.DoesNotContain(ContractDiagnostics(Contracts), d => d.Id == "SRPC008");

    private static void AssertCompiles(SourceGeneratorTestHelper.GeneratorRunResult result)
        => Assert.Empty(result.OutputDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

    private static string Generated(SourceGeneratorTestHelper.GeneratorRunResult result)
        => string.Concat(result.GeneratedSyntaxTrees.Select(tree => tree.ToString())).Replace("\r\n", "\n");

    private SourceGeneratorTestHelper.GeneratorRunResult RunServer()
        => SourceGeneratorTestHelper.RunGenerators(
            [("Referee.cs", ServerHandlers)], [new ServerRpcHandlerGenerator()], output, Server,
            alsoReferenceMetadata: [RunCommon(Contracts).OutputCompilation.ToMetadataReference()]);

    private SourceGeneratorTestHelper.GeneratorRunResult RunClient(string contracts = Contracts, string handlers = ClientHandlers)
        => SourceGeneratorTestHelper.RunGenerators(
            [("Events.cs", handlers)], [new ServerRpcEventGenerator()], output, Client,
            alsoReferenceMetadata: [RunCommon(contracts).OutputCompilation.ToMetadataReference()],
            withoutReferences: AsAClientMod);

    private List<Diagnostic> ContractDiagnostics(string contracts)
        => [.. RunCommon(contracts).CompilationDiagnostics];

    /// <summary>The mod's Common project, where the contracts and the shapes they name live together.</summary>
    private SourceGeneratorTestHelper.GeneratorRunResult RunCommon(string contracts)
        => SourceGeneratorTestHelper.RunGenerators(
            [("Shapes.cs", Shapes), ("Contracts.cs", contracts)],
            [new ArchetypeGenerator(), new ArchetypeMixinGenerator(), new ServerRpcContractGenerator()],
            output, Common,
            assemblyName: "Other.Common");
}
