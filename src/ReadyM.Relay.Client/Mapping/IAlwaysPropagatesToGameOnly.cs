using ReadyM.Api.Mapping;

namespace ReadyM.Relay.Client.Mapping;

// NOTE(api): This is used for events that are only sent to clients from the server and therefore are never 
// triggered / sent locally
public interface IAlwaysPropagatesToGameOnly : IMappingContext<EmptyContext>
{
    // empty
}