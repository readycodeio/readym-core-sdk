using System.Runtime.InteropServices;
using ReadyM.Api.Idents;
using ReadyM.Relay.Server.Sdk.Interop;

namespace ReadyM.SDK.Server.Characters;

internal sealed class CharacterApi : ICharacters
{
    private readonly GetCharactersDelegate _getCharacters;
    private readonly GetCurrentCharacterDelegate _getCurrent;
    private readonly GetLastSelectedCharacterDelegate _getLastSelected;
    private readonly CreateCharacterDelegate _create;
    private readonly SelectCharacterDelegate _select;

    internal CharacterApi(CharacterApiPointers pointers)
    {
        _getCharacters = Marshal.GetDelegateForFunctionPointer<GetCharactersDelegate>(pointers.GetCharacters);
        _getCurrent = Marshal.GetDelegateForFunctionPointer<GetCurrentCharacterDelegate>(pointers.GetCurrentCharacter);
        _getLastSelected =
            Marshal.GetDelegateForFunctionPointer<GetLastSelectedCharacterDelegate>(pointers.GetLastSelectedCharacter);
        _create = Marshal.GetDelegateForFunctionPointer<CreateCharacterDelegate>(pointers.CreateCharacter);
        _select = Marshal.GetDelegateForFunctionPointer<SelectCharacterDelegate>(pointers.SelectCharacter);
    }

    public unsafe IReadOnlyList<CharacterInfo> GetCharacters(PlayerId player)
    {
        // Asked again when the list grew between the two calls
        var buffer = new CharacterInfoData[8];
        while (true)
        {
            int count;
            fixed (CharacterInfoData* pointer = buffer)
                count = _getCharacters(player, pointer, buffer.Length);

            if (count <= buffer.Length)
                return buffer.Take(count).Select(ToInfo).ToArray();

            buffer = new CharacterInfoData[count];
        }
    }

    public unsafe CharacterId? GetCurrentCharacter(PlayerId player)
    {
        Guid id;
        return _getCurrent(player, &id) != 0 ? new CharacterId(id) : null;
    }

    public unsafe CharacterId? GetLastSelectedCharacter(PlayerId player)
    {
        Guid id;
        return _getLastSelected(player, &id) != 0 ? new CharacterId(id) : null;
    }

    public unsafe CharacterId? CreateCharacter(PlayerId player)
    {
        Guid id;
        return _create(player, &id) != 0 ? new CharacterId(id) : null;
    }

    public bool SelectCharacter(PlayerId player, CharacterId id) => _select(player, id.Value) != 0;

    private static CharacterInfo ToInfo(CharacterInfoData data) => new(
        new CharacterId(data.Id),
        new DateTime(data.CreatedTicks, DateTimeKind.Utc),
        new DateTime(data.LastPlayedTicks, DateTimeKind.Utc));
}
