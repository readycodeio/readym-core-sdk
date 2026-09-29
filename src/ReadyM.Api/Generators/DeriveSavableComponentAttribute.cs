using System;

namespace ReadyM.Api.Generators;

/// <summary>
/// Opts a component into the save format: the generator emits the ISavableComponent implementation.
/// For a plain value type, use <see cref="DeriveSaveSerializableAttribute"/> instead.
/// </summary>
[AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class DeriveSavableComponentAttribute : Attribute;
