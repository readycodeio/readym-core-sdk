using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using ReadyM.Api.Attributes;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Generators;

namespace ReadyM.Api.Multiplayer.ECS.Components;

[DeriveINetworkedComponent(emitDirtyMask: false), NativeComponent]
[StructLayout(LayoutKind.Sequential)]
public partial struct AreaScopeComponent : IIndexedComponent<AreaId>
{
    private byte _dirtyMask;
    private byte _apiMask;
    
    private AreaId _areaId;
    private PlayerId _masterClient;

    public AreaId GetIndexedValue()
        => _areaId;
}