using System.Collections.Generic;
using System.Linq;

namespace ReadyM.Api.Generators.Archetypes;

/// Emits the wrapper a mapping handler is handed for one collection.
internal static class CollectionAccessEmitter
{
    public static string NameOf(DeclarationModel model, string collection) => model.Name + collection;

    public static string QualifiedNameOf(DeclarationModel model, string collection)
    {
        var ns = ArchetypeNames.NamespaceOf(model.Symbol);
        var name = NameOf(model, collection);

        return ns.Length == 0 ? "global::" + name : $"global::{ns}.{name}";
    }

    /// The collections a shape holds, declared or named on a component it borrows.
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

    /// What the component holds a borrowed collection in, read off its whole-collection setter.
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

    /// The member as a collection reads it, with the value name taken back out.
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
