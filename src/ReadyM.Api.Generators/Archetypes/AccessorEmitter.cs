using System.Collections.Generic;

namespace ReadyM.Api.Generators.Archetypes;

/// <summary>
/// Emits the accessor class that stands between a declaration and anyone who includes it.
/// </summary>
/// <remarks>
/// The component behind a mixin is private to the assembly that declared it, so an archetype in
/// another assembly cannot name it. It calls these methods instead, which is also what lets the
/// declaring assembly move a value to a different component without breaking a compiled consumer:
/// only these signatures are promised, not the storage behind them.
/// </remarks>
internal static class AccessorEmitter
{
    public static string ClassNameOf(string declaration) => declaration + "Accessors";

    public static void Emit(SourceWriter writer, DeclarationModel model, string componentSet, ChunkNames? chunks = null)
    {
        writer.Line("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");

        using (writer.Braces($"public static class {ClassNameOf(model.Name)}"))
        {
            writer.Line($"public static {ArchetypeNames.ComponentSet} Components {{ get; }} = {componentSet};");

            if (!model.HasOwnComponent)
                return;

            var component = model.QualifiedComponent;

            writer.Line();
            writer.Line($"public static bool Has(in {ArchetypeNames.EntityHandle} handle) => handle.HasComponent<{component}>();");
            writer.Line();
            writer.Line($"public static void Add(in {ArchetypeNames.EntityHandle} handle) => handle.AddComponent<{component}>();");

            foreach (var accessor in model.Accessors)
                Members(writer, accessor, component);

            if (chunks is null)
                return;

            foreach (var accessor in model.Accessors)
                ChunkMembers(writer, accessor, component, chunks);
        }
    }

    /// <summary>
    /// The same values reached off a chunk instead of an entity. Only the declaring assembly can
    /// name the component, which is why the reinterpret happens here rather than in the view.
    /// </summary>
    private static void ChunkMembers(SourceWriter writer, AccessorModel accessor, string component, ChunkNames chunks)
    {
        var read = $"chunk.As<{component}>(index).{accessor.Field}";

        writer.Line();
        writer.Line($"public static {accessor.Type} Get{accessor.Name}(in {chunks.ComponentChunk} chunk, int index)");
        writer.Line($"    => {read};");

        if (!accessor.HasSetter)
            return;

        writer.Line();
        writer.Line($"public static void Set{accessor.Name}(in {chunks.ComponentChunk} chunk, int index, {accessor.Type} value)");
        writer.Line($"    => {read} = value;");
    }

    private static void Members(SourceWriter writer, AccessorModel accessor, string component)
    {
        var read = $"handle.GetComponent<{component}>().{accessor.Field}";

        writer.Line();
        writer.Line($"public static {accessor.Type} Get{accessor.Name}(in {ArchetypeNames.EntityHandle} handle) => {read};");

        if (accessor.HasSetter)
        {
            writer.Line();
            writer.Line($"public static void Set{accessor.Name}(in {ArchetypeNames.EntityHandle} handle, {accessor.Type} value)");
            writer.Line($"    => {read} = value;");
        }

        writer.Line();

        var signature = $"public static bool TryGet{accessor.Name}(in {ArchetypeNames.EntityHandle} handle, out {accessor.Type} value)";

        using (writer.Braces(signature))
        {
            using (writer.Braces($"if (handle.TryGetComponent<{component}>(out var component))"))
            {
                writer.Line($"value = component.{accessor.Field};");
                writer.Line("return true;");
            }

            writer.Line();
            writer.Line("value = default!;");
            writer.Line("return false;");
        }
    }

    /// <summary>Calls that a consumer in any assembly can make.</summary>
    public static string Get(IncludeModel include, AccessorModel accessor)
        => $"{include.Accessors}.Get{accessor.Name}(_handle)";

    public static string Set(IncludeModel include, AccessorModel accessor, string value)
        => $"{include.Accessors}.Set{accessor.Name}(_handle, {value})";

    public static IReadOnlyList<string> Sets(DeclarationModel model)
    {
        var sets = new List<string>();

        if (model.HasOwnComponent)
            sets.Add($"{ArchetypeNames.ComponentSet}.Of<{model.QualifiedComponent}>()");

        // Right after the own component, matching the order ComponentOwners walks.
        if (model.NeedsMarker)
            sets.Add($"{ArchetypeNames.ComponentSet}.Of<{model.QualifiedMarker}>()");

        foreach (var include in model.Includes)
            sets.Add($"{include.Accessors}.Components");

        return sets;
    }
}
