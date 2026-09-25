using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Archetypes;

/// Declares that a shape's component replicates, and how its changes travel.
internal static class ReplicationEmitter
{
    public static void Emit(SourceWriter writer, DeclarationModel model, Compilation compilation)
    {
        if (!model.RegistersReplication)
            return;

        writer.Line();
        writer.Line("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");

        using (writer.Braces($"public static class {model.Name}Replication"))
        {
            if (ExtendsEmitter.HasModuleInitializer(compilation))
                writer.Line("[global::System.Runtime.CompilerServices.ModuleInitializer]");

            writer.Line("public static void Register()");
            writer.Line($"    => {ArchetypeNames.ReplicationRegistry}.Register<{model.QualifiedComponent}>(");
            writer.Line($"        {ArchetypeNames.Delivery}.{model.Delivery});");
        }
    }
}
