using System.Runtime.CompilerServices;
using Friflo.Engine.ECS;

namespace ReadyM.SDK.Tests;

/// <summary>
/// Creates the process-wide <see cref="EntitySchema"/> once, before any test body runs.
/// <para>
/// The schema is never created implicitly, so it has to be created here. These tests load no mods and
/// register no component types explicitly, so scanning the loaded assemblies is enough.
/// </para>
/// </summary>
internal static class TestEcsSchemaBootstrap
{
    [ModuleInitializer]
    internal static void CreateSchema()
    {
        // A module initializer runs once per assembly, before any other code in it, so this is the only
        // creator in this process. Repeated initialization is therefore left at its default: a hard failure.
        SchemaBootstrap.InitializeFromLoadedAssemblies();
    }
}
