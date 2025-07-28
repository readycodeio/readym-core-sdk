using System;

namespace ReadyM.Api.Serialization;

[AttributeUsage(AttributeTargets.Struct)]
public sealed class DeriveJsonSerializableAttribute : Attribute;