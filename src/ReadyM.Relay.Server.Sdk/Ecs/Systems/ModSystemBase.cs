using ReadyM.Api.Multiplayer;

namespace ReadyM.Relay.Server.Sdk.Ecs.Systems;

/// <summary>
/// A base class for server-side mod systems that need to perform updates each tick.
/// Inherit from this class and implement the <see cref="OnUpdate(UpdateTick)"/> method to define your system's behavior.
/// </summary>
public abstract class ModSystemBase
{
    /// <summary>
    /// Holds the time information for the current update tick.
    /// </summary>
    /// <param name="deltaTime">Time since last tick, in seconds.</param>
    /// <param name="time">Total time since server start, in seconds.</param>
    protected readonly struct UpdateTick(float deltaTime, float time, uint netTicks)
    {
        /// <summary> The time in seconds since the last tick. </summary>
        public readonly float DeltaTime = deltaTime;

        /// <summary> The time at the beginning of the current frame since application start. </summary>
        public readonly float Time = time;

        /// <summary> The number of server authority network ticks since server start. </summary>
        public readonly uint NetTicks = netTicks;
    }

    /// <summary>
    /// Called every tick to update the system. Implement this method in derived classes to define the system's behavior.
    /// </summary>
    /// <param name="tick"> The current update tick information, including delta time and total time.</param>
    protected abstract void OnUpdate(UpdateTick tick);

    /// <summary>
    /// Runs the system update for the current tick.
    /// </summary>
    /// <param name="deltaTime">Time since last tick, in seconds.</param>
    /// <param name="time">Total time since server start, in seconds.</param>
    /// <param name="netTicks">The number of server authority network ticks since server start.</param>
    public void Update(float deltaTime, float time, uint netTicks)
    {
        // Mod writes are authoritative: auto-mark so owned-component overrides reach the owner.
        // NOTE: Already on the right ECS thread
        using var _ = ComponentWriteContext.EnterServerAuthoring(netTicks);
        OnUpdate(new UpdateTick(deltaTime, time, netTicks));
    }
}
