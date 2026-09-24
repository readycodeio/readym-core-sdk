using System.Reflection;
using Microsoft.CodeAnalysis;
using ReadyM.Api.Generators.Services;
using Xunit;

namespace ReadyM.Api.Generators.Tests.Services;

/// What [Service] makes of a class, and what it refuses. The update and the two ends of the lifetime
/// are found by their name rather than by an attribute, so what counts as one is worth pinning from
/// both sides. The analyzer is what says so, since a generator's diagnostics never reach the editor.
public class ServiceGeneratorTests(ITestOutputHelper output)
{
    private static readonly Assembly[] Sdk = [typeof(SDK.Attributes.ServiceAttribute).Assembly];

    private static string Source(string service) =>
        $$"""
          using ReadyM.SDK.Attributes;
          using ReadyM.SDK.Services;

          namespace Mod;

          [Archetype]
          public readonly partial struct Subject
          {
              public partial int Mark { get; set; }
          }

          [ArchetypeMixin]
          public readonly partial struct Nothing;

          {{service}}
          """;

    private string Generated(string service)
    {
        var result = SourceGeneratorTestHelper.RunGenerators(
            [("Service.cs", Source(service))], [new ServiceGenerator()], output, Sdk);

        return string.Concat(result.GeneratedSyntaxTrees.Select(tree => tree.ToString()));
    }

    private Diagnostic[] Reported(string service)
    {
        var result = SourceGeneratorTestHelper.RunGenerators(
            [("Service.cs", Source(service))], [new ServiceGenerator()], output, Sdk);

        return [.. SourceGeneratorTestHelper.Analyze(result.OutputCompilation, new ServiceAnalyzer())];
    }

    private void AssertReports(string id, string service)
    {
        var reported = Reported(service);

        Assert.Contains(reported, diagnostic => diagnostic.Id == id);
        Assert.All(reported.Where(d => d.Id == id), d => Assert.Equal(DiagnosticSeverity.Error, d.Severity));

        // Nothing is written for a class the analyzer refuses, so the two never disagree.
        Assert.Empty(Generated(service));
    }

    [Fact]
    public void A_service_updates_through_the_contract_a_game_holds()
    {
        var generated = Generated("""
            [Service]
            public sealed partial class Regeneration
            {
                private void Update() { }
            }
            """);

        Assert.Contains("partial class Regeneration : global::ReadyM.SDK.Services.IUpdatingService", generated);
        Assert.Contains("Update();", generated);
        Assert.Contains("ServiceRegistry.Register<global::Mod.Regeneration>()", generated);
    }

    /// An update takes nothing. The clock arrives as a property the service reads when it wants it,
    /// which is where a Unity author already looks for it.
    [Fact]
    public void A_service_reads_the_clock_off_itself()
    {
        var generated = Generated("""
            [Service]
            public sealed partial class Counting
            {
                private void Update() { }
            }
            """);

        Assert.Contains("private global::ReadyM.SDK.Services.UpdateTime Time { get; set; }", generated);
        Assert.Contains("Time = time;", generated);
    }

    /// Nothing is given a clock it never runs on.
    [Fact]
    public void A_service_that_does_not_update_has_no_clock()
        => Assert.DoesNotContain("Time", Generated("""
            [Service]
            public sealed partial class Settings
            {
                private void Start() { }
            }
            """));

    /// Nothing runs on it, but it is still one instance anything can ask for.
    [Fact]
    public void A_service_with_nothing_to_run_is_still_registered()
    {
        var generated = Generated("""
            [Service]
            public sealed partial class Settings
            {
                public int Difficulty { get; set; }
            }
            """);

        Assert.Contains("ServiceRegistry.Hold<global::Mod.Settings>()", generated);
        Assert.DoesNotContain("IUpdatingService", generated);
        Assert.DoesNotContain("IHostedService", generated);
    }

    [Fact]
    public void A_service_that_is_not_partial_is_refused()
        => AssertReports("READYM022", """
            [Service]
            public sealed class Regeneration
            {
                private void Update() { }
            }
            """);

    /// The SDK builds one of it under its own name, so a second one down a derived type would never
    /// be reached.
    [Fact]
    public void A_service_that_is_not_sealed_is_refused()
        => AssertReports("READYM023", """
            [Service]
            public partial class Regeneration
            {
                private void Update() { }
            }
            """);

    /// A service asks for what it needs in its constructor and reads the clock off itself, so an
    /// update takes nothing at all.
    [Fact]
    public void An_update_asking_for_anything_is_refused()
        => AssertReports("READYM024", """
            [Service]
            public sealed partial class Regeneration
            {
                private void Update(float deltaTime) { }
            }
            """);

    [Fact]
    public void Nor_one_returning_something()
        => AssertReports("READYM024", """
            [Service]
            public sealed partial class Regeneration
            {
                private int Update() => 1;
            }
            """);

    [Fact]
    public void Nor_a_static_one()
        => AssertReports("READYM024", """
            [Service]
            public sealed partial class Regeneration
            {
                private static void Update() { }
            }
            """);

    /// Only the SDK calls it, and the generated half sits inside the class, so nothing is gained by
    /// letting anyone else in.
    [Fact]
    public void Nor_a_public_one()
        => AssertReports("READYM024", """
            [Service]
            public sealed partial class Regeneration
            {
                public void Update() { }
            }
            """);

    [Fact]
    public void A_service_with_both_ends_is_hosted()
    {
        var generated = Generated("""
            [Service]
            public sealed partial class Bookends
            {
                private void Start() { }

                private void Stop() { }
            }
            """);

        Assert.Contains("partial class Bookends : global::ReadyM.Api.DI.IHostedService", generated);
        Assert.Contains("global::ReadyM.Api.DI.IHostedService.OnScopeStart()", generated);
        Assert.Contains("=> Start();", generated);
        Assert.Contains("global::System.IDisposable.Dispose()", generated);
        Assert.Contains("=> Stop();", generated);
    }

    /// Either end alone is enough, and the interface still wants the other one.
    [Fact]
    public void A_service_with_only_a_start_is_hosted_and_disposes_of_nothing()
    {
        var generated = Generated("""
            [Service]
            public sealed partial class StartOnly
            {
                private void Start() { }
            }
            """);

        Assert.Contains("partial class StartOnly : global::ReadyM.Api.DI.IHostedService", generated);
        Assert.Contains("=> Start();", generated);
        Assert.DoesNotContain("=> Stop();", generated);
        Assert.Contains("global::System.IDisposable.Dispose()", generated);
    }

    /// An update and a lifetime are separate things, and a service may hold both.
    [Fact]
    public void A_service_may_update_and_be_hosted()
    {
        var generated = Generated("""
            [Service]
            public sealed partial class Both
            {
                private void Start() { }

                private void Update() { }
            }
            """);

        Assert.Contains(
            "partial class Both : global::ReadyM.SDK.Services.IUpdatingService, global::ReadyM.Api.DI.IHostedService",
            generated);
        Assert.Contains("ServiceRegistry.Register<global::Mod.Both>()", generated);
    }

    [Fact]
    public void A_start_taking_anything_is_refused()
        => AssertReports("READYM024", """
            [Service]
            public sealed partial class Regeneration
            {
                private void Start(float seconds) { }
            }
            """);

    [Fact]
    public void A_stop_that_is_not_private_is_refused()
        => AssertReports("READYM024", """
            [Service]
            public sealed partial class Regeneration
            {
                internal void Stop() { }
            }
            """);

    /// A service that only watches a shape needs no update and no lifetime.
    [Fact]
    public void A_service_with_only_a_create_handler_is_accepted()
        => Assert.Empty(Reported("""
            [Service]
            public sealed partial class Bookkeeping
            {
                [CreateHandler(typeof(Subject))]
                private void Track(Subject subject) { }
            }
            """));

    /// It has nothing to tick, so it is held rather than added to the update list.
    [Fact]
    public void A_service_with_only_a_create_handler_does_not_update()
    {
        var generated = Generated("""
            [Service]
            public sealed partial class Bookkeeping
            {
                [CreateHandler(typeof(Subject))]
                private void Track(Subject subject) { }
            }
            """);

        Assert.Contains("ServiceRegistry.Hold<global::Mod.Bookkeeping>()", generated);
        Assert.Contains("global::Mod.Subject.ObserveCreated(", generated);
        Assert.DoesNotContain("IUpdatingService", generated);
    }

    /// One service may watch a shape more than once, and update as well.
    [Fact]
    public void A_service_may_update_and_watch()
    {
        var generated = Generated("""
            [Service]
            public sealed partial class Both
            {
                private void Update() { }

                [CreateHandler(typeof(Subject))]
                private void First(Subject subject) { }

                [CreateHandler(typeof(Subject))]
                private void Second(Subject subject) { }
            }
            """);

        Assert.Contains("ServiceRegistry.Register<global::Mod.Both>()", generated);
        Assert.Contains(".First(new global::Mod.Subject(handle))", generated);
        Assert.Contains(".Second(new global::Mod.Subject(handle))", generated);
    }

    /// A handler takes the shape it watches and nothing else.
    [Fact]
    public void A_create_handler_taking_the_wrong_thing_is_refused()
        => AssertReports("READYM025", """
            [Service]
            public sealed partial class Bookkeeping
            {
                [CreateHandler(typeof(Subject))]
                private void Track(int other) { }
            }
            """);

    /// A shape's own handler watches itself. One sitting in a service has to say what it watches,
    /// and would otherwise be built and never called.
    [Fact]
    public void A_create_handler_naming_no_shape_is_refused()
        => AssertReports("READYM026", """
            [Service]
            public sealed partial class Bookkeeping
            {
                [CreateHandler]
                private void Track(Subject subject) { }
            }
            """);

    /// A type that is not a shape carries no watch point, and nothing would ever call the handler.
    [Fact]
    public void A_create_handler_watching_something_that_is_not_a_shape_is_refused()
        => AssertReports("READYM026", """
            [Service]
            public sealed partial class Bookkeeping
            {
                [CreateHandler(typeof(string))]
                private void Track(string other) { }
            }
            """);

    /// It is exactly what it includes, so nothing appearing on an entity says it arrived.
    [Fact]
    public void A_create_handler_watching_a_shape_that_holds_nothing_of_its_own_is_refused()
        => AssertReports("READYM027", """
            [Service]
            public sealed partial class Bookkeeping
            {
                [CreateHandler(typeof(Nothing))]
                private void Track(Nothing nothing) { }
            }
            """);

    /// A class nobody declared a service is nobody's business, whatever it is called.
    [Fact]
    public void A_class_that_is_not_a_service_is_left_alone()
        => Assert.Empty(Reported("""
            public class Regeneration
            {
                private int Update() => 1;
            }
            """));
}
