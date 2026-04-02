using System;
using JetBrains.Annotations;

namespace ReadyM.Api.Serialization;

[AttributeUsage(AttributeTargets.Class)]
[MeansImplicitUse]
internal sealed class RegisterJsonConverterAttribute : Attribute;