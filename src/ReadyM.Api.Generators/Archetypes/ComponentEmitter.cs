using System.Collections.Generic;
using System.Linq;

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
    /// <summary>
    /// Carries no values: its presence is the whole message, which is that the entity was created as
    /// this archetype or as one including it. That is what makes an archetype with no components of
    /// its own queryable, and what makes asking whether an entity is one mean something.
    /// </summary>
    public static void EmitMarker(SourceWriter writer, string name)
        => writer.Line($"internal struct {name} : {ArchetypeNames.Component};");

    public static void Emit(SourceWriter writer, string name, IReadOnlyList<AccessorModel> accessors)
    {
        var index = accessors.FirstOrDefault(accessor => accessor.IsIndex);
        var contracts = index is null
            ? ArchetypeNames.Component
            : $"{ArchetypeNames.IndexedComponent}<{index.Type}>";

        using (writer.Braces($"internal struct {name} : {contracts}"))
        {
            foreach (var accessor in accessors)
                writer.Line($"public {accessor.Type} {accessor.Field};");

            if (index is null)
                return;

            writer.Line();
            writer.Line($"public {index.Type} GetIndexedValue() => {index.Field};");
        }
    }
}
