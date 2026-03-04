using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Generators;

namespace ReadyM.Api.Multiplayer.ECS.Components;

[DeriveINetworkedComponent]
[StructLayout(LayoutKind.Auto)]
public partial struct AreaScopeComponent : IIndexedComponent<AreaId>
{
    private AreaId _areaId;
    private PlayerId _masterClient;

    public AreaId GetIndexedValue()
        => AreaId;
}