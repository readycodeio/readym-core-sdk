using System;

namespace ReadyM.Api.Mapping.Events;

/// <summary>
/// The event is never sent to the ECS; it runs where it happens, and is always played where it arrives.
/// Picks the policy that <see cref="DeriveIGameEventAttribute"/> generates for the event.
/// </summary>
[AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class AlwaysPropagatesToGameOnlyAttribute : Attribute;
