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

        [ArchetypeMixin]
        [Replicated]
        [Propagates(Propagation.OwnershipBased)]
        public readonly partial struct Vitals
        {
            public partial int Hp { get; set; }
        }

        [ArchetypeMixin]
        public readonly partial struct Mood
        {
            public partial int Cheer { get; set; }
        }

        [Archetype]
        [Include(typeof(Vitals))]
        [Include(typeof(Mood))]
        public readonly partial struct Npc;
        """;

    /// <param name="body">Statements in a method holding a 'vitals' and a 'mood'.</param>
    private Diagnostic[] Report(string body, string runtime = "Client")
        => ReportSource(
            $$"""
              namespace Mod;

              public static class Use
              {
                  public static void Run(Vitals vitals, Mood mood)
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

        return [.. reported.Where(diagnostic => diagnostic.Id == "READYM017")];
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

    /// Nothing sends it, so nobody is kept from writing it.
    [Fact]
    public void A_local_value_is_left_alone()
        => Assert.Empty(Report("        mood.Cheer = 5;"));

    /// Reading says nothing about who owns the value.
    [Fact]
    public void Reading_is_left_alone()
        => Assert.Empty(Report("        var read = vitals.Hp;"));

    /// The server is the one deciding, and the API saying otherwise is not compiled for it.
    [Fact]
    public void A_server_writes_what_it_likes()
        => Assert.Empty(Report("        vitals.Hp = 5;", runtime: "Server"));

    /// Inside a mapping the write is the pull, which is how the game's answer reaches the ECS. The
    /// same write a step outside it is not, which is what tells the two apart.
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
