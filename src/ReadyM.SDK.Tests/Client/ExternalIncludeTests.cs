using ReadyM.SDK.Client.Entity;
using ReadyM.SDK.Tests.Client.Fixtures;
using ReadyM.SDK.Tests.ExternalMod;

namespace ReadyM.SDK.Tests.Client;

/// <summary>
/// Including a mixin and an archetype declared in another assembly, whose components this one cannot
/// name. Everything here goes through the generated accessor classes.
/// </summary>
public class ExternalIncludeTests : ClientSdkTest
{
    [Fact]
    public void An_external_mixin_is_flattened_onto_the_archetype()
    {
        var trader = Spawn<Trader>();

        trader.Coins = 42;

        Assert.Equal(42, trader.Coins);
    }

    [Fact]
    public void An_external_archetypes_own_accessor_is_flattened_too()
    {
        var trader = Spawn<Trader>();

        trader.Reputation = 7;

        Assert.Equal(7, trader.Reputation);
    }

    [Fact]
    public void Local_and_external_accessors_sit_side_by_side()
    {
        var trader = Spawn<Trader>();

        trader.Haggling = 1;
        trader.X = 2f;
        trader.Coins = 3;
        trader.Reputation = 4;

        Assert.Equal(1, trader.Haggling);
        Assert.Equal(2f, trader.X);
        Assert.Equal(3, trader.Coins);
        Assert.Equal(4, trader.Reputation);
    }

    [Fact]
    public void The_component_set_reaches_across_assemblies()
    {
        var names = ComponentsOf<Trader>().Types.Select(type => type.Name).ToArray();

        Assert.Contains("TraderComponent", names);
        Assert.Contains("PlacementComponent", names);
        Assert.Contains("WalletComponent", names);
        Assert.Contains("MerchantComponent", names);
    }

    [Fact]
    public void An_external_component_appears_once_even_when_included_twice()
    {
        // Trader includes Wallet directly, and Merchant includes it as well.
        var names = ComponentsOf<Trader>().Types.Select(type => type.Name).ToArray();

        Assert.Equal(1, names.Count(name => name == "WalletComponent"));
    }

    [Fact]
    public void Converting_up_to_an_external_archetype()
    {
        var trader = Spawn<Trader>();
        trader.Reputation = 9;
        trader.Coins = 5;

        Merchant merchant = trader;

        Assert.True(merchant.IsValid);
        Assert.Equal(9, merchant.Reputation);
        Assert.Equal(5, merchant.Coins);

        merchant.Reputation = 11;
        Assert.Equal(11, trader.Reputation);
    }

    [Fact]
    public void Casting_back_down_from_an_external_archetype()
    {
        var trader = Spawn<Trader>();
        trader.Haggling = 6;

        Merchant merchant = trader;
        var again = (Trader)merchant;

        Assert.Equal(6, again.Haggling);
    }

    [Fact]
    public void Casting_down_throws_when_the_entity_is_only_the_external_archetype()
    {
        var merchant = Spawn<Merchant>();

        Assert.False(merchant.Is<Trader>());
        Assert.Throws<InvalidCastException>(() => (Trader)merchant);
    }

    [Fact]
    public void A_query_by_an_external_archetype_finds_the_local_one()
    {
        Spawn<Trader>().Reputation = 2;
        Spawn<Merchant>().Reputation = 3;
        SpawnMonster();

        var reputations = new List<int>();
        foreach (var merchant in Entities.Query<Merchant>())
            reputations.Add(merchant.Reputation);

        reputations.Sort();
        Assert.Equal([2, 3], reputations);
    }

    [Fact]
    public void A_query_by_an_external_mixin_finds_the_local_archetype()
    {
        Spawn<Trader>().Coins = 8;

        var coins = new List<int>();
        foreach (var wallet in Entities.Query<Wallet>())
            coins.Add(wallet.Coins);

        Assert.Equal([8], coins);
    }
}
