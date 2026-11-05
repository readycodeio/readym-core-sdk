using LiteNetLib.Utils;
using ReadyM.Api.Idents;

namespace ReadyM.Api.Multiplayer.Client;

internal struct RelayConnectionOptions : INetSerializable
{
    public ConnectionTicket Ticket { get; set; }
    public PlayerIdMode PlayerIdMode { get; set; }
    public PlayerId PlayerId { get; set; }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.Put(Ticket);
        writer.Put((byte)PlayerIdMode);
        writer.Put(PlayerId);
    }

    public void Deserialize(NetDataReader reader)
    {
        Ticket = reader.Get<ConnectionTicket>();
        PlayerIdMode = (PlayerIdMode)reader.GetByte();
        PlayerId = reader.Get<PlayerId>();
    }
}