using System;
using ReadyM.Api.Idents;

namespace ReadyM.Api.Multiplayer.Mapping.CreateDestroy;

internal interface IMappingCreateDeletePolicyFactory
{
    bool Supports(Type gameObjType);
    
    IMappingCreateDeletePolicyBase CreatePolicy(ArchetypeId archetypeId, Type gameObjType);
}