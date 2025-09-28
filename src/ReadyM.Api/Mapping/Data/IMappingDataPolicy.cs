namespace ReadyM.Api.Mapping.Data;

public interface IMappingDataPolicy<TContext> : IMappingDataPolicyBase
    where TContext : struct
{
    // Should data be copied from the ECS entity to the game object
    bool ShouldEcsCopyToGame(in TContext context);
    
    // Should data be copied from the game object to the ECS entity
    bool ShouldGameCopyToEcs(in TContext context);

    // Should data be allowed to be stored / set locally (analogous to ShouldRunLocally for events).
    bool ShouldGameSetLocally(in TContext context);
}
