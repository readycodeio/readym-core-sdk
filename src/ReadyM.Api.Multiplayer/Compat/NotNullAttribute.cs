using System;

namespace ReadyM.Api.Multiplayer.Compat;

[AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
internal sealed class NotNullAttribute : Attribute
{
    // empty
}