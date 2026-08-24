using System.Runtime.InteropServices;
using LiteNetLib;
using ReadyM.Api.Idents;

namespace ReadyM.Api.Multiplayer.Interop;

[StructLayout(LayoutKind.Sequential)]
public struct ServerEventPayload
{
    public PlayerId Player;
    public AreaId Area;
    public CellId Cell;
    public int EntityId;
    public DisconnectReason Reason;
}
