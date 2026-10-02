using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReadyM.Api.DI;
using ReadyM.SDK.Attributes;
using ReadyM.SDK.Mods;

namespace ReadyM.SDK.Tests.Mods;

/// <summary>
/// A class a mod declares and the SDK fills from a file in that mod's own folder.
/// </summary>
/// <remarks>
/// Read before the mod's entry point is built, so it is already in the container by the time
/// anything asks for it, whether that is the entry point itself or a service built much later.
/// </remarks>
public class ModConfigTests : IDisposable
{
    private readonly TestContainer _container = new();

    private readonly string _directory =
        Path.Combine(Path.GetTempPath(), "readym-modconfig-" + Guid.NewGuid().ToString("N"));

    public ModConfigTests()
    {
        Directory.CreateDirectory(_directory);
        _container.RegisterSingleton<ILogger>(NullLogger.Instance);
    }

    public void Dispose()
    {
        _container.Dispose();

        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    private void Write(string fileName, string json)
        => File.WriteAllText(Path.Combine(_directory, fileName), json);

    private int Register() => ModConfigRegistry.RegisterAll(typeof(Settings).Assembly, _container, _directory);

    [Fact]
    public void A_config_is_read_from_the_mods_own_folder()
    {
        Write("config.json", """{ "Rounds": 5 }""");
        Register();

        Assert.Equal(5, _container.Resolve<Settings>().Rounds);
    }

    /// A mod that ships no file still gets its config, so nothing has to guard against a null one.
    [Fact]
    public void A_missing_file_leaves_the_defaults()
    {
        Register();

        Assert.Equal(3, _container.Resolve<Settings>().Rounds);
    }

    /// The name on the attribute is what is read, so two configs can live in one folder.
    [Fact]
    public void A_config_can_name_its_own_file()
    {
        Write("rooms.json", """{ "Name": "arena" }""");
        Register();

        Assert.Equal("arena", _container.Resolve<Rooms>().Name);
    }

    [Fact]
    public void Every_config_the_assembly_declares_is_read()
        => Assert.Equal(2, Register());

    /// An assembly declaring none is not an error: most mods have no config at all.
    [Fact]
    public void An_assembly_that_declares_none_reads_nothing()
        => Assert.Equal(0, ModConfigRegistry.RegisterAll(typeof(IModEntry).Assembly, _container, _directory));

    /// One instance, so a service and the entry point see the same values.
    [Fact]
    public void It_is_the_one_the_container_hands_out()
    {
        Register();

        Assert.Same(_container.Resolve<Settings>(), _container.Resolve<Settings>());
    }

    private sealed class TestContainer : DependencyContainerBase;
}

[ModConfig]
public sealed partial class Settings
{
    public int Rounds { get; set; } = 3;
}

[ModConfig("rooms.json")]
public sealed partial class Rooms
{
    public string Name { get; set; } = "none";
}
