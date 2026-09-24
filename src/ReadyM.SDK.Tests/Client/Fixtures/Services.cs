using ReadyM.SDK.Attributes;
using ReadyM.SDK.Client.Entities;
using ReadyM.SDK.Services;

namespace ReadyM.SDK.Tests.Client.Fixtures;

/// A service asks for what it needs in its constructor, the way any class does.
[Service]
public sealed partial class Regeneration(IEntities entities)
{
    public float Healed { get; private set; }

    private void Update()
    {
        foreach (var chest in entities.Query<Chest>())
            chest.Gold += (int)(5f * Time.DeltaTime);

        Healed += Time.DeltaTime;
    }
}

/// An update takes nothing: a service that cares about time reads it off its own Time.
[Service]
public sealed partial class Counting
{
    public int Ticks { get; private set; }

    private void Update() => Ticks++;
}

/// Reads the clock off itself, so a test can see what a game put together.
[Service]
public sealed partial class Recording
{
    public UpdateTime Last { get; private set; }

    private void Update() => Last = Time;
}

/// A service is a singleton like any other, so one can be handed another.
[Service]
public sealed partial class Auditing(Counting counting)
{
    public int Seen { get; private set; }

    private void Update() => Seen = counting.Ticks;
}

/// Nothing runs on it and nothing watches through it. It is still one instance anything can ask for,
/// which is what most services are for.
[Service]
public sealed partial class Settings
{
    public int Difficulty { get; set; } = 3;
}

/// Start and Stop are the game's own bookends, and a service may declare either one alone.
[Service]
public sealed partial class Bookends
{
    public int Started { get; private set; }

    public int Stopped { get; private set; }

    private void Start() => Started++;

    private void Stop() => Stopped++;
}

[Service]
public sealed partial class StartOnly
{
    public bool Started { get; private set; }

    private void Start() => Started = true;
}

/// Watches a shape it did not declare, which is how a service reaches a moment a mixin owns.
[Service]
public sealed partial class Bookkeeping
{
    public List<int> Seen { get; } = [];

    [CreateHandler(typeof(Parcel))]
    private void Track(Parcel parcel) => Seen.Add(parcel.Mark);
}
