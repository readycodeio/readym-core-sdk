namespace ReadyM.Api.Multiplayer.Mapping.Tags;

// NOTE(api): This is used for events that are only sent to clients from the server and therefore are never 
// triggered / sent locally
public interface IAlwaysPropagatesToGameOnly : IMappingContext<EmptyContext>
{
    // empty
}