using ReadyM.Api.Multiplayer.ECS.Archetypes;
using ReadyM.SDK.Core;

namespace ReadyM.SDK.Tests;

/// The relay names the shapes every game has as strings, because it does not reference the SDK a
/// mod writes against. A rename here would otherwise go unnoticed until a mod's extensions
/// quietly stopped arriving.
public class ArchetypeShapeNameTests
{
    public static TheoryData<WellKnownArchetype, Type> Shapes => new()
    {
        { WellKnownArchetype.World, typeof(World) },
        { WellKnownArchetype.Area, typeof(Area) },
        { WellKnownArchetype.Player, typeof(Player) },
        { WellKnownArchetype.Cell, typeof(Cell) }
    };

    [Theory]
    [MemberData(nameof(Shapes))]
    public void Each_well_known_archetype_names_its_shape(WellKnownArchetype archetype, Type shape)
        => Assert.Equal(shape.FullName, ArchetypeShapes.Of(archetype));
}
