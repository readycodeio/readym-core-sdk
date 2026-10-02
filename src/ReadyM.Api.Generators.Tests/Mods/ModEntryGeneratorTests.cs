using System.Reflection;
using Microsoft.CodeAnalysis;
using ReadyM.Api.Generators.Mods;
using Xunit;

namespace ReadyM.Api.Generators.Tests.Mods;

/// What [ModEntry] makes of a class, and what it refuses. A mod is entered through exactly one of
/// them, and its start is found by name rather than by an attribute, so both are worth pinning from
/// either side. The analyzer is what says so, since a generator's diagnostics never reach the editor.
public class ModEntryGeneratorTests(ITestOutputHelper output)
{
    private static readonly Assembly[] Sdk = [typeof(SDK.Attributes.ModEntryAttribute).Assembly];

    private static string Source(string entry) =>
        $$"""
          using ReadyM.SDK.Attributes;

          namespace Mod;

          {{entry}}
          """;

    private string Generated(string entry)
    {
        var result = SourceGeneratorTestHelper.RunGenerators(
            [("Entry.cs", Source(entry))], [new ModEntryGenerator()], output, Sdk);

        return string.Concat(result.GeneratedSyntaxTrees.Select(tree => tree.ToString()));
    }

    private Diagnostic[] Reported(string entry)
    {
        var result = SourceGeneratorTestHelper.RunGenerators(
            [("Entry.cs", Source(entry))], [new ModEntryGenerator()], output, Sdk);

        return [.. SourceGeneratorTestHelper.Analyze(result.OutputCompilation, new ModEntryAnalyzer())];
    }

    private void AssertReports(string id, string entry)
    {
        var reported = Reported(entry);

        Assert.Contains(reported, diagnostic => diagnostic.Id == id);
        Assert.All(reported.Where(d => d.Id == id), d => Assert.Equal(DiagnosticSeverity.Error, d.Severity));
    }

    // -- what it writes ----------------------------------------------------------------------------

    [Fact]
    public void An_entry_point_is_registered_so_the_loader_can_find_it()
        => Assert.Contains(
            "ModEntryRegistry.Declare<global::Mod.Entry>()",
            Generated("[ModEntry] public sealed partial class Entry;"));

    /// The folder is written on rather than asked for, so a mod reads it without a constructor
    /// parameter the loader would have to supply.
    [Fact]
    public void It_is_given_the_folder_it_was_loaded_from()
        => Assert.Contains(
            "public string ModDirectory { get; private set; }",
            Generated("[ModEntry] public sealed partial class Entry;"));

    [Fact]
    public void A_declared_start_is_called()
        => Assert.Contains(
            "=> Start();",
            Generated("[ModEntry] public sealed partial class Entry { private void Start() { } }"));

    /// Most of a mod's work is its constructor and its services, so a start is worth leaving out.
    [Fact]
    public void A_start_is_optional()
    {
        var generated = Generated("[ModEntry] public sealed partial class Entry;");

        Assert.Contains("IModEntry.Start()", generated);
        Assert.DoesNotContain("=> Start();", generated);
    }

    // -- what it refuses ---------------------------------------------------------------------------

    [Fact]
    public void An_entry_point_that_is_not_partial_is_refused()
        => AssertReports("READYM027", "[ModEntry] public sealed class Entry { }");

    [Fact]
    public void An_entry_point_that_is_not_sealed_is_refused()
        => AssertReports("READYM028", "[ModEntry] public partial class Entry { }");

    /// A public start would be callable by anything, and the loader calls it exactly once.
    [Theory]
    [InlineData("public void Start() { }")]
    [InlineData("private static void Start() { }")]
    [InlineData("private int Start() => 0;")]
    [InlineData("private void Start(int unused) { }")]
    public void A_start_the_loader_cannot_call_is_refused(string start)
        => AssertReports("READYM029", $"[ModEntry] public sealed partial class Entry {{ {start} }}");

    /// A mod is entered once, so a second one is a question the loader has no answer to.
    [Fact]
    public void A_second_entry_point_is_refused()
        => AssertReports(
            "READYM030",
            """
            [ModEntry] public sealed partial class Entry;

            [ModEntry] public sealed partial class Other;
            """);
}
