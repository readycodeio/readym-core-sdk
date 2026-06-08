namespace ReadyM.Relay.Server.Sdk.Ecs.Systems;

public interface ISystemRegistry
{
    void RegisterSystem<T>() where T : PluginSystemBase;
}