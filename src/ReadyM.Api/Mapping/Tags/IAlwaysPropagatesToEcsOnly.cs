namespace ReadyM.Api.Mapping.Tags;

// NOTE(api): This is used for events that are only sent to the server and therefore are never received
// by clients

/// <exclude />
public interface IAlwaysPropagatesToEcsOnly : IMappingContext<EmptyContext>
{
    // empty
}