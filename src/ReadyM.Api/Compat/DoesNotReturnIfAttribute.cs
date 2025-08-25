namespace System.Diagnostics.CodeAnalysis;

internal class DoesNotReturnIfAttribute : Attribute
{
    public DoesNotReturnIfAttribute(bool parameterValue)
        => ParameterValue = parameterValue;

    public bool ParameterValue { get; }
}