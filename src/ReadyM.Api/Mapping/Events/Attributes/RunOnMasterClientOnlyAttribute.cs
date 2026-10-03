using System;

namespace ReadyM.Api.Mapping.Events;

/// <summary>
/// Only the master client runs the event: it runs its own directly, and a client that owns the subject sends the
/// event to the master instead of running it.
/// Picks the policy that <see cref="DeriveIGameEventAttribute"/> generates for the event.
/// </summary>
/// <param name="subject">The field holding the subject entity, written with <c>nameof</c>: an <c>Entity</c> or a
/// <c>RawEntity</c>.</param>
[AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class RunOnMasterClientOnlyAttribute(string subject) : Attribute
{
    public string Subject { get; } = subject;
}
