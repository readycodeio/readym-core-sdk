namespace ReadyM.Api.Mapping.Tags;

// NOTE(api): This is used for events that are only sent to clients from the server and therefore are never 
// triggered / sent locally

/// <exclude />
public interface IAlwaysPropagatesToGameOnly : IMappingContext<EmptyContext>
{
    // empty
}