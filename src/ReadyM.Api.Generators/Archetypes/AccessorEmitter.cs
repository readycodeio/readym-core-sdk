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

            foreach (var accessor in model.Accessors)
                Members(writer, accessor, component, model.IndexedBy, model.IsReplicated);

            foreach (var forward in model.Forwards)
                Forward(writer, forward, component);

            if (chunks is null)
                return;

            foreach (var accessor in model.Accessors)
                ChunkMembers(writer, accessor, component, chunks, model.IndexedBy is not null, model.IsReplicated);

            foreach (var forward in model.Forwards)
                ChunkForward(writer, forward, component, chunks);
        }
    }

    /// <summary>
    /// The same values reached off a chunk instead of an entity. Only the declaring assembly can
    /// name the component, which is why the reinterpret happens here rather than in the view.
    /// </summary>
    private static void ChunkMembers(
        SourceWriter writer,
        AccessorModel accessor,
        string component,
        ChunkNames chunks,
        bool indexed,
        bool replicated)
    {
        var read = $"chunk.As<{component}>(index).{accessor.Field}";
        var write = replicated
            ? Write($"chunk.As<{component}>(index)", accessor)
            : $"{read} = value";

        writer.Line();
        writer.Line($"public static {accessor.Type} Get{accessor.Name}(in {chunks.ComponentChunk} chunk, int index)");
        writer.Line($"    => {read};");

        // A chunk write moves no index, so an indexed value is read-only here and written by handle.
        if (!accessor.HasSetter || indexed)
            return;

        writer.Line();
        writer.Line($"public static void Set{accessor.Name}(in {chunks.ComponentChunk} chunk, int index, {accessor.Type} value)");
        writer.Line($"    => {write};");
    }

    /// <summary>A member of the component itself, reached through a handle.</summary>
    private static void Forward(SourceWriter writer, ForwardModel forward, string component)
    {
        var target = "handle.GetComponent<" + component + ">()." + forward.Name;

        writer.Line();

        if (forward.IsProperty)
        {
            writer.Line($"public static {forward.ReturnType} {forward.Name}(scoped in {ArchetypeNames.EntityHandle} handle) => {target};");
            return;
        }

        writer.Line($"public static {forward.ReturnType} {forward.Name}({forward.ParametersAfter($"scoped in {ArchetypeNames.EntityHandle} handle")})");
        writer.Line($"    => {target}({forward.Arguments});");
    }

    /// <summary>The same member, reached off a chunk.</summary>
    private static void ChunkForward(SourceWriter writer, ForwardModel forward, string component, ChunkNames chunks)
    {
        var target = "chunk.As<" + component + ">(row)." + forward.Name;

        writer.Line();

        if (forward.IsProperty)
        {
            writer.Line($"public static {forward.ReturnType} {forward.Name}({chunks.ComponentChunk} chunk, int row) => {target};");
            return;
        }

        writer.Line($"public static {forward.ReturnType} {forward.Name}({forward.ParametersAfter($"{chunks.ComponentChunk} chunk, int row")})");
        writer.Line($"    => {target}({forward.Arguments});");
    }

    private static void Members(
        SourceWriter writer,
        AccessorModel accessor,
        string component,
        string? indexedBy,
        bool replicated)
    {
        var read = $"handle.GetComponent<{component}>().{accessor.Field}";

        // A replicated component writes through what the replicated emitter put over the field,
        // which is what sets the dirty bit. Writing the field would change the value and tell
        // nobody. Reads stay on the field: there is nothing to mark.
        var target = $"handle.GetComponent<{component}>()";
        var write = replicated ? Write(target, accessor) : $"{read} = value";

        writer.Line();
        writer.Line($"public static {accessor.Type} Get{accessor.Name}(in {ArchetypeNames.EntityHandle} handle) => {read};");

        if (accessor.HasSetter)
        {
            writer.Line();
            writer.Line($"public static void Set{accessor.Name}(in {ArchetypeNames.EntityHandle} handle, {accessor.Type} value)");

            if (indexedBy is null)
            {
                writer.Line($"    => {write};");
            }
            else
            {
                using (writer.Braces(string.Empty))
                {
                    writer.Line($"var updated = handle.GetComponent<{component}>();");
                    writer.Line();
                    writer.Line($"{(replicated ? Write("updated", accessor) : $"updated.{accessor.Field} = value")};");
                    writer.Line();
                    writer.Line($"handle.ReplaceIndexed<{component}, {indexedBy}>(updated);");
                }
            }
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

    /// Calls that a consumer in any assembly can make.
    public static string Get(IncludeModel include, AccessorModel accessor)
        => $"{include.Accessors}.Get{accessor.Name}(_handle)";

    public static string Set(IncludeModel include, AccessorModel accessor, string value)
        => $"{include.Accessors}.Set{accessor.Name}(_handle, {value})";

    /// <summary>A forwarded member on the shape, passing the handle it holds.</summary>
    public static void Forwarded(SourceWriter writer, ForwardModel forward, string accessors)
    {
        writer.Line();

        if (forward.IsProperty)
        {
            writer.Line($"public {forward.ReturnType} {forward.Name} => {accessors}.{forward.Name}(_handle);");
            return;
        }

        writer.Line($"public {forward.Signature}");
        writer.Line($"    => {accessors}.{forward.Name}(_handle{Separator(forward)}{forward.Arguments});");
    }

    /// <summary>The same, on a chunk view, which passes the chunk and the row it sits on.</summary>
    public static void ForwardedFromChunk(SourceWriter writer, ForwardModel forward, string accessors, string field)
    {
        writer.Line();

        if (forward.IsProperty)
        {
            writer.Line($"public {forward.ReturnType} {forward.Name} => {accessors}.{forward.Name}({field}, _index);");
            return;
        }

        writer.Line($"public {forward.Signature}");
        writer.Line($"    => {accessors}.{forward.Name}({field}, _index{Separator(forward)}{forward.Arguments});");
    }

    /// Writing one value of a replicated component. A native collection has no property to assign:
    /// the replicated emitter gives it a setter method, which copies the contents rather than taking
    /// the collection itself.
    private static string Write(string target, AccessorModel accessor)
        => accessor.IsNativeContainer
            ? $"{target}.Set{accessor.Name}(value)"
            : $"{target}.{accessor.Name} = value";

    private static string Separator(ForwardModel forward) => forward.Separator;

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
