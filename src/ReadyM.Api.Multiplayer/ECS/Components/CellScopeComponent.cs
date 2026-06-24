using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using ReadyM.Api.Attributes;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Generators;

namespace ReadyM.Api.Multiplayer.ECS.Components;

[DeriveINetworkedComponent(emitDirtyMask: false), NativeComponent]
[StructLayout(LayoutKind.Sequential)]
internal partial struct CellScopeComponent : IIndexedComponent<CellId>
{
    private byte _dirtyMask;
    private byte _apiMask;

    private CellId _cellId;
    private AreaId _parentAreaId;
    private PlayerId _masterClient;

    public CellId GetIndexedValue()
        => _cellId;
}
