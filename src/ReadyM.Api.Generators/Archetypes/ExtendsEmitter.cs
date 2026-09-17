using System.Linq;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Archetypes;

/// Registers a shape against the archetypes it extends, so creating one carries it.
internal static class ExtendsEmitter
{
    public static void Emit(SourceWriter writer, DeclarationModel model, Compilation compilation)
    {
        if (model.Extends.Count == 0)
            return;

        var name = model.Name + "Extends";

        writer.Line();
        writer.Line("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");

        using (writer.Braces($"public static class {name}"))
        {
            if (HasModuleInitializer(compilation))
                writer.Line("[global::System.Runtime.CompilerServices.ModuleInitializer]");

            using (writer.Braces("public static void Register()"))
            {
                foreach (var archetype in model.Extends)
                {
                    var qualified = archetype.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                    writer.Line($"{ArchetypeNames.Registry}.Extend(typeof({qualified}), {model.QualifiedAccessors}.Components);");
                }
            }
        }
    }

    /// A target without one leaves Register for the host to call when it loads the mod.
    private static bool HasModuleInitializer(Compilation compilation)
        => compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.ModuleInitializerAttribute") is not null;
}
