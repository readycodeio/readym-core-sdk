using ReadyM.Api.Mapping.Tags;

namespace ReadyM.Api.Tests.TestEvents;

public struct ManagedEvent : IAlwaysPropagates
{
    public int IntValue { get; init; }
    public float FloatValue { get; init; }
}