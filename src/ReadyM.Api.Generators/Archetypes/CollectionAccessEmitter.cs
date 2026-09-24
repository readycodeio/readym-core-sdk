using System.Collections.Generic;
using System.Linq;

namespace ReadyM.Api.Generators.Archetypes;

/// <summary>
/// Emits the wrapper a mapping handler is handed for one collection.
/// </summary>
/// <remarks>
/// The collection itself never leaves the component: handing it over would let a caller keep it
/// past the entity and change it where the policy is not looking. This holds the entity instead and
/// forwards to the same accessors the shape does, so the rules a change keeps are the same ones.
///
/// It is a ref struct wherever the runtime carries one. A netstandard2.0 client is hosted by one
/// that does not, so there it is an ordinary readonly struct, which costs the same and only gives
/// up being unable to store it.
/// </remarks>
internal static class CollectionAccessEmitter
{
    public static string NameOf(DeclarationModel model, string collection) => model.Name + collection;

    public static string QualifiedNameOf(DeclarationModel model, string collection)
    {
        var ns = ArchetypeNames.NamespaceOf(model.Symbol);
        var name = NameOf(model, collection);

        return ns.Length == 0 ? "global::" + name : $"global::{ns}.{name}";
    }

    /// The collections a shape holds, from whichever side they came: one it declared and had a
    /// component generated for, or one it named on a component it borrows.
    public static IEnumerable<(string Name, string? Held, IReadOnlyList<ForwardModel> Members)> Of(DeclarationModel model)
    {
        if (!model.IsReplicated)
            yield break;

        if (model.EmitsComponent)
        {
            foreach (var accessor in model.Accessors.Where(accessor => accessor.IsNativeContainer))
                yield return (accessor.Name, accessor.Type, NativeCollectionForwards.For(accessor, model.Name));

            yield break;
        }

        foreach (var collection in model.Collections)
        {
            var members = model.Forwards.Where(forward => forward.Name.Contains(collection)).ToList();

            yield return (collection, HeldAs(members, collection), members);
        }
    }

    /// What the component holds a borrowed collection in, which is what its Fields entry is typed
    /// by. It is the whole-collection setter's parameter: the field support puts one there for every
    /// kind it carries, and it is the one member naming the type rather than what is in it.
    private static string? HeldAs(IReadOnlyList<ForwardModel> members, string collection)
    {
        foreach (var member in members)
            if (member.Name == "Set" + collection && member.Parameters.Count == 1)
                return member.Parameters[0].Type;

        return null;
    }

    public static void Emit(SourceWriter writer, DeclarationModel model)
    {
        foreach (var (collection, _, members) in Of(model))
            One(writer, model, collection, members);
    }

    private static void One(
        SourceWriter writer,
        DeclarationModel model,
        string collection,
        IReadOnlyList<ForwardModel> members)
    {
        var name = NameOf(model, collection);
        var handle = ArchetypeNames.EntityHandle;

        writer.Line();
        writer.Line("#if NET10_0_OR_GREATER");
        writer.Line($"public readonly ref struct {name}");
        writer.Line("#else");
        writer.Line($"public readonly struct {name}");
        writer.Line("#endif");

        using (writer.Braces(string.Empty))
        {
            writer.Line($"private readonly {handle} _handle;");
            writer.Line();
            writer.Line("/// <exclude />");
            writer.Line($"public {name}(in {handle} handle) => _handle = handle;");

            foreach (var forward in members)
                Member(writer, forward, collection, model.QualifiedAccessors);
        }
    }

    /// The member as a collection reads it, with the value's name taken back out of it: a component
    /// has to say which collection it means, and something already holding one does not.
    private static void Member(SourceWriter writer, ForwardModel forward, string value, string accessors)
    {
        var name = forward.Name.Replace(value, string.Empty);
        var call = $"{accessors}.{forward.Name}";

        writer.Line();

        if (forward.IsProperty)
        {
            writer.Line($"public {forward.ReturnType} {name} => {call}(_handle);");
            return;
        }

        writer.Line($"public {forward.Declaration(name)}");
        writer.Line($"    => {call}(_handle{forward.Separator}{forward.Arguments});");
    }
}
