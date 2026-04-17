using System.Text;
using Microsoft.CodeAnalysis;
using ReadyM.Api.Generators.Derive.Cpp.FieldSupport;

namespace ReadyM.Api.Generators.Derive.Cpp;

internal class CppFieldSupportRegistry
{
    private static readonly ICppFieldTypeSupportImpl[] CppFieldSupportImpls =
    [
        new PrimitiveFieldTypeSupportImpl(),
        new EnumFieldTypeSupportImpl(),
        new VectorLikeFieldTypeSupportImpl(),
        new NativeStringFieldTypeSupportImpl(),
        new NativeContainerFieldTypeSupportImpl(),
        new DeltaEquatableFieldTypeSupportImpl(),
        new EquatableFieldTypeSupportImpl(),
    ];

    internal static readonly DeriveTypeSupportVisitorBase<ICppFieldTypeSupportImpl> FieldTypeSupportVisitor = new(
        CppFieldSupportImpls,
        new FallbackFieldTypeSupportImpl()
    );
    
    internal static CppEmitFieldSupportContext CreateEmitFieldSupportContext(
        StringBuilder sb,
        string generatedPropertyName,
        string fieldName,
        ITypeSymbol fieldType,
        ITypeSymbol maskType,
        int maskIndex,
        CppModuleState moduleState)
    {
        var state = new CppEmitState(sb, moduleState);
        var context = new CppEmitFieldSupportContext(state, maskType, maskIndex);
        var cppType = DeriveCppUtils.CppTypeName(fieldType);
        context.State.SetGeneratedPropertyName(generatedPropertyName);
        context.State.PushCurrent(fieldName, fieldType, cppType);
        return context;
    }
}