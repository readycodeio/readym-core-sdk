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
