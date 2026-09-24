using ReadyM.SDK.Attributes;
using ReadyM.SDK.Client.Entities;
using ReadyM.SDK.Systems;

namespace ReadyM.SDK.Tests.Client.Fixtures;

/// A system asks for what it needs in its constructor, the way any class does.
[System]
public partial class Regeneration(IEntities entities)
{
    public float Healed { get; private set; }

    private void Update(Tick tick)
    {
        foreach (var chest in entities.Query<Chest>())
            chest.Gold += (int)(5f * tick.DeltaTime);

        Healed += tick.DeltaTime;
    }
}

/// The tick is optional: a system that does not care about time need not name it.
[System]
public partial class Counting
{
    public int Ticks { get; private set; }

    private void Update() => Ticks++;
}
