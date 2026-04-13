using System.Linq;
using Microsoft.CodeAnalysis;
using ReadyM.Api.Generators.FieldSupport.Cpp;
using ReadyM.Api.Generators.FieldSupport.CSharp;

namespace ReadyM.Api.Generators.FieldSupport;

internal class FieldSupportRegistry
{
    private static readonly ICSharpFieldTypeSupport[] CSharpFieldSupports =
    [
        new CSharp.PrimitiveFieldTypeSupport(),
        new CSharp.EnumFieldTypeSupport(),
        new CSharp.VectorLikeFieldTypeSupport(),
        new CSharp.DeltaEquatableFieldTypeSupport(),
        new CSharp.EquatableFieldTypeSupport(),
        new CSharp.CustomSerializableFieldTypeSupport(),
    ];

    private static readonly ICppFieldTypeSupport[] CppFieldSupports =
    [
        new Cpp.PrimitiveFieldTypeSupport(),
        new Cpp.EnumFieldTypeSupport(),
        new Cpp.VectorLikeFieldTypeSupport(),
        new Cpp.DeltaEquatableFieldTypeSupport(),
        new Cpp.EquatableFieldTypeSupport(),
        new Cpp.CustomSerializableFieldTypeSupport(),
    ];
    
    internal static ICSharpFieldTypeSupport? ResolveCSharpSupport(ITypeSymbol type)
        => CSharpFieldSupports.FirstOrDefault(s => s.CanHandle(type));

    internal static ICppFieldTypeSupport? ResolveCppSupport(ITypeSymbol type)
        => CppFieldSupports.FirstOrDefault(s => s.CanHandle(type));
}