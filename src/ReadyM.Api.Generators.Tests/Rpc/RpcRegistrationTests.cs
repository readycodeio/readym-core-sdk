using System.Reflection;
using Microsoft.CodeAnalysis;
using ReadyM.Api.Generators;
using ReadyM.Api.Generators.Archetypes;
using Xunit;

namespace ReadyM.Api.Generators.Tests.Rpc;

/// <summary>
/// An RPC class is put in DI for the mod, the way a [Service] is. Nothing names it: the generated
/// half declares it, the aggregator collects that, and the host registers everything declared.
/// </summary>
public class RpcRegistrationTests(ITestOutputHelper output)
{
    private static readonly Assembly[] Sdk = [typeof(ReadyM.SDK.Attributes.RpcContractsAttribute).Assembly];

    /// <summary>A client mod cannot reach the server SDK, which is how a baseless class is placed.</summary>
    private static readonly string[] AsAClientMod =
    [
        typeof(ReadyM.Relay.Server.Sdk.Rpc.ServerRpcHandlersBase).Assembly.GetName().Name!,
    ];

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
        using ReadyM.SDK.Attributes;
        using Mod.Common;

        namespace Mod;

        [RpcHandlersFor(typeof(Contracts))]
        public partial class Rpc
        {
        }
        """;

    /// <summary>
    /// The side is the generator's, not the host's: one process may run both, and a client class in
    /// a server container would be started with no relay client to register its handlers with.
    /// </summary>
    [Fact]
    public void A_client_class_declares_itself_for_the_client()
        => Assert.Contains(
            Declaration("Client"), Run(new ServerRpcEventGenerator(), Handlers, AsAClientMod));

    [Fact]
    public void A_server_class_declares_itself_for_the_server()
        => Assert.Contains(
            Declaration("Server"), Run(new ServerRpcHandlerGenerator(), Handlers, []));

    /// <summary>
    /// Saying it twice registers nothing twice: a module initializer runs it where the target has
    /// one, and the mod loader runs it for every assembly it loads.
    /// </summary>
    [Fact]
    public void Declaring_it_is_safe_to_say_twice()
    {
        var generated = Run(new ServerRpcEventGenerator(), Handlers, AsAClientMod);

        Assert.Contains("private static bool _registered;", generated);
        Assert.Contains("if (_registered)", generated);
    }

    /// <summary>
    /// Without this the registration only fires once something in the assembly is touched, and a
    /// netstandard2.0 mod has no module initializers at all, so nothing would ever run it.
    /// </summary>
    [Fact]
    public void The_aggregator_names_it()
        => Assert.Contains(
            "global::Mod.Rpc.Registration.Register();",
            Run(new RegistrationAggregatorGenerator(), Handlers, AsAClientMod));

    /// <summary>Nothing is written for a class no generator completes, so naming it would not compile.</summary>
    [Fact]
    public void The_aggregator_leaves_out_a_class_that_is_not_partial()
        => Assert.DoesNotContain(
            "global::Mod.Rpc.Registration.Register();",
            Run(new RegistrationAggregatorGenerator(), """
                using ReadyM.SDK.Attributes;
                using Mod.Common;

                namespace Mod;

                [RpcHandlersFor(typeof(Contracts))]
                public class Rpc
                {
                }
                """, AsAClientMod));

    [Fact]
    public void The_aggregator_leaves_out_a_class_naming_something_that_is_not_a_contract_set()
        => Assert.DoesNotContain(
            "global::Mod.Rpc.Registration.Register();",
            Run(new RegistrationAggregatorGenerator(), """
                using ReadyM.SDK.Attributes;

                namespace Mod;

                public static class NotContracts;

                [RpcHandlersFor(typeof(NotContracts))]
                public partial class Rpc
                {
                }
                """, AsAClientMod));

    private static string Declaration(string side) =>
        "global::ReadyM.Api.Multiplayer.RPC.RpcHandlerRegistry.Declare<global::Mod.Rpc>("
        + $"global::ReadyM.Api.Multiplayer.RPC.RpcSide.{side});";

    private string Run(IIncrementalGenerator generator, string source, string[] without)
    {
        var declared = SourceGeneratorTestHelper.RunGenerators(
            [("Contracts.cs", Contracts)], [new ServerRpcContractGenerator()], output, Sdk,
            assemblyName: "Mod.Common");

        var result = SourceGeneratorTestHelper.RunGenerators(
            [("Rpc.cs", source)], [generator], output, Sdk,
            alsoReferenceMetadata: [declared.OutputCompilation.ToMetadataReference()],
            withoutReferences: without);

        return string.Concat(result.GeneratedSyntaxTrees.Select(tree => tree.ToString()));
    }
}
