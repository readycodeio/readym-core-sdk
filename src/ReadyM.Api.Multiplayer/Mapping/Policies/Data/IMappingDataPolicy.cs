namespace ReadyM.Api.Multiplayer.Mapping.Policies.Data;

internal interface IMappingDataPolicy<TContext> : IMappingDataPolicyBase
{
    /// Should data be copied from the game object to the ECS entity
    bool ShouldGameCopyToEcs(in TContext context);

    /// Should data be copied from the ECS entity to the game object
    bool ShouldEcsCopyToGame(in TContext context);
    
    /// Should data be allowed to be set from the API.
    bool CanSetFromApi(in TContext context);

    /// Should data be allowed to be stored / set locally (analogous to ShouldRunLocally for events).
    bool CanGameSetLocally(in TContext context);
}