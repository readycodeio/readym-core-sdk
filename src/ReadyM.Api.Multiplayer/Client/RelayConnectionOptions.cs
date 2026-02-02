using System;
using System.Diagnostics.Contracts;
using LiteNetLib.Utils;
using ReadyM.Api.Idents;

namespace ReadyM.Api.Multiplayer.Client;

public struct RelayConnectionOptions : INetSerializable
{
    public Guid UserGuid { get; set; }
    public PlayerIdMode PlayerIdMode { get; set; }
    public PlayerId PlayerId { get; set; }

    public RelayConnectionOptions(RelayConnectionOptions options)
    {
        UserGuid = options.UserGuid;
        PlayerIdMode = options.PlayerIdMode;
        PlayerId = options.PlayerId;
    }

    [Pure]
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(UserGuid.ToString());
        writer.Put((byte)PlayerIdMode);
        writer.Put(PlayerId);
    }

    public void Deserialize(NetDataReader reader)
    {
        UserGuid = Guid.Parse(reader.GetString());
        PlayerIdMode = (PlayerIdMode)reader.GetByte();
        PlayerId = reader.Get<PlayerId>();
    }
}