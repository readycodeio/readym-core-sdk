using ReadyM.Api.ECS.Worlds;

namespace ReadyM.Api.ECS.Registry;

public interface IArchetypeRegistration
{
    void Register(IArchetypeRegistry registry);
}