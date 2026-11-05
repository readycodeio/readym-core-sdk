using System;

namespace ReadyM.Api.Multiplayer.Generators;

/// <summary>
/// The annotated float or vector will be serialized on the wire as 16-bit floats, saving 2 bytes per float.
/// </summary>
/// <remarks>
/// Keeps about three significant digits and reaches up to 65504, so it suits values with no fixed range, like speeds,
/// velocities and play rates. Do not use it for world positions: far from the origin a step becomes tens of units.
/// A change is sent only when the 16-bit value changes and the change is over 0.1, the cutoff plain fields use.
/// </remarks>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class HalfPrecisionAttribute : Attribute;
