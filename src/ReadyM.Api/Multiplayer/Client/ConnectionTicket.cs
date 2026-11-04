using System;
using System.Diagnostics.CodeAnalysis;
using LiteNetLib.Utils;

namespace ReadyM.Api.Multiplayer.Client;

internal struct ConnectionTicket : IEquatable<ConnectionTicket>, INetSerializable
{
    private Guid _value;

    private ConnectionTicket(Guid value)
    {
        _value = value;
    }

    public static ConnectionTicket New() => new(Guid.NewGuid());

    public static ConnectionTicket Parse(string text)
    {
        return new ConnectionTicket(Guid.Parse(text));
    }

    public static bool TryParse(string text, [NotNullWhen(true)] out ConnectionTicket? ticket)
    {
        if (Guid.TryParse(text, out var guid))
        {
            ticket = new ConnectionTicket(guid);
            return true;
        }

        ticket = null;
        return false;
    }

    public override string ToString() => _value.ToString("N");

    public bool Equals(ConnectionTicket other)
    {
        return _value.Equals(other._value);
    }

    public override bool Equals(object? obj)
    {
        return obj is ConnectionTicket other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _value.GetHashCode();
    }

    public static bool operator ==(ConnectionTicket left, ConnectionTicket right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ConnectionTicket left, ConnectionTicket right)
    {
        return !left.Equals(right);
    }

    public readonly void Serialize(NetDataWriter writer)
    {
        writer.PutBytesWithLength(_value.ToByteArray());
    }
    public void Deserialize(NetDataReader reader)
    {
        _value = new Guid(reader.GetBytesWithLength());
    }
}