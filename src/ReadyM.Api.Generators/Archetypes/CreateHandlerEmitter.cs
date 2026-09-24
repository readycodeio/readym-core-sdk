using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Archetypes;

/// <summary>
/// Registers what a shape asked to run as one of its entities is created.
/// </summary>
/// <remarks>
/// The registration sits inside the shape so it can call a handler the author kept private, which is
/// where a handler belongs: it is the shape's own startup, not an entry point for anyone else.
/// </remarks>
internal static class CreateHandlerEmitter
{
    public static readonly DiagnosticDescriptor NotAHandler = new(
        "READYM020",
        "Create handler cannot be run",
        "'{0}.{1}' is a create handler, so it has to be an instance method returning void. A handler "
        + "runs on the entity as it is created, and there is nobody to hand a result to.",
        "ReadyM",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TooManyHandlers = new(
        "READYM021",
        "Shape declares more than one create handler",
        "'{0}' declares {1} create handlers, and the order between them would be nobody's to say. "
        + "Keep one and call the rest from it.",
        "ReadyM",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static ImmutableArray<Diagnostic> Check(DeclarationModel model)
    {
        if (model.CreateHandlers.Count == 0)
            return [];

        var at = model.Symbol.Locations.FirstOrDefault() ?? Location.None;

        if (model.CreateHandlers.Count > 1)
            return [Diagnostic.Create(TooManyHandlers, at, model.Name, model.CreateHandlers.Count)];

        var handler = model.CreateHandlers[0];

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
        if (Check(model).Length > 0 || model.CreateHandlers.Count == 0)
            return;

        var handler = model.CreateHandlers[0];
        var component = model.NeedsMarker ? model.QualifiedMarker : model.QualifiedComponent;
        var arguments = string.Join(", ", handler.Parameters.Select(Resolved));

        writer.Line();
        writer.Line("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");

        using (writer.Braces($"public static class {handler.Name}Registration"))
        {
            if (ExtendsEmitter.HasModuleInitializer(compilation))
                writer.Line("[global::System.Runtime.CompilerServices.ModuleInitializer]");

            writer.Line("public static void Register()");
            writer.Line($"    => {ArchetypeNames.CreateHandlers}.Register<{component}>(");
            writer.Line($"        static (in {ArchetypeNames.EntityHandle} handle)");
            writer.Line($"            => new {model.QualifiedName}(handle).{handler.Name}({arguments}));");
        }
    }

    /// A handler asks for what it needs by naming it, and the game's services answer.
    private static string Resolved(IParameterSymbol parameter)
        => $"{ArchetypeNames.CreateHandlers}.Services.Resolve<"
           + $"{parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>()";
}
