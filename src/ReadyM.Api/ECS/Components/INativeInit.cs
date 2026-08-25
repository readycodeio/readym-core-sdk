using Yooni.Native.LowLevel;

namespace ReadyM.Api.ECS.Components;

public interface INativeInit
{
    void Init(AllocatorKind allocatorKind);
}
