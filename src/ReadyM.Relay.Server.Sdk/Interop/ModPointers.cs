namespace ReadyM.Relay.Server.Sdk.Interop;

/// <exclude/>
public struct ModPointers
{
    public IntPtr TickSystems;

    /// Invoked by the host after it creates an entity, so mod components can run their native init.
    public IntPtr PostCreateEntityInit;

    /// Invoked by the host before it deletes an entity, so mod shapes can run their delete handlers.
    public IntPtr EntityDeleted;

    /// <c>SeederDescriptor[SeederCount]</c>, in the order the server registers them. Mod host memory, never freed.
    public IntPtr Seeders;

    public int SeederCount;

    /// <c>SeedScopeDelegate</c> over the indexes of <see cref="Seeders"/>; zero when there are none.
    public IntPtr SeedScope;
}