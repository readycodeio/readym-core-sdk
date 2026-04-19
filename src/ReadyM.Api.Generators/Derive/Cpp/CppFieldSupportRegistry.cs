using System.Text;
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
        DeriveMemberModel member,
        DeriveTargetModel model,
        CppModuleState moduleState)
    {
        var state = new CppEmitState(sb, moduleState);
        var context = new CppEmitFieldSupportContext(state, member, model);
        var fieldName = member.Source.Name;
        var fieldType = member.Source.Type;
        var cppType = DeriveCppUtils.CppTypeName(fieldType);
        context.State.PushCurrent(fieldName, fieldType, cppType);
        return context;
    }
}