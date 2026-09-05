using Friflo.Engine.ECS;
using Xunit;

namespace ReadyM.Api.Tests;

/// <summary>
/// The counterpart to the relay suite's guard tests. This process has a single creator and therefore leaves
/// repeated initialization at its default, so it is the one that can assert the STRICT behaviour: a repeat is
/// a hard failure here, shape match or not. It also creates its schema by the other mechanism, so between the
/// two suites both sources and both modes are covered.
/// </summary>
public class EcsSchemaGuardTests
{
    [Fact]
    public void SchemaWasCreatedFromLoadedAssemblies()
    {
        Assert.Equal(EntitySchemaSource.LoadedAssemblies, SchemaBootstrap.SchemaSource);
    }

    [Fact]
    public void ARepeatIsAHardFailureByDefault()
    {
        // This is a same-mechanism repeat, so unlike a mechanism conflict it is refused only because the
        // process has not opted into tolerating repeats. That is a precondition on process-global state, so
        // state it: if this assembly ever enables the flag, this test is asserting the wrong mode and should
        // say so rather than fail with a bare "did not throw".
        Assert.False(SchemaBootstrap.RepeatedInitializationAllowed,
            "this test asserts the strict default, so this assembly must not allow repeated initialization");

        // Same mechanism, and a rescan here would produce the same shape, yet it still fails: tolerating
        // repeats is opt-in, and this process has not opted in.
        var ex = Assert.Throws<InvalidOperationException>(
            () => SchemaBootstrap.InitializeFromLoadedAssemblies());

        Assert.Contains($"already created from {EntitySchemaSource.LoadedAssemblies}", ex.Message);
        Assert.Contains("duplicate initialization", ex.Message);

        Assert.Equal(EntitySchemaSource.LoadedAssemblies, SchemaBootstrap.SchemaSource);
    }

    [Fact]
    public void TheOtherMechanismIsRefusedAndNamesBoth()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => SchemaBootstrap.InitializeFromRegisteredTypes(new NativeAOT()));

        Assert.Contains($"already created from {EntitySchemaSource.LoadedAssemblies}", ex.Message);
        Assert.Contains($"{EntitySchemaSource.RegisteredTypes} tried to create it again", ex.Message);
        Assert.Contains("mutually exclusive", ex.Message);

        Assert.Equal(EntitySchemaSource.LoadedAssemblies, SchemaBootstrap.SchemaSource);
    }
}
