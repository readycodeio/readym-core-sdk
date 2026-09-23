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

    public static readonly DiagnosticDescriptor PropagationUnstated = new(
        "READYM014",
        "Replicated shape does not say how its values travel",
        "'{0}' replicates values of its own, so it has to say how they travel between the game and "
        + "the ECS and who may write them: add [Propagates(...)]. Left unsaid, nothing stops a "
        + "client writing a value it does not own.",
        "ReadyM",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor PropagationDisagrees = new(
        "READYM015",
        "Stated propagation is not what the component says",
        "'{0}' says it propagates as {1}, but '{2}' backs it and does not carry that contract. The "
        + "component a shape does not own decides this; state what it says, or drop [Propagates].",
        "ReadyM",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor PropagationNeedsReplication = new(
        "READYM016",
        "Propagation is stated on a shape nothing sends",
        "'{0}' is not replicated. Drop [Propagates], or replicate the shape.",
        "ReadyM",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// What a shape says about propagating, and whether it is in a position to say it.
    public static ImmutableArray<Diagnostic> CheckPropagation(DeclarationModel model)
    {
        var at = model.Symbol.Locations.FirstOrDefault() ?? Location.None;

        if (model.Propagation is not { } propagation)
            // A shape over a component it does not own takes that component's word for it.
            return model.IsReplicated && model.EmitsComponent
                ? [Diagnostic.Create(PropagationUnstated, at, model.Name)]
                : [];

        if (model.ExplicitComponent is { } component)
            return PropagationContracts.DeclaredBy(component, propagation)
                ? []
                : [Diagnostic.Create(PropagationDisagrees, at, model.Name, propagation, component.ToDisplayString())];

        // Nothing sends a local shape's values, so nothing has to be stopped from writing them.
        return model.IsReplicated ? [] : [Diagnostic.Create(PropagationNeedsReplication, at, model.Name)];
    }

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
