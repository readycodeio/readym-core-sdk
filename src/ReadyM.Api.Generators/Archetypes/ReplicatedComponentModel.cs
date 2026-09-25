
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Archetypes;

/// Describes a component that does not exist yet, in the terms the replicated emitter speaks.
/// <remarks>
/// A component written by hand is read back off its own symbol. One a shape declares is emitted in
/// the same pass, so there is no symbol to read: its name, accessibility and members are stated
/// here instead. The members do have symbols, since they are the shape's own partial properties,
/// which is what lets the field support decide how each type goes over the wire.
/// </remarks>
internal static class ReplicatedComponentModel
{
    /// Null when the shape cannot be described, which is a shape whose properties are not in this
    /// compilation. Nothing can be emitted for one of those anyway.
    public static DeriveTargetModel? For(DeclarationModel model, Compilation compilation)
    {
        if (model.Accessors.Count == 0)
            return null;

        var members = new List<DeriveMemberInfo>(model.Accessors.Count);

        for (var i = 0; i < model.Accessors.Count; i++)
        {
            var accessor = model.Accessors[i];

            if (accessor.Declared is null)
                return null;

            members.Add(new DeriveMemberInfo(
                symbol: accessor.Declared,
                name: accessor.Field,
                type: accessor.Declared.Type,
                order: i,
                readOnly: false,
                errors: []));
        }

        var target = new DeriveTargetInfo(
            isExternal: false,
            symbol: model.Symbol,
            name: ArchetypeNames.ComponentOf(model.Symbol),
            @namespace: model.Namespace,
            members: members,
            isNullable: false,
            errors: [],
            requestedDirtyMaskType: null,
            emitDirtyMask: true,
            emitBindDelete: false,
            mapSettings: DeriveUtils.GetMapSettings(DefaultMode),
            accessibility: "internal",
            qualifiedName: ArchetypeNames.QualifiedComponentOf(model.Symbol));

        return new DeriveTargetModel(
            target,
            DeriveComponentUtils.GetMemberModelList(target, null),
            DeriveComponentUtils.GetMaskInfo(target, compilation));
    }

    /// Fields, including private ones, which is what a component declared as properties amounts to.
    /// The same default a hand-written component takes.
    private const byte DefaultMode = (1 << 0) | (1 << 2);
}
