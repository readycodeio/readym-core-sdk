using System.Collections.Generic;
using Friflo.Engine.ECS;
using ReadyM.Api.Idents;

namespace ReadyM.Api.State;

public interface IClientState
{
    PlayerId? LocalPlayerId { get; }
    Entity? LocalPlayerEntity { get; }
    AreaId? CurrentAreaId { get; }
    
    IReadOnlyList<PlayerId> AreaPlayers { get; }
}