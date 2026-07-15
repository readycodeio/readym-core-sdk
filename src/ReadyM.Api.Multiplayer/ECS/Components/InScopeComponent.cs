using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using Friflo.Json.Fliox;

namespace ReadyM.Api.Multiplayer.ECS.Components;

[StructLayout(LayoutKind.Auto)]
internal struct InScopeComponent(Entity scopeEntity) : ILinkComponent
{
    [Ignore]
    public Entity ScopeEntity = scopeEntity;

    public Entity GetIndexedValue()
        => ScopeEntity;
}