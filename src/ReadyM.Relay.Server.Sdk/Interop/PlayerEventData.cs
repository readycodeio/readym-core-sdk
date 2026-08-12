using System.Runtime.InteropServices;
using ReadyM.Api.Idents;

namespace ReadyM.Relay.Server.Sdk.Interop;

[StructLayout(LayoutKind.Sequential)]
public struct PlayerEventData
{
    public PlayerEventKind Kind;
    public PlayerId PlayerId;
    public Guid ReadyMId;
}
