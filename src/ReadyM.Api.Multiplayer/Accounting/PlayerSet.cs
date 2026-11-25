using System;
using System.Collections.Generic;
using ReadyM.Api.Idents;

namespace ReadyM.Api.Multiplayer.Accounting;

/// <summary>A small set of players, kept sorted and compared by value.</summary>
internal readonly struct PlayerSet : IEquatable<PlayerSet>
{
    private readonly PlayerId[]? _players;

    private PlayerSet(PlayerId[] players)
    {
        _players = players;
    }

    public static PlayerSet Of(List<PlayerId> players)
    {
        var sorted = players.ToArray();
        Array.Sort(sorted, static (a, b) => a.RawValue.CompareTo(b.RawValue));
        return new PlayerSet(sorted);
    }

    public bool Equals(PlayerSet other)
    {
        var a = _players ?? [];
        var b = other._players ?? [];
        if (a.Length != b.Length)
            return false;

        for (var i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i])
                return false;
        }

        return true;
    }

    public override bool Equals(object? obj)
        => obj is PlayerSet other && Equals(other);

    public override int GetHashCode()
    {
        var hash = 17;
        foreach (var p in _players ?? [])
            hash = hash * 31 + p.RawValue;
        return hash;
    }
}
