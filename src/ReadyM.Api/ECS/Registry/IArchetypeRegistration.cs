using ReadyM.Api.ECS.Worlds;

namespace ReadyM.Api.ECS.Registry;

public interface IArchetypeRegistration
{
    public void Register(Store world);
}