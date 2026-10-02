using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReadyM.Api.DI;
using ReadyM.SDK.Attributes;
using ReadyM.SDK.Mods;

namespace ReadyM.SDK.Tests.Mods;

/// <summary>
/// What a mod is entered through: one class the loader builds out of the container and starts.
/// </summary>
/// <remarks>
/// Built through the registry rather than through a loader, because the two loaders differ in
/// everything except this: they hand over an assembly, a container and the mod's own folder.
/// </remarks>
public class ModEntryTests
{
    private readonly TestContainer _container = new();

    public ModEntryTests()
    {
        // As a host does. Reading a mod's config reports what it found through it.
        _container.RegisterSingleton<ILogger>(NullLogger.Instance);
    }

    private IModEntry? Start(string directory = "mods/example")
        => ModEntryRegistry.Start(typeof(ExampleEntry).Assembly, _container, directory);

    [Fact]
    public void An_assembly_that_declares_one_says_so()
        => Assert.True(ModEntryRegistry.Declares(typeof(ExampleEntry).Assembly));

    [Fact]
    public void An_assembly_that_declares_none_says_so()
        => Assert.False(ModEntryRegistry.Declares(typeof(IModEntry).Assembly));

    /// The whole point: what it asks for in its constructor is resolved, not passed in by the loader.
    [Fact]
    public void The_entry_point_is_built_out_of_the_container()
    {
        _container.RegisterSingleton(new Dependency("wired"));

        Assert.Equal("wired", ((ExampleEntry)Start()!).Saw);
    }

    [Fact]
    public void Its_start_runs()
    {
        _container.RegisterSingleton(new Dependency("wired"));

        Assert.True(((ExampleEntry)Start()!).Started);
    }

    /// Pushed in after the constructor, so it is the folder by the time anything the mod does reads it.
    [Fact]
    public void It_is_told_which_folder_it_was_loaded_from()
    {
        _container.RegisterSingleton(new Dependency("wired"));

        Assert.Equal("mods/example", ((ExampleEntry)Start("mods/example")!).ModDirectory);
    }

    /// One instance, so a service taking the entry point and the loader see the same object.
    [Fact]
    public void It_is_the_one_the_container_hands_out()
    {
        _container.RegisterSingleton(new Dependency("wired"));

        Assert.Same(Start(), _container.Resolve<ExampleEntry>());
    }

    private sealed class TestContainer : DependencyContainerBase;
}

public sealed record Dependency(string Value);

[ModEntry]
public sealed partial class ExampleEntry(Dependency dependency)
{
    public string Saw { get; } = dependency.Value;

    public bool Started { get; private set; }

    private void Start() => Started = true;
}
