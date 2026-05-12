using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive.Cpp.FieldSupport;

internal class CppOverrideFieldTypeSupportImpl : CppFieldTypeSupportImplBase
{
    public override bool Supports(DeriveMemberModel model)
    {
        return !string.IsNullOrEmpty(model.CppSettings.CppTypeName);
    }

    public override void EmitAccessorMethods(ITypeSymbol symbol, CppEmitFieldSupportContext context, bool emitPublic)
    {
        foreach (var include in context.Member.CppSettings.Includes)
        {
            context.State.ModuleState.AddInclude(include, false);
        }
        
        base.EmitAccessorMethods(symbol, context, emitPublic);
    }

    protected override void EmitGetterType(ITypeSymbol symbol, CppEmitFieldSupportContext context)
    {
        if (context.Member.CppSettings.GetterTypeName != null)
            context.Append(context.Member.CppSettings.GetterTypeName);
        else
            context.Append(context.Member.CppSettings.CppTypeName!);
    }

    protected override void EmitSetterType(ITypeSymbol symbol, CppEmitFieldSupportContext context)
    {
        if (context.Member.CppSettings.SetterTypeName != null)
            context.Append(context.Member.CppSettings.SetterTypeName);
        else
            context.Append(context.Member.CppSettings.CppTypeName!);
    }

    protected override void EmitAssign(ITypeSymbol symbol, CppEmitFieldSupportContext context)
    {
        if (context.Member.CppSettings.UseMove)
            context.AppendLine($"{context.State.CurrentVar} = std::move(value);");
        else
            context.AppendLine($"{context.State.CurrentVar} = value;");
    }

    protected override void EmitEqualCheck(ITypeSymbol symbol, CppEmitFieldSupportContext context)
        => context.Append($"{context.State.CurrentVar} == value");

    protected override void EmitNotEqualCheck(ITypeSymbol symbol, CppEmitFieldSupportContext context)
        => context.Append($"{context.State.CurrentVar} != value");

    public override bool HasAssignComponent(ITypeSymbol sourceType, CppEmitFieldSupportContext context)
        => !context.Member.CppSettings.UseMove;

    public override void EmitBackingField(ITypeSymbol symbol, CppEmitFieldSupportContext context)
    {
        var cppTypeName = context.Member.CppSettings.CppTypeName!;
        var defaultValue = context.Member.CppSettings.DefaultValue ?? "{}";
        
        context.AppendLine($"{cppTypeName} {context.Member.Source.Name} = {defaultValue};");
    }
}