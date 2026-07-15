using System;

namespace ReadyM.Api.Multiplayer.Generators;

/// <exclude />
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class IncludeSerializableAttribute : Attribute;