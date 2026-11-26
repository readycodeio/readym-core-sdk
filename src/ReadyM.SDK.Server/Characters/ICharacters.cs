using ReadyM.Api.Idents;

namespace ReadyM.SDK.Server.Characters;

/// <summary>
/// A player's characters on this server. A connected player always has one current character: at connect the last
/// selected one, or a new one. Call on the ECS thread.
/// </summary>
public interface ICharacters
{
    /// <summary>The player's characters in creation order, without loading any of them.</summary>
    IReadOnlyList<CharacterInfo> GetCharacters(PlayerId player);

    /// <summary>The player's current character.</summary>
    CharacterId? GetCurrentCharacter(PlayerId player);

    /// <summary>The character the player selected last.</summary>
    CharacterId? GetLastSelectedCharacter(PlayerId player);

    /// <summary>Creates a new character and selects it. The current one is saved first.</summary>
    CharacterId? CreateCharacter(PlayerId player);

    /// <summary>
    /// Selects one of the player's characters. True when it is current after the call. The current one is saved first.
    /// </summary>
    bool SelectCharacter(PlayerId player, CharacterId id);
}
