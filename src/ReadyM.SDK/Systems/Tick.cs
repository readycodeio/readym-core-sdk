namespace ReadyM.SDK.Systems;

/// <param name="deltaTime">Seconds since the last update.</param>
/// <param name="time">Seconds since the game started.</param>
/// <param name="count">Updates that came before this one.</param>
public readonly struct Tick(float deltaTime, float time, ulong count = 0)
{
    /// Seconds since the last update.
    public float DeltaTime { get; } = deltaTime;

    /// Seconds since the game started.
    public float Time { get; } = time;

    /// How many updates came before this one.
    public ulong Count { get; } = count;

    public override string ToString() => $"tick {Count}: {DeltaTime}s of {Time}s";
}
