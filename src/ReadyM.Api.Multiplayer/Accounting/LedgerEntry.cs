using Friflo.Engine.ECS;
using ReadOnlyPlayers = ReadyM.Api.Helpers.ReadOnlyList<ReadyM.Api.Idents.PlayerId>;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Values;

namespace ReadyM.Api.Multiplayer.Accounting;

/// <summary>
/// The inputs that pick a set of entities, and the players that are owed exactly that set; the ledger turns the inputs
/// into the entities with <see cref="EntityLedgerBase.Entities"/>. A null scope entity is the global scope.
/// </summary>
internal readonly struct LedgerEntry(
    int id,
    LedgerEntryKind kind,
    Entity scope,
    NetworkId scopeNetId,
    Entity from,
    bool unsent,
    PlayerId creator,
    Entity player,
    PlayerId excludedOrigin,
    ReadOnlyPlayers players,
    ReadyM.Api.Helpers.ReadOnlyList<NetworkId> netIds)
{
    /// <summary>Identifies the entry to <see cref="EntityLedgerBase.Sent"/>; never reused within a ledger.</summary>
    public int Id { get; } = id;

    public LedgerEntryKind Kind { get; } = kind;

    /// <summary>The scope the entities are in now, or the scope entered or left, or where deleted ones were.</summary>
    public Entity Scope { get; } = scope;

    /// <summary>The scope's net id, a default one for the global scope; a scope left may be gone already.</summary>
    public NetworkId ScopeNetId { get; } = scopeNetId;

    /// <summary>The scope the entities were last sent in: the same as <see cref="Scope"/> unless they moved since.</summary>
    public Entity From { get; } = from;

    /// <summary>Whether the entities were created since the last send.</summary>
    public bool Unsent { get; } = unsent;

    /// <summary>For unsent creates a client made from a scope it was still sent, that client, which holds them.</summary>
    public PlayerId Creator { get; } = creator;

    /// <summary>A player entity, picked on its own: the snapshot of its player scope carries it too.</summary>
    public Entity Player { get; } = player;

    /// <summary>For a create or delete a client made, that client, which already has it.</summary>
    public PlayerId ExcludedOrigin { get; } = excludedOrigin;

    public ReadOnlyPlayers Players { get; } = players;

    /// <summary>For a delete, the entities' net ids: the entities themselves may be gone.</summary>
    public ReadyM.Api.Helpers.ReadOnlyList<NetworkId> NetIds { get; } = netIds;
}
