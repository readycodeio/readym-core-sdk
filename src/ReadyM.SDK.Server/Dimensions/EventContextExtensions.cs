using ReadyM.Api.Idents;
using ReadyM.SDK.Events;

namespace ReadyM.SDK.Server.Dimensions;

/// <summary>What a server handler of a networked event can read about its dimension.</summary>
public static class EventContextExtensions
{
    /// <summary>
    /// The dimension the event is in: where its origin is, or its subject's for an event the server raised. Pass it to
    /// lookups that should see it.
    /// </summary>
    public static DimensionId Dimension(this in EventContext context) => context.EventDimension;
}
