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

    /// The values alone, for a component whose other half is emitted by the replicated emitter.
    /// <remarks>
    /// A hand-written networked component declares its own fields and the emitter adds everything
    /// around them. A generated one has nobody to declare them, so they come from here, in a partial
    /// the other half completes. The contracts sit on that other half.
    /// </remarks>
    public static void EmitFields(SourceWriter writer, string name, IReadOnlyList<AccessorModel> accessors)
    {
        var index = accessors.FirstOrDefault(accessor => accessor.IsIndex);
        // The other half states IReadyComponent and the propagation contract. Indexing and
        // allocation follow the fields, so they belong on the half that declares them.
        var contracts = Contracts(accessors, index is null ? null : $"{ArchetypeNames.IndexedComponent}<{index.Type}>");

        using (writer.Braces($"internal partial struct {name}{contracts}"))
        {
            foreach (var accessor in accessors)
                writer.Line($"public {accessor.Type} {accessor.Field};");

            EmitInit(writer, accessors);
            EmitIndexedValue(writer, index);
        }
    }

    /// <summary>The extra contracts a component carries beyond being a component.</summary>
    private static string Contracts(IReadOnlyList<AccessorModel> accessors, string? first)
    {
        var carried = new List<string>();

        if (first is not null)
            carried.Add(first);

        if (accessors.Any(accessor => accessor.IsNativeContainer))
            carried.Add(ArchetypeNames.NativeInit);

        return carried.Count == 0 ? string.Empty : " : " + string.Join(", ", carried);
    }

    /// Creates the collections the component holds. A native collection is a handle to memory
    /// nobody has taken yet, so reading or adding before this has run faults. The store calls it
    /// once, as the entity is created.
    private static void EmitInit(SourceWriter writer, IReadOnlyList<AccessorModel> accessors)
    {
        var collections = accessors.Where(accessor => accessor.IsNativeContainer).ToList();

        if (collections.Count == 0)
            return;

        writer.Line();

        using (writer.Braces($"public void Init({ArchetypeNames.AllocatorKind} allocatorKind)"))
            foreach (var accessor in collections)
                writer.Line($"{accessor.Field}.TryCreate(allocatorKind);");
    }

    public static void Emit(SourceWriter writer, string name, IReadOnlyList<AccessorModel> accessors)
    {
        var index = accessors.FirstOrDefault(accessor => accessor.IsIndex);
        var contracts = Contracts(accessors, index is null
            ? ArchetypeNames.Component
            : $"{ArchetypeNames.IndexedComponent}<{index.Type}>");

        using (writer.Braces($"internal struct {name}{contracts}"))
        {
            foreach (var accessor in accessors)
                writer.Line($"public {accessor.Type} {accessor.Field};");

            EmitInit(writer, accessors);
            EmitIndexedValue(writer, index);
        }
    }

    /// Friflo asks the component itself for the value it is indexed by.
    private static void EmitIndexedValue(SourceWriter writer, AccessorModel? index)
    {
        if (index is null)
            return;

        writer.Line();
        writer.Line($"public {index.Type} GetIndexedValue() => {index.Field};");
    }
}
