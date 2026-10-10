using Friflo.Engine.ECS;
using ReadOnlyPlayers = ReadyM.Api.Helpers.ReadOnlyList<ReadyM.Api.Idents.PlayerId>;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Values;

namespace ReadyM.Api.Multiplayer.Accounting;

/// <summary>
/// The inputs of one query and the players that are owed exactly what it returns. A null scope entity is the global
/// scope.
/// </summary>
internal readonly struct LedgerEntry(
    int id,
    LedgerEntryKind kind,
    Entity scope,
    Entity from,
    PlayerId excludedOrigin,
    ReadOnlyPlayers players,
    ReadyM.Api.Helpers.ReadOnlyList<NetworkId> netIds)
{
    /// <summary>Identifies the entry to <see cref="EntityLedgerBase.Sent"/>; never reused within a ledger.</summary>
    public int Id { get; } = id;

    public LedgerEntryKind Kind { get; } = kind;

    /// <summary>The scope the entities are in now, or the scope entered or left.</summary>
    public Entity Scope { get; } = scope;

    /// <summary>For a scope change, the scope the entities were last sent in.</summary>
    public Entity From { get; } = from;

    /// <summary>For a create or delete a client made, that client, which already has it.</summary>
    public PlayerId ExcludedOrigin { get; } = excludedOrigin;

    public ReadOnlyPlayers Players { get; } = players;

    /// <summary>For a delete, the entities' net ids: the entities themselves are gone.</summary>
    public ReadyM.Api.Helpers.ReadOnlyList<NetworkId> NetIds { get; } = netIds;
}
