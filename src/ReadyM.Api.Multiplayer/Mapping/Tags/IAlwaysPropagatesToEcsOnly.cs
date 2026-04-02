namespace ReadyM.Api.Multiplayer.Mapping.Tags;

// NOTE(api): This is used for events that are only sent to the server and therefore are never received
// by clients
public interface IAlwaysPropagatesToEcsOnly : IMappingContext<EmptyContext>
{
    // empty
}