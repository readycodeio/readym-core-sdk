using System;

namespace ReadyM.Api.Multiplayer.Generators;

/// <summary>
/// The float or vector field is sent over the wire as a fixed-point number in <c>bits</c> bits (8, 16 or 24) between <c>min</c> and
/// <c>max</c>, instead of a 4-byte float per number.
/// </summary>
/// <remarks>
/// Values outside the range are clamped, so pick a range the value can never leave. The step between two values the
/// network can tell apart is <c>(max - min) / (2^bits - 2)</c>, which keeps both ends and the middle of the range exact.
/// A change is sent only when it crosses into another step and is over 0.1, the cutoff plain fields use.
/// </remarks>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class QuantizeAttribute(float min, float max, int bits) : Attribute
{
    public float Min { get; } = min;
    public float Max { get; } = max;
    public int Bits { get; } = bits;
}
