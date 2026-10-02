using System.Reflection;
using Microsoft.CodeAnalysis;
using ReadyM.Api.Generators.Services;
using Xunit;

namespace ReadyM.Api.Generators.Tests.Services;

/// What [UpdateOrder] puts in the registration, and the constraints it refuses outright. A cycle is
/// caught here rather than at load, since an edge is a typeof and one spanning assemblies would need
/// those assemblies to reference each other.
public class UpdateOrderTests(ITestOutputHelper output)
{
    private static readonly Assembly[] Sdk = [typeof(SDK.Attributes.ServiceAttribute).Assembly];

    private static string Source(string services) =>
        $$"""
          using ReadyM.SDK.Attributes;

          namespace Mod;

          {{services}}
          """;

    private string Generated(string services)
    {
        var result = SourceGeneratorTestHelper.RunGenerators(
            [("Services.cs", Source(services))], [new ServiceGenerator()], output, Sdk);

        return string.Concat(result.GeneratedSyntaxTrees.Select(tree => tree.ToString()));
    }

    private Diagnostic[] Reported(string services)
    {
        var result = SourceGeneratorTestHelper.RunGenerators(
            [("Services.cs", Source(services))], [new ServiceGenerator()], output, Sdk);

        return [.. SourceGeneratorTestHelper.Analyze(result.OutputCompilation, new ServiceAnalyzer())];
    }

    private const string Plain = """
        [Service]
        public sealed partial class Hud
        {
            private void Update() { }
        }
        """;

    // -- what it writes ----------------------------------------------------------------------------

    /// A service that says nothing registers as it always did, so nothing carries an order it never set.
    [Fact]
    public void Saying_nothing_registers_without_an_order()
        => Assert.Contains("ServiceRegistry.Register<global::Mod.Hud>();", Generated(Plain));

    [Fact]
    public void A_priority_reaches_the_registration()
        => Assert.Contains(
            "ServiceRegistry.Register<global::Mod.Physics>(0, null, null);",
            Generated(Plain + """

                [Service]
                public sealed partial class Physics
                {
                    [UpdateOrder(0)]
                    private void Update() { }
                }
                """));

    [Fact]
    public void A_constraint_reaches_the_registration()
        => Assert.Contains(
            "typeof(global::Mod.Hud)",
            Generated(Plain + """

                [Service]
                public sealed partial class Physics
                {
                    [UpdateOrder(Before = new[] { typeof(Hud) })]
                    private void Update() { }
                }
                """));

    // -- what it refuses ---------------------------------------------------------------------------

    private void AssertReports(string id, string services)
    {
        var reported = Reported(services);

        Assert.Contains(reported, diagnostic => diagnostic.Id == id);
        Assert.All(reported.Where(d => d.Id == id), d => Assert.Equal(DiagnosticSeverity.Error, d.Severity));
    }

    /// Ordering against something that never ticks reads as though it does something.
    [Fact]
    public void Ordering_against_a_non_service_is_refused()
        => AssertReports("READYM033", """
            public sealed class NotAService { }

            [Service]
            public sealed partial class Physics
            {
                [UpdateOrder(After = new[] { typeof(NotAService) })]
                private void Update() { }
            }
            """);

    [Fact]
    public void Ordering_against_a_service_that_does_not_update_is_refused()
        => AssertReports("READYM033", """
            [Service]
            public sealed partial class Quiet { }

            [Service]
            public sealed partial class Physics
            {
                [UpdateOrder(After = new[] { typeof(Quiet) })]
                private void Update() { }
            }
            """);

    /// There is no update to order, so the attribute says nothing and looks as though it does.
    [Fact]
    public void An_order_on_a_service_that_does_not_update_is_refused()
        => AssertReports("READYM034", """
            [Service]
            public sealed partial class Quiet
            {
                [UpdateOrder(0)]
                private void NotAnUpdate() { }
            }
            """);

    /// No order satisfies them, so it is refused where it is written rather than thrown at load.
    [Fact]
    public void A_cycle_is_refused()
        => AssertReports("READYM035", """
            [Service]
            public sealed partial class A
            {
                [UpdateOrder(Before = new[] { typeof(B) })]
                private void Update() { }
            }

            [Service]
            public sealed partial class B
            {
                [UpdateOrder(Before = new[] { typeof(A) })]
                private void Update() { }
            }
            """);

    /// Both ends are marked, so the editor shows the loop wherever you are standing in it.
    [Fact]
    public void Every_service_in_a_cycle_is_marked()
    {
        var reported = Reported("""
            [Service]
            public sealed partial class A
            {
                [UpdateOrder(Before = new[] { typeof(B) })]
                private void Update() { }
            }

            [Service]
            public sealed partial class B
            {
                [UpdateOrder(Before = new[] { typeof(A) })]
                private void Update() { }
            }
            """);

        Assert.Equal(2, reported.Count(d => d.Id == "READYM035"));
    }
}
