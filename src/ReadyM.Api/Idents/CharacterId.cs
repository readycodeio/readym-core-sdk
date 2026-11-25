using System;

namespace ReadyM.Api.Idents;

/// <summary>
/// Identifies one of a player's characters on a server. Stable across sessions.
/// </summary>
public readonly record struct CharacterId(Guid Value)
{
    /// <summary>A new, unique character id.</summary>
    public static CharacterId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString("N");
}
