using System.Linq;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Archetypes;

/// The contract a component carries so the runtime's policy directory can find its policy. The
/// declaration lives on the shape; this is how it reaches the component the shape generates.
internal static class PropagationContracts
{
    private const string Tags = "global::ReadyM.Api.Mapping.Tags.";

    public static string? Of(Propagation? propagation) => propagation switch
    {
        Archetypes.Propagation.ToEcsOnly => Tags + "IAlwaysPropagatesToEcsOnly",
        Archetypes.Propagation.ToGameOnly => Tags + "IAlwaysPropagatesToGameOnly",
        Archetypes.Propagation.Both => Tags + "IAlwaysPropagates",
        Archetypes.Propagation.ServerAuthoritative => Tags + "IServerAuthoritative",
        Archetypes.Propagation.OwnershipBased => Tags + "IOwnershipBased",
        _ => null
    };

    /// Whether a component already carries this contract, for a shape that named one it does not own.
    public static bool DeclaredBy(INamedTypeSymbol component, Propagation propagation)
        => Of(propagation) is { } contract
           && component.AllInterfaces.Any(i =>
               i.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == contract);
}
