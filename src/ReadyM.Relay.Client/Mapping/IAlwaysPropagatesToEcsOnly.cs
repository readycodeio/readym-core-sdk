using ReadyM.Api.Mapping;

namespace ReadyM.Relay.Client.Mapping;

// NOTE(api): This is used for events that are only sent to the server and therefore are never received
// by clients
public interface IAlwaysPropagatesToEcsOnly : IMappingContext<EmptyContext>
{
    // empty
}