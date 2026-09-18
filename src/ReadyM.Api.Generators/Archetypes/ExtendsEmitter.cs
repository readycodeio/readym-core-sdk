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
                foreach (var (archetype, _) in model.Extends)
                {
                    var qualified = archetype.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                    writer.Line($"{ArchetypeNames.Registry}.Extend(typeof({qualified}), {model.QualifiedAccessors}.Components);");
                }
            }
        }
    }

    /// A target without one leaves Register for the host to call when it loads the mod.
    /// <summary>
    /// Present is not enough: a netstandard2.0 target has no such attribute of its own, and a
    /// polyfill one of its references declares is internal to that assembly, so naming it here
    /// would not compile.
    /// </summary>
    private static bool Accessible(Compilation compilation, string metadataName)
    {
        var type = compilation.GetTypeByMetadataName(metadataName);

        return type is not null && compilation.IsSymbolAccessibleWithin(type, compilation.Assembly);
    }

    internal static bool HasModuleInitializer(Compilation compilation)
        => Accessible(compilation, "System.Runtime.CompilerServices.ModuleInitializerAttribute");
}
