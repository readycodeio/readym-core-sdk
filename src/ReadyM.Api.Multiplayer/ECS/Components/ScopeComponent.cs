using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.Generators;

namespace ReadyM.Api.Multiplayer.ECS.Components;

[NetworkedComponent]
[StructLayout(LayoutKind.Auto)]
public partial struct ScopeComponent(Entity scopeEntity) : ILinkComponent
{
    public Entity ScopeEntity = scopeEntity;

    public Entity GetIndexedValue()
        => ScopeEntity;
}