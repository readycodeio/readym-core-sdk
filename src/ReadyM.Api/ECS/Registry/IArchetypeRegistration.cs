using ReadyM.Api.ECS.Worlds;

namespace ReadyM.Api.ECS.Registry;

internal interface IArchetypeRegistration
{
    void Register(Store world);
}