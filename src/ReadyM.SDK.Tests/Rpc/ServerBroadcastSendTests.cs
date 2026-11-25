using System.Collections.Generic;
using System.Linq;
using ReadyM.Api.Idents;
using ReadyM.SDK.Attributes;
using ReadyM.SDK.TestHarness;
using ReadyM.SDK.Tests.ExternalMod;
using Xunit;

namespace ReadyM.SDK.Tests.Rpc;

/// <summary>A server-to-client RPC sent to every player, or to a set of them, carries what a send to one would.</summary>
public class ServerBroadcastSendTests
{
    private static readonly PlayerId First = new(2);
    private static readonly PlayerId Second = new(5);
    private static readonly PlayerId Third = new(9);

    private readonly FakeRpcRelay _relay = new();
    private readonly BroadcastHandlers _handlers;

    public ServerBroadcastSendTests()
    {
        _handlers = new BroadcastHandlers { Rpc = _relay.CreateRpcApi() };
    }

    [Fact]
    public void To_all_hands_the_relay_one_message_for_every_player()
    {
        _handlers.SendAnnouncementToAll("dice", 6);

        Assert.Empty(_relay.Sent);
        Assert.Equal(SentToOne(handlers => handlers.SendAnnouncement(First, "dice", 6)), Assert.Single(_relay.SentToAll));
    }

    [Fact]
    public void To_all_without_a_payload_hands_the_relay_the_bare_code()
    {
        _handlers.SendPingToAll();

        Assert.Empty(_relay.Sent);
        Assert.Equal(SentToOne(handlers => handlers.SendPing(First)), Assert.Single(_relay.SentToAll));
    }

    [Fact]
    public void A_set_gets_the_same_message_once_each_in_its_order()
    {
        _handlers.SendAnnouncement([Third, First, Second], "dice", 6);

        var expected = SentToOne(handlers => handlers.SendAnnouncement(First, "dice", 6));

        Assert.Empty(_relay.SentToAll);
        Assert.Equal([Third, First, Second], _relay.Sent.Select(sent => sent.Player));
        Assert.All(_relay.Sent, sent => Assert.Equal(expected, sent.Message));
    }

    [Fact]
    public void A_set_without_a_payload_gets_the_bare_code()
    {
        _handlers.SendPing(new List<PlayerId> { Second, Third });

        var expected = SentToOne(handlers => handlers.SendPing(First));

        Assert.Empty(_relay.SentToAll);
        Assert.Equal([Second, Third], _relay.Sent.Select(sent => sent.Player));
        Assert.All(_relay.Sent, sent => Assert.Equal(expected, sent.Message));
    }

    [Fact]
    public void An_empty_set_sends_nothing()
    {
        _handlers.SendAnnouncement([], "dice", 6);

        Assert.Empty(_relay.Sent);
        Assert.Empty(_relay.SentToAll);
    }

    /// <summary>The bytes a send to one player puts on the wire, through a relay of its own.</summary>
    private static byte[] SentToOne(System.Action<BroadcastHandlers> send)
    {
        var relay = new FakeRpcRelay();
        send(new BroadcastHandlers { Rpc = relay.CreateRpcApi() });

        return Assert.Single(relay.Sent).Message;
    }
}

[RpcHandlersFor(typeof(BroadcastContracts))]
public partial class BroadcastHandlers;
