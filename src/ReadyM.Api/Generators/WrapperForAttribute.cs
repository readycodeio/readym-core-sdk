using System;

namespace ReadyM.Api.Generators;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class WrapperForAttribute : Attribute
{
    public Type TargetType { get; }
    public WrapperForAttribute(Type targetType) => TargetType = targetType;
}