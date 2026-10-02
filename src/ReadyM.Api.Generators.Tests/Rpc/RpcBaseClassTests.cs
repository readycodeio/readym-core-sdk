using System.Reflection;
using Microsoft.CodeAnalysis;
using ReadyM.Api.Generators;
using Xunit;

namespace ReadyM.Api.Generators.Tests.Rpc;

/// <summary>
/// An RPC class names its contracts and nothing else: the base it needs is written for it, and which
/// side it is on is read off the project rather than off the class. A class that still writes a base
/// keeps the one it wrote, so a mod can drop them one at a time.
/// </summary>
public class RpcBaseClassTests(ITestOutputHelper output)
{
    private static readonly Assembly[] Sdk = [typeof(ReadyM.Api.Multiplayer.ServerRpcForAttribute).Assembly];

    /// A client mod has no reference to the server SDK, which is what the generators read the side
    /// off. The test assembly references everything, so this is how a client project is described.
    private static readonly string[] Client =
    [
        typeof(ReadyM.Relay.Server.Sdk.Rpc.ServerRpcHandlersBase).Assembly.GetName().Name!,
    ];

    private static readonly string[] Server = [];

    private const string Contracts = """
        using ReadyM.Api.Multiplayer;

        namespace Mod.Common;

        [ServerRpcContracts]
        public static partial class Contracts
        {
            [ClientToServer] public static partial void Ping(int value);
            [ServerToClient] public static partial void Pong(int value);
        }
        """;

    private const string Baseless = """
        using ReadyM.Api.Multiplayer;
        using Mod.Common;

        namespace Mod;

        [ServerRpcFor(typeof(Contracts))]
        public partial class Rpc
        {
        }
        """;

    // -- which side a baseless class lands on ------------------------------------------------------

    /// Only a server mod can reach the server SDK, which is what makes this a server mod.
    [Fact]
    public void A_baseless_class_in_a_server_project_is_given_the_server_base()
    {
        var generated = Run(new ServerRpcHandlerGenerator(), Baseless, Server);

        Assert.Contains(
            "public partial class Rpc : global::ReadyM.Relay.Server.Sdk.Rpc.ServerRpcHandlersBase",
            generated);
        Assert.Contains("partial void OnPing(global::ReadyM.Relay.Server.Sdk.Rpc.RpcContext context, int value);", generated);
        Assert.Contains("public void SendPong(global::ReadyM.Api.Idents.PlayerId recipient, int value)", generated);
    }

    [Fact]
    public void A_baseless_class_in_a_client_project_is_given_the_client_base()
    {
        var generated = Run(new ServerRpcEventGenerator(), Baseless, Client);

        Assert.Contains("public partial class Rpc : global::ReadyM.Api.Multiplayer.RPC.RpcBase", generated);
        Assert.Contains("public void SendPing(int value)", generated);
        Assert.Contains("partial void OnPong(int value);", generated);
    }

    /// Both generators run on a server mod, so exactly one of them has to claim the class or the two
    /// halves each bring an InitRpc.
    [Fact]
    public void The_client_generator_writes_nothing_in_a_server_project()
        => Assert.Empty(Run(new ServerRpcEventGenerator(), Baseless, Server));

    [Fact]
    public void The_server_generator_writes_nothing_in_a_client_project()
        => Assert.Empty(Run(new ServerRpcHandlerGenerator(), Baseless, Client));

    // -- a class that still writes its own base ----------------------------------------------------

    [Fact]
    public void A_class_that_writes_the_server_base_keeps_it()
    {
        var generated = Run(new ServerRpcHandlerGenerator(), """
            using ReadyM.Api.Multiplayer;
            using ReadyM.Relay.Server.Sdk.Rpc;
            using Mod.Common;

            namespace Mod;

            [ServerRpcFor(typeof(Contracts))]
            public partial class Rpc : ServerRpcHandlersBase
            {
            }
            """, Server);

        Assert.Contains("public partial class Rpc", generated);
        Assert.DoesNotContain("Rpc :", generated);
    }

    /// The 0.x client base is this side's wherever it is compiled, so a server project that still
    /// holds one keeps generating the client half for it.
    [Fact]
    public void A_class_that_writes_the_client_base_is_a_client_even_in_a_server_project()
    {
        var source = """
            using ReadyM.Api.Multiplayer;
            using ReadyM.Api.Multiplayer.RPC;
            using Mod.Common;

            namespace Mod;

            [ServerRpcFor(typeof(Contracts))]
            public partial class Rpc : ServerRpcClient
            {
            }
            """;

        Assert.Contains("public void SendPing(int value)", Run(new ServerRpcEventGenerator(), source, Server));
        Assert.Empty(Run(new ServerRpcHandlerGenerator(), source, Server));
    }

    private string Run(IIncrementalGenerator generator, string source, string[] without)
    {
        var contracts = SourceGeneratorTestHelper.RunGenerators(
            [("Contracts.cs", Contracts)], [new ServerRpcContractGenerator()], output, Sdk,
            assemblyName: "Mod.Common");

        var result = SourceGeneratorTestHelper.RunGenerators(
            [("Rpc.cs", source)], [generator], output, Sdk,
            alsoReferenceMetadata: [contracts.OutputCompilation.ToMetadataReference()],
            withoutReferences: without);

        return string.Concat(result.GeneratedSyntaxTrees.Select(tree => tree.ToString()));
    }
}
