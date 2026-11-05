using System;
using System.Collections.Generic;
using ReadyM.Api.Multiplayer.ECS.Components;

namespace ReadyM.Api.Multiplayer.ECS;

/// <summary>One world's hooks into reading and sending deltas, per component type. Used by prediction.</summary>
internal sealed class PredictionDeltaHooks
{
    private readonly Dictionary<Type, object> _byComponent = new();

    public PredictionDeltaHooks<T> For<T>() where T : struct, INetworkedComponent
    {
        if (!_byComponent.TryGetValue(typeof(T), out var hooks))
            _byComponent[typeof(T)] = hooks = new PredictionDeltaHooks<T>();

        return (PredictionDeltaHooks<T>)hooks;
    }
}

/// <summary>Hooks into reading and sending one component type's deltas.</summary>
internal sealed class PredictionDeltaHooks<T> where T : struct, INetworkedComponent
{
    public delegate void DeltaHook(int entityId, ref T component);

    public DeltaHook? BeforeDeltaRead;

    public DeltaHook? AfterDeltaRead;

    /// <summary>Before an owner's changed fields are sent.</summary>
    public Action? BeforeDeltaSend;
}
