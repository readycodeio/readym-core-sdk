namespace ReadyM.Api.Multiplayer.Accounting;

/// <summary>What a ledger entry owes its players.</summary>
internal enum LedgerEntryKind : byte
{
    EnterScope,
    LeaveScope,
    Create,
    Delete,
    OwnerChange,
    ScopeChange,
}
