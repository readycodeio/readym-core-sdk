using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive.CSharp.FieldSupport;

internal class NativeContainerFieldTypeSupportImpl : NativeContainerFieldTypeSupportImplBase
{
    public override bool Supports(ITypeSymbol type)
        => SerializationHelper.IsNativeDictionary(type, out _, out _, out _) &&
           !SerializationHelper.IsNativeString(type, out _) &&
           !SerializationHelper.IsNativeList(type, out _) &&
           !SerializationHelper.IsNativeDictionary(type, out _, out _, out _);
}