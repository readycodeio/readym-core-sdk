using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ReadyM.Api.Serialization;

namespace ReadyM.Api.Multiplayer.Serialization;

internal static class ReflectionHelpers
{
    public static IEnumerable<Type> GetTypesWithAttribute<T>()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var attributeAsmFullName = typeof(T).Assembly.FullName;

        return assemblies
            .Where(asm => asm.FullName == attributeAsmFullName || asm.GetReferencedAssemblies().Any(x => x.FullName == attributeAsmFullName))
            .SelectMany(asm =>
            {
                try
                {
                    return asm.ExportedTypes;
                }
                catch (ReflectionTypeLoadException ex)
                {
                    return ex.Types.Where(t => t != null);
                }
            })
            .Where(t => t.GetCustomAttribute<RegisterJsonConverterAttribute>(inherit: false) != null);
    }
}