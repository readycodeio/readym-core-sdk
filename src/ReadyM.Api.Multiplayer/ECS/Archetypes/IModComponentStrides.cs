using System;

namespace ReadyM.Api.Multiplayer.ECS.Archetypes;

/// Turns a mod component id into what an archetype needs to hold one.
/// <exclude/>
public interface IModComponentStrides
{
    (int StructIndex, int Stride) Of(int componentId);
}

/// Used where no mod host is running, and never asked in that case.
/// <exclude/>
public sealed class NoModComponentStrides : IModComponentStrides
{
    public (int StructIndex, int Stride) Of(int componentId)
        => throw new InvalidOperationException(
            $"Mod component {componentId} was named by an archetype, but no mod host is running.");
}
