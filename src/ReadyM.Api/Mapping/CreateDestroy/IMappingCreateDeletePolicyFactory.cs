using System;
using ReadyM.Api.Idents;

namespace ReadyM.Api.Mapping.CreateDestroy;

internal interface IMappingCreateDeletePolicyFactory
{
    bool Supports(Type gameObjType);
    
    IMappingCreateDeletePolicyBase CreatePolicy(ArchetypeId archetypeId, Type gameObjType);
}