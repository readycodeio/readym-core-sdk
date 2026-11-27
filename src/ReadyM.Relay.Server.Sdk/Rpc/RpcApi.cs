using System.ComponentModel;
using System.Runtime.InteropServices;
using LiteNetLib;
using LiteNetLib.Utils;
using ReadyM.Api.Idents;
using ReadyM.Api.Interop;
using ReadyM.Api.Multiplayer;
using ReadyM.Api.Multiplayer.ConflictResolution;
using ReadyM.Api.Multiplayer.Interop;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.Api.Multiplayer.Protocol.Enums;
using ReadyM.Native.Container;
using ReadyM.Relay.Server.Sdk.Interop;

namespace ReadyM.Relay.Server.Sdk.Rpc;

/// <summary>
/// Low-level API for handling RPC messages in the ReadyM Relay Server SDK.
/// </summary>
/// <exclude />
[EditorBrowsable(EditorBrowsableState.Never)]
public class RpcApi
{
    private readonly AddServerRpcMessageHandlerDelegate _addServerRpcMessageHandler;
    private readonly RemoveServerRpcMessageHandlerDelegate _removeServerRpcMessageHandler;
    private readonly SendToOneDelegate _sendToOne;
    private readonly NameMessageCodeDelegate _nameMessageCode;

    private readonly INetworkTime _netTime;
    private readonly IChangeTrackingStore _changeTracking;
    private readonly Dictionary<Delegate, ServerRpcHandlerDelegate> _pinnedDelegates = new();
    private readonly PinnedDelegateStore _pinnedDelegateStore = new();
    // A code once per time it was added, as the relay holds it
    private readonly Dictionary<Delegate, List<RelayMessageCode>> _toCode = new();

    internal RpcApi(RpcApiPointers pointers, INetworkTime netTime, IChangeTrackingStore changeTracking)
    {
        _netTime = netTime;
        _changeTracking = changeTracking;
        _addServerRpcMessageHandler = Marshal.GetDelegateForFunctionPointer<AddServerRpcMessageHandlerDelegate>(pointers.AddServerRpcMessageHandler);
        _removeServerRpcMessageHandler = Marshal.GetDelegateForFunctionPointer<RemoveServerRpcMessageHandlerDelegate>(pointers.RemoveServerRpcMessageHandler);
        _sendToOne = Marshal.GetDelegateForFunctionPointer<SendToOneDelegate>(pointers.SendToOne);
        _nameMessageCode = Marshal.GetDelegateForFunctionPointer<NameMessageCodeDelegate>(pointers.NameMessageCode);
    }

    /// <summary>Tells the relay what a code is called, for its metrics.</summary>
    internal void NameMessageCode(RelayMessageCode code, string name)
        => _nameMessageCode(code, new NativeString256(name, false));

    /// <exclude/>
    public void AddServerRpcMessageHandler(RelayMessageCode eventCode, Action<ServerEventHeader, NetDataReader> handler)
        => Add(eventCode, handler, (header, _, reader) => handler(header, reader));

    /// <exclude/>
    /// <summary>A handler that gets the sender's current dimension with each message.</summary>
    public void AddServerRpcMessageHandler(
        RelayMessageCode eventCode, Action<ServerEventHeader, DimensionId, NetDataReader> handler)
        => Add(eventCode, handler, handler);

    /// <exclude/>
    public void RemoveServerRpcMessageHandler(RelayMessageCode eventCode, Action<ServerEventHeader, NetDataReader> handler)
        => Remove(eventCode, handler);

    /// <exclude/>
    public void RemoveServerRpcMessageHandler(
        RelayMessageCode eventCode, Action<ServerEventHeader, DimensionId, NetDataReader> handler)
        => Remove(eventCode, handler);

    // Kept by the handler the caller passed, which is how a later remove names it
    private unsafe void Add(
        RelayMessageCode eventCode, Delegate key, Action<ServerEventHeader, DimensionId, NetDataReader> handler)
    {
        if (!_pinnedDelegates.TryGetValue(key, out var realHandler))
        {
            realHandler = (header, dimension, data, size) =>
            {
                // convert to NetDataReader
                var reader = new NetDataReader(new Span<byte>(data, size).ToArray());

                // RPC handler writes are authoritative: auto-mark so overrides reach the owner.
                using var _ = ComponentWriteContext.EnterServerAuthoring(_netTime.GetCurrentTime(), _changeTracking);

                // NOTE: ATTENTION!!! RPC server-side handlers have been moved to the ECS thread.
                // The current implementation does the scheduling in a architecturally questionable place inside
                // `ModHostInitializer` assignment to `ptrToAddServerRpcMessageHandler`. There's probably a better
                // place to put this.
                // Being on the wrong thread means that any [ThreadStatic] state such as `ComponentWriteContext.Current`
                // would have to be carried into the ECS thread inside the scheduling capability
                handler(header, dimension, reader);
            };
            _pinnedDelegates.Add(key, realHandler);
            _pinnedDelegateStore.PinDelegate(realHandler);
        }

        _toCode.TryAdd(key, []);
        _toCode[key].Add(eventCode);

        _addServerRpcMessageHandler(eventCode, realHandler);
    }

    private void Remove(RelayMessageCode eventCode, Delegate key)
    {
        // The handler stays pinned while another code still has it
        if (!_pinnedDelegates.TryGetValue(key, out var realHandler)
            || !_toCode.TryGetValue(key, out var codes)
            || !codes.Remove(eventCode))
        {
            throw new InvalidOperationException(
                "Handler not found. Make sure to only remove handlers that were added.");
        }

        _removeServerRpcMessageHandler(eventCode, realHandler);

        if (codes.Count == 0)
        {
            _pinnedDelegateStore.UnpinDelegate(realHandler);
            _pinnedDelegates.Remove(key);
            _toCode.Remove(key);
        }
    }

    /// <summary>
    /// Sends a message to a specific player with the given data and delivery method.
    /// </summary>
    /// <param name="player"></param>
    /// <param name="data"></param>
    /// <param name="deliveryMethod"></param>
    public unsafe void SendToOne(PlayerId player, NetDataWriter data, DeliveryMethod deliveryMethod)
    {
        fixed (byte* ptr = data.Data)
        {
            _sendToOne(player, ptr, data.Length, deliveryMethod);
        }
    }
}
