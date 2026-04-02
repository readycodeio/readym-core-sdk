using Friflo.Engine.ECS;

namespace ReadyM.Api.Multiplayer.Mapping.Policies.Data.Common;

internal readonly struct MappedEntityDataPolicy(IMappingDataPolicy<Entity> dataPolicy)
{
    public bool ShouldGameCopyToEcs(Entity? tamerEntity)
    {
        if (!tamerEntity.HasValue)
            return false;
        
        return dataPolicy.ShouldGameCopyToEcs(tamerEntity.Value);
    }
    
    public bool ShouldEcsCopyToGame(Entity? tamerEntity)
    {
        if (!tamerEntity.HasValue)
            return false;

        return dataPolicy.ShouldEcsCopyToGame(tamerEntity.Value);
    }

    public bool ShouldGameSetLocally(Entity? tamerEntity)
    {
        if (!tamerEntity.HasValue)
            return false;

        return dataPolicy.CanGameSetLocally(tamerEntity.Value);
    }
}