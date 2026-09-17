using System.Reflection;
using Microsoft.CodeAnalysis;
using ReadyM.Api.Generators.Archetypes;
using Xunit;

namespace ReadyM.Api.Generators.Tests.Archetypes;

/// <summary>
/// The archetype generators run against the SDK itself, rather than against stand-ins.
/// </summary>
/// <remarks>
/// Every other test here declares its own attributes, so the generator matches them whatever the
/// real ones are called. That is the gap this closes: the generator names the SDK's attributes and
/// types as strings, and when the SDK moved them the generator matched nothing and emitted nothing
/// for an entire compilation, with no diagnostic and a green suite. Only a compilation that
/// references the real assemblies can notice. Twice now a rename has cost an afternoon.
/// </remarks>
public class GeneratorAgainstTheSdkTests(ITestOutputHelper output)
{
    /// The assemblies a mod is compiled against, both halves, as a test host sees them.
    private static readonly Assembly[] Sdk =
    [
        typeof(SDK.Attributes.ArchetypeAttribute).Assembly,
        typeof(SDK.Chunks.ChunkSlot).Assembly,
        typeof(SDK.Server.Entity.IEntities).Assembly,
        typeof(SDK.Client.Entity.IEntities).Assembly
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

    private SourceGeneratorTestHelper.GeneratorRunResult Run(string source = Declarations)
        => SourceGeneratorTestHelper.RunGenerators(
            [("Declarations.cs", source)],
            [new ArchetypeGenerator(), new ArchetypeMixinGenerator(), new ChunkCombinationGenerator()],
            output,
            Sdk);

    /// <summary>
    /// Nothing at all is what a name drifting looks like, so it is worth its own assertion: the
    /// message then says the generator matched nothing rather than that some member is missing.
    /// </summary>
    [Fact]
    public void The_generators_emit_for_declarations_written_against_the_real_attributes()
    {
        var result = Run();

        // Unimplemented partial members are what the generator is about to supply, so before it runs
        // they are the one expected error. Anything else means the declarations missed the SDK.
        AssertNoErrors(
            result.InputDiagnostics.Where(diagnostic => diagnostic.Id != "CS9248"),
            "the declarations do not compile against the SDK");

        Assert.NotEmpty(result.GeneratedSyntaxTrees);
    }

    [Theory]
    [InlineData("PositionComponent")]
    [InlineData("PositionAccessors")]
    [InlineData("NpcComponent")]
    [InlineData("NpcAccessors")]
    [InlineData("NpcArchetypeMarker")]
    public void Each_part_of_a_shape_is_generated(string expected)
        => Assert.Contains(expected, Generated(Run()));

    /// <summary>
    /// What guards the type names the generators write into their output. They are strings, so a
    /// rename in the SDK leaves them pointing at nothing, and only compiling the result notices.
    /// </summary>
    [Fact]
    public void The_generated_code_compiles_against_the_real_sdk()
    {
        var result = Run();

        AssertNoErrors(result.OutputDiagnostics, "the generated code does not compile against the SDK");
    }

    // -- the chunk fast path ----------------------------------------------------------------------

    /// <summary>
    /// The fast path turning itself off is silent by design: a shape without a view still compiles
    /// and still walks, just by identity. Asserting the view exists is what makes it loud.
    /// </summary>
    [Theory]
    [InlineData("NpcView")]
    [InlineData("PositionView")]
    public void A_shape_in_scope_of_the_chunk_types_gets_a_view(string expected)
        => Assert.Contains(expected, Generated(Run()));

    [Fact]
    public void The_generator_does_not_report_the_fast_path_as_disabled()
    {
        var reported = Run().DriverRunResult.Diagnostics;

        Assert.DoesNotContain(reported, diagnostic => diagnostic.Id == "READYM001");
    }

    /// <summary>
    /// A loop written the way a mod writes one, compiled. It binds a generated GetEnumerator, so it
    /// fails if the binding was not emitted for the half the query came from.
    /// </summary>
    [Theory]
    [InlineData("entities.Query<Npc>()", "npc", "_ = npc.X;")]
    [InlineData("entities.Query<Position, Vitals>()", "(position, vitals)", "_ = position.X + vitals.Hp;")]
    public void A_query_a_mod_would_write_compiles(string query, string binding, string body)
    {
        var loop =
            $$"""
              namespace Mod;

              public static class Use
              {
                  public static void Run(ReadyM.SDK.Server.Entity.IEntities entities)
                  {
                      foreach (var {{binding}} in {{query}})
                          {{body}}
                  }
              }
              """;

        var result = SourceGeneratorTestHelper.RunGenerators(
            [("Declarations.cs", Declarations), ("Use.cs", loop)],
            [new ArchetypeGenerator(), new ArchetypeMixinGenerator(), new ChunkCombinationGenerator()],
            output,
            Sdk);

        AssertNoErrors(result.OutputDiagnostics, "the query does not compile");
    }

    private static string Generated(SourceGeneratorTestHelper.GeneratorRunResult result)
        => string.Join("\n", result.GeneratedSyntaxTrees.Select(tree => tree.GetText().ToString()));

    private static void AssertNoErrors(IEnumerable<Diagnostic> diagnostics, string what)
    {
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();

        Assert.True(errors.Length == 0, $"{what}:\n{string.Join("\n", errors.Select(e => e.ToString()))}");
    }
}
