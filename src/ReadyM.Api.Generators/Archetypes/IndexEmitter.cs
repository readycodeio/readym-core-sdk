using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Archetypes;

/// Declares a shape as indexed, and registers which component holds the index behind it.
internal static class IndexEmitter
{
    /// <summary>The extra contract an indexed shape carries, so a lookup can name it.</summary>
    public static string Contract(DeclarationModel model)
        => model.IndexedBy is null ? string.Empty : $", {ArchetypeNames.Indexed}<{model.IndexedBy}>";

    public static void Emit(SourceWriter writer, DeclarationModel model, Compilation compilation)
    {
        if (model.IndexedBy is null)
            return;

        writer.Line();
        writer.Line("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");

        using (writer.Braces($"public static class {model.Name}Index"))
        {
            if (ExtendsEmitter.HasModuleInitializer(compilation))
                writer.Line("[global::System.Runtime.CompilerServices.ModuleInitializer]");

            writer.Line("public static void Register()");
            writer.Line($"    => {ArchetypeNames.IndexRegistry}.Register<{model.QualifiedName}, {model.IndexedBy}, {model.QualifiedComponent}>();");
        }
    }
}
