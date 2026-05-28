using System.Runtime.InteropServices;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Interop;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.Api.Multiplayer.Protocol.Enums;
using ReadyM.Relay.Server.Sdk.Interop;

namespace ReadyM.Relay.Server.Sdk;

public class RpcApi(RpcApiPointers pointers)
{
    private readonly AddServerRpcMessageHandlerDelegate _addServerRpcMessageHandler = Marshal.GetDelegateForFunctionPointer<AddServerRpcMessageHandlerDelegate>(pointers.AddServerRpcMessageHandler);
    private readonly RemoveServerRpcMessageHandlerDelegate _removeServerRpcMessageHandler = Marshal.GetDelegateForFunctionPointer<RemoveServerRpcMessageHandlerDelegate>(pointers.RemoveServerRpcMessageHandler);

    private readonly PinnedDelegateStore _pinnedDelegateStore = new();

    public unsafe void AddServerRpcMessageHandler(RelayMessageCode eventCode, Action<ServerEventHeader, NetDataReader> handler)
    {
        ServerRpcHandlerDelegate realHandler = (header, data, length) =>
        {
            // convert to NetDataReader
            var dataSpan = new Span<byte>(data, length);
            var reader = new NetDataReader(dataSpan.ToArray());
            handler(header, reader);
        };

        _pinnedDelegateStore.PinDelegate(realHandler);
        _addServerRpcMessageHandler(eventCode, realHandler);
    }

    public void RemoveServerRpcMessageHandler(RelayMessageCode eventCode, Action<ServerEventHeader, NetDataReader> handler)
    {
        throw new NotImplementedException();
    }
}