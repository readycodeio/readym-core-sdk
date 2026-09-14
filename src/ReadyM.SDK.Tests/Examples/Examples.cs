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