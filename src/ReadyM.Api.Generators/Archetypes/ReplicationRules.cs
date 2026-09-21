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

        return [.. found];
    }
}
