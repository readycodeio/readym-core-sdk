using System.Linq;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Archetypes;

/// Declares that a shape's component has to be given its memory before anything reads it.
internal static class NativeInitEmitter
{
    public static bool Applies(DeclarationModel model)
        => model.EmitsComponent && model.Accessors.Any(accessor => accessor.IsNativeContainer);

    public static void Emit(SourceWriter writer, DeclarationModel model, Compilation compilation)
    {
        if (!Applies(model))
            return;

        writer.Line();
        writer.Line("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");

        using (writer.Braces($"public static class {model.Name}NativeInit"))
        {
            if (ExtendsEmitter.HasModuleInitializer(compilation))
                writer.Line("[global::System.Runtime.CompilerServices.ModuleInitializer]");

            writer.Line("public static void Register()");
            writer.Line($"    => {ArchetypeNames.NativeInitRegistry}.Register<{model.QualifiedComponent}>();");
        }
    }
}
