using ReadyM.Api.Mapping.Events;

namespace ReadyM.Api.Tests.TestEvents;

[DeriveIGameEvent, AlwaysPropagates]
public partial struct ManagedEvent
{
    public int IntValue { get; init; }
    public float FloatValue { get; init; }
}