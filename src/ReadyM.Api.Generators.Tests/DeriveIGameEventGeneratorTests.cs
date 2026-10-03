using System.Globalization;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Xunit;

namespace ReadyM.Api.Generators.Tests;

public sealed class DeriveIGameEventGeneratorTests(ITestOutputHelper output)
{
    // Fakes of what the generated methods read, with the real names, so each test sets who owns the subject and
    // who is master.
    private const string ContextStubs = """
#pragma warning disable CS0436
namespace ReadyM.Relay.Client.State
{
    public sealed class ClientOwnershipManager(bool owns)
    {
        public bool OwnsEntity(global::Friflo.Engine.ECS.Entity entity) => owns;
        public bool OwnsEntity(global::Friflo.Engine.ECS.RawEntity entity) => owns;
    }

    public sealed class ClientState(bool master)
    {
        public bool IsMasterClient => master;
    }
}
""";

    private const string Events = """
using Friflo.Engine.ECS;
using ReadyM.Api.Mapping.Events;

namespace GameEventTests;

[DeriveIGameEvent, OwnershipBased(nameof(Attacker))]
public partial struct OwnershipRaw { public RawEntity Attacker; public int Damage; }

[DeriveIGameEvent, OwnershipBased(nameof(Subject))]
public readonly partial struct OwnershipEntity(Entity subject) { public readonly Entity Subject = subject; }

[DeriveIGameEvent, AlwaysPropagates]
public partial struct Always { public int Value; }

[DeriveIGameEvent, AlwaysPropagatesToEcsOnly]
public partial struct ToEcsOnly { public int Value; }

[DeriveIGameEvent, AlwaysPropagatesToGameOnly]
public partial struct ToGameOnly { public int Value; }

[DeriveIGameEvent, MasterClientManaged]
public readonly partial struct Master { public readonly int Value; }

[DeriveIGameEvent, RunOnMasterClientOnly(nameof(Owner))]
public partial struct RunOnMaster { public RawEntity Owner; }

public static class Probe
{
    private sealed class Registration(System.Action<ReadyM.Api.ECS.Registry.IAllTypeRegistry> register)
        : ReadyM.Api.ECS.Registry.IAllTypeRegistration
    {
        public void Register(ReadyM.Api.ECS.Registry.IAllTypeRegistry registry) => register(registry);
    }

    private sealed class Source(bool owns, bool master) : IGameEventContextSource
    {
        public T Get<T>() where T : class
            => typeof(T) == typeof(ReadyM.Relay.Client.State.ClientOwnershipManager)
                ? (T)(object)new ReadyM.Relay.Client.State.ClientOwnershipManager(owns)
                : (T)(object)new ReadyM.Relay.Client.State.ClientState(master);
    }

    public static string Ask<TEvent>(TEvent ev, bool owns, bool master) where TEvent : struct, IGameEvent
    {
        var types = new ReadyM.Api.ECS.Registry.AllTypeRegistry([new Registration(r => r.RegisterEvent<TEvent>())]);
        var contexts = new GameEventContextRegistry(types, new Source(owns, master));
        return $"{ev.CanGameEventNotifyEcs(contexts)} {ev.CanGameEventRunLocally(contexts)} {ev.CanEcsInvokeGameEvent(contexts)}";
    }

    public static string Ask(string name, bool owns, bool master) => name switch
    {
        nameof(OwnershipRaw) => Ask(new OwnershipRaw(), owns, master),
        nameof(OwnershipEntity) => Ask(new OwnershipEntity(default), owns, master),
        nameof(Always) => Ask(new Always(), owns, master),
        nameof(ToEcsOnly) => Ask(new ToEcsOnly(), owns, master),
        nameof(ToGameOnly) => Ask(new ToGameOnly(), owns, master),
        nameof(Master) => Ask(new Master(), owns, master),
        nameof(RunOnMaster) => Ask(new RunOnMaster(), owns, master),
        _ => throw new System.ArgumentOutOfRangeException(nameof(name), name, null),
    };
}
""";

    private Func<string, bool, bool, string> CompileProbe()
    {
        var result = SourceGeneratorTestHelper.RunGeneratorSeeingInternals<DeriveIGameEventGenerator>(
            [("Stubs.cs", ContextStubs), ("Events.cs", Events)], output);

        var errors = result.OutputDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);

        var assembly = SourceGeneratorTestHelper.EmitToAssembly(result.OutputCompilation, output);
        var ask = assembly.GetType("GameEventTests.Probe")!
            .GetMethod("Ask", BindingFlags.Public | BindingFlags.Static, [typeof(string), typeof(bool), typeof(bool)])!;
        return (name, owns, master) => (string)ask.Invoke(null, [name, owns, master])!;
    }

    [Fact]
    public void EveryDiscriminatorAnswersTheTable()
    {
        var ask = CompileProbe();
        var cases = new (string Event, bool Owns, bool Master, string Expected)[]
        {
            ("OwnershipRaw", true, false, "Notify RunAll DontRun"),
            ("OwnershipRaw", false, false, "DontNotify Rejected RunAll"),
            ("OwnershipRaw", true, true, "Notify RunAll DontRun"),
            ("OwnershipRaw", false, true, "DontNotify Rejected RunAll"),
            ("OwnershipEntity", true, false, "Notify RunAll DontRun"),
            ("OwnershipEntity", false, false, "DontNotify Rejected RunAll"),

            ("Always", false, false, "Notify RunAll RunAll"),
            ("Always", true, true, "Notify RunAll RunAll"),
            ("ToEcsOnly", false, false, "Notify RunAll DontRun"),
            ("ToEcsOnly", true, true, "Notify RunAll DontRun"),
            ("ToGameOnly", false, false, "DontNotify RunAll RunAll"),
            ("ToGameOnly", true, true, "DontNotify RunAll RunAll"),

            ("Master", false, true, "Notify RunAll DontRun"),
            ("Master", true, true, "Notify RunAll DontRun"),
            ("Master", false, false, "DontNotify Rejected RunAll"),
            ("Master", true, false, "DontNotify Rejected RunAll"),

            ("RunOnMaster", true, true, "DontNotify RunAll RunAll"),
            ("RunOnMaster", false, true, "DontNotify RunAll RunAll"),
            ("RunOnMaster", true, false, "Notify DontRun DontRun"),
            ("RunOnMaster", false, false, "DontNotify Rejected DontRun"),
        };

        foreach (var (name, owns, master, expected) in cases)
        {
            Assert.True(expected == ask(name, owns, master),
                $"{name} owns={owns} master={master}: expected '{expected}', got '{ask(name, owns, master)}'");
        }
    }

    [Fact]
    public void GeneratedPartOfAReadonlyStructIsReadonly()
    {
        var result = SourceGeneratorTestHelper.RunGeneratorSeeingInternals<DeriveIGameEventGenerator>(
            [("Stubs.cs", ContextStubs), ("Events.cs", Events)], output);

        var master = result.GeneratedSyntaxTrees.Single(t => t.FilePath.Contains("GameEventTests.Master."));
        Assert.Contains("readonly partial struct Master : global::ReadyM.Api.Mapping.Events.IGameEvent", master.ToString());
        var raw = result.GeneratedSyntaxTrees.Single(t => t.FilePath.Contains("GameEventTests.OwnershipRaw."));
        Assert.DoesNotContain("readonly partial struct", raw.ToString());
    }

    private string[] GeneratorErrors(string eventSource)
    {
        var result = SourceGeneratorTestHelper.RunGeneratorSeeingInternals<DeriveIGameEventGenerator>(
            [("Stubs.cs", ContextStubs), ("Event.cs", eventSource)], output);
        return result.OutputDiagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => d.GetMessage(CultureInfo.InvariantCulture))
            .ToArray();
    }

    [Fact]
    public void SubjectNamingAMissingFieldIsAnError()
    {
        var errors = GeneratorErrors("""
using Friflo.Engine.ECS;
using ReadyM.Api.Mapping.Events;
namespace GameEventTests;
[DeriveIGameEvent, OwnershipBased("Attacker")]
public partial struct Broken { public RawEntity Target; }
""");
        Assert.Contains(errors, e => e.Contains("#error") && e.Contains("Broken") && e.Contains("'Attacker'"));
    }

    [Fact]
    public void SubjectOfAnotherTypeIsAnError()
    {
        var errors = GeneratorErrors("""
using System;
using ReadyM.Api.Mapping.Events;
namespace GameEventTests;
[DeriveIGameEvent, RunOnMasterClientOnly(nameof(Actor))]
public partial struct Broken { public IntPtr Actor; }
""");
        Assert.Contains(errors, e => e.Contains("#error") && e.Contains("Broken") && e.Contains("Entity or RawEntity"));
    }

    [Fact]
    public void TwoDiscriminatorsAreAnError()
    {
        var errors = GeneratorErrors("""
using ReadyM.Api.Mapping.Events;
namespace GameEventTests;
[DeriveIGameEvent, AlwaysPropagates, MasterClientManaged]
public partial struct Broken { public int Value; }
""");
        Assert.Contains(errors, e => e.Contains("#error") && e.Contains("Broken") && e.Contains("one discriminator"));
    }

    [Fact]
    public void ADiscriminatorWithoutDeriveIGameEventIsAnError()
    {
        var errors = GeneratorErrors("""
using ReadyM.Api.Mapping.Events;
namespace GameEventTests;
[AlwaysPropagates]
public partial struct Broken { public int Value; }
""");
        Assert.Contains(errors, e => e.Contains("#error") && e.Contains("Broken") && e.Contains("[DeriveIGameEvent]"));
    }

    [Fact]
    public void DeriveIGameEventWithoutADiscriminatorIsAnError()
    {
        var errors = GeneratorErrors("""
using ReadyM.Api.Mapping.Events;
namespace GameEventTests;
[DeriveIGameEvent]
public partial struct Broken { public int Value; }
""");
        Assert.Contains(errors, e => e.Contains("#error") && e.Contains("Broken") && e.Contains("one discriminator"));
    }

    [Fact]
    public void AnEventWithNeitherADiscriminatorNorItsOwnPolicyIsNotAGameEvent()
    {
        var errors = GeneratorErrors("""
using ReadyM.Api.Mapping.Events;
namespace GameEventTests;
public struct Plain { public int Value; }
public static class Use
{
    public static void Ask<T>(T ev) where T : struct, IGameEvent { }
    public static void Call() => Ask(new Plain());
}
""");
        Assert.Contains(errors, e => e.Contains("Plain") && e.Contains("IGameEvent"));
    }

    private static string Generated(SourceGeneratorTestHelper.GeneratorRunResult result, string name)
        => result.GeneratedSyntaxTrees.Single(t => t.FilePath.Contains(name)).ToString();

    [Fact]
    public void EachDiscriminatorAddsTheMarkersOfWhatItsMethodsRead()
    {
        var result = SourceGeneratorTestHelper.RunGeneratorSeeingInternals<DeriveIGameEventGenerator>(
            [("Stubs.cs", ContextStubs), ("Events.cs", Events)], output);

        const string owner = "global::ReadyM.Relay.Client.State.ClientOwnershipManager";
        const string state = "global::ReadyM.Relay.Client.State.ClientState";
        var expected = new (string Event, string[] Requires)[]
        {
            ("OwnershipRaw", [owner]), ("OwnershipEntity", [owner]), ("Master", [state]), ("RunOnMaster", [owner, state]),
            ("Always", []), ("ToEcsOnly", []), ("ToGameOnly", []),
        };

        foreach (var (name, requires) in expected)
        {
            var code = Generated(result, $"GameEventTests.{name}.");
            var markers = System.Text.RegularExpressions.Regex.Matches(code, @"IGameEventRequiresContext<([^>]+)>")
                .Select(m => m.Groups[1].Value).ToArray();
            var accepted = System.Text.RegularExpressions.Regex.Matches(code, @"visitor\.Accept<([^>]+)>\(\)")
                .Select(m => m.Groups[1].Value).ToArray();
            Assert.True(requires.SequenceEqual(markers), $"{name}: markers {string.Join(", ", markers)}");
            Assert.True(requires.SequenceEqual(accepted), $"{name}: accepted {string.Join(", ", accepted)}");
            Assert.Equal(requires.Length > 0, code.Contains("AcceptContexts"));
        }
    }

    [Fact]
    public void TheRegistrationListsEveryGameEventOfTheAssembly()
    {
        var result = SourceGeneratorTestHelper.RunGeneratorSeeingInternals<DeriveIGameEventGenerator>(
            [("Stubs.cs", ContextStubs), ("Events.cs", Events), ("HandWritten.cs", HandWrittenWithMarkers)], output);

        var registered = System.Text.RegularExpressions.Regex.Matches(Generated(result, "GameEventRegistration"), @"RegisterEvent<global::GameEventTests\.(\w+)>")
            .Select(m => m.Groups[1].Value).ToArray();
        Assert.Equal(["Always", "HandWrittenWithMarkers", "Master", "OwnershipEntity", "OwnershipRaw", "RunOnMaster", "ToEcsOnly", "ToGameOnly"], registered);
    }

    private const string HandWrittenWithMarkers = """
using ReadyM.Api.Mapping.Events;
namespace GameEventTests;
public sealed class Needed { }
public partial struct HandWrittenWithMarkers : IGameEvent, IGameEventRequiresContext<Needed>
{
    public GameEventNotifyResult CanGameEventNotifyEcs(GameEventContextRegistry contexts) => GameEventNotifyResult.Notify;
    public GameEventResult CanGameEventRunLocally(GameEventContextRegistry contexts) => GameEventResult.RunAll;
    public GameEventResult CanEcsInvokeGameEvent(GameEventContextRegistry contexts) => GameEventResult.RunAll;
}
""";

    [Fact]
    public void AHandWrittenEventWithMarkersGetsOnlyItsAcceptContexts()
    {
        var result = SourceGeneratorTestHelper.RunGeneratorSeeingInternals<DeriveIGameEventGenerator>(
            [("Stubs.cs", ContextStubs), ("HandWritten.cs", HandWrittenWithMarkers)], output);

        Assert.Empty(result.OutputDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        var code = Generated(result, "GameEventTests.HandWrittenWithMarkers.");
        Assert.Contains("visitor.Accept<global::GameEventTests.Needed>();", code);
        Assert.DoesNotContain("CanGameEventNotifyEcs", code);
    }

    [Fact]
    public void AHandWrittenEventWithMarkersMustBePartial()
    {
        var errors = GeneratorErrors("""
using ReadyM.Api.Mapping.Events;
namespace GameEventTests;
public sealed class Needed { }
public struct Broken : IGameEvent, IGameEventRequiresContext<Needed>
{
    public GameEventNotifyResult CanGameEventNotifyEcs(GameEventContextRegistry contexts) => GameEventNotifyResult.Notify;
    public GameEventResult CanGameEventRunLocally(GameEventContextRegistry contexts) => GameEventResult.RunAll;
    public GameEventResult CanEcsInvokeGameEvent(GameEventContextRegistry contexts) => GameEventResult.RunAll;
}
""");
        Assert.Contains(errors, e => e.Contains("Broken") && e.Contains("not partial"));
    }

    [Fact]
    public void AHandWrittenPolicyNeedsNoDiscriminator()
    {
        var errors = GeneratorErrors("""
using ReadyM.Api.Mapping.Events;
namespace GameEventTests;
public readonly struct HandWritten : IGameEvent
{
    public GameEventNotifyResult CanGameEventNotifyEcs(GameEventContextRegistry contexts) => GameEventNotifyResult.Notify;
    public GameEventResult CanGameEventRunLocally(GameEventContextRegistry contexts) => GameEventResult.RunAll;
    public GameEventResult CanEcsInvokeGameEvent(GameEventContextRegistry contexts) => GameEventResult.DontRun;
}
""");
        Assert.Empty(errors);
    }
}
