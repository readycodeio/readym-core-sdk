namespace ReadyM.SDK.Systems;

/// <param name="deltaTime">Seconds since the last update.</param>
/// <param name="time">Seconds since the game started.</param>
public readonly struct Tick(float deltaTime, float time)
{
    /// Seconds since the last update.
    public float DeltaTime { get; } = deltaTime;

    /// Seconds since the game started.
    public float Time { get; } = time;

    public override string ToString() => $"tick: {DeltaTime}s of {Time}s";
}
