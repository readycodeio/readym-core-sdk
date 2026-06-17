using System.Runtime.InteropServices;
using LiteNetLib;
using LiteNetLib.Utils;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Interop;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.Api.Multiplayer.Protocol.Enums;
using ReadyM.Relay.Server.Sdk.Interop;

namespace ReadyM.Relay.Server.Sdk.Rpc;

public class RpcApi
{
    private readonly AddServerRpcMessageHandlerDelegate _addServerRpcMessageHandler;

    private readonly RemoveServerRpcMessageHandlerDelegate _removeServerRpcMessageHandler;

    private readonly SendToOneDelegate _sendToOne;

    private readonly Dictionary<Delegate, ServerRpcHandlerDelegate> _pinnedDelegates = new();
    private readonly PinnedDelegateStore _pinnedDelegateStore = new();
    private readonly Dictionary<Delegate, HashSet<RelayMessageCode>> _toCode = new();

    internal RpcApi(RpcApiPointers pointers)
    {
        _addServerRpcMessageHandler = Marshal.GetDelegateForFunctionPointer<AddServerRpcMessageHandlerDelegate>(pointers.AddServerRpcMessageHandler);
        _removeServerRpcMessageHandler = Marshal.GetDelegateForFunctionPointer<RemoveServerRpcMessageHandlerDelegate>(pointers
            .RemoveServerRpcMessageHandler);
        _sendToOne = Marshal.GetDelegateForFunctionPointer<SendToOneDelegate>(pointers.SendToOne);
    }

    public unsafe void AddServerRpcMessageHandler(RelayMessageCode eventCode, Action<ServerEventHeader, NetDataReader> handler)
    {
        if (!_pinnedDelegates.TryGetValue(handler, out var realHandler))
        {
            realHandler = (header, data, size) =>
            {
                // convert to NetDataReader
                var reader = new NetDataReader(new Span<byte>(data, size).ToArray());
                handler(header, reader);
            };
            _pinnedDelegates.Add(handler, realHandler);
            _pinnedDelegateStore.PinDelegate(realHandler);
        }

        _toCode.TryAdd(handler, []);
        _toCode[handler].Add(eventCode);

        _addServerRpcMessageHandler(eventCode, realHandler);
    }

    public void RemoveServerRpcMessageHandler(RelayMessageCode eventCode, Action<ServerEventHeader, NetDataReader> handler)
    {
        if (!_pinnedDelegates.Remove(handler, out var realHandler))
        {
            throw new InvalidOperationException(
                "Handler not found. Make sure to only remove handlers that were added.");
        }

        _removeServerRpcMessageHandler(eventCode, realHandler);

        if (!_toCode.TryGetValue(handler, out var codes))
        {
            throw new InvalidOperationException("Handler not found. Make sure to remove handlers that were added.");
        }

        if (!codes.Contains(eventCode))
        {
            throw new InvalidOperationException("Handler not found. Make sure to remove handlers that were added.");
        }

        codes.Remove(eventCode);

        if (codes.Count == 0)
        {
            _pinnedDelegateStore.UnpinDelegate(realHandler);
            _pinnedDelegates.Remove(handler);
            _toCode.Remove(handler);
        }
    }

    public unsafe void SendToOne(PlayerId player, NetDataWriter data, DeliveryMethod deliveryMethod)
    {
        fixed (byte* ptr = data.Data)
        {
            _sendToOne(player, ptr, data.Length, deliveryMethod);
        }
    }
}