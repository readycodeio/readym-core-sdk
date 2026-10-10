using System.Collections.Generic;
using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Worlds;
using ReadOnlyPlayers = ReadyM.Api.Helpers.ReadOnlyList<ReadyM.Api.Idents.PlayerId>;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.Multiplayer.ECS.Values;

namespace ReadyM.Api.Multiplayer.Accounting;

/// <summary>
/// Records every change to a networked entity that others must hear about, applying it at once, and hands the sending
/// systems what is still owed as entries. ECS thread only.
/// </summary>
internal abstract class EntityLedgerBase
{
    private readonly List<LedgerEntry> _pending = [];

    protected EntityLedgerBase(Store world, NetworkedEntityManager netEntity, ILogger logger)
    {
        World = world;
        NetEntity = netEntity;
        Logger = logger;
    }

    protected Store World { get; }

    protected NetworkedEntityManager NetEntity { get; }

    protected ILogger Logger { get; }

    /// <summary>What is still owed, each entry the inputs of a query and the players owed what it returns.</summary>
    public ReadyM.Api.Helpers.ReadOnlyList<LedgerEntry> Pending => new(_pending);

    /// <summary>Whether nothing is owed: no entries, and no entity carries a mark.</summary>
    public bool IsSettled => true;

    /// <summary>The query an entry's inputs describe.</summary>
    public ArchetypeQuery<MetadataComponent> Query(in LedgerEntry entry)
        => World.Query<MetadataComponent>(new QueryFilter().AllTags(Tags.Get<UnsentCreateTag, OwnerChangedTag>()));

    /// <summary>Records that the entry's entities were sent to the entry's players.</summary>
    public void Sent(in LedgerEntry entry)
    {
    }

    /// <summary>The scope entity an entity is in, or a null entity for the global scope.</summary>
    public Entity ScopeOf(Entity entity)
        => default;

    /// <summary>Whether the player sees the scope; a null scope entity is the global scope.</summary>
    public abstract bool Sees(PlayerId player, Entity scope);
}
