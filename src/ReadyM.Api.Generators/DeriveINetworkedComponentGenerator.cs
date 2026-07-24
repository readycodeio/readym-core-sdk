using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReadyM.Api.Generators.Derive.CSharp;
using ReadyM.Api.Generators.Derive.CSharp.FieldSupport;
using static ReadyM.Api.Generators.DeriveCSharpUtils;

namespace ReadyM.Api.Generators;

[Generator]
internal sealed class DeriveINetworkedComponentGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var codeProvider = context.SyntaxProvider.CreateSyntaxProvider(Predicate, Transform);

        context.RegisterSourceOutput(
            codeProvider,
            static (spc, nameAndContent) => spc.AddSource($"{nameAndContent.Name}.g.cs", nameAndContent.Code));
    }

    private bool Predicate(SyntaxNode syntaxNode, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return false;

        if (syntaxNode is not StructDeclarationSyntax { AttributeLists.Count: > 0 } structDecl)
            return false;

        var attributes = structDecl.AttributeLists.SelectMany(static x => x.Attributes).ToList();
        return attributes.Any(x => x.Name is IdentifierNameSyntax { Identifier.Text: "DeriveINetworkedComponent" or "DeriveINetworkedComponentAttribute" });
    }

    private (string Name, string Code) Transform(GeneratorSyntaxContext context, CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
            return (string.Empty, string.Empty);

        var symbol = DeriveUtils.GetTargetSymbol(context, ct);
        var targetModel = DeriveComponentUtils.GetTargetModel(false, symbol, context);
        var code = GenerateNetworkedComponent(targetModel);
        var genName = DeriveUtils.GetGeneratedFileName(symbol);

        return (genName, code);
    }

    private string GenerateNetworkedComponent(DeriveTargetModel model)
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

        sb.Append($@"
namespace {info.Namespace};

{access} partial struct {info.Name} : INetworkedComponent{(hasDispose ? ", IDisposable" : string.Empty)}
{{
");

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
        }

        EmitFieldEnums(sb, model, classState);
        EmitApiFlagHelpers(sb, model);

        EmitSerialize(sb, model, classState);
        EmitDeserialize(sb, model, classState);
        EmitWriteDelta(sb, model, classState);
        EmitReadDelta(sb, model, classState);

        sb.AppendLine("""
    /// <exclude />
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

    private void AddDefaultUsings(CSharpModuleState moduleState)
    {
        moduleState.AddUsingList([
            "System",
            "System.Numerics",
            "LiteNetLib.Utils",
            "ReadyM.Api.Generators",
            "ReadyM.Api.Multiplayer",
            "ReadyM.Api.Mapping.Data",
            "ReadyM.Api.Multiplayer.Extensions",
            "ReadyM.Api.Multiplayer.ECS.Components"
        ]);
    }

    private void EmitUsings(StringBuilder sb, CSharpModuleState moduleState)
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

    private void EmitDirtyMask(StringBuilder sb, DeriveTargetModel model)
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

    private void EmitDirtyMethods(
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

    private void EmitAccessorMethods(
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

    private void EmitFieldEnums(
        StringBuilder sb,
        DeriveTargetModel model,
        CSharpClassState classState)
    {
        sb.AppendLine("    /// <exclude />\n");
        sb.AppendLine("    public static class Fields\n        {");
        foreach (var member in model.Members)
        {
            var impl = GetEmitFieldSupportImpl(member, true);
            var context = CreateEmitContext(sb, member, model, classState);
            impl.EmitFieldEnum(member.Source.Type, context);
        }

        sb.AppendLine("        }\n");
    }

    private void EmitApiFlagHelpers(StringBuilder sb, DeriveTargetModel model)
    {
        sb.AppendLine("/// <exclude />");
        sb.AppendLine("public void ClearApiFlag() => _apiMask = 0;");
        sb.AppendLine("/// <exclude />");
        sb.AppendLine($"public void ClearApiFlag(int field) => _apiMask = ({model.MaskInfo!.Type.Name})(_apiMask & ~(({model.MaskInfo!.Type.Name})1 << field));");
        sb.AppendLine("/// <exclude />");
        sb.AppendLine("public readonly bool ChangedFromApi => _apiMask != 0;");
        sb.AppendLine("/// <exclude />");
        sb.AppendLine("public void MarkChangedFromApi() => _apiMask = _dirtyMask;");
    }

    private void EmitSerialize(
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

    private void EmitDeserialize(
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
            impl.EmitDeserializeBody(member.Source.Type, context);
        }

        sb.AppendLine("""
    }

""");
    }

    private void EmitWriteDelta(
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

    private void EmitReadDelta(
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
            impl.EmitReadDeltaBody(member.Source.Type, context);
        }

        sb.AppendLine("""
    }

""");
    }

    private bool HasDispose(
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

    private void EmitDispose(
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

    private void EmitAssign(
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
        var context = new CSharpEmitFieldSupportContext(emitState, member, model, CSharpFieldSupportRegistry.EmitSerializeVisitor, CSharpFieldSupportRegistry.EmitDeserializeVisitor);
        var fieldName = member.Source.Name;
        var fieldType = member.Source.Type;
        context.State.ResetCurrent(fieldName, fieldType);
        return context;
    }
}
