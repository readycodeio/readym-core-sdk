using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive.Cpp.FieldSupport;

internal class NativeContainerFieldTypeSupportImpl : NativeContainerFieldTypeSupportImplBase
{
    protected override bool Supports(ITypeSymbol type)
        => SerializationHelper.IsNativeContainer(type) &&
           !SerializationHelper.IsNativeString(type, out _) &&
           !SerializationHelper.IsNativeList(type, out _) &&
           !SerializationHelper.IsNativeDictionary(type, out _, out _, out _);
}