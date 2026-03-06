using Friflo.Engine.ECS;

namespace ReadyM.Api.Multiplayer.Mapping;

public interface IReadyComponent : IComponent
{
    bool ChangedFromApi { get; }
    void ClearApiFlag(int field);
}

public interface IMappingContext<TContext>
{
    // empty
}