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

    protected UpdateTick Tick;

    protected abstract void OnUpdate();

    public void Update(float deltaTime, float time)
    {
        Tick = new UpdateTick(deltaTime, time);
        OnUpdate();
    }
}