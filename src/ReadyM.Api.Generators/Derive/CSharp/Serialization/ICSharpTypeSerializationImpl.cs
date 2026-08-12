using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive.CSharp.Serialization;

internal interface ICSharpTypeSerializationImpl
    : IDeriveSupportImpl<ITypeSymbol, CSharpEmitSerializeContext>,
      IDeriveSupportImpl<ITypeSymbol, CSharpEmitDeserializeContext>
{
    // empty
}