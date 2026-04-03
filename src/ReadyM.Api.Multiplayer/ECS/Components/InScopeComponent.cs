using Friflo.Engine.ECS;
using Friflo.Json.Fliox;

namespace ReadyM.Api.Multiplayer.ECS.Components;

public struct InScopeComponent(Entity scopeEntity) : ILinkComponent
{
    [Ignore]
    public Entity ScopeEntity = scopeEntity;

    public Entity GetIndexedValue()
        => ScopeEntity;
}