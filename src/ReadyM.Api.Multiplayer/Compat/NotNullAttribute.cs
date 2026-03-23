// ReSharper disable CheckNamespace
#if !NETCOREAPP3_0_OR_GREATER

namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
internal sealed class NotNullAttribute : Attribute
{
    // empty
}

#endif