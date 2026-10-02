using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using Microsoft.Extensions.Logging;
using ReadyM.Api.DI;
using ReadyM.Api.Loader;

namespace ReadyM.SDK.Mods;

/// Holds the config classes each mod assembly declares, until there is a folder to read them from.
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ModConfigRegistry
{
    private static readonly ConcurrentDictionary<Assembly, ConcurrentDictionary<Type, Declaration>> Declared = new();

    /// Called by generated code for each [ModConfig] class an assembly declares.
    public static void Declare<TConfig>(string fileName) where TConfig : class, new()
        => Declared.GetOrAdd(typeof(TConfig).Assembly, _ => new ConcurrentDictionary<Type, Declaration>())
            [typeof(TConfig)] = new Declaration<TConfig>(fileName);

    /// <summary>
    /// Reads every config this assembly declares out of the mod's folder and registers it.
    /// </summary>
    public static int RegisterAll(Assembly assembly, IDependencyContainer container, string modDirectory)
    {
        if (!Declared.TryGetValue(assembly, out var declarations))
            return 0;

        var logger = container.Resolve<ILogger>();
        var read = 0;

        foreach (var declaration in declarations.Values)
        {
            declaration.Register(container, modDirectory, logger);
            read++;
        }

        return read;
    }

    private abstract class Declaration
    {
        public abstract void Register(IDependencyContainer container, string modDirectory, ILogger logger);
    }

    private sealed class Declaration<TConfig>(string fileName) : Declaration where TConfig : class, new()
    {
        public override void Register(IDependencyContainer container, string modDirectory, ILogger logger)
            => container.RegisterSingleton(ModConfigReader.Read<TConfig>(modDirectory, fileName, logger));
    }
}
