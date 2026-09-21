using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Archetypes;

/// Checks what a shape asks to replicate, before anything is emitted against it.
internal static class ReplicationRules
{
    public static readonly DiagnosticDescriptor NothingToReplicate = new(
        "READYM009",
        "Shape has nothing to replicate",
        "'{0}' holds no values, so there is nothing to replicate. Creating the entity already tells "
        + "the other side it is this shape.",
        "ReadyM",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ComponentCannotReplicate = new(
        "READYM010",
        "Explicit component cannot replicate",
        "'{0}' backs '{1}' and is not a networked component, so '{1}' cannot replicate. Drop "
        + "[Replicated], or give the component the networked contract.",
        "ReadyM",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor CollectionIsExposed = new(
        "READYM011",
        "Replicated collection is reachable as a whole",
        "'{0}.{1}' is a collection on a replicated shape, so it has to be declared private. Handing "
        + "the collection itself to a caller lets them change it without the change being sent. Its "
        + "own members, Add{1} and the rest, are generated public.",
        "ReadyM",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static ImmutableArray<Diagnostic> Check(DeclarationModel model)
    {
        if (!model.HasReplicatedAttribute)
            return [];

        var found = new List<Diagnostic>();
        var at = model.Symbol.Locations.FirstOrDefault() ?? Location.None;

        if (model.ExplicitComponent is { } component)
        {
            if (!DeclarationModel.IsNetworkedComponent(component))
                found.Add(Diagnostic.Create(
                    ComponentCannotReplicate, at, component.ToDisplayString(), model.Name));

            return [.. found];
        }

        if (model.Accessors.Count == 0)
            found.Add(Diagnostic.Create(NothingToReplicate, at, model.Name));

        foreach (var accessor in model.Accessors)
            if (accessor.IsNativeContainer && accessor.IsExposed)
                found.Add(Diagnostic.Create(
                    CollectionIsExposed,
                    accessor.Declared?.Locations.FirstOrDefault() ?? at,
                    model.Name,
                    accessor.Name));

        return [.. found];
    }
}
