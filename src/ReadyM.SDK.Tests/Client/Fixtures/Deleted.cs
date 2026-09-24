using ReadyM.SDK.Attributes;

namespace ReadyM.SDK.Tests.Client.Fixtures;

/// Keeps what a delete handler saw, because the entity is gone by the time a test can look.
[Service]
public sealed partial class Ledger
{
    public List<string> Order { get; } = [];

    public List<int> Chalk { get; } = [];
}

/// What a shape lets go of as one of its entities goes. A handler asks the game for what it needs,
/// the same way a create handler does.
[ArchetypeMixin]
public readonly partial struct Chalked
{
    public partial int Chalk { get; set; }

    [CreateHandler]
    private void OnCreated() => Chalk = 3;

    [DeleteHandler]
    private void OnDeleted(Ledger ledger)
    {
        ledger.Order.Add("shape");
        ledger.Chalk.Add(Chalk);
    }
}

[Archetype]
[Include(typeof(Chalked))]
public readonly partial struct Crateload;

/// A service watching a shape it did not declare, on the way out this time.
[Service]
public sealed partial class Undertaking(Ledger ledger)
{
    [DeleteHandler(typeof(Crateload))]
    private void Track(Crateload crate)
    {
        ledger.Order.Add("service");
        ledger.Chalk.Add(crate.Chalk);
    }
}
