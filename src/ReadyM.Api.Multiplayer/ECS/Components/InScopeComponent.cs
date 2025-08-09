using System.Runtime.InteropServices;
using Friflo.Engine.ECS;

namespace ReadyM.Api.Multiplayer.ECS.Components;

[StructLayout(LayoutKind.Auto)]
public struct InScopeComponent(Entity scopeEntity) : ILinkComponent
{
    public Entity ScopeEntity = scopeEntity;

    public Entity GetIndexedValue()
        => ScopeEntity;
}