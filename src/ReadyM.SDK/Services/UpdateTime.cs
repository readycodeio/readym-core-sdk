namespace ReadyM.SDK.Services;

/// <param name="deltaTime">Seconds since the last update.</param>
/// <param name="time">Seconds since the game started.</param>
/// <param name="ticks">Updates that came before this one.</param>
public readonly struct UpdateTime(float deltaTime, float time, ulong ticks = 0)
{
    /// Seconds since the last update.
    public float DeltaTime { get; } = deltaTime;

    /// Seconds since the game started.
    public float Elapsed { get; } = time;

    /// How many updates came before this one.
    public ulong Ticks { get; } = ticks;

    public override string ToString() => $"update {Ticks}: {DeltaTime}s of {Elapsed}s";
}
