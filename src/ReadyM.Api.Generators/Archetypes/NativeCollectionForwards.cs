
using System.Collections.Generic;
using static ReadyM.Api.Generators.DeriveCSharpUtils;

namespace ReadyM.Api.Generators.Archetypes;

/// What a replicated component exposes for a value that is a native collection.
/// <remarks>
/// The component is generated in the same pass, so its members cannot be read off a symbol: this
/// says what the field support will have put there. It is the one place that knows, and everything
/// that carries a member onward, the accessors, the shape, its chunk view and any archetype it
/// extends, works from the forwards it returns.
///
/// Reading or replacing the whole collection is left out. The accessors already offer that through
/// the field, and a second overload differing only by <c>in</c> would not compile. So is the
/// replication plumbing, which a shape must never gain.
/// </remarks>
internal static class NativeCollectionForwards
{
    public static IReadOnlyList<ForwardModel> For(AccessorModel accessor, string shape)
    {
        if (accessor.Declared is null)
            return [];

        var name = accessor.Name;
        var entry = accessor.FieldEntry;
        var token = $"{shape}.{ValuesEmitter.ClassName}.{accessor.Name}";
        var type = accessor.Declared.Type;

        if (SerializationHelper.IsNativeList(type, out var item))
        {
            var each = FullyQualifiedTypeName(item);

            return
            [
                Property(name, $"{name}Count", "int"),
                Method(name, $"Get{name}", each, ("", "int", "index")),
                Changes((entry, token, name), $"Set{name}", "void", ("", "int", "index"), ("in ", each, "value")),
                Method(name, $"Contains{name}", "bool", ("in ", each, "value")),
                Changes((entry, token, name), $"Add{name}", "void", ("in ", each, "value")),
                Changes((entry, token, name), $"Insert{name}", "void", ("", "int", "index"), ("in ", each, "value")),
                Changes((entry, token, name), $"RemoveAt{name}", each, ("", "int", "index")),
                Changes((entry, token, name), $"Clear{name}", "void")
            ];
        }

        if (SerializationHelper.IsNativeDictionary(type, out var key, out var value, out _))
        {
            var byKey = FullyQualifiedTypeName(key);
            var held = FullyQualifiedTypeName(value);

            return
            [
                Property(name, $"{name}Count", "int"),
                Method(name, $"Get{name}", held, ("in ", byKey, "key")),
                Changes((entry, token, name), $"Set{name}", "void", ("in ", byKey, "key"), ("in ", held, "value")),
                Method(name, $"Contains{name}Key", "bool", ("in ", byKey, "key")),
                Method(name, $"Contains{name}", "bool", ("in ", byKey, "key"), ("in ", held, "value")),
                Changes((entry, token, name), $"Add{name}", "bool", ("in ", byKey, "key"), ("in ", held, "value")),
                Changes((entry, token, name), $"Remove{name}", "bool", ("in ", byKey, "key")),
                Changes((entry, token, name), $"Clear{name}", "void")
            ];
        }

        return [];
    }

    private static ForwardModel Property(string owns, string name, string type)
        => new(name, type, [], isProperty: true, owns: owns);

    private static ForwardModel Method(
        string owns,
        string name,
        string returnType,
        params (string Modifier, string Type, string Name)[] parameters)
        => new(name, returnType, parameters, owns: owns);

    /// A member that changes the collection, which makes it a write like any other.
    private static ForwardModel Changes(
        (string Field, string Token, string Owns) collection,
        string name,
        string returnType,
        params (string Modifier, string Type, string Name)[] parameters)
        => new(name, returnType, parameters, changes: collection.Field, collection: collection.Token, owns: collection.Owns);
}
