using System.Reflection;
using Microsoft.CodeAnalysis;
using ReadyM.Api.Generators.Analyzers;
using ReadyM.Api.Generators.Archetypes;
using Xunit;

namespace ReadyM.Api.Generators.Tests.Analyzers;

/// <summary>
/// READYM017, run against the real SDK so the shapes it recognises are the ones a mod compiles against.
/// </summary>
/// <remarks>
/// What it leaves alone matters as much as what it catches. A server writes what it likes, a local
/// shape is the mod's own business, and inside a mapping the write is the pull.
/// </remarks>
public class ReplicatedWriteAnalyzerTests(ITestOutputHelper output)
{
    private static readonly Assembly[] Sdk =
    [
        typeof(SDK.Attributes.ArchetypeAttribute).Assembly,
        typeof(SDK.Chunks.ChunkSlot).Assembly,
        typeof(SDK.Client.Entities.IEntities).Assembly
    ];

    private const string Declarations =
        """
        using ReadyM.SDK.Attributes;

        namespace Mod;

        [ArchetypeMixin(Replication.OwnershipBased)]
        public readonly partial struct Vitals
        {
            public partial int Hp { get; set; }

            private partial global::ReadyM.Native.Container.NativeList<int> Marks { get; set; }
        }

        [ArchetypeMixin(Replication.None)]
        public readonly partial struct Mood
        {
            public partial int Cheer { get; set; }
        }

        [ArchetypeMixin(Replication.ServerAuthoritative)]
        public readonly partial struct Curse
        {
            [Predictable] public partial bool Active { get; set; }

            public partial int Turns { get; set; }
        }

        [Archetype(Networked = false)]
        [Include(typeof(Vitals))]
        [Include(typeof(Mood))]
        [Include(typeof(Curse), "Curse")]
        public readonly partial struct Npc;
        """;

    /// <param name="body">Statements in a method holding a 'vitals', a 'mood', a 'curse' and an 'npc'.</param>
    private Diagnostic[] Report(string body, string runtime = "Client")
        => ReportSource(
            $$"""
              using ReadyM.SDK.Events;

              namespace Mod;

              public static class Use
              {
                  public static void Run(Vitals vitals, Mood mood, Curse curse, Npc npc, INetworkedEventRaise raise)
                  {
              {{body}}
                  }
              }
              """,
            runtime);

    private Diagnostic[] ReportSource(string use, string runtime = "Client")
    {
        var properties = new Dictionary<string, string> { ["build_property.SdkRuntime"] = runtime };

        var result = SourceGeneratorTestHelper.RunGenerators(
            [("Declarations.cs", Declarations), ("Use.cs", use)],
            [new ArchetypeGenerator(), new ArchetypeMixinGenerator()],
            output,
            Sdk,
            buildProperties: properties);

        var reported = SourceGeneratorTestHelper.Analyze(
            result.OutputCompilation, new ReplicatedWriteAnalyzer(), properties);

        return [.. reported.Where(diagnostic => diagnostic.Id is "READYM017" or "READYM019")];
    }

    [Fact]
    public void A_client_cannot_write_a_replicated_value()
        => Assert.Single(Report("        vitals.Hp = 5;"));

    [Fact]
    public void Nor_add_to_one()
        => Assert.Single(Report("        vitals.Hp += 5;"));

    [Fact]
    public void Nor_step_one()
        => Assert.Single(Report("        vitals.Hp++;"));

    /// <summary>A collection has no setter, so what is refused is every member that changes it.</summary>
    [Fact]
    public void Nor_change_a_replicated_collection()
        => Assert.Single(Report("        vitals.AddMarks(1);"));

    [Fact]
    public void Nor_clear_one()
        => Assert.Single(Report("        vitals.ClearMarks();"));

    /// <summary>Reading one says nothing about who owns it, the same as reading a value.</summary>
    [Fact]
    public void Reading_a_collection_is_left_alone()
        => Assert.Empty(Report("        var read = vitals.MarksCount;"));

    /// <summary>Nothing sends it, so nobody is kept from writing it.</summary>
    [Fact]
    public void A_local_value_is_left_alone()
        => Assert.Empty(Report("        mood.Cheer = 5;"));

    /// <summary>Reading says nothing about who owns the value.</summary>
    [Fact]
    public void Reading_is_left_alone()
        => Assert.Empty(Report("        var read = vitals.Hp;"));

    /// <summary>The server is the one deciding, and the API saying otherwise is not compiled for it.</summary>
    [Fact]
    public void A_server_writes_what_it_likes()
        => Assert.Empty(Report("        vitals.Hp = 5;", runtime: "Server"));

    // -- a value a client may guess at ---------------------------------------------------------------

    /// <summary>
    /// A guess is its own call, so writing one is the ordinary refusal. What changes is where it sends the
    /// reader: a mapping is not what they want, and the guess is.
    /// </summary>
    [Fact]
    public void Writing_a_guessable_value_sends_the_reader_to_the_guess()
    {
        var reported = Assert.Single(Report("        curse.Active = true;"));

        Assert.Equal("READYM017", reported.Id);
        Assert.Contains("PredictActive", reported.GetMessage());
        Assert.DoesNotContain("Pull", reported.GetMessage());
    }

    /// <summary>Its unmarked neighbour has nowhere else to go, so it is sent where it always was.</summary>
    [Fact]
    public void Writing_the_value_beside_it_sends_the_reader_to_a_mapping()
    {
        var reported = Assert.Single(Report("        curse.Turns = 3;"));

        Assert.Contains("Pull", reported.GetMessage());
        Assert.DoesNotContain("Predict", reported.GetMessage());
    }

    /// <summary>
    /// Guessing is a call of its own, so there is nothing to decide about where it was called from. Whether
    /// a write is a guess is a question about control flow, which is why the setter does not answer it.
    /// </summary>
    [Fact]
    public void Guessing_at_it_is_left_alone_wherever_it_is_done()
    {
        Assert.Empty(Report("        curse.PredictActive(true);"));
        Assert.Empty(Report("        raise.Predicting(() => curse.PredictActive(true));"));
        Assert.Empty(Report("        npc.PredictCurseActive(true);"));
    }

    /// <summary>The server writes the value itself, so nothing there is a guess.</summary>
    [Fact]
    public void A_server_writes_a_guessable_value_like_any_other()
        => Assert.Empty(Report("        curse.Active = true;", runtime: "Server"));

    [Fact]
    public void Reading_a_guessable_value_is_left_alone()
        => Assert.Empty(Report("        var read = curse.Active;"));

    /// <summary>
    /// Inside a mapping the write is the pull, which is how the game's answer reaches the ECS. The
    /// same write a step outside it is not, which is what tells the two apart.
    /// </summary>
    [Fact]
    public void A_mapping_handler_writes_the_whole_shape()
    {
        var reported = ReportSource(
            """
            using ReadyM.SDK.Client.Mapping;

            namespace Mod;

            public sealed class Dial
            {
                public int Reading { get; set; }
            }

            public sealed class Mappings : IShapeMappings
            {
                public void Register(IShapeMappingRegistry registry)
                    => registry.For<Vitals, Dial>()
                        .Map(Vitals.Field.All, (vitals, dial) => vitals.Hp = dial.Reading);

                public static void Meddle(Vitals vitals, Dial dial) => vitals.Hp = dial.Reading;
            }
            """);

        var meddling = Assert.Single(reported);

        Assert.Contains("Meddle", meddling.Location.SourceTree?.GetText().ToString()
            .Substring(meddling.Location.SourceSpan.Start - 60, 60) ?? string.Empty);
    }
}
