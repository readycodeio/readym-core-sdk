using System;
using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using LiteNetLib;
using ReadyM.Api.Idents;

namespace ReadyM.Api.Multiplayer.Interop;

[StructLayout(LayoutKind.Sequential)]
internal struct ServerEventPayload
{
    public PlayerId Player;
    public AreaId Area;
    public CellId Cell;
    public RawEntity Entity;
    public DisconnectReason Reason;
    public Guid ReadyMId;
}
