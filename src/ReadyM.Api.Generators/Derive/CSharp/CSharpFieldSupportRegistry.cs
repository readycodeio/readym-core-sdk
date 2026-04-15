using System.Text;
using Microsoft.CodeAnalysis;
using ReadyM.Api.Generators.Derive.CSharp.FieldSupport;
using ReadyM.Api.Generators.Derive.CSharp.Serialization;

namespace ReadyM.Api.Generators.Derive.CSharp;

internal class CSharpFieldSupportRegistry
{
    private static readonly ICSharpFieldTypeSupportImpl[] CSharpFieldSupportImpls =
    [
        new PrimitiveFieldTypeSupportImpl(),
        new EnumFieldTypeSupportImpl(),
        new VectorLikeFieldTypeSupportImpl(),
        new NativeContainerFieldTypeSupportImpl(),
        new DeltaEquatableFieldTypeSupportImpl(),
        new EquatableFieldTypeSupportImpl(),
    ];

    private static readonly ICSharpTypeSerializationImpl[] CSharpSerializationImpls =
    [
        new PrimitiveSerializationImpl(),
        new EnumSerializationImpl(),
        new VectorLikeSerializationImpl(),
        new NativeListSerializationImpl(),
        new NativeDictionarySerializationImpl(),
        new CustomMethodSerializationImpl(),
    ];
    
    internal static readonly DeriveTypeSupportVisitorBase<ICSharpFieldTypeSupportImpl> FieldTypeSupportVisitor = new(
        CSharpFieldSupportImpls,
        new FallbackFieldTypeSupportImpl()
    );

    internal static readonly DeriveTypeSupportVisitor<ICSharpTypeSerializationImpl, CSharpEmitSerializeContext> EmitSerializeVisitor = new(
        CSharpSerializationImpls,
        new FallbackTypeSerializationImpl()
    );
    
    internal static readonly DeriveTypeSupportVisitor<ICSharpTypeSerializationImpl, CSharpEmitDeserializeContext> EmitDeserializeVisitor = new(
        CSharpSerializationImpls,
        new FallbackTypeSerializationImpl()
    );
    
    internal static CSharpEmitFieldSupportContext CreateEmitFieldSupportContext(
        StringBuilder sb,
        string generatedPropertyName,
        string fieldName,
        ITypeSymbol fieldType,
        ITypeSymbol maskType,
        int maskIndex,
        CSharpModuleState moduleState)
    {
        var state = new CSharpEmitState(sb, moduleState);
        var context = new CSharpEmitFieldSupportContext(state, maskType, maskIndex, EmitSerializeVisitor, EmitDeserializeVisitor);
        context.State.SetGeneratedPropertyName(generatedPropertyName);
        context.State.ResetCurrent(fieldName, fieldType);
        return context;
    }
    
    internal static CSharpEmitSerializeContext CreateEmitSerializeContext(
        string fieldName,
        ITypeSymbol fieldType,
        CSharpModuleState moduleState)
    {
        var sb = new StringBuilder();
        var state = new CSharpEmitState(sb, moduleState);
        var context = new CSharpEmitSerializeContext(state, EmitSerializeVisitor);
        context.State.ResetCurrent(fieldName, fieldType);
        return context;
    }
    
    internal static CSharpEmitDeserializeContext CreateEmitDeserializeContext(
        string fieldName,
        ITypeSymbol fieldType,
        CSharpModuleState moduleState)
    {
        var sb = new StringBuilder();
        var state = new CSharpEmitState(sb, moduleState);
        var context = new CSharpEmitDeserializeContext(state, EmitDeserializeVisitor);
        context.State.ResetCurrent(fieldName, fieldType);
        return context;
    }
}