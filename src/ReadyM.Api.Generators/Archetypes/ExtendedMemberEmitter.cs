using System.Linq;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Archetypes;

/// Puts a mixin's values on the archetypes it extends, so a shape from another mod reads as if it
/// had always carried them.
internal static class ExtendedMemberEmitter
{
    public static void Emit(SourceWriter writer, DeclarationModel model, ChunkNames? chunks)
    {
        if (model.Extends.Count == 0 || model.Accessors.Count == 0)
            return;

        foreach (var (archetype, prefix) in model.Extends)
        {
            var target = archetype.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            writer.Line();
            writer.Line("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");

            using (writer.Braces($"public static class {model.Name}On{archetype.Name}"))
            {
                Shape(writer, model, target, prefix);

                if (chunks is not null && DeclarationModel.For(archetype).SupportsChunks)
                    View(writer, model, archetype, prefix);
            }
        }
    }

    /// <summary>On the shape itself, which reaches the value the same way its own members do.</summary>
    private static void Shape(SourceWriter writer, DeclarationModel model, string target, string prefix)
    {
        using (writer.Braces($"extension(in {target} shape)"))
            foreach (var accessor in model.Accessors)
                Members(writer, model, accessor, prefix, $"{ArchetypeNames.EntityHandle}.Of(shape)");
    }

    /// <summary>
    /// The same on the archetype's chunk view. The value is not in that chunk, so it is reached
    /// through the entity, which is why the remarks say what to write instead in a hot loop.
    /// </summary>
    private static void View(SourceWriter writer, DeclarationModel model, INamedTypeSymbol archetype, string prefix)
    {
        var view = ArchetypeNames.QualifiedViewOf(archetype);

        writer.Line();

        var remarks = new[]
        {
            "/// <remarks>",
            $"/// Reached through the entity rather than the chunk, because {model.Name} is not part of",
            $"/// {archetype.Name}'s own chunk. Naming it in the query walks both by chunk instead:",
            $"/// <c>Query&lt;{archetype.Name}, {model.Name}&gt;()</c>.",
            "/// </remarks>"
        };

        using (writer.Braces($"extension(in {view} view)"))
            foreach (var accessor in model.Accessors)
                Members(writer, model, accessor, prefix, "view.Handle", remarks);
    }

    /// <summary>
    /// A getter as a property, and a setter as a method. C# takes the receiver of an extension
    /// property by value, so assigning through one needs a writable variable: a foreach variable is
    /// not, and neither is anything returned by a call. A method has no such restriction.
    /// </summary>
    private static void Members(
        SourceWriter writer,
        DeclarationModel model,
        AccessorModel accessor,
        string prefix,
        string handle,
        string[]? remarks = null)
    {
        writer.Line();

        foreach (var line in remarks ?? [])
            writer.Line(line);

        writer.Line($"public {accessor.Type} {prefix}{accessor.Name}");
        writer.Line($"    => {model.QualifiedAccessors}.Get{accessor.Name}({handle});");

        if (!accessor.HasSetter)
            return;

        writer.Line();

        foreach (var line in remarks ?? [])
            writer.Line(line);

        writer.Line($"public void Set{prefix}{accessor.Name}({accessor.Type} value)");
        writer.Line($"    => {model.QualifiedAccessors}.Set{accessor.Name}({handle}, value);");
    }
}
