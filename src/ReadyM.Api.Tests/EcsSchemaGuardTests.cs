using Friflo.Engine.ECS;
using Xunit;

namespace ReadyM.Api.Tests;

/// <summary>
/// Mirror of the relay suite's guard test from the other side: this assembly's schema comes from an assembly
/// scan, so the explicit-registration mechanism must be refused here, and the message must say so.
/// </summary>
public class EcsSchemaGuardTests
{
    [Fact]
    public void SchemaWasCreatedFromLoadedAssemblies()
    {
        Assert.True(SchemaBootstrap.IsSchemaCreated);
        Assert.Equal(EntitySchemaSource.LoadedAssemblies, SchemaBootstrap.SchemaSource);
    }

    [Fact]
    public void CreatingFromRegisteredTypesIsRefusedAndNamesBothMechanisms()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => SchemaBootstrap.CreateFromRegisteredTypes(new NativeAOT()));

        Assert.Contains($"already created from {EntitySchemaSource.LoadedAssemblies}", ex.Message);
        Assert.Contains($"{EntitySchemaSource.RegisteredTypes} tried to create it again", ex.Message);
        Assert.Contains("mutually exclusive", ex.Message);

        Assert.Equal(EntitySchemaSource.LoadedAssemblies, SchemaBootstrap.SchemaSource);
    }

    [Fact]
    public void CreatingFromLoadedAssembliesTwiceIsRefusedAsADuplicate()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => SchemaBootstrap.CreateFromLoadedAssemblies());

        Assert.Contains($"already created from {EntitySchemaSource.LoadedAssemblies}", ex.Message);
        Assert.Contains("duplicate call", ex.Message);
    }
}
