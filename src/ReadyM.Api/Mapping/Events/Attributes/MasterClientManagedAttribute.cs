using System;

namespace ReadyM.Api.Mapping.Events;

/// <summary>
/// Only the master client sends the event and runs it; every other client plays it when it arrives.
/// Picks the policy that <see cref="DeriveIGameEventAttribute"/> generates for the event.
/// </summary>
[AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class MasterClientManagedAttribute : Attribute;
