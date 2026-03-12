using System;

namespace ReadyM.Api.Generators;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
internal sealed class WrapperForAttribute(Type targetType) : Attribute
{
    public Type TargetType { get; } = targetType;
}