using System.Collections.Generic;
using System.Linq;

namespace ReadyM.Api.Generators.Archetypes;

/// Names each of a shape's values without exposing the component holding it, so an assembly that
/// cannot see the component can still say which value it means.
internal static class ValuesEmitter
{
    public const string ClassName = "Field";

    public static void Emit(SourceWriter writer, DeclarationModel model)
    {
        // Only a replicated component carries the field table these are built from.
        if (!model.IsReplicated || !model.HasOwnComponent)
            return;

        var exposed = model.Accessors.Where(accessor => accessor.IsExposed && !accessor.IsNativeContainer).ToList();

        // A collection is kept out of reach, and named here all the same: the token is the only way
        // to it, which is the point of keeping it out of reach in the first place.
        var collections = CollectionAccessEmitter.Of(model).ToList();

        if (exposed.Count == 0 && collections.Count == 0)
            return;

        var component = model.QualifiedComponent;

        writer.Line();

        using (writer.Braces($"public static class {ClassName}"))
        {
            writer.Line();
            writer.Line("/// Every value at once, for a correspondence that cannot be split.");
            writer.Line($"public static {ArchetypeNames.ShapeToken}<{model.QualifiedName}> All {{ get; }}");
            writer.Line($"    = {ArchetypeNames.ShapeToken}.Of<{model.QualifiedName}, {component}>();");

            foreach (var accessor in exposed)
            {
                writer.Line();
                // The value the component is indexed by writes back through the index.
                var of = accessor.IsIndex ? "OfIndexed" : "Of";

                writer.Line($"public static {ArchetypeNames.Value}<{model.QualifiedName}, {accessor.Type}> {accessor.Name} {{ get; }}");
                writer.Line($"    = {ArchetypeNames.Value}.{of}<{model.QualifiedName}, {component}, {accessor.Type}>(");
                writer.Line($"        {component}.Fields.{accessor.FieldEntry});");
            }

            foreach (var (collection, held, _) in collections)
            {
                if (held is null)
                    continue;

                var access = CollectionAccessEmitter.QualifiedNameOf(model, collection);

                writer.Line();
                writer.Line($"public static {ArchetypeNames.Collection}<{model.QualifiedName}, {access}> {collection} {{ get; }}");
                writer.Line($"    = {ArchetypeNames.Collection}.Of<{model.QualifiedName}, {component}, {held}, {access}>(");
                writer.Line($"        {component}.Fields.{collection},");
                writer.Line($"        static (in {ArchetypeNames.EntityHandle} handle) => new {access}(in handle));");
            }
        }
    }
}
