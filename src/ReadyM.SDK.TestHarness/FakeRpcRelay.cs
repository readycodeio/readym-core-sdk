using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using LiteNetLib.Utils;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ConflictResolution;
using ReadyM.Api.Multiplayer.Interop;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.Api.Multiplayer.Protocol.Enums;
using ReadyM.Relay.Server.Sdk.Interop;
using ReadyM.Relay.Server.Sdk.Rpc;

namespace ReadyM.SDK.TestHarness;

/// <summary>
/// Stands in for the relay's RPC entry points, reached through function pointers like the real ones. Records
/// what is sent, and delivers a message to the handler added for its code.
/// </summary>
internal sealed unsafe class FakeRpcRelay
{
    private readonly Dictionary<RelayMessageCode, ServerRpcHandlerDelegate> _handlers = [];

    // Rooted, so the GC cannot collect a delegate whose function pointer is already handed out.
    private readonly AddServerRpcMessageHandlerDelegate _addHandler;
    private readonly RemoveServerRpcMessageHandlerDelegate _removeHandler;
    private readonly SendToOneDelegate _sendToOne;

    public FakeRpcRelay()
    {
        _addHandler = (code, handler) => _handlers[code] = handler;
        _removeHandler = (code, _) => _handlers.Remove(code);
        _sendToOne = (player, data, size, _) => Sent.Add((player, new Span<byte>(data, size).ToArray()));
    }

    /// <summary>Every message sent, in order, with the player it went to.</summary>
    public List<(PlayerId Player, byte[] Message)> Sent { get; } = [];

    public RpcApi CreateRpcApi() => new(
        new RpcApiPointers
        {
            AddServerRpcMessageHandler = Marshal.GetFunctionPointerForDelegate(_addHandler),
            RemoveServerRpcMessageHandler = Marshal.GetFunctionPointerForDelegate(_removeHandler),
            SendToOne = Marshal.GetFunctionPointerForDelegate(_sendToOne),
        },
        new FixedNetworkTime(),
        new NoChangeTracking());

    public void Deliver(RelayMessageCode code, PlayerId sender, NetDataWriter body)
    {
        fixed (byte* data = body.Data)
            _handlers[code](new ServerEventHeader(code, sender), data, body.Length);
    }

    private sealed class FixedNetworkTime : INetworkTime
    {
        public uint GetCurrentTime() => 0;
    }

    private sealed class NoChangeTracking : IChangeTrackingStore
    {
        public ref T GetChangeComponent<T>(int id) where T : struct, IComponent
            => throw new NotSupportedException("The fake RPC relay tracks no changes.");
    }
}
