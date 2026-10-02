using System.Reflection;
using Microsoft.CodeAnalysis;
using ReadyM.Api.Generators.Services;
using Xunit;

namespace ReadyM.Api.Generators.Tests.Services;

/// The switch every [Service] carries, and the two hooks a service can declare to be told when it
/// is moved. Both are found by their name, the way the rest of a service's lifetime is.
public class ServiceSwitchTests(ITestOutputHelper output)
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

    /// What the compiler makes of the two halves together, which is what refuses an assignment the
    /// generated half never offered a setter for.
    private Diagnostic[] Compiled(string services)
    {
        var result = SourceGeneratorTestHelper.RunGenerators(
            [("Services.cs", Source(services))], [new ServiceGenerator()], output, Sdk);

        return [.. result.OutputDiagnostics];
    }

    private Diagnostic[] Reported(string services)
    {
        var result = SourceGeneratorTestHelper.RunGenerators(
            [("Services.cs", Source(services))], [new ServiceGenerator()], output, Sdk);

        return [.. SourceGeneratorTestHelper.Analyze(result.OutputCompilation, new ServiceAnalyzer())];
    }

    private const string Ticking = """
        [Service]
        public sealed partial class Hud
        {
            private void Update() { }
        }
        """;

    private const string Held = """
        [Service]
        public sealed partial class Settings
        {
            public int Difficulty { get; set; }
        }
        """;

    [Fact]
    public void A_service_that_ticks_carries_the_switch()
        => Assert.Contains("public bool Enabled => _switch.On;", Generated(Ticking));

    /// Including one with nothing to update, so a mod can switch off anything it can name.
    [Fact]
    public void A_service_with_nothing_to_update_carries_it_too()
        => Assert.Contains("public bool Enabled => _switch.On;", Generated(Held));

    /// A service that moved its own switch would skip its own OnEnabled and OnDisabled. Enabled has
    /// no setter, and what it reads is held in a ServiceSwitch only the SDK can move, so there is no
    /// field to write instead: the two halves of a partial class are one class.
    [Fact]
    public void A_service_cannot_switch_itself()
    {
        var reported = Compiled("""
            [Service]
            public sealed partial class Hud
            {
                private void Update() => Enabled = false;
            }
            """);

        Assert.Contains(reported, diagnostic => diagnostic.Id == "CS0200");
    }

    [Fact]
    public void A_service_with_nothing_to_update_is_still_a_service()
        => Assert.Contains("partial class Settings : global::ReadyM.SDK.Services.IService", Generated(Held));

    /// IUpdatingService is an IService, so one that ticks says the one and means both.
    [Fact]
    public void A_service_that_ticks_names_only_the_updating_one()
    {
        var generated = Generated(Ticking);

        Assert.Contains("partial class Hud : global::ReadyM.SDK.Services.IUpdatingService", generated);
        Assert.DoesNotContain("IUpdatingService, global::ReadyM.SDK.Services.IService", generated);
    }

    [Fact]
    public void A_disabled_service_runs_nothing_and_reads_no_clock()
    {
        var generated = Generated(Ticking);
        var update = generated[generated.IndexOf("IUpdatingService.Update", StringComparison.Ordinal)..];

        Assert.Contains("if (!Enabled)", update);
        Assert.True(
            update.IndexOf("if (!Enabled)", StringComparison.Ordinal)
            < update.IndexOf("Time = time;", StringComparison.Ordinal));
    }

    /// Nothing was declared, so there is nothing to tell it about and the switch moves on its own.
    [Fact]
    public void A_service_that_declared_neither_hook_has_nothing_to_run()
    {
        var generated = Generated(Ticking);

        Assert.Contains("IService.Switched(bool enabled)", generated);
        Assert.DoesNotContain("OnEnabled();", generated);
        Assert.DoesNotContain("OnDisabled();", generated);
    }

    [Fact]
    public void OnEnabled_is_called_on_the_way_up()
    {
        var generated = Generated(Hooks("private void OnEnabled() { }"));

        Assert.Contains("if (enabled)", generated);
        Assert.Contains("OnEnabled();", generated);
    }

    [Fact]
    public void OnDisabled_is_called_on_the_way_down()
    {
        var generated = Generated(Hooks("private void OnDisabled() { }"));

        Assert.Contains("if (!enabled)", generated);
        Assert.Contains("OnDisabled();", generated);
    }

    /// Both ends are two sides of one branch, so exactly one runs for a switch that moved.
    [Fact]
    public void Declaring_both_puts_them_on_the_two_sides_of_one_branch()
    {
        var generated = Generated(Hooks("""
            private void OnEnabled() { }

                private void OnDisabled() { }
            """));

        Assert.Contains("if (enabled)", generated);
        Assert.Contains("OnEnabled();", generated);
        Assert.Contains("OnDisabled();", generated);
        Assert.DoesNotContain("if (!enabled)", generated);
    }

    /// Or the first OnEnabled would never be reached: nothing builds a service that only ticks until
    /// the first tick, which is after the game said it was ready.
    [Fact]
    public void Declaring_OnEnabled_makes_a_service_started()
        => Assert.Contains(
            "global::ReadyM.SDK.Services.IStartedService.Start()",
            Generated(Hooks("private void OnEnabled() { }")));

    /// A service is set up before it is told it is about to run.
    [Fact]
    public void Start_comes_before_the_first_OnEnabled()
    {
        var generated = Generated(Hooks("""
            private void Start() { }

                private void OnEnabled() { }
            """));

        var scopeStart = generated[generated.IndexOf("IStartedService.Start()", StringComparison.Ordinal)..];

        Assert.True(
            scopeStart.IndexOf("Start();", StringComparison.Ordinal)
            < scopeStart.IndexOf("OnEnabled();", StringComparison.Ordinal));
    }

    /// Something may have switched it off while the mods were loading, which is before any of this.
    [Fact]
    public void The_first_OnEnabled_is_skipped_when_it_was_already_switched_off()
        => Assert.Contains("if (Enabled)", Generated(Hooks("private void OnEnabled() { }")));

    /// The handlers go with the update: a disabled service is not told about shapes coming or going
    /// either, so it does not act on a world it was not watching.
    [Fact]
    public void A_disabled_service_is_not_told_about_the_shapes_it_watches()
    {
        var generated = Generated(Watching);

        Assert.Equal(2, Occurrences(generated, "if (!service.Enabled)"));
    }

    /// The watch itself stays on, since taking it off and putting it back would move the handler to
    /// the end of the list every time a service is switched.
    [Fact]
    public void A_service_that_can_be_switched_still_registers_its_watches()
    {
        var generated = Generated(Watching);

        Assert.Contains("ObserveCreated", generated);
        Assert.Contains("ObserveDeleted", generated);
    }

    private const string Watching = """
        [Archetype]
        public readonly partial struct Parcel
        {
            public partial int Mark { get; set; }
        }

        [Service]
        public sealed partial class Bookkeeping
        {
            [CreateHandler(typeof(Parcel))]
            private void Arrived(Parcel parcel) { }

            [DeleteHandler(typeof(Parcel))]
            private void Left(Parcel parcel) { }
        }
        """;

    private static int Occurrences(string text, string looked)
    {
        var found = 0;

        for (var at = text.IndexOf(looked, StringComparison.Ordinal);
             at >= 0;
             at = text.IndexOf(looked, at + looked.Length, StringComparison.Ordinal))
            found++;

        return found;
    }

    /// The two hooks are read the way the rest of the lifetime is, so a shape they cannot be called
    /// in is refused rather than quietly ignored.
    [Theory]
    [InlineData("public void OnEnabled() { }")]
    [InlineData("private void OnEnabled(bool enabled) { }")]
    [InlineData("private bool OnEnabled() => true;")]
    [InlineData("private static void OnDisabled() { }")]
    public void A_hook_the_SDK_cannot_call_is_refused(string hook)
    {
        var reported = Reported(Hooks(hook));

        Assert.Contains(reported, diagnostic => diagnostic.Id == "READYM024");
        Assert.Empty(Generated(Hooks(hook)));
    }

    private static string Hooks(string members) =>
        $$"""
          [Service]
          public sealed partial class Hud
          {
              private void Update() { }

              {{members}}
          }
          """;
}
