using System;

namespace ReadyM.SDK.Attributes;

// Takes the rest of the line verbatim, spacing and all
// On the last parameter, which must be a string. Rejoining tokens would turn two spaces into one
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class RemainderAttribute : Attribute;
