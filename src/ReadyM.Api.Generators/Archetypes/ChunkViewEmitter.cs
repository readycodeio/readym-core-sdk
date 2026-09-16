using System.Collections.Generic;
using System.Linq;

namespace ReadyM.Api.Generators.Archetypes;

/// <summary>
/// Emits the chunk-backed twin of a declaration: the same properties, reached off a chunk.
/// </summary>
/// <remarks>
/// The slots arrive in the order the component set lists them, which is the declaration's own
/// component first and then one per mixin it includes. That order is only knowable at compile time
/// while every include contributes exactly one component, which is why an optional mixin or an
/// included archetype turns the chunk path off for that declaration rather than guessing.
/// </remarks>
internal static class ChunkViewEmitter
{
    public static void Emit(SourceWriter writer, DeclarationModel model, ChunkNames chunks)
    {
        var view = ArchetypeNames.ViewOf(model.Symbol);
        var qualified = ArchetypeNames.QualifiedViewOf(model.Symbol);
        var slots = Slots(model);
        var fields = slots.Select(slot => slot.Field).ToList();

        writer.Line("/// Walks this shape by chunk. Valid only inside the loop that produced it.");

        using (writer.Braces($"public readonly ref struct {view} : {chunks.ChunkView}<{qualified}>"))
        {
            foreach (var field in fields)
                writer.Line($"private readonly {chunks.ComponentChunk} {field};");

            writer.Line($"private readonly global::System.ReadOnlySpan<{ArchetypeNames.RawEntity}> _entities;");
            writer.Line($"private readonly {ArchetypeNames.EntityHandle} _prototype;");
            writer.Line("private readonly int _index;");
            writer.Line();

            EmitConstructor(writer, view, fields, chunks);
            EmitBinding(writer, model, qualified, slots, chunks);
            EmitAccessors(writer, model, slots);
        }
    }

    /// <summary>
    /// What actually puts this shape on the chunk path. A foreach over EntityQuery&lt;X&gt; finds no
    /// GetEnumerator on the type itself, so it binds an extension, and this one beats the SDK's
    /// general one because a non-generic candidate is preferred over a generic one. The view type
    /// never appears in anything an author writes.
    /// </summary>
    public static void EmitQueryBinding(SourceWriter writer, DeclarationModel model, ChunkNames chunks)
    {
        var qualified = ArchetypeNames.QualifiedViewOf(model.Symbol);

        writer.Line("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");

        using (writer.Braces($"public static class {ArchetypeNames.ChunkQueryOf(model.Symbol)}"))
        {
            writer.Line($"public static {chunks.ChunkQuery}<{qualified}>.Enumerator GetEnumerator(");
            writer.Line($"    this {chunks.EntityQuery}<{model.QualifiedName}> query)");
            writer.Line($"    => query.Chunks<{qualified}>();");
        }
    }

    private static void EmitConstructor(SourceWriter writer, string view, IReadOnlyList<string> fields, ChunkNames chunks)
    {
        var parameters = fields.Select(field => $"{chunks.ComponentChunk} {Parameter(field)}").ToList();

        parameters.Add($"global::System.ReadOnlySpan<{ArchetypeNames.RawEntity}> entities");
        parameters.Add($"{ArchetypeNames.EntityHandle} prototype");
        parameters.Add("int index");

        using (writer.Braces($"private {view}({string.Join(", ", parameters)})"))
        {
            foreach (var field in fields)
                writer.Line($"{field} = {Parameter(field)};");

            writer.Line("_entities = entities;");
            writer.Line("_prototype = prototype;");
            writer.Line("_index = index;");
        }
    }

    private static void EmitBinding(SourceWriter writer, DeclarationModel model, string qualified, IReadOnlyList<Slot> slots, ChunkNames chunks)
    {
        var arguments = slots
            .Select(slot => $"new {chunks.ComponentChunk}(slots[{slot.Index}])")
            .Concat(["entities", "prototype", "0"]);

        writer.Line();
        writer.Line($"public static int SlotCount => {slots.Count};");
        writer.Line();
        writer.Line($"public static {ArchetypeNames.ComponentSet} Components => {model.QualifiedAccessors}.Components;");
        writer.Line();
        writer.Line($"public static {qualified} Bind(");
        writer.Line($"    global::System.ReadOnlySpan<{chunks.ChunkSlot}> slots,");
        writer.Line($"    global::System.ReadOnlySpan<{ArchetypeNames.RawEntity}> entities,");
        writer.Line($"    {ArchetypeNames.EntityHandle} prototype)");
        writer.Line($"    => new({string.Join(", ", arguments)});");
        writer.Line();

        var moved = slots.Select(slot => slot.Field).Concat(["_entities", "_prototype", "index"]);

        writer.Line("[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
        writer.Line($"public {qualified} At(int index) => new({string.Join(", ", moved)});");
        writer.Line();
        writer.Line("/// The way out to anything a chunk view cannot do, such as a structural change after the loop.");
        writer.Line($"public {ArchetypeNames.EntityHandle} Handle => _prototype.For(_entities[_index]);");
    }

    private static void EmitAccessors(SourceWriter writer, DeclarationModel model, IReadOnlyList<Slot> slots)
    {
        var own = slots.FirstOrDefault(slot => slot.Include is null);

        if (own is not null)
            foreach (var accessor in model.Accessors)
                Property(writer, accessor, model.QualifiedAccessors, own.Field);

        foreach (var (include, accessor) in model.FlattenedAccessors())
        {
            var slot = slots.FirstOrDefault(candidate => candidate.Include?.TypeName == include.TypeName);

            if (slot is not null)
                Property(writer, accessor, include.Accessors, slot.Field);
        }
    }

    private static void Property(SourceWriter writer, AccessorModel accessor, string accessors, string field)
    {
        writer.Line();

        if (!accessor.HasSetter)
        {
            writer.Line($"public {accessor.Type} {accessor.Name} => {accessors}.Get{accessor.Name}({field}, _index);");
            return;
        }

        using (writer.Braces($"public {accessor.Type} {accessor.Name}"))
        {
            writer.Line($"get => {accessors}.Get{accessor.Name}({field}, _index);");
            writer.Line($"set => {accessors}.Set{accessor.Name}({field}, _index, value);");
        }
    }

    /// The component set's order, which is what the query asks the owning side for.
    private static IReadOnlyList<Slot> Slots(DeclarationModel model)
        => model.ComponentOwners().Select((owner, index) => new Slot(index, owner)).ToList();

    private static string Parameter(string field) => field.TrimStart('_');

    private sealed class Slot(int index, IncludeModel? include)
    {
        public int Index { get; } = index;

        public IncludeModel? Include { get; } = include;

        // Named by position rather than by the include, because two declarations in different
        // namespaces can share a simple name and the fields would collide.
        public string Field { get; } = include is null ? "_own" : $"_slot{index}";
    }
}
