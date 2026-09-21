using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReadyM.Api.Generators.Derive.CSharp;
using ReadyM.Api.Generators.Derive.CSharp.ConflictResolution;
using ReadyM.Api.Generators.Derive.CSharp.FieldSupport;
using static ReadyM.Api.Generators.DeriveCSharpUtils;

namespace ReadyM.Api.Generators;

/// Emits a component that replicates: the dirty and api masks, the change component, the
/// accessors that mark a field changed, and the serialization either side of the wire.
/// <remarks>
/// Lifted out of <c>DeriveINetworkedComponentGenerator</c> so the archetype and mixin generators
/// can reach it too. Generators cannot chain, so a generated component cannot be handed to another
/// generator to finish: every entry point has to emit the whole thing from here.
///
/// Replicated is the SDK's word for the concept. INetworkedComponent is the runtime contract it
/// compiles down to, and that name is unchanged.
/// </remarks>
internal static class ReplicatedComponentEmitter
{
    internal static string Emit(DeriveTargetModel model)
    {
        var info = model.Source;
        var members = model.Members;
        var access = model.Source.Symbol.DeclaredAccessibility.ToString().ToLower(); // public, internal, etc.

        if (model.MaskInfo.Bits < members.Count)
        {
            if (model.Source.EmitDirtyMask)
            {
                model.MaskInfo.AddError(
                    $"Too many synced fields in `{model.Source.Name}` " +
                    $"to fit in a dirty mask. Maximum supported is {model.MaskInfo.Bits.ToString(CultureInfo.InvariantCulture)}" +
                    $", but {members.Count.ToString(CultureInfo.InvariantCulture)} were found.");
            }
            else
            {
                model.MaskInfo.AddError(
                    $"Too many synced members in `{model.Source.Name}` " +
                    $"to fit in the user-supplied _dirtyMask. Maximum supported is {model.MaskInfo.Bits.ToString(CultureInfo.InvariantCulture)}" +
                    $", but {members.Count.ToString(CultureInfo.InvariantCulture)} were found.");
            }
        }

        foreach (var member in members)
        {
            if (!HasGetEmitFieldSupportImpl(member, false))
            {
                member.Source.AddError($"Unsupported type '{member.Source.Type.ToDisplayString()}' for networked member '{member.Source.Name}'.");
            }
        }

        var sb = new StringBuilder();
        var moduleState = new CSharpModuleState();
        var classState = new CSharpClassState(moduleState);

        AddDefaultUsings(moduleState);

        var hasDispose = HasDispose(sb, model, classState);

        sb.Append($$"""
namespace {{info.Namespace}};

{{access}} partial struct {{info.Name}} : INetworkedComponent{{(hasDispose ? ", IDisposable" : string.Empty)}}
{

""");

        if (info.HasErrors)
        {
            foreach (var error in info.Errors)
            {
                sb.AppendLine($"""
    #error {error}
""");
            }

            sb.AppendLine();
        }

        EmitChangeComponent(sb, model);

        EmitDirtyMask(sb, model);

        foreach (var member in members)
        {
            if (member.Source.ReadOnly)
                sb.AppendLine($"""
    #error Field {member.Source.Name} cannot be read-only
""");

            if (member.Source.Type.ContainingNamespace is { } ns)
            {
                moduleState.AddUsing(ns.ToDisplayString());
            }

            EmitDirtyMethods(sb, member, model, classState);
            EmitAccessorMethods(sb, member, model, classState);
            EmitNotifyChangesMethods(sb, member, model, classState);
        }

        EmitFieldEnums(sb, model, classState);
        EmitApiFlagHelpers(sb, model);

        EmitSerialize(sb, model, classState);
        EmitDeserialize(sb, model, classState);
        EmitWriteDelta(sb, model, classState);
        EmitReadDelta(sb, model, classState);

        sb.AppendLine("""
    /// <exclude />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearDirty() => _dirtyMask = 0;
    
    /// <exclude />
    public bool IsDirty => _dirtyMask != 0;
""");

        EmitAssign(sb, model, classState);

        if (hasDispose)
        {
            EmitDispose(sb, model, classState);
        }

        if (classState.Members.Count > 0)
        {
            foreach (var generatedMember in classState.Members)
            {
                if (generatedMember.IsThreadStatic)
                {
                    sb.AppendLine("""
    [ThreadStatic]
""");
                }

                sb.AppendLine($"""
    private {(generatedMember.IsStatic ? "static " : " ")}{FullyQualifiedTypeName(generatedMember.MemberType)} {generatedMember.MemberName};
""");
            }
        }

        sb.AppendLine("""
}
""");

        // NOTE: Emitting beginning of the file here so that all usings added during generation are added.
        var usingSb = new StringBuilder();
        usingSb.Append("""
// <auto-generated/>
#nullable enable

""");

        EmitUsings(usingSb, moduleState);

        sb.Insert(0, usingSb.ToString());

        sb.AppendLine("""

#nullable disable
""");

        return sb.ToString();
    }

    private static void AddDefaultUsings(CSharpModuleState moduleState)
    {
        moduleState.AddUsingList([
            "System",
            "System.Numerics",
            "System.Runtime.CompilerServices",
            "LiteNetLib.Utils",
            "ReadyM.Api.Generators",
            "ReadyM.Api.Multiplayer",
            "ReadyM.Api.Mapping.Data",
            "ReadyM.Api.Multiplayer.Extensions",
            "ReadyM.Api.Multiplayer.ECS.Components"
        ]);
    }

    private static void EmitUsings(StringBuilder sb, CSharpModuleState moduleState)
    {
        var usings = moduleState.Usings.Distinct().ToList();
        usings.Sort();

        foreach (var ns in usings)
        {
            sb.AppendLine($"""
using {ns};
""");
        }
    }

    private static void EmitChangeComponent(StringBuilder sb, DeriveTargetModel model)
    {
        sb.AppendLine($$"""

    /// <exclude />
    [global::Friflo.Engine.ECS.ComponentKey("{{model.Source.Name}}.ChangeComponent")]
    public struct ChangeComponent : global::Friflo.Engine.ECS.IComponent
    {
""");

        foreach (var member in model.Members)
        {
            sb.AppendLine($"""
        public uint {member.GeneratedPropertyName}LastChanged;
""");
        }

        sb.AppendLine("""
    }
    
""");

        sb.AppendLine("""
    public System.Type GetChangeComponent()
        => typeof(ChangeComponent);
    
""");
    }

    private static void EmitDirtyMask(StringBuilder sb, DeriveTargetModel model)
    {
        var mask = model.MaskInfo;

        if (model.Source.EmitDirtyMask)
        {
            sb.AppendLine($"""
    private {FullyQualifiedTypeName(mask.Type)} _dirtyMask;
    private {FullyQualifiedTypeName(mask.Type)} _apiMask;

""");
        }

        if (mask.HasErrors)
        {
            foreach (var error in mask.Errors)
            {
                sb.AppendLine($"""
    #error {error}

""");
            }
        }
    }

    private static void EmitDirtyMethods(
        StringBuilder sb,
        DeriveMemberModel member,
        DeriveTargetModel model,
        CSharpClassState classState)
    {
        var impl = GetEmitFieldSupportImpl(member, true);
        var context = CreateEmitContext(sb, member, model, classState);

        context.State.ResetIndent("    ");
        impl.EmitDirtyMethods(member.Source.Type, context);
    }

    private static void EmitAccessorMethods(
        StringBuilder sb,
        DeriveMemberModel member,
        DeriveTargetModel model,
        CSharpClassState classState)
    {
        if (member.Source.HasErrors)
        {
            foreach (var error in member.Source.Errors)
            {
                sb.AppendLine($"""
    #error {error}

""");
            }
        }

        var impl = GetEmitFieldSupportImpl(member, true);
        var context = CreateEmitContext(sb, member, model, classState);

        context.State.ResetIndent("    ");
        impl.EmitAccessorMethods(member.Source.Type, context);
    }

    private static void EmitNotifyChangesMethods(
        StringBuilder sb,
        DeriveMemberModel member,
        DeriveTargetModel model,
        CSharpClassState classState)
    {
        var impl = GetEmitFieldSupportImpl(member, true);
        var context = CreateEmitContext(sb, member, model, classState);

        context.State.ResetIndent("    ");
        impl.EmitNotifyChangesMethods(member.Source.Type, context);
    }

    private static void EmitFieldEnums(
        StringBuilder sb,
        DeriveTargetModel model,
        CSharpClassState classState)
    {
        sb.AppendLine("""
    /// <exclude />
    public static class Fields
    {
""");
        foreach (var member in model.Members)
        {
            var impl = GetEmitFieldSupportImpl(member, true);
            var context = CreateEmitContext(sb, member, model, classState);
            context.State.ResetIndent("        ");
            impl.EmitFieldEnum(member.Source.Type, context);
        }

        sb.AppendLine("""
    }

""");
    }

    private static void EmitApiFlagHelpers(StringBuilder sb, DeriveTargetModel model)
    {
        sb.AppendLine($"""
    /// <exclude />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearApiFlag() => _apiMask = 0;
    /// <exclude />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearApiFlag(int field) => _apiMask = ({model.MaskInfo!.Type.Name})(_apiMask & ~(({model.MaskInfo!.Type.Name})1 << field));
    /// <exclude />
    public readonly bool ChangedFromApi => _apiMask != 0;
    /// <exclude />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MarkChangedFromApi() => _apiMask = _dirtyMask;

""");
    }

    private static void EmitSerialize(
        StringBuilder sb,
        DeriveTargetModel model,
        CSharpClassState classState)
    {
        sb.AppendLine("""
    public void Serialize(NetDataWriter writer)
    {
""");

        var methodContext = new CSharpMethodState(classState);
        foreach (var member in model.Members)
        {
            var impl = GetEmitFieldSupportImpl(member, true);
            var context = CreateEmitContext(sb, member, model, methodContext);

            context.State.ResetIndent("        ");
            impl.EmitSerializeBody(member.Source.Type, context);
        }

        sb.AppendLine("""
    }

""");
    }

    private static void EmitDeserialize(
        StringBuilder sb,
        DeriveTargetModel model,
        CSharpClassState classState)
    {
        sb.AppendLine("""
    public void Deserialize(NetDataReader reader)
    {
""");

        var methodContext = new CSharpMethodState(classState);
        foreach (var member in model.Members)
        {
            var impl = GetEmitFieldSupportImpl(member, true);
            var context = CreateEmitContext(sb, member, model, methodContext);

            context.State.ResetIndent("        ");
            impl.EmitDeserializeBody(member.Source.Type, context, false);
        }

        sb.AppendLine("""
    }

""");

        sb.AppendLine("""
    /// <exclude />
    public void DeserializeTracking(NetDataReader reader, int id)
    {
""");

        methodContext = new CSharpMethodState(classState);
        foreach (var member in model.Members)
        {
            var impl = GetEmitFieldSupportImpl(member, true);
            var context = CreateEmitContext(sb, member, model, methodContext);

            context.State.ResetIndent("        ");
            impl.EmitDeserializeBody(member.Source.Type, context, true);
        }

        sb.AppendLine("""
    }

""");
    }

    private static void EmitWriteDelta(
        StringBuilder sb,
        DeriveTargetModel model,
        CSharpClassState classState)
    {
        sb.AppendLine("""
    /// <exclude />
    public void WriteDelta(NetDataWriter writer)
    {
        var mask = _dirtyMask;
        writer.Put(mask);
""");

        var methodContext = new CSharpMethodState(classState);
        foreach (var member in model.Members)
        {
            var impl = GetEmitFieldSupportImpl(member, true);
            var context = CreateEmitContext(sb, member, model, methodContext);

            context.State.ResetIndent("        ");
            impl.EmitWriteDeltaBody(member.Source.Type, context);
        }

        sb.AppendLine("""
    }

""");
    }

    private static void EmitReadDelta(
        StringBuilder sb,
        DeriveTargetModel model,
        CSharpClassState classState)
    {
        sb.AppendLine("""
    /// <exclude />
    public void ReadDelta(NetDataReader reader)
    {
""");
        sb.AppendLine($"""
        var mask = reader.{GetDeserializationMethod(model.MaskInfo.Type)}();
""");

        var methodContext = new CSharpMethodState(classState);
        foreach (var member in model.Members)
        {
            var impl = GetEmitFieldSupportImpl(member, true);
            var context = CreateEmitContext(sb, member, model, methodContext);
            context.SetCurrentMaskVarName("mask");

            context.State.ResetIndent("        ");
            impl.EmitReadDeltaBody(member.Source.Type, context, false);
        }

        sb.AppendLine("""
    }

""");


        sb.AppendLine("""
    /// <exclude />
    public void ReadDeltaTracking(NetDataReader reader, int id)
    {
""");
        sb.AppendLine($"""
        var mask = reader.{GetDeserializationMethod(model.MaskInfo.Type)}();
""");

        methodContext = new CSharpMethodState(classState);
        foreach (var member in model.Members)
        {
            var impl = GetEmitFieldSupportImpl(member, true);
            var context = CreateEmitContext(sb, member, model, methodContext);
            context.SetCurrentMaskVarName("mask");

            context.State.ResetIndent("        ");
            impl.EmitReadDeltaBody(member.Source.Type, context, true);
        }

        sb.AppendLine("""
    }

""");
    }

    private static bool HasDispose(
        StringBuilder sb,
        DeriveTargetModel model,
        CSharpClassState classState)
    {
        var result = false;
        var methodContext = new CSharpMethodState(classState);
        foreach (var member in model.Members)
        {
            var impl = GetEmitFieldSupportImpl(member, true);
            var context = CreateEmitContext(sb, member, model, methodContext);

            result = result || impl.HasDispose(member.Source.Type, context);
        }

        return result;
    }

    private static void EmitDispose(
        StringBuilder sb,
        DeriveTargetModel model,
        CSharpClassState classState)
    {
        sb.AppendLine("""
    public void Dispose()
    {
""");

        var methodContext = new CSharpMethodState(classState);
        foreach (var member in model.Members)
        {
            var impl = GetEmitFieldSupportImpl(member, true);
            var context = CreateEmitContext(sb, member, model, methodContext);

            context.State.ResetIndent("        ");
            impl.EmitDisposeBody(member.Source.Type, context);
        }

        sb.AppendLine("""
    }

""");
    }

    private static void EmitAssign(
        StringBuilder sb,
        DeriveTargetModel model,
        CSharpClassState classState)
    {
        sb.Append(@$"
    public void Assign(in {FullyQualifiedTypeName(model.Source.Symbol)} value)
");

        sb.AppendLine("""
    {
""");

        var methodContext = new CSharpMethodState(classState);
        foreach (var member in model.Members)
        {
            var impl = GetEmitFieldSupportImpl(member, true);
            var context = CreateEmitContext(sb, member, model, methodContext);

            context.State.ResetIndent("        ");
            impl.EmitAssignComponentBody(member.Source.Type, context);
        }

        sb.AppendLine("""
    }

""");
    }

    private static string GetDeserializationMethod(ITypeSymbol type)
        => SerializationHelper.GetDeserializationMethod(type.SpecialType);

    private static bool HasGetEmitFieldSupportImpl(DeriveMemberModel member, bool fallback)
        => CSharpFieldSupportRegistry.FieldTypeSupportVisitor.TryGetImpl(member.Source.Type, fallback, out _);

    private static ICSharpFieldTypeSupportImpl GetEmitFieldSupportImpl(DeriveMemberModel member, bool fallback)
        => CSharpFieldSupportRegistry.FieldTypeSupportVisitor.GetImpl(member.Source.Type, fallback);

    private static CSharpEmitFieldSupportContext CreateEmitContext(
        StringBuilder sb,
        DeriveMemberModel member,
        DeriveTargetModel model,
        CSharpClassState classState)
    {
        var methodState = new CSharpMethodState(classState);
        return CreateEmitContext(sb, member, model, methodState);
    }

    private static CSharpEmitFieldSupportContext CreateEmitContext(
        StringBuilder sb,
        DeriveMemberModel member,
        DeriveTargetModel model,
        CSharpMethodState methodState)
    {
        var emitState = new CSharpEmitState(sb, methodState);
        var context = new CSharpEmitFieldSupportContext(
            emitState,
            member,
            model,
            CSharpFieldSupportRegistry.EmitSerializeVisitor,
            CSharpFieldSupportRegistry.EmitDeserializeVisitor);
        var fieldName = member.Source.Name;
        var fieldType = member.Source.Type;
        context.State.ResetCurrent(fieldName, fieldType);

        context.SetAutoMark("global::ReadyM.Api.Multiplayer.ComponentWriteContext.Current.AutoMarkApiOnWrite");
        var changeContext = new CSharpEmitConflictSupportContext(emitState, member, model);
        changeContext.SetResolver(
            "global::ReadyM.Api.Multiplayer.ComponentWriteContext.Current.ConflictResolver",
            "global::ReadyM.Api.Multiplayer.ComponentWriteContext.Current.LastObservedTime");
        context.SetEmitConflictResolver(new DefaultEmitConflictSupportImpl(), changeContext);

        return context;
    }
}
