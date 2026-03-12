// ReSharper disable once CheckNamespace
namespace System.Diagnostics.CodeAnalysis;

public class DoesNotReturnIfAttribute : Attribute
{
    public DoesNotReturnIfAttribute(bool parameterValue)
        => ParameterValue = parameterValue;

    public bool ParameterValue { get; }
}