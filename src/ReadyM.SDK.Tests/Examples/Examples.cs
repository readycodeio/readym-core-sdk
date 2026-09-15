using ReadyM.SDK.Client.Entity;
using ReadyM.SDK.Services;
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

        // access the Equipment
        var maybeWeapon = character.Weapon ?? "unknown"; // nullable get-only
        if (character.TryGetEquipment(out var equipment))
        {
            equipment.Weapon = "sword";
        }

        var required = character.RequireEquipment();
        required.Weapon = "axe";

        var ensured = character.EnsureEquipment(); // TODO: This is a structural change
        ensured.Weapon = "mace";

        // casting

        ReadyObject obj = character; // implicit, always allowed
        // var again = (MainCharacter)obj;              // explicit, throws InvalidCastException when obj is not one
        // if (MainCharacter.TryFrom(obj, out var main))
        // {
        //     main.Hp = 10;
        // }        

        // tags

        var npc = new Npc();

        if (npc.Has<Hostile>())
        {
            npc.Set<Hostile>(false);
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

        foreach (var npc in entities.Query<Npc>().With<Hostile>().Without<Sleeping>())
        {
            // do something
        }

        entities.Query<MainCharacter>().ForEach(npc => npc.Hp = npc.MaxHp);

        foreach (var npc in entities.Query<Npc>().With<Hostile>())
        {
            if (npc.Hp <= 0f)
            {
                npc.Set<Hostile>(false); // CommandBuffer, applied on foreach block end
            }
        }

        entities.Query<Npc>().ForEach(npc =>
        {
            npc.Set<Hostile>(false); // CommandBuffer, applied on ForEach method end
        });

        foreach (var (vitals, eq) in entities.Query<Vitals, Equipment>())
        {
            if (vitals.Hp <= 0f)
            {
                eq.Weapon = "spear";
            }
        }

        entities.Query<Vitals, Equipment>().ForEach((vitals, eq) =>
        {
            if (vitals.Hp <= 0f)
            {
                eq.Weapon = "maul";
            }
        });
    }
}