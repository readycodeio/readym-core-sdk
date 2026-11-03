using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using ReadyM.Api.Generators.Archetypes;
using Xunit;

namespace ReadyM.Api.Generators.Tests.Archetypes;

/// <summary>
/// What a hidden mixin brings onto a shape is hidden there too, so the plumbing it carries does not show up
/// on every shape that includes it.
/// </summary>
public class HiddenIncludeTests(ITestOutputHelper output)
{
    private const string Hidden =
        "[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]";

    private static readonly Assembly[] Sdk =
    [
        typeof(SDK.Attributes.ArchetypeAttribute).Assembly,
        typeof(SDK.Chunks.ChunkSlot).Assembly,
        typeof(SDK.Server.Entities.IEntities).Assembly,
        typeof(SDK.Client.Entities.IEntities).Assembly,
        typeof(Api.Multiplayer.ECS.Components.INetworkedComponent).Assembly
    ];

    private const string Shapes =
        """
        using System.ComponentModel;
        using ReadyM.SDK.Attributes;

        namespace Mod;

        [EditorBrowsable(EditorBrowsableState.Never)]
        [ArchetypeMixin(Replication.None)]
        public readonly partial struct Plumbing
        {
            public partial int Seq { get; set; }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [ArchetypeMixin(Replication.OwnershipBased)]
        public readonly partial struct Intents
        {
            public partial global::ReadyM.Native.Container.NativeList<int> Entries { get; set; }
        }

        [ArchetypeMixin(Replication.None)]
        public readonly partial struct Health
        {
            public partial float Value { get; set; }
        }

        [Archetype(Networked = false)]
        [Include(typeof(Plumbing))]
        [Include(typeof(Intents))]
        [Include(typeof(Health))]
        public readonly partial struct Hero;
        """;

    [Fact]
    public void A_value_from_a_hidden_mixin_is_hidden_on_the_shape()
        => Assert.All(Declarations(Generated("Hero"), "public int Seq"), previous => Assert.Equal(Hidden, previous));

    [Fact]
    public void A_collection_member_from_a_hidden_mixin_is_hidden_on_the_shape()
        => Assert.All(Declarations(Generated("Hero"), "public int GetEntries("), previous => Assert.Equal(Hidden, previous));

    [Fact]
    public void A_value_from_a_visible_mixin_stays_visible()
        => Assert.All(Declarations(Generated("Hero"), "public float Value"), previous => Assert.NotEqual(Hidden, previous));

    [Fact]
    public void The_collection_type_of_a_hidden_mixin_is_hidden()
        => Assert.Contains(Hidden + "\n#if NET\npublic readonly ref struct IntentsEntries", Normalized(Generated()));

    [Fact]
    public void It_all_compiles()
        => Assert.DoesNotContain(Run().OutputDiagnostics, d => d.Severity == DiagnosticSeverity.Error);

    /// <summary>The line before each declaration that starts with the text, at least one of them.</summary>
    private static List<string> Declarations(string generated, string start)
    {
        var lines = Normalized(generated).Split('\n').Select(line => line.Trim()).ToList();
        var previous = lines
            .Select((line, index) => (line, index))
            .Where(entry => entry.line.StartsWith(start, StringComparison.Ordinal) && entry.index > 0)
            .Select(entry => lines[entry.index - 1])
            .ToList();

        Assert.NotEmpty(previous);
        return previous;
    }

    private static string Normalized(string text) => Regex.Replace(text, "\r\n", "\n");

    private string Generated(string file = "")
    {
        var trees = Run().GeneratedSyntaxTrees.Where(tree => tree.FilePath.Contains(file, StringComparison.Ordinal)).ToList();
        return string.Concat(trees.Select(tree => tree.ToString()));
    }

    private SourceGeneratorTestHelper.GeneratorRunResult Run()
        => SourceGeneratorTestHelper.RunGenerators(
            [("Shapes.cs", Shapes)],
            [new ArchetypeGenerator(), new ArchetypeMixinGenerator()],
            output,
            Sdk,
            SourceGeneratorTestHelper.SdkInternalsAssembly);
}
