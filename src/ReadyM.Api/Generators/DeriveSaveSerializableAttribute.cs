using System;

namespace ReadyM.Api.Generators;

/// <summary>
/// Opts a value type into the save format: the generator emits the ISaveSerializable implementation.
/// Use <see cref="DeriveSavableComponentAttribute"/> for a component instead.
/// </summary>
[AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class DeriveSaveSerializableAttribute : Attribute;
