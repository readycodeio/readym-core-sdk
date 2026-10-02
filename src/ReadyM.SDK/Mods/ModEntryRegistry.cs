using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using ReadyM.Api.DI;

namespace ReadyM.SDK.Mods;

/// Holds the entry class each mod assembly declares, until there is a container to build it from.
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ModEntryRegistry
{
    private static readonly ConcurrentDictionary<Assembly, Declaration> Declared = new();

    /// Called by generated code for the one [ModEntry] class an assembly declares.
    public static void Declare<TEntry>() where TEntry : class, IModEntry
        => Declared[typeof(TEntry).Assembly] = new Declaration<TEntry>();

    /// Whether this assembly is a mod written against the SDK rather than against a base class (old SDK v0).
    public static bool Declares(Assembly assembly) 
        => Declared.ContainsKey(assembly);

    /// Builds the entry class out of the container and runs what it declared, if anything.
    public static IModEntry? Start(Assembly assembly, IDependencyContainer container, string modDirectory)
    {
        if (!Declared.TryGetValue(assembly, out var declaration))
            return null;

        // Ahead of the entry point, so a config this mod declares reaches its constructor.
        ModConfigRegistry.RegisterAll(assembly, container, modDirectory);

        var entry = declaration.Build(container);

        entry.SetModDirectory(modDirectory);
        entry.Start();

        return entry;
    }

    /// Starts the entry point of every assembly given, in the order given, and says how many ran.
    public static int StartAll(
        IEnumerable<(Assembly Assembly, string Directory)> loaded,
        IDependencyContainer container,
        Action<Assembly>? onStarted = null,
        Action<Assembly, Exception>? onFailed = null)
    {
        var started = 0;

        foreach (var (assembly, directory) in loaded)
        {
            try
            {
                if (Start(assembly, container, directory) is null)
                    continue;

                started++;
                onStarted?.Invoke(assembly);
            }
            catch (Exception ex)
            {
                onFailed?.Invoke(assembly, ex);
            }
        }

        return started;
    }

    private abstract class Declaration
    {
        public abstract IModEntry Build(IDependencyContainer container);
    }

    private sealed class Declaration<TEntry> : Declaration where TEntry : class, IModEntry
    {
        public override IModEntry Build(IDependencyContainer container)
        {
            container.RegisterSingleton<TEntry>();

            return container.Resolve<TEntry>();
        }
    }
}
