using Friflo.Engine.ECS;

namespace ReadyM.Api.Multiplayer.Mapping.Tags;

/// <exclude />
public interface IReadyComponent : IComponent
{
    bool ChangedFromApi { get; }
    void ClearApiFlag();
    void ClearApiFlag(int field);
}