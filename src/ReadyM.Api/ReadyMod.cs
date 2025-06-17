using System;
using Friflo.Engine.ECS;

namespace ReadyM.Api;

public abstract class ReadyMod : IEcs
{
    public Store World { get; }
    public CommandBufferSynced CommandBuffer { get; }

    public bool IsInitialized { get; private set; }

    protected ReadyMod()
    {
        World = ReadyMApp.CreateEntityStore();

        var cb = World.GetCommandBuffer();
        cb.ReuseBuffer = true;
        CommandBuffer = cb.Synced;
    }

    protected abstract void ConfigureMod(IModConfig config);

    public virtual void Initialize()
    {
        if (IsInitialized)
            throw new InvalidOperationException("Mod is already initialized.");

        IsInitialized = true;
        
        var builder = new ModConfig(this);
        ConfigureMod(builder);
    }

    public virtual void Deinitialize()
    {
        if (!IsInitialized)
            throw new InvalidOperationException("Mod is not initialized.");

        IsInitialized = false;
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