using System;

namespace ReadyM.Api.Multiplayer.Mappings.Events;

public struct PropagatingToGame(string EventName) : IEquatable<PropagatingToGame>
{
    private string _eventName = EventName;
    public bool Equals(PropagatingToGame other)
    {
        return _eventName == other._eventName;
    }
    public override bool Equals(object? obj)
    {
        return obj is PropagatingToGame other && Equals(other);
    }
    public override int GetHashCode()
    {
        return _eventName.GetHashCode();
    }
}