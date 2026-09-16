using System;

namespace ReadyM.Api.Generators;

/// <summary>
/// Excludes a member from the save format.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class SaveIgnoreAttribute : Attribute;
