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
public sealed class DeriveINetworkedComponentGenerator : IIncrementalGenerator
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
        return attributes.Any(x => x.Name is IdentifierNameSyntax { Identifier.Text: "DeriveINetworkedComponent" });
    }

    private (string Name, string Code) Transform(GeneratorSyntaxContext context, CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
            return (string.Empty, string.Empty);

        var symbol = DeriveUtils.GetAttributedSymbol(context, ct);
        var targetModel = DeriveComponentUtils.GetTargetModel(symbol, context);
        var code = GenerateNetworkedComponent(targetModel);
        var genName = DeriveUtils.GetGeneratedFileName(symbol);

        return (genName, code);
    }

    private string GenerateNetworkedComponent(DeriveTargetModel model)
    {
        var info = model.SourceTarget;
        var members = model.Members;
        
        if (model.MaskInfo.Bits < members.Count)
        {
            if (model.SourceTarget.EmitDirtyMask)
            {
                model.MaskInfo.AddError(
                    $"Too many synced fields in `{model.SourceTarget.Name}` " +
                    $"to fit in a dirty mask. Maximum supported is {model.MaskInfo.Bits.ToString(CultureInfo.InvariantCulture)}" +
                    $", but {members.Count.ToString(CultureInfo.InvariantCulture)} were found.");
            }
            else
            {
                model.MaskInfo.AddError(
                    $"Too many synced members in `{model.SourceTarget.Name}` " +
                    $"to fit in the user-supplied _dirtyMask. Maximum supported is {model.MaskInfo.Bits.ToString(CultureInfo.InvariantCulture)}" +
                    $", but {members.Count.ToString(CultureInfo.InvariantCulture)} were found.");
            }
        }
        
        foreach (var member in members)
        {
            if (!HasGetEmitFieldSupportImpl(member, false))
            {
                member.SourceMember.AddError($"Unsupported type '{member.SourceMember.Type.ToDisplayString()}' for networked member '{member.SourceMember.Name}'.");
            }
        }
        
        var sb = new StringBuilder();
        var moduleState = new CSharpModuleState();
        var classState = new CSharpClassState(moduleState);
        
        AddDefaultUsings(moduleState);
        
        sb.Append($@"
namespace {info.Namespace};

public partial struct {info.Name} : INetworkedComponent
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
            EmitAccessors(sb, member, model, classState);
            sb.AppendLine();
        }

        EmitSerialize(sb, model, classState);
        EmitDeserialize(sb, model, classState);
        EmitWriteDelta(sb, model, classState);
        EmitReadDelta(sb, model, classState);
        EmitSkipDelta(sb, model, classState);

        sb.AppendLine("""
    public void ClearDirty() => _dirtyMask = 0;
    public bool IsDirty => _dirtyMask != 0;
""");

        if (classState.Members.Count > 0)
        {
            sb.AppendLine();
            
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
        
        // NOTE: Emitting usings
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
            "ReadyM.Api.Multiplayer.Extensions",
            "ReadyM.Api.Multiplayer.ECS.Components",
            "ReadyM.Relay.Common"
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
        
        if (model.SourceTarget.EmitDirtyMask)
        {
            sb.AppendLine($"""
    private {FullyQualifiedTypeName(mask.Type)} _dirtyMask;
                                   
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
    
    private void EmitAccessors(
        StringBuilder sb,
        DeriveMemberModel member,
        DeriveTargetModel model,
        CSharpClassState classState)
    {
        if (member.SourceMember.HasErrors)
        {
            foreach (var error in member.SourceMember.Errors)
            {
                sb.AppendLine($"""
    #error {error}

""");
            }
        }
        
        var impl = GetEmitFieldSupportImpl(member, true);
        var context = CreateEmitContext(sb, member, model, classState);
        
        context.State.ResetIndent("    ");
        impl.EmitAccessors(member.SourceMember.Type, context);
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
            impl.EmitSerializeBody(member.SourceMember.Type, context);
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
            impl.EmitDeserializeBody(member.SourceMember.Type, context);
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
            impl.EmitWriteDeltaBody(member.SourceMember.Type, context);
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
            impl.EmitReadDeltaBody(member.SourceMember.Type, context);
        }

        sb.AppendLine("""
    }

""");
    }

    private void EmitSkipDelta(
        StringBuilder sb,
        DeriveTargetModel model,
        CSharpClassState classState)
    {
        sb.AppendLine("""
    public void SkipDelta(NetDataReader reader)
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
            impl.EmitSkipDeltaBody(member.SourceMember.Type, context);
        }

        sb.AppendLine("""
    }

""");
    }

    private static string GetDeserializationMethod(ITypeSymbol type)
        => SerializationHelper.GetDeserializationMethod(type.SpecialType);

    private static bool HasGetEmitFieldSupportImpl(DeriveMemberModel member, bool fallback)
        => CSharpFieldSupportRegistry.FieldTypeSupportVisitor.TryGetImpl(member.SourceMember.Type, fallback, out _);

    private static ICSharpFieldTypeSupportImpl GetEmitFieldSupportImpl(DeriveMemberModel member, bool fallback)
        => CSharpFieldSupportRegistry.FieldTypeSupportVisitor.GetImpl(member.SourceMember.Type, fallback);

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
        emitState.SetGeneratedPropertyName(member.GeneratedPropertyName);
        var context = new CSharpEmitFieldSupportContext(emitState, model.MaskInfo.Type, member.Index, CSharpFieldSupportRegistry.EmitSerializeVisitor, CSharpFieldSupportRegistry.EmitDeserializeVisitor);
        context.State.ResetCurrent(member.SourceMember.Name, member.SourceMember.Type);
        return context;
    }
}