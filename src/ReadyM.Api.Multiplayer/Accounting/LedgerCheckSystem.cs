using Friflo.Engine.ECS.Systems;
using Microsoft.Extensions.Logging;

namespace ReadyM.Api.Multiplayer.Accounting;

/// <summary>Runs after the sending systems and logs an error if anything is still owed; it changes nothing.</summary>
internal sealed class LedgerCheckSystem(EntityLedgerBase ledger, ILogger logger) : BaseSystem
{
    protected override void OnUpdateGroup()
    {
        if (!ledger.IsSettled)
            logger.LogError("The entity ledger still owes {Count} entries after every send", ledger.Pending.Count);
    }
}
