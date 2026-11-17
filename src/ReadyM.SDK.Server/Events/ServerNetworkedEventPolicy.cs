using ReadyM.Api.Idents;
using ReadyM.SDK.Attributes;
using ReadyM.SDK.Entities;
using ReadyM.SDK.Server.Entities;

namespace ReadyM.SDK.Server.Events;

/// <summary>Decides whether a player may raise an event, before any handler runs.</summary>
internal sealed class ServerNetworkedEventPolicy(ServerNetworkedEventScopes scopes)
{
    /// <param name="subject">The entity the event marks <c>[Subject]</c>, or null.</param>
    /// <param name="area">The area the event marks <c>[Area]</c>, or null.</param>
    /// <param name="cell">The cell the event marks <c>[Cell]</c>, or null.</param>
    /// <param name="origin">The player whose connection the event arrived on.</param>
    /// <exception cref="ArgumentOutOfRangeException">The policy checks a member the event does not have.</exception>
    /// <param name="dimension">The dimension the named area or cell is looked up in.</param>
    public bool Allows(
        Policy policy, EntityHandle? subject, AreaId? area, FullCellId? cell, PlayerId origin,
        DimensionId dimension = default)
    {
        if (origin == PlayerId.Server)
            return true;

        return policy switch
        {
            Policy.Anyone => true,
            Policy.ServerAuthoritative => false,
            Policy.SubjectOwner => Required(subject, "[Subject]").TryGetOwner(out var owner) && owner == origin,
            Policy.CellMaster => scopes.TryFindCell(Required(cell, "[Cell]"), dimension, out var named)
                && named.MasterClient == origin,
            Policy.AreaMaster => scopes.TryFindArea(Required(area, "[Area]"), dimension, out var named)
                && named.MasterClient == origin,
            _ => throw new ArgumentOutOfRangeException(nameof(policy), policy, "Unhandled networked event policy."),
        };
    }

    // The generator refuses these declarations (NEVT004, NEVT007, NEVT008), so a miss means the two disagree
    private static TMember Required<TMember>(TMember? member, string marker) where TMember : struct
        => member ?? throw new ArgumentOutOfRangeException(
            nameof(member), $"This policy checks the event's {marker} member, and the event has none.");
}
