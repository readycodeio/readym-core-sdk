using ReadyM.SDK.Attributes;
using ReadyM.SDK.Client.Entities;
using ReadyM.SDK.Tests.Examples.Declarations;

namespace ReadyM.SDK.Tests.Examples;

// Scratchpad for testing if the new API semantics even compile
public static class Examples
{
    public static void Example()
    {
        var character = new MainCharacter();

        // access the single properties
        var id = character.PlayerId;

        // access the Vitals
        var hp = character.Hp;
        character.MaxHp = 100;

        // equipment lives on a narrower shape rather than an optional include
        if (character.TryAs<EquippedCharacter>(out var equipped))
        {
            equipped.Weapon = "sword";
        }

        // casting

        ReadyObject obj = character; // implicit, always allowed
        // var again = (MainCharacter)obj;              // explicit, throws InvalidCastException when obj is not one
        // if (MainCharacter.TryFrom(obj, out var main))
        // {
        //     main.Hp = 10;
        // }        

        // there are no tags: a shape that sometimes differs is a narrower archetype, and a per-entity
        // flag is a mixin with a bool
        if (character.TryAs<EquippedCharacter>(out _))
        {
            // do something with the equipped ones
        }
    }
}

[Service]
public partial class Regeneration(IEntities entities)
{
    [UpdateHandler]
    private void Regenerate()
    {
        foreach (var c in entities.Query<MainCharacter>())
        {
            if (c.Hp < c.MaxHp)
            {
                c.Hp += 1f;
            }
        }
        
        foreach (var npc in entities.Query<Npc>())
        {
            if (npc.Hp <= 0f)
            {
                npc.Hp = 0f;
            }
        }
        
        foreach (var (vitals, eq) in entities.Query<Vitals, Equipment>())
        {
            if (vitals.Hp <= 0f)
            {
                eq.Weapon = "spear";
            }
        }
    }
}