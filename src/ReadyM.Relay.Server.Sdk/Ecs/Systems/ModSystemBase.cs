using ReadyM.Api.Multiplayer;

namespace ReadyM.Relay.Server.Sdk.Ecs.Systems;

public abstract class ModSystemBase
{
    protected readonly struct UpdateTick(float deltaTime, float time)
    {
        /// <summary> The time in seconds since the last tick. </summary>
        public readonly float deltaTime = deltaTime;

        /// <summary> The time at the beginning of the current frame since application start. </summary>
        public readonly float time = time;
    }

    protected abstract void OnUpdate(UpdateTick tick);

    public void Update(float deltaTime, float time)
    {
        // Mod writes are authoritative: auto-mark so owned-component overrides reach the owner.
        using var _ = ComponentWriteContext.EnterServerAuthoring();
        OnUpdate(new UpdateTick(deltaTime, time));
    }
}