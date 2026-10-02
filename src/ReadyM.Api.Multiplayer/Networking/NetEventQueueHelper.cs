using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using LiteNetLib;
using Microsoft.Extensions.Logging;

namespace ReadyM.Api.Multiplayer.Networking;

/// <summary>A LiteNetLib listener that queues every callback for one network thread to handle later.</summary>
internal sealed class NetEventQueueHelper : INetEventListener
{
    /// <summary>One LiteNetLib callback as it was raised, holding references only.</summary>
    private struct Event
    {
        public NetEventKind Kind;
        public NetPeer? Peer;
        public NetPacketReader? Reader;
        public ConnectionRequest? Request;
        public DisconnectInfo Disconnect;
        public byte Channel;
        public DeliveryMethod Method;
        public int Latency;
    }

    // Producers are LiteNetLib's threads; the network thread swaps the two queues under _gate and drains _draining alone.
    private readonly object _gate = new();
    private Queue<Event> _pending = new();
    private Queue<Event> _draining = new();

    /// <summary>Released by any thread to wake the network thread; every release made before a pass is served by that one pass.</summary>
    public SemaphoreSlim Wake { get; } = new(0);

    void INetEventListener.OnConnectionRequest(ConnectionRequest request)
        => Enqueue(new Event { Kind = NetEventKind.ConnectionRequest, Request = request });

    void INetEventListener.OnPeerConnected(NetPeer peer)
        => Enqueue(new Event { Kind = NetEventKind.Connected, Peer = peer });

    void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        // LiteNetLib recycles an event without data as soon as this returns, so its reader stays behind.
        if (disconnectInfo.AdditionalData is { IsNull: true })
            disconnectInfo.AdditionalData = null!;

        Enqueue(new Event { Kind = NetEventKind.Disconnected, Peer = peer, Disconnect = disconnectInfo });
    }

    void INetEventListener.OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        => Enqueue(new Event { Kind = NetEventKind.Receive, Peer = peer, Reader = reader, Channel = channelNumber, Method = deliveryMethod });

    void INetEventListener.OnNetworkLatencyUpdate(NetPeer peer, int latency)
        => Enqueue(new Event { Kind = NetEventKind.LatencyUpdate, Peer = peer, Latency = latency });

    void INetEventListener.OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
    }

    void INetEventListener.OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        => reader.Recycle();

    private void Enqueue(in Event e)
    {
        lock (_gate)
            _pending.Enqueue(e);
        if (Wake.CurrentCount == 0)
            Wake.Release();
    }

    /// <summary>Network thread: handles every queued event through the owner's listener methods, then recycles its data.</summary>
    public void Drain(INetEventListener owner, ILogger logger)
    {
        while (TryNext(out var e))
        {
            try
            {
                Dispatch(owner, e);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error handling network event {Kind}", e.Kind);
            }
            finally
            {
                e.Reader?.Recycle();
                e.Disconnect.AdditionalData?.Recycle();
            }
        }
    }

    /// <summary>Network thread, once woken: takes every pending release, since the pass that follows handles all their work.</summary>
    public void ConsumeWakes()
    {
        while (Wake.Wait(0))
        {
        }
    }

    /// <summary>Network thread: returns everything still queued to LiteNetLib without handling it.</summary>
    public void Discard()
    {
        while (TryNext(out var e))
        {
            e.Reader?.Recycle();
            e.Disconnect.AdditionalData?.Recycle();
        }
    }

    // Takes over everything queued so far once the last batch is done, so producers never wait on a handler.
    private bool TryNext(out Event e)
    {
        if (_draining.Count == 0)
        {
            lock (_gate)
                (_pending, _draining) = (_draining, _pending);
        }

        if (_draining.Count == 0)
        {
            e = default;
            return false;
        }

        e = _draining.Dequeue();
        return true;
    }

    private static void Dispatch(INetEventListener owner, in Event e)
    {
        switch (e.Kind)
        {
            case NetEventKind.ConnectionRequest:
                owner.OnConnectionRequest(e.Request!);
                break;
            case NetEventKind.Connected:
                owner.OnPeerConnected(e.Peer!);
                break;
            case NetEventKind.Disconnected:
                owner.OnPeerDisconnected(e.Peer!, e.Disconnect);
                break;
            case NetEventKind.Receive:
                owner.OnNetworkReceive(e.Peer!, e.Reader!, e.Channel, e.Method);
                break;
            case NetEventKind.LatencyUpdate:
                owner.OnNetworkLatencyUpdate(e.Peer!, e.Latency);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(e), e.Kind, "Unknown network event kind");
        }
    }
}
