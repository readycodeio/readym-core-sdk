using System;

namespace ReadyM.Api.Multiplayer.Generators;

/// <summary>
/// Sends a NativeString256, or a NativeList of them, as a small id once the server has seen the string.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class InternedAttribute : Attribute;
