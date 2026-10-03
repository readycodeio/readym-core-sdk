using System;

namespace ReadyM.Api.Mapping.Events;

/// <summary>
/// Generates the event's <see cref="IGameEvent"/> methods, with the policy its discriminator attribute chooses
/// (<see cref="OwnershipBasedAttribute"/> and the rest). An event that writes its policy by hand does not carry it.
/// </summary>
[AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class DeriveIGameEventAttribute : Attribute;
