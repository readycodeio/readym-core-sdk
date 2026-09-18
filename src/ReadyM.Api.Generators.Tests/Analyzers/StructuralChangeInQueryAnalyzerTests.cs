using System.Reflection;
using Microsoft.CodeAnalysis;
using ReadyM.Api.Generators.Analyzers;
using ReadyM.Api.Generators.Archetypes;
using Xunit;

namespace ReadyM.Api.Generators.Tests.Analyzers;

/// <summary>
/// READYM002, run against the real SDK so the types it recognises are the ones a mod compiles against.
/// </summary>
/// <remarks>
/// It reads the loop as written, so what it does not catch matters as much as what it does. The cases
/// below fix both: the shapes that must be an error, and the shapes that must be left alone, because
/// an analyzer reporting an error a mod author cannot argue with has to be right.
/// </remarks>
public class StructuralChangeInQueryAnalyzerTests(ITestOutputHelper output)
{
    private static readonly Assembly[] Sdk =
    [
        typeof(SDK.Attributes.ArchetypeAttribute).Assembly,
        typeof(SDK.Chunks.ChunkSlot).Assembly,
        typeof(SDK.Server.Entity.IEntities).Assembly,
        typeof(SDK.Client.Entities.IEntities).Assembly
    ];

    private const string Declarations =
        """
        using ReadyM.SDK.Attributes;

        namespace Mod;

        [ArchetypeMixin]
        public readonly partial struct Position
        {
            public partial float X { get; set; }
        }

        [ArchetypeMixin]
        public readonly partial struct Vitals
        {
            public partial int Hp { get; set; }
        }

        [Archetype]
        [Include(typeof(Position))]
        [Include(typeof(Vitals))]
        public readonly partial struct Npc
        {
            public partial int Faction { get; set; }
        }
        """;

    /// <param name="body">Statements in a method holding a half's IEntities, named 'entities'.</param>
    private Diagnostic[] Report(string body, string half = "Server")
    {
        var entities = SourceGeneratorTestHelper.EntityNamespace(half);

        var use =
            $$"""
              namespace Mod;

              public static class Use
              {
                  public static void Run(ReadyM.SDK.{{half}}.{{entities}}.IEntities entities)
                  {
              {{body}}
                  }
              }
              """;

        var result = SourceGeneratorTestHelper.RunGenerators(
            [("Declarations.cs", Declarations), ("Use.cs", use)],
            [new ArchetypeGenerator(), new ArchetypeMixinGenerator(), new ChunkCombinationGenerator()],
            output,
            Sdk);

        var reported = SourceGeneratorTestHelper.Analyze(
            result.OutputCompilation,
            new StructuralChangeInQueryAnalyzer());

        return [.. reported.Where(diagnostic => diagnostic.Id == "READYM002")];
    }

    private void AssertReported(string body, string half = "Server")
    {
        var reported = Report(body, half);

        Assert.Single(reported);
        Assert.Equal(DiagnosticSeverity.Error, reported[0].Severity);
    }

    // -- what has to be an error ------------------------------------------------------------------

    [Fact]
    public void Creating_inside_a_query_loop_is_an_error()
        => AssertReported("""
                                  foreach (var npc in entities.Query<Npc>())
                                      entities.Create<Npc>();
                          """);

    [Fact]
    public void Creating_inside_a_deconstructing_loop_is_an_error()
        => AssertReported("""
                                  foreach (var (position, vitals) in entities.Query<Position, Vitals>())
                                      entities.Create<Npc>();
                          """);

    [Fact]
    public void Creating_inside_a_nested_query_loop_is_an_error()
        => AssertReported("""
                                  foreach (var npc in entities.Query<Npc>())
                                      foreach (var position in entities.Query<Position>())
                                          entities.Create<Npc>();
                          """);

    /// The client half is held to the same rule, and the analyzer names neither half specially.
    [Fact]
    public void The_rule_applies_to_the_client_half_too()
        => AssertReported("""
                                  foreach (var npc in entities.Query<Npc>())
                                      entities.Create<Npc>();
                          """, half: "Client");

    // -- what has to be left alone ----------------------------------------------------------------

    [Fact]
    public void Deleting_inside_a_query_loop_is_allowed()
        => Assert.Empty(Report("""
                                       foreach (var npc in entities.Query<Npc>())
                                           entities.Delete(npc.Handle);
                               """));

    [Fact]
    public void Creating_after_the_loop_is_allowed()
        => Assert.Empty(Report("""
                                       foreach (var npc in entities.Query<Npc>())
                                           _ = npc.Faction;

                                       entities.Create<Npc>();
                               """));

    [Fact]
    public void Creating_inside_a_loop_over_something_that_is_not_a_query_is_allowed()
        => Assert.Empty(Report("""
                                       foreach (var i in new System.Collections.Generic.List<int>())
                                           entities.Create<Npc>();
                               """));

    /// <summary>
    /// The limit, pinned so it stays a decision rather than a surprise: a lambda body may run
    /// anywhere, so reporting it would be a guess. The run-time refusal is what covers this.
    /// </summary>
    [Fact]
    public void Creating_inside_a_lambda_in_the_loop_is_left_to_the_run_time()
        => Assert.Empty(Report("""
                                       foreach (var npc in entities.Query<Npc>())
                                       {
                                           System.Action later = () => entities.Create<Npc>();
                                           later();
                                       }
                               """));
}
