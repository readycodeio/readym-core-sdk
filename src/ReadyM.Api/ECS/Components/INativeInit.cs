using Yooni.Native.LowLevel;

namespace ReadyM.Api.ECS.Components;

/// <summary>
/// Interface for components that require native initialization, such as allocating unmanaged resources or setting up internal state.
/// </summary>
public interface INativeInit
{
    void Init(AllocatorKind allocatorKind);
}
