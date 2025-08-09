using System;

namespace ReadyM.Api.Multiplayer.Generators;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class ExcludeSerializableAttribute : Attribute;