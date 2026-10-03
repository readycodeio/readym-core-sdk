using System;

namespace ReadyM.Api.Mapping.Events;

/// <summary>
/// The event is always sent to the ECS and runs where it happens, but is never played where it arrives.
/// Picks the policy that <see cref="DeriveIGameEventAttribute"/> generates for the event.
/// </summary>
[AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class AlwaysPropagatesToEcsOnlyAttribute : Attribute;
