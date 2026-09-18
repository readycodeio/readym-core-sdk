using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace ReadyM.SDK.Archetypes;

/// Applies the registrations generated for the shapes an assembly declares: what each mixin adds to
/// an archetype, and which component holds an index. Called it for every assembly a host loads, before
/// anything creates an entity.
[EditorBrowsable(EditorBrowsableState.Never)]
public static class GeneratedShapes
{
    /// The type a compilation gets when it declares shapes that need registering.
    private const string EntryPoint = "ReadyM.SDK.Generated.ShapeRegistrations";

    private const string Method = "RegisterAll";

    public static int ApplyAll(IEnumerable<Assembly> assemblies, Action<Assembly>? onApplied = null, Action<Assembly, Exception>? onFailed = null)
    {
        var applied = 0;

        foreach (var assembly in assemblies.Distinct())
        {
            try
            {
                if (!Apply(assembly))
                    continue;

                applied++;
                onApplied?.Invoke(assembly);
            }
            catch (Exception ex)
            {
                onFailed?.Invoke(assembly, ex);
            }
        }

        return applied;
    }

    public static bool Apply(Assembly assembly)
    {
        var entry = assembly.GetType(EntryPoint, throwOnError: false);

        if (entry?.GetMethod(Method, BindingFlags.Public | BindingFlags.Static) is not { } register)
            return false;

        register.Invoke(null, null);
        return true;
    }
}
