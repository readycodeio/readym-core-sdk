using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Idents;

namespace ReadyM.Api.Multiplayer.Accounting;

/// <summary>
/// What the entities of one group share, which decides what each player is owed about them: the scope they are in now,
/// the scope they were last sent in, and who holds them already. A null scope entity is the global scope.
/// </summary>
internal readonly struct LedgerGroup(Entity scope, Entity from, bool unsent, PlayerId origin, Entity asScope)
    : IEquatable<LedgerGroup>
{
    /// <summary>The scope the entities are in now.</summary>
    public Entity Scope { get; } = scope;

    /// <summary>The scope the entities were last sent in, or created in.</summary>
    public Entity From { get; } = from;

    /// <summary>Whether the entities were created since the last send.</summary>
    public bool Unsent { get; } = unsent;

    /// <summary>For an unsent create a client made from inside its scope, that client, which holds the entities already.</summary>
    public PlayerId Origin { get; } = origin;

    /// <summary>For a player entity, itself: the snapshot of its player scope carries it too.</summary>
    public Entity AsScope { get; } = asScope;

    /// <summary>Whether an entity of this group, its owner unchanged, is owed to nobody.</summary>
    public bool OwesNothing => !Unsent && From == Scope;

    public LedgerGroup In(Entity newScope)
        => new(newScope, From, Unsent, Origin, AsScope);

    public bool Equals(LedgerGroup other)
        => Scope == other.Scope && From == other.From && Unsent == other.Unsent && Origin == other.Origin
           && AsScope == other.AsScope;

    public override bool Equals(object? obj)
        => obj is LedgerGroup other && Equals(other);

    public override int GetHashCode()
    {
        var hash = Scope.GetHashCode();
        hash = hash * 31 + From.GetHashCode();
        hash = hash * 31 + (Unsent ? 1 : 0);
        hash = hash * 31 + Origin.GetHashCode();
        return hash * 31 + AsScope.GetHashCode();
    }
}
