using System.Reflection;
using Microsoft.CodeAnalysis;
using ReadyM.Api.Generators.Mods;
using Xunit;

namespace ReadyM.Api.Generators.Tests.Mods;

/// What [ModConfig] makes of a class, and what it refuses. The file it names and the fact that the
/// SDK has to be able to build one are the whole of the contract, so both are worth pinning.
public class ModConfigGeneratorTests(ITestOutputHelper output)
{
    private static readonly Assembly[] Sdk = [typeof(SDK.Attributes.ModConfigAttribute).Assembly];

    private static string Source(string config) =>
        $$"""
          using ReadyM.SDK.Attributes;

          namespace Mod;

          {{config}}
          """;

    private string Generated(string config)
    {
        var result = SourceGeneratorTestHelper.RunGenerators(
            [("Config.cs", Source(config))], [new ModConfigGenerator()], output, Sdk);

        return string.Concat(result.GeneratedSyntaxTrees.Select(tree => tree.ToString()));
    }

    private void AssertReports(string id, string config)
    {
        var result = SourceGeneratorTestHelper.RunGenerators(
            [("Config.cs", Source(config))], [new ModConfigGenerator()], output, Sdk);

        var reported = SourceGeneratorTestHelper.Analyze(result.OutputCompilation, new ModConfigAnalyzer()).ToArray();

        Assert.Contains(reported, diagnostic => diagnostic.Id == id);
        Assert.All(reported.Where(d => d.Id == id), d => Assert.Equal(DiagnosticSeverity.Error, d.Severity));
        Assert.Empty(Generated(config));
    }

    // -- what it writes ----------------------------------------------------------------------------

    /// Leaving the name out reads the one every mod uses, so most configs say nothing at all.
    [Fact]
    public void A_config_reads_the_default_file()
        => Assert.Contains(
            """ModConfigRegistry.Declare<global::Mod.Settings>("config.json")""",
            Generated("[ModConfig] public sealed partial class Settings { public int Rounds { get; set; } }"));

    [Fact]
    public void A_config_can_name_its_own_file()
        => Assert.Contains(
            """ModConfigRegistry.Declare<global::Mod.Settings>("rooms.json")""",
            Generated("""[ModConfig("rooms.json")] public sealed partial class Settings { public int Rounds { get; set; } }"""));

    // -- what it refuses ---------------------------------------------------------------------------

    /// <summary>The SDK builds one to fill, so it has to be something that can be built.</summary>
    [Theory]
    [InlineData("public abstract partial class Settings { }")]
    [InlineData("public partial class Settings { private Settings() { } }")]
    [InlineData("public partial class Settings<T> { }")]
    public void A_config_the_sdk_cannot_build_is_refused(string config)
        => AssertReports("READYM031", $"[ModConfig] {config}");

    /// An empty name reads nothing, and silently falling back to the default would hide the typo.
    [Fact]
    public void A_config_naming_no_file_is_refused()
        => AssertReports("READYM032", """[ModConfig("")] public sealed partial class Settings { }""");
}
