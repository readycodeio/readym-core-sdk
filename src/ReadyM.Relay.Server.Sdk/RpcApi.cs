using System.Runtime.InteropServices;
using LiteNetLib;
using LiteNetLib.Utils;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Interop;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.Api.Multiplayer.Protocol.Enums;
using ReadyM.Relay.Server.Sdk.Interop;

namespace ReadyM.Relay.Server.Sdk;

public class RpcApi(RpcApiPointers pointers)
{
    private readonly AddServerRpcMessageHandlerDelegate _addServerRpcMessageHandler = Marshal.GetDelegateForFunctionPointer<AddServerRpcMessageHandlerDelegate>(pointers.AddServerRpcMessageHandler);
    private readonly RemoveServerRpcMessageHandlerDelegate _removeServerRpcMessageHandler = Marshal.GetDelegateForFunctionPointer<RemoveServerRpcMessageHandlerDelegate>(pointers.RemoveServerRpcMessageHandler);
    private readonly SendToOneDelegate _sendToOne = Marshal.GetDelegateForFunctionPointer<SendToOneDelegate>(pointers.SendToOne);

    private readonly Dictionary<Delegate, ServerRpcHandlerDelegate> _pinnedDelegates = new();
    private readonly PinnedDelegateStore _pinnedDelegateStore = new();

    public void AddServerRpcMessageHandler(RelayMessageCode eventCode, Action<ServerEventHeader, NetDataReader> handler)
    {
        ServerRpcHandlerDelegate realHandler = (header, data) =>
        {
            // convert to NetDataReader
            var reader = new NetDataReader(data.ToArray());
            handler(header, reader);
        };

        _pinnedDelegates.Add(handler, realHandler);
        _pinnedDelegateStore.PinDelegate(realHandler);

        _addServerRpcMessageHandler(eventCode, realHandler);
    }

    public void RemoveServerRpcMessageHandler(RelayMessageCode eventCode, Action<ServerEventHeader, NetDataReader> handler)
    {
        if (!_pinnedDelegates.Remove(handler, out var realHandler))
        {
            throw new InvalidOperationException("Handler not found. Make sure to only remove handlers that were added.");
        }

        _removeServerRpcMessageHandler(eventCode, realHandler);
        _pinnedDelegateStore.UnpinDelegate(realHandler);
    }

    public void SendToOne(PlayerId player, NetDataWriter data, DeliveryMethod deliveryMethod)
    {
        var span = new Span<byte>(data.Data, 0, data.Length);
        _sendToOne(player, span, deliveryMethod);
    }
}