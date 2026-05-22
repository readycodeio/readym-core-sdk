using System;
using ReadyM.Api.Idents;

namespace ReadyM.Api.Mapping.CreateDestroy;

internal class FuncCreateDeletePolicyFactory<TGameObject>(
    Func<Type, IMappingCreateDeletePolicy<TGameObject>> createPolicy) : IMappingCreateDeletePolicyFactory
    where TGameObject : class
{
    public bool Supports(Type gameObjType)
        => gameObjType == typeof(TGameObject);

    public IMappingCreateDeletePolicyBase CreatePolicy(ArchetypeId archetypeId, Type gameObjType)
        => createPolicy(gameObjType);
}