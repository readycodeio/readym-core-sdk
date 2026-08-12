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
        new NativeStringFieldTypeSupportImpl(),
        new NativeDictionaryFieldTypeSupportImpl(),
        new NativeListFieldTypeSupportImpl(),
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
        new NativeStringSerializationImpl(),
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
}