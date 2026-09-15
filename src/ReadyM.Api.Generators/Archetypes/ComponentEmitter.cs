using System.Collections.Generic;

namespace ReadyM.Api.Generators.Archetypes;

/// <summary>
/// Emits the component behind a mixin or an archetype's own accessors.
/// </summary>
/// <remarks>
/// Generators cannot be chained, so an archetype cannot declare a component for another generator to
/// finish. Everything a component needs has to be emitted from here. Today that is a plain local
/// component; a networked one will need the mask, change tracking and serialization that
/// <c>DeriveINetworkedComponentGenerator</c> emits today, lifted into this emitter so both entry
/// points produce the same thing.
/// </remarks>
internal static class ComponentEmitter
{
    public static void Emit(SourceWriter writer, string name, IReadOnlyList<AccessorModel> accessors)
    {
        using (writer.Braces($"internal struct {name} : {ArchetypeNames.Component}"))
        {
            foreach (var accessor in accessors)
                writer.Line($"public {accessor.Type} {accessor.Field};");
        }
    }
}
