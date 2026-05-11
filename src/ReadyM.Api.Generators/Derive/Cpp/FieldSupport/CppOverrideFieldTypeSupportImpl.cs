using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive.Cpp.FieldSupport;

internal class CppOverrideFieldTypeSupportImpl : CppFieldTypeSupportImplBase
{
    private string? GetOverrideCppTypeName(DeriveMemberModel model)
        => AttributeUtils.GetAttribute<string?>(model.Source.Symbol, "CppNativeFieldTypeAttribute", "cppTypeName", null);
    
    private string? GetOverrideGetterType(DeriveMemberModel model)
        => AttributeUtils.GetAttribute<string?>(model.Source.Symbol, "CppNativeFieldTypeAttribute", "getterTypeName", null)
            ?? GetOverrideCppTypeName(model);
    
    private string? GetOverrideSetterType(DeriveMemberModel model)
        => AttributeUtils.GetAttribute<string?>(model.Source.Symbol, "CppNativeFieldTypeAttribute", "setterTypeName", null)
            ?? GetOverrideCppTypeName(model);

    private string? GetOverrideDefaultValue(DeriveMemberModel model)
        => AttributeUtils.GetAttribute<string?>(model.Source.Symbol, "CppNativeFieldTypeAttribute", "defaultValue", null);

    private bool GetOverrideUseMove(DeriveMemberModel model)
        => AttributeUtils.GetAttribute<bool>(model.Source.Symbol, "CppNativeFieldTypeAttribute", "useMove", false);

    public override bool Supports(DeriveMemberModel model)
    {
        var cppTypeName = GetOverrideCppTypeName(model);
        return !string.IsNullOrEmpty(cppTypeName);
    }

    public override void EmitAccessorMethods(ITypeSymbol symbol, CppEmitFieldSupportContext context, bool emitPublic)
    {
        var includes = AttributeUtils.GetArrayAttribute<string>(context.Member.Source.Symbol, "CppNativeFieldTypeAttribute", "includes");
        if (includes != null)
        {
            foreach (var include in includes)
            {
                context.State.ModuleState.AddInclude(include, false);
            }
        }
        
        base.EmitAccessorMethods(symbol, context, emitPublic);
    }

    protected override void EmitGetterType(ITypeSymbol symbol, CppEmitFieldSupportContext context)
    {
        context.Append(GetOverrideGetterType(context.Member)!);
    }

    protected override void EmitSetterType(ITypeSymbol symbol, CppEmitFieldSupportContext context)
    {
        context.Append(GetOverrideSetterType(context.Member)!);
    }

    protected override void EmitAssign(ITypeSymbol symbol, CppEmitFieldSupportContext context)
    {
        if (GetOverrideUseMove(context.Member))
            context.AppendLine($"{context.State.CurrentVar} = std::move(value);");
        else
            context.AppendLine($"{context.State.CurrentVar} = value;");
    }

    protected override void EmitEqualCheck(ITypeSymbol symbol, CppEmitFieldSupportContext context)
        => context.Append($"{context.State.CurrentVar} == value");

    protected override void EmitNotEqualCheck(ITypeSymbol symbol, CppEmitFieldSupportContext context)
        => context.Append($"{context.State.CurrentVar} != value");

    public override bool HasAssignComponent(ITypeSymbol sourceType, CppEmitFieldSupportContext context)
        => !GetOverrideUseMove(context.Member);

    public override void EmitBackingField(ITypeSymbol symbol, CppEmitFieldSupportContext context)
    {
        var cppTypeName = GetOverrideCppTypeName(context.Member)!;
        var defaultValue = GetOverrideDefaultValue(context.Member) ?? "{}";
        
        context.AppendLine($"{cppTypeName} {context.Member.Source.Name} = {defaultValue};");
    }
}