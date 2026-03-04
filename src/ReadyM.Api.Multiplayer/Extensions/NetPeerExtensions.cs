using LiteNetLib;
using LiteNetLib.Utils;

namespace ReadyM.Api.Multiplayer.Extensions;

public static class NetPeerExtensions
{
    public static void SendImmediately(this NetPeer peer, byte[] data, int start, int length, DeliveryMethod options)
    {
        peer.Send(data, start, length, options);
        peer.NetManager.TriggerUpdate();
    }

    public static void SendImmediately(this NetPeer peer, NetDataWriter dataWriter, DeliveryMethod deliveryMethod)
    {
        peer.Send(dataWriter, deliveryMethod);
        peer.NetManager.TriggerUpdate();
    }
}