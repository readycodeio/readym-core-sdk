using ReadyM.SDK.Entity;
using ReadyM.SDK.Tests.Client.Fixtures;

namespace ReadyM.SDK.Tests.Client;

/// <summary>
/// Toggling a tag inside a query loop, which the client defers through a command buffer played back
/// when the loop ends.
/// </summary>
public class StructuralChangeTests : ClientSdkTest
{
    [Fact]
    public void Accessor_writes_are_not_deferred()
    {
        var monster = SpawnMonster(hp: 1f);

        foreach (var found in Entities.Query<Monster>())
        {
            found.Hp = 42f;
            Assert.Equal(42f, found.Hp);
        }

        Assert.Equal(42f, monster.Hp);
    }

    [Fact]
    public void A_deleted_entity_is_no_longer_valid()
    {
        var monster = SpawnMonster();
        Assert.True(monster.IsValid);

        Store.GetEntityByRawEntity(EntityHandle.Of(monster).RawEntity).DeleteEntity();

        Assert.False(monster.IsValid);
    }

    [Fact]
    public void A_handle_to_a_deleted_entity_stays_invalid_when_its_id_is_reused()
    {
        var monster = SpawnMonster(level: 1);
        var id = EntityHandle.Of(monster).Id;
        Store.GetEntityByRawEntity(EntityHandle.Of(monster).RawEntity).DeleteEntity();

        var replacement = SpawnMonster(level: 2);

        Assert.Equal(id, EntityHandle.Of(replacement).Id);
        Assert.False(monster.IsValid);
        Assert.True(replacement.IsValid);
        Assert.Equal(2, replacement.Level);
    }
}
