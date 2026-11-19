using System.Reflection;
using Microsoft.CodeAnalysis;
using ReadyM.Api.Generators.Archetypes;
using Xunit;

namespace ReadyM.Api.Generators.Tests.Archetypes;

/// <summary>
/// A key is held once in the world or once per dimension. Only a shape whose key is held once can be looked up with no
/// dimension: the mistake of looking up an area or a cell with none is a compile error, not a wrong copy in play.
/// </summary>
public class IndexHeldOnceTests(ITestOutputHelper output)
{
    private static readonly Assembly[] Sdk =
    [
        typeof(SDK.Attributes.ArchetypeAttribute).Assembly,
        typeof(SDK.Chunks.ChunkSlot).Assembly,
        typeof(SDK.Server.Entities.IEntities).Assembly,
        typeof(SDK.Client.Entities.IEntities).Assembly,
        typeof(Api.Multiplayer.ECS.Components.INetworkedComponent).Assembly,
        typeof(Api.ECS.Components.IKeyHeldOnce).Assembly
    ];

    private const string Shapes =
        """
        using Friflo.Engine.ECS;
        using ReadyM.Api.ECS.Components;
        using ReadyM.SDK.Attributes;

        namespace Mod;

        /// <summary>Generated component, key held once.</summary>
        [ArchetypeMixin(Replication.None)]
        public readonly partial struct Badge
        {
            [Index(HeldOnce = true)]
            public partial int Number { get; set; }
        }

        /// <summary>Generated component, key held once per dimension.</summary>
        [ArchetypeMixin(Replication.None)]
        public readonly partial struct Dock
        {
            [Index]
            public partial int Berth { get; set; }
        }

        /// <summary>Explicit component, key held once.</summary>
        public struct PassComponent : IIndexedComponent<int>, IKeyHeldOnce
        {
            public int Id;

            public int GetIndexedValue() => Id;
        }

        [ArchetypeMixin(Replication.None)]
        [ExplicitComponent(typeof(PassComponent))]
        public readonly partial struct Pass
        {
            public partial int Id { get; set; }
        }
        """;

    private static string Use(string body) =>
        $$"""
          using ReadyM.Api.Idents;
          using ReadyM.SDK.Server.Entities;

          namespace Mod;

          public static class Use
          {
              {{body}}
          }
          """;

    [Fact]
    public void A_key_held_once_is_looked_up_with_no_dimension()
    {
        var run = Run(Use(
            """
            public static bool Find(IEntities entities)
                => entities.TryLookup<Badge, int>(1, out _) && entities.TryLookup<Pass, int>(2, out _);
            """));

        Assert.Empty(Errors(run));
    }

    [Fact]
    public void A_key_held_per_dimension_looked_up_with_no_dimension_does_not_compile()
    {
        var run = Run(Use(
            """
            public static bool Find(IEntities entities) => entities.TryLookup<Dock, int>(1, out _);
            """));

        // The type argument does not satisfy IIndexedOnce<int>
        Assert.Contains(Errors(run), error => error.Id is "CS0315" or "CS0311");
    }

    [Fact]
    public void A_key_held_per_dimension_is_looked_up_in_a_dimension()
    {
        var run = Run(Use(
            """
            public static bool Find(IEntities entities)
                => entities.TryLookup<Dock, int>(1, DimensionId.Main, out _)
                   && entities.TryLookup<Badge, int>(1, DimensionId.Main, out _);
            """));

        Assert.Empty(Errors(run));
    }

    [Fact]
    public void A_generated_component_with_a_key_held_once_says_so()
    {
        var run = Run(Use(string.Empty));
        var generated = string.Concat(run.GeneratedSyntaxTrees.Select(tree => tree.ToString()));

        Assert.Empty(Errors(run));
        Assert.Contains(ArchetypeNames.KeyHeldOnce, generated);
        Assert.Contains($"{ArchetypeNames.IndexedOnce}<int>", generated);
        Assert.Contains($"{ArchetypeNames.Indexed}<int>", generated);
    }

    private static Diagnostic[] Errors(SourceGeneratorTestHelper.GeneratorRunResult run)
        => [.. run.OutputDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)];

    private SourceGeneratorTestHelper.GeneratorRunResult Run(string use)
        => SourceGeneratorTestHelper.RunGenerators(
            [("Shapes.cs", Shapes), ("Use.cs", use)],
            [new ArchetypeGenerator(), new ArchetypeMixinGenerator(), new RegistrationAggregatorGenerator()],
            output,
            Sdk,
            SourceGeneratorTestHelper.SdkInternalsAssembly);
}
