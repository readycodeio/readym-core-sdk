namespace ReadyM.Relay.Server.Sdk.Interop;

/// <exclude/>
public struct ModPointers
{
    public IntPtr TickSystems;

    /// Invoked by the host after it creates an entity, so mod components can run their native init.
    public IntPtr PostCreateEntityInit;

    /// Invoked by the host before it deletes an entity, so mod shapes can run their delete handlers.
    public IntPtr EntityDeleted;
}