namespace ReadyM.Api.Generators.Derive.CSharp.Serialization;

internal interface ICSharpTypeSerializationImpl
    : IDeriveTypeSupportImpl<CSharpEmitSerializeContext>,
      IDeriveTypeSupportImpl<CSharpEmitDeserializeContext>
{
    // empty
}