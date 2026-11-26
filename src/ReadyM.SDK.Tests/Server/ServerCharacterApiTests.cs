using System.Runtime.InteropServices;
using ReadyM.Api.Idents;
using ReadyM.Relay.Server.Sdk.Interop;
using ReadyM.SDK.Server.Characters;

namespace ReadyM.SDK.Tests.Server;

/// <summary>
/// The host answers a refused character call with 0 or an empty list, and the SDK passes that on as false or null.
/// A list longer than the first buffer is asked for again.
/// </summary>
public class ServerCharacterApiTests
{
    private static readonly PlayerId Player = new(1);
    private static readonly Guid Hero = Guid.Parse("7c9e6679-7425-40de-944b-e07fc1f90ae7");
    private static readonly DateTime Created = new(2026, 11, 25, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Played = new(2026, 11, 26, 18, 30, 0, DateTimeKind.Utc);

    // Rooted for the test's life: the api holds only their function pointers.
    private readonly List<Delegate> _rooted = [];

    private unsafe CharacterApi ApiAnswering(bool done, int characters = 1)
    {
        var answer = done ? (byte)1 : (byte)0;
        GetCharactersDelegate getCharacters = (_, buffer, capacity) =>
        {
            if (!done)
                return 0;

            for (var i = 0; i < Math.Min(characters, capacity); i++)
                buffer[i] = new CharacterInfoData { Id = Hero, CreatedTicks = Created.Ticks, LastPlayedTicks = Played.Ticks };
            return characters;
        };
        GetCurrentCharacterDelegate getCurrent = (_, id) =>
        {
            *id = Hero;
            return answer;
        };
        GetLastSelectedCharacterDelegate getLastSelected = (_, id) =>
        {
            *id = Hero;
            return answer;
        };
        CreateCharacterDelegate create = (_, id) =>
        {
            *id = Hero;
            return answer;
        };
        SelectCharacterDelegate select = (_, _) => answer;

        _rooted.AddRange([getCharacters, getCurrent, getLastSelected, create, select]);

        return new CharacterApi(new CharacterApiPointers
        {
            GetCharacters = Marshal.GetFunctionPointerForDelegate(getCharacters),
            GetCurrentCharacter = Marshal.GetFunctionPointerForDelegate(getCurrent),
            GetLastSelectedCharacter = Marshal.GetFunctionPointerForDelegate(getLastSelected),
            CreateCharacter = Marshal.GetFunctionPointerForDelegate(create),
            SelectCharacter = Marshal.GetFunctionPointerForDelegate(select),
        });
    }

    [Fact]
    public void Refusals_return_false_null_or_empty()
    {
        var api = ApiAnswering(done: false);

        Assert.Empty(api.GetCharacters(Player));
        Assert.Null(api.GetCurrentCharacter(Player));
        Assert.Null(api.GetLastSelectedCharacter(Player));
        Assert.Null(api.CreateCharacter(Player));
        Assert.False(api.SelectCharacter(Player, new CharacterId(Hero)));
    }

    [Fact]
    public void A_call_that_is_done_answers_what_the_host_wrote()
    {
        var api = ApiAnswering(done: true);

        Assert.Equal(new CharacterId(Hero), api.GetCurrentCharacter(Player));
        Assert.Equal(new CharacterId(Hero), api.GetLastSelectedCharacter(Player));
        Assert.Equal(new CharacterId(Hero), api.CreateCharacter(Player));
        Assert.True(api.SelectCharacter(Player, new CharacterId(Hero)));
    }

    [Fact]
    public void The_list_crosses_with_its_times_in_utc()
    {
        var character = Assert.Single(ApiAnswering(done: true).GetCharacters(Player));

        Assert.Equal(new CharacterInfo(new CharacterId(Hero), Created, Played), character);
        Assert.Equal(DateTimeKind.Utc, character.LastPlayedUtc.Kind);
    }

    [Fact]
    public void A_list_longer_than_the_first_buffer_is_asked_for_again()
        => Assert.Equal(20, ApiAnswering(done: true, characters: 20).GetCharacters(Player).Count);
}
