using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Loader;
using ReadyM.Relay.Server.Sdk.Ecs.Components;

namespace ReadyM.Relay.Server.Sdk;

public abstract class ServerModBase
{
    protected IServerDependencyContainer Services { get; private set; } = null!;

    /// <summary>
    /// This mod's own <c>server</c> folder, which holds its assemblies and any config files.
    /// </summary>
    protected string ModDirectory { get; private set; } = null!;

    [UsedImplicitly]
    public void InitializeAot(IModComponentRegistry registry)
    {
        RegisterComponents(registry);
    }

    [UsedImplicitly]
    public void Initialize(IServerDependencyContainer services, string modDirectory)
    {
        Services = services;
        ModDirectory = modDirectory;
        Init();
    }

    /// <summary>
    /// Reads <paramref name="fileName"/> from this mod's server folder and registers the result as a singleton.
    /// </summary>
    /// <remarks>
    /// A missing file yields defaults. A file that exists but does not parse, or that carries a key the
    /// config type does not declare, throws <see cref="ModConfigException" />.
    /// </remarks>
    protected void RegisterConfig<TConfig>(string fileName = ModConfigReader.DefaultFileName)
        where TConfig : class, new()
    {
        var config = ModConfigReader.Read<TConfig>(ModDirectory, fileName, Services.Resolve<ILogger>());
        Services.RegisterSingleton(config);
    }
    
    private class FunctionalArchetypeRegistration(Action<IArchetypeRegistry> callback) : IArchetypeRegistration
    {
        public void Register(IArchetypeRegistry registry)
        {
            callback(registry);
        }
    }
    
    /// <summary>
    /// Register new archetypes or modify existing.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    protected void RegisterArchetypes(Action<IArchetypeRegistry> configure)
    {
        Services.RegisterSingleton<IArchetypeRegistration>(new FunctionalArchetypeRegistration(configure));
    }

    /// <summary>
    /// Any components defined in the mod must be registered here.
    /// </summary>
    protected virtual void RegisterComponents(IModComponentRegistry registry) { }

    protected abstract void Init();
}