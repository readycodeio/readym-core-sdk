using System;

namespace ReadyM.Api.ECS.Registry;

[AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)]
public class NativeComponentAttribute<T> : Attribute
    where T : unmanaged
{
    // empty
}