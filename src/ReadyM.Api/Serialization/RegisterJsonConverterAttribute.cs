using System;
using JetBrains.Annotations;

namespace ReadyM.Api.Serialization;

[AttributeUsage(AttributeTargets.Class)]
[MeansImplicitUse]
public sealed class RegisterJsonConverterAttribute : Attribute;