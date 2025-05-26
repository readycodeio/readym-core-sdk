using Friflo.Engine.ECS;

namespace ReadyM.Api;

public class ReadyMod
{
    public Store World { get; }
    public CommandBufferSynced CommandBuffer { get; }

    public ReadyMod()
    {
        World = ReadyMApp.CreateEntityStore();

        var cb = World.GetCommandBuffer();
        cb.ReuseBuffer = true;
        CommandBuffer = cb.Synced;
    }

    /// <summary>
    /// Execute the mod logic for the current tick.
    /// Make sure to call this method in the game loop, once per frame, to ensure the mod is updated.
    /// </summary>
    /// <param name="tick">Delta time since the last tick.</param>
    public void Tick(UpdateTick tick)
    {
        lock (CommandBuffer)
        {
            CommandBuffer.Playback();
        }

        World.SystemRoot.Update(default);
    }
}