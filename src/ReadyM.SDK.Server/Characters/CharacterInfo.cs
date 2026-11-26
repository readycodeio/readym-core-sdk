using ReadyM.Api.Idents;

namespace ReadyM.SDK.Server.Characters;

/// <summary>One of a player's characters, as their list keeps it.</summary>
/// <param name="Id">The character.</param>
/// <param name="CreatedUtc">When it was created.</param>
/// <param name="LastPlayedUtc">When it was last selected.</param>
public readonly record struct CharacterInfo(CharacterId Id, DateTime CreatedUtc, DateTime LastPlayedUtc);
