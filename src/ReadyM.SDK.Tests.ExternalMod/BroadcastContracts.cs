using ReadyM.Api.Multiplayer;
using ReadyM.SDK.Attributes;

namespace ReadyM.SDK.Tests.ExternalMod;

/// <summary>Server-to-client RPCs, one with a payload and one without, for the broadcast sends.</summary>
[RpcContracts]
public static partial class BroadcastContracts
{
    [ServerToClient] public static partial void Announcement(string text, int roll);
    [ServerToClient] public static partial void Ping();
}
