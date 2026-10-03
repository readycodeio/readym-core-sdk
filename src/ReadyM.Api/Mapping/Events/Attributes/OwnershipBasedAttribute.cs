using System;

namespace ReadyM.Api.Mapping.Events;

/// <summary>
/// The owner of the subject entity sends the event and runs it; every other machine plays it when it arrives.
/// Picks the policy that <see cref="DeriveIGameEventAttribute"/> generates for the event.
/// </summary>
/// <param name="subject">The field holding the subject entity, written with <c>nameof</c>: an <c>Entity</c> or a
/// <c>RawEntity</c>.</param>
[AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class OwnershipBasedAttribute(string subject) : Attribute
{
    public string Subject { get; } = subject;
}
