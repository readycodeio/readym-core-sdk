using System.Collections.Generic;
using Friflo.Engine.ECS.Systems;

namespace ReadyM.Api.Multiplayer.Accounting;

/// <summary>Sends every pending entry of one kind and reports each to the ledger as sent. ECS thread only.</summary>
internal abstract class LedgerSendSystemBase<TLedger>(TLedger ledger, LedgerEntryKind kind) : BaseSystem
    where TLedger : EntityLedgerBase
{
    // Reused each update: sending an entry changes the ledger's pending list.
    private readonly List<LedgerEntry> _batch = [];

    protected TLedger Ledger => ledger;

    protected override void OnUpdateGroup()
    {
        _batch.Clear();
        foreach (var entry in ledger.Pending)
        {
            if (entry.Kind == kind)
                _batch.Add(entry);
        }

        foreach (var entry in _batch)
        {
            Send(entry);
            ledger.Sent(entry);
        }
    }

    /// <summary>Sends the entry's entities to the entry's players, in one message.</summary>
    protected abstract void Send(in LedgerEntry entry);
}
