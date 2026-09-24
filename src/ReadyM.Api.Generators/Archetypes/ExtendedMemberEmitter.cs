using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Archetypes;

/// Puts a mixin's values on the archetypes it extends, so a shape from another mod reads as if it
/// had always carried them.
internal static class ExtendedMemberEmitter
{
    public static void Emit(SourceWriter writer, DeclarationModel model, ChunkNames? chunks, bool client = false)
    {
        if (model.Extends.Count == 0 || (model.Accessors.Count == 0 && model.Forwards.Count == 0))
            return;

        foreach (var (archetype, prefix) in model.Extends)
        {
            var target = archetype.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            writer.Line();
            writer.Line("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");

            using (writer.Braces($"public static class {model.Name}On{archetype.Name}"))
            {
                Shape(writer, model, target, prefix, client);

                if (chunks is not null && DeclarationModel.For(archetype).SupportsChunks)
                    View(writer, model, archetype, prefix, client);
            }
        }
    }

    /// On the shape itself, which reaches the value the same way its own members do.
    private static void Shape(SourceWriter writer, DeclarationModel model, string target, string prefix, bool client)
    {
        using (writer.Braces($"extension(in {target} shape)"))
        {
            var handle = $"{ArchetypeNames.EntityHandle}.Of(shape)";

            foreach (var accessor in model.Accessors)
                Members(writer, model, accessor, prefix, handle, client);

            foreach (var forward in model.Forwards)
                Forwarded(writer, model, forward, prefix, handle);
        }
    }

    /// The same on the archetype's chunk view. The value is not in that chunk, so it is reached
    /// through the entity, which is why the remarks say what to write instead in a hot loop.
    private static void View(SourceWriter writer, DeclarationModel model, INamedTypeSymbol archetype, string prefix, bool client)
    {
        var view = ArchetypeNames.QualifiedViewOf(archetype);

        writer.Line();

        var remarks = new[]
        {
            $"/// Reached through the entity rather than the chunk, because {model.Name} is not part of",
            $"/// {archetype.Name}'s own chunk. Naming it in the query walks both by chunk instead:",
            $"/// <c>Query&lt;{archetype.Name}, {model.Name}&gt;()</c>."
        };

        using (writer.Braces($"extension(in {view} view)"))
        {
            foreach (var accessor in model.Accessors)
                Members(writer, model, accessor, prefix, "view.Handle", client, remarks);

            foreach (var forward in model.Forwards)
                Forwarded(writer, model, forward, prefix, "view.Handle", remarks);
        }
    }

    /// A getter as a property, and a setter as a method, which C# forces rather than us choosing it.
    /// A shape's own setters can be properties: the shape is a readonly struct, so the compiler knows
    /// an instance setter takes this by readonly ref and cannot modify the receiver, and it allows the
    /// assignment even where the receiver is readonly. It does not carry that over to extension
    /// property setters. An in receiver is not enough: assigning through one on a foreach variable is
    /// CS1654, and on anything returned by a call CS1612, which is most of where these get used. ref
    /// fails harder still, and the only receiver that takes an assignment is a reference type, which
    /// means boxing the shape on every access. So the setter is a method until the compiler treats an
    /// in extension receiver the way it already treats a readonly struct's own this, which is tracked
    /// as https://github.com/dotnet/roslyn/issues/82249. When that lands, the setter can become a
    /// property here, added alongside SetX so nothing written against the current surface breaks.
    private static void Members(
        SourceWriter writer,
        DeclarationModel model,
        AccessorModel accessor,
        string prefix,
        string handle,
        bool client = false,
        string[]? remarks = null)
    {
        if (!accessor.IsExposed)
            return;

        var name = $"{prefix}{accessor.Name}";
        var get = $"{model.QualifiedAccessors}.Get{accessor.Name}({handle})";
        var set = $"{model.QualifiedAccessors}.Set{accessor.Name}({handle}, value)";

        writer.Line();

        // A client writes a replicated value through its token instead.
        if (!accessor.HasSetter || (client && AccessorEmitter.Marked(accessor, model.IsReplicated)))
        {
            Remarks(writer, remarks);
            writer.Line($"public {accessor.Type} {name}");
            writer.Line($"    => {get};");
            return;
        }

        Remarks(writer, remarks,
        [
            $"/// Inside a query call <c>Set{name}</c> instead."
        ]);

        using (writer.Braces($"public {accessor.Type} {name}"))
        {
            writer.Line($"get => {get};");
            writer.Line($"set => {set};");
        }

        writer.Line();

        Remarks(writer, remarks, [
            "/// Use inside queries instead of the property setter."
        ]);
        writer.Line($"public void Set{name}({accessor.Type} value)");
        writer.Line($"    => {set};");
    }

    /// A member the mixin forwards from its explicit component, most often a native collection's
    /// methods. Emitted as-is, so a collection reads and writes on the extended shape exactly as it
    /// does on the mixin. These are methods rather than properties, so unlike a setter they work
    /// inside a query body too.
    private static void Forwarded(
        SourceWriter writer,
        DeclarationModel model,
        ForwardModel forward,
        string prefix,
        string handle,
        string[]? remarks = null)
    {
        var call = $"{model.QualifiedAccessors}.{forward.Name}";

        writer.Line();

        Remarks(writer, remarks);

        if (forward.IsProperty)
        {
            writer.Line($"public {forward.ReturnType} {prefix}{forward.Name} => {call}({handle});");
            return;
        }

        AccessorEmitter.Marker(writer, forward);
        writer.Line($"public {forward.Declaration($"{prefix}{forward.Name}")}");
        writer.Line($"    => {call}({handle}{forward.Separator}{forward.Arguments});");
    }

    /// One remarks block per member, however many notes went into it.
    private static void Remarks(SourceWriter writer, params string[]?[] parts)
    {
        var any = false;

        foreach (var part in parts)
            any |= part is { Length: > 0 };

        if (!any)
            return;

        writer.Line("/// <remarks>");

        foreach (var part in parts)
        foreach (var line in part ?? [])
            writer.Line(line);

        writer.Line("/// </remarks>");
    }
}