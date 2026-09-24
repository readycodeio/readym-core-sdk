using System.Reflection;
using Microsoft.CodeAnalysis;
using ReadyM.Api.Generators.Systems;
using Xunit;

namespace ReadyM.Api.Generators.Tests.Systems;

/// <summary>
/// What [System] makes of a class, and what it refuses. The update is found by its shape rather than
/// by an attribute, so what counts as one is worth pinning from both sides.
/// </summary>
/// <remarks>
/// A system that is quietly never updated is the shape worth refusing: it compiles, it is built, and
/// nothing calls it. The analyzer is what says so, since a generator's diagnostics never reach the
/// editor.
/// </remarks>
public class SystemGeneratorTests(ITestOutputHelper output)
{
    private static readonly Assembly[] Sdk = [typeof(SDK.Attributes.SystemAttribute).Assembly];

    private static string Source(string system) =>
        $$"""
          using ReadyM.SDK.Attributes;
          using ReadyM.SDK.Systems;

          namespace Mod;

          {{system}}
          """;

    private string Generated(string system)
    {
        var result = SourceGeneratorTestHelper.RunGenerators(
            [("System.cs", Source(system))], [new SystemGenerator()], output, Sdk);

        return string.Concat(result.GeneratedSyntaxTrees.Select(tree => tree.ToString()));
    }

    private void AssertReports(string id, string system)
    {
        var result = SourceGeneratorTestHelper.RunGenerators(
            [("System.cs", Source(system))], [new SystemGenerator()], output, Sdk);

        var reported = SourceGeneratorTestHelper.Analyze(result.OutputCompilation, new SystemAnalyzer());

        Assert.Contains(reported, diagnostic => diagnostic.Id == id);
        Assert.All(reported.Where(d => d.Id == id), d => Assert.Equal(DiagnosticSeverity.Error, d.Severity));

        // Nothing is written for a class the analyzer refuses, so the two never disagree.
        Assert.Empty(Generated(system));
    }

    [Fact]
    public void A_system_updates_through_the_contract_a_game_holds()
    {
        var generated = Generated("""
            [System]
            public partial class Regeneration
            {
                private void Update(Tick tick) { }
            }
            """);

        Assert.Contains("partial class Regeneration : global::ReadyM.SDK.Systems.IModSystem", generated);
        Assert.Contains("=> Update(tick);", generated);
        Assert.Contains("SystemRegistry.Register<global::Mod.Regeneration>()", generated);
    }

    /// The tick is optional: a system that does not care about time need not name it.
    [Fact]
    public void An_update_that_did_not_ask_for_the_tick_is_called_without_one()
        => Assert.Contains("=> Update();", Generated("""
            [System]
            public partial class Counting
            {
                private void Update() { }
            }
            """));

    [Fact]
    public void A_system_that_is_not_partial_is_refused()
        => AssertReports("READYM022", """
            [System]
            public class Regeneration
            {
                private void Update() { }
            }
            """);

    /// It would be built and never called, which is the quiet failure worth a compile error.
    [Fact]
    public void A_system_with_no_update_is_refused()
        => AssertReports("READYM023", """
            [System]
            public partial class Regeneration
            {
                private void Regenerate() { }
            }
            """);

    /// Both shapes are callable, so which one was meant is nobody's to say.
    [Fact]
    public void A_system_with_two_callable_updates_is_refused()
        => AssertReports("READYM024", """
            [System]
            public partial class Regeneration
            {
                private void Update() { }

                private void Update(Tick tick) { }
            }
            """);

    /// A system asks for what it needs in its constructor, so an update takes the tick or nothing.
    [Fact]
    public void An_update_asking_for_anything_but_the_tick_is_refused()
        => AssertReports("READYM025", """
            [System]
            public partial class Regeneration
            {
                private void Update(Tick tick, string other) { }
            }
            """);

    [Fact]
    public void Nor_one_returning_something()
        => AssertReports("READYM025", """
            [System]
            public partial class Regeneration
            {
                private int Update() => 1;
            }
            """);

    [Fact]
    public void Nor_a_static_one()
        => AssertReports("READYM025", """
            [System]
            public partial class Regeneration
            {
                private static void Update() { }
            }
            """);

    /// A class nobody declared a system is nobody's business, whatever it is called.
    [Fact]
    public void A_class_that_is_not_a_system_is_left_alone()
    {
        var result = SourceGeneratorTestHelper.RunGenerators(
            [("System.cs", Source("""
                public class Regeneration
                {
                    private int Update() => 1;
                }
                """))],
            [new SystemGenerator()],
            output,
            Sdk);

        Assert.Empty(SourceGeneratorTestHelper.Analyze(result.OutputCompilation, new SystemAnalyzer()));
    }
}
