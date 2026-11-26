using System.Runtime.InteropServices;
using ReadyM.Api.Idents;

namespace ReadyM.Relay.Server.Sdk.Interop;

/// <summary>One of a player's characters as it crosses to a mod; times are UTC ticks.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CharacterInfoData
{
    public Guid Id;
    public long CreatedTicks;
    public long LastPlayedTicks;
}


/// <summary>
/// Writes up to <paramref name="capacity"/> of the player's characters into <paramref name="buffer"/> and answers how
/// many the player has.
/// </summary>
internal unsafe delegate int GetCharactersDelegate(PlayerId player, CharacterInfoData* buffer, int capacity);

internal unsafe delegate byte GetCurrentCharacterDelegate(PlayerId player, Guid* current);

internal unsafe delegate byte GetLastSelectedCharacterDelegate(PlayerId player, Guid* lastSelected);

internal unsafe delegate byte CreateCharacterDelegate(PlayerId player, Guid* created);

internal delegate byte SelectCharacterDelegate(PlayerId player, Guid character);
