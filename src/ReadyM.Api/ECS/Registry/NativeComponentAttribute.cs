using System;

namespace ReadyM.Api.ECS.Registry;

[AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)]
public class NativeComponentAttribute : Attribute
{
    // empty
}