using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Archetypes;

/// <summary>
/// Registers what a shape asked to run just before one of its entities goes.
/// </summary>
/// <remarks>
/// The registration sits inside the shape so it can call a handler the author kept private, which is
/// where a handler belongs: it is the shape's own teardown, not an entry point for anyone else.
/// </remarks>
internal static class DeleteHandlerEmitter
{
    public static readonly DiagnosticDescriptor NotAHandler = new(
        "READYM028",
        "Delete handler cannot be run",
        "'{0}.{1}' is a delete handler, so it has to be an instance method returning void. A handler "
        + "runs on the entity as it goes, and there is nobody left to hand a result to.",
        "ReadyM",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TooManyHandlers = new(
        "READYM029",
        "Shape declares more than one delete handler",
        "'{0}' declares {1} delete handlers, and the order between them would be nobody's to say. "
        + "Keep one and call the rest from it.",
        "ReadyM",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static ImmutableArray<Diagnostic> Check(DeclarationModel model)
    {
        if (model.DeleteHandlers.Count == 0)
            return [];

        var at = model.Symbol.Locations.FirstOrDefault() ?? Location.None;

        if (model.DeleteHandlers.Count > 1)
            return [Diagnostic.Create(TooManyHandlers, at, model.Name, model.DeleteHandlers.Count)];

        var handler = model.DeleteHandlers[0];

        return handler.IsStatic || !handler.ReturnsVoid
            ? [Diagnostic.Create(
                NotAHandler,
                handler.Locations.FirstOrDefault() ?? at,
                model.Name,
                handler.Name)]
            : [];
    }

    public static void Emit(SourceWriter writer, DeclarationModel model, Compilation compilation)
    {
        EmitWatchPoint(writer, model);

        if (Check(model).Length > 0 || model.DeleteHandlers.Count == 0)
            return;

        var handler = model.DeleteHandlers[0];
        var arguments = string.Join(", ", handler.Parameters.Select(Resolved));

        writer.Line();
        writer.Line("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");

        using (writer.Braces($"public static class {handler.Name}DeleteRegistration"))
        {
            if (ExtendsEmitter.HasModuleInitializer(compilation))
                writer.Line("[global::System.Runtime.CompilerServices.ModuleInitializer]");

            writer.Line("public static void Register()");
            writer.Line($"    => {ArchetypeNames.DeleteHandlers}.Register({model.QualifiedAccessors}.Components,");
            writer.Line($"        static (in {ArchetypeNames.EntityHandle} handle)");
            writer.Line($"            => new {model.QualifiedName}(handle).{handler.Name}({arguments}));");
        }
    }

    /// Where a service watching this shape hands its handler in. The components a shape is made of
    /// never leave the assembly that declared it, and a service watching one may sit anywhere, so
    /// the shape hands them over from the inside.
    private static void EmitWatchPoint(SourceWriter writer, DeclarationModel model)
    {
        writer.Line();
        writer.Line("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
        writer.Line($"public static void ObserveDeleted({ArchetypeNames.EntityHandler} handler)");
        writer.Line($"    => {ArchetypeNames.DeleteHandlers}.Observe({model.QualifiedAccessors}.Components, handler);");
    }

    /// A handler asks for what it needs by naming it, and the game's services answer.
    private static string Resolved(IParameterSymbol parameter)
        => $"{ArchetypeNames.DeleteHandlers}.Services.Resolve<"
           + $"{parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>()";
}
