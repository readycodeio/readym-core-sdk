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
        "Replication is stated on a borrowed component",
        "'{0}' backs '{1}' and decides for itself whether it crosses the wire. Drop [Replicated]: a "
        + "shape cannot send a component it does not own, and a networked one already sends itself.",
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
        "Propagation is stated on a borrowed component",
        "'{0}' says it propagates as {1}, but '{2}' backs it and carries that contract itself. The "
        + "component a shape does not own decides this, and the runtime reads it there. Drop "
        + "[Propagates].",
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

    public static readonly DiagnosticDescriptor CollectionNeedsReplication = new(
        "READYM018",
        "Collection sits on a shape nothing sends",
        "'{0}.{1}' is a collection, and only a replicated component is given the members that work "
        + "one: Add{1}, Clear{1} and the rest. On '{0}' it would be a handle to memory with nothing "
        + "to reach it by. Replicate the shape, or hold the values some other way.",
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
            return [Diagnostic.Create(PropagationDisagrees, at, model.Name, propagation, component.ToDisplayString())];

        // Nothing sends a local shape's values, so nothing has to be stopped from writing them.
        return model.IsReplicated ? [] : [Diagnostic.Create(PropagationNeedsReplication, at, model.Name)];
    }

    public static ImmutableArray<Diagnostic> Check(DeclarationModel model)
    {
        var found = new List<Diagnostic>();
        var at = model.Symbol.Locations.FirstOrDefault() ?? Location.None;

        if (model.ExplicitComponent is { } component)
            return model.HasReplicatedAttribute
                ? [Diagnostic.Create(ComponentCannotReplicate, at, component.ToDisplayString(), model.Name)]
                : [];

        // Both rules hold whether or not the shape asked to replicate, because what a collection is
        // reachable by, and what nothing is reachable by, is decided before anything is sent.
        foreach (var accessor in model.Accessors)
        {
            if (!accessor.IsNativeContainer)
                continue;

            var declared = accessor.Declared?.Locations.FirstOrDefault() ?? at;

            if (!model.IsReplicated)
                found.Add(Diagnostic.Create(CollectionNeedsReplication, declared, model.Name, accessor.Name));
            else if (accessor.IsExposed)
                found.Add(Diagnostic.Create(CollectionIsExposed, declared, model.Name, accessor.Name));
        }

        if (model.HasReplicatedAttribute && model.Accessors.Count == 0)
            found.Add(Diagnostic.Create(NothingToReplicate, at, model.Name));

        return [.. found];
    }
}
