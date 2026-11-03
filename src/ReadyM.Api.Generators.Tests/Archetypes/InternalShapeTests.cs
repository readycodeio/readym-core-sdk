using System.Reflection;
using Microsoft.CodeAnalysis;
using ReadyM.Api.Generators.Archetypes;
using Xunit;

namespace ReadyM.Api.Generators.Tests.Archetypes;

/// <summary>A shape a mod keeps to itself compiles, and what is generated for it stays as private as it is.</summary>
public class InternalShapeTests(ITestOutputHelper output)
{
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
        using ReadyM.SDK.Archetypes.Core;
        using ReadyM.SDK.Attributes;

        namespace Mod;

        [ArchetypeMixin(Replication.None)]
        internal readonly partial struct Health
        {
            public partial float Value { get; set; }
        }

        [ArchetypeMixin(Replication.OwnershipBased)]
        internal readonly partial struct Intents
        {
            public partial global::ReadyM.Native.Container.NativeList<int> Entries { get; set; }
        }

        [ArchetypeMixin(Replication.None)]
        [Extends(typeof(Player))]
        internal readonly partial struct Wallet
        {
            public partial int Balance { get; set; }
        }

        [Archetype]
        [Include(typeof(Health))]
        [Include(typeof(Intents))]
        internal readonly partial struct Hero
        {
            [Index] public partial int Number { get; set; }
        }

        [Archetype(Networked = false)]
        [IncludeArchetype(typeof(Hero))]
        internal readonly partial struct Champion;
        """;

    [Fact]
    public void Internal_shapes_compile()
    {
        var errors = Run().OutputDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();

        foreach (var error in errors)
            output.WriteLine(error.ToString());

        Assert.Empty(errors);
    }

    [Fact]
    public void Nothing_generated_for_an_internal_shape_is_public()
    {
        var compilation = Run().OutputCompilation;
        var leaked = compilation.GetSymbolsWithName(_ => true, SymbolFilter.Type)
            .OfType<INamedTypeSymbol>()
            .Where(type => type.ContainingType is null && type.DeclaredAccessibility == Accessibility.Public)
            .Where(type => type.ContainingNamespace?.ToDisplayString() is "Mod" or "<global namespace>")
            .Select(type => type.ToDisplayString())
            .ToList();

        Assert.Empty(leaked);
    }

    /// <summary>A public archetype may build on mixins the mod keeps to itself.</summary>
    [Fact]
    public void A_public_archetype_can_include_an_internal_mixin()
    {
        var errors = Run(Shapes.Replace("internal readonly partial struct Hero", "public readonly partial struct Hero")
                .Replace("internal readonly partial struct Champion", "public readonly partial struct Champion"))
            .OutputDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();

        foreach (var error in errors)
            output.WriteLine(error.ToString());

        Assert.Empty(errors);
    }

    private SourceGeneratorTestHelper.GeneratorRunResult Run(string shapes = Shapes)
        => SourceGeneratorTestHelper.RunGenerators(
            [("Shapes.cs", shapes)],
            [new ArchetypeGenerator(), new ArchetypeMixinGenerator()],
            output,
            Sdk,
            SourceGeneratorTestHelper.SdkInternalsAssembly);
}
