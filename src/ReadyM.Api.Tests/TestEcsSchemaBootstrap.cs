using System.Runtime.CompilerServices;
using Friflo.Engine.ECS;

namespace ReadyM.Api.Tests;

/// <summary>
/// Creates the process-wide <see cref="EntitySchema"/> once, before any test body runs.
/// <para>
/// The schema is never created implicitly, so it has to be created here. These tests load no mods and
/// need no explicitly registered component types, so scanning the loaded assemblies is enough.
/// </para>
/// </summary>
internal static class TestEcsSchemaBootstrap
{
    [ModuleInitializer]
    internal static void CreateSchema()
    {
        if (!SchemaBootstrap.IsSchemaCreated)
            SchemaBootstrap.CreateFromLoadedAssemblies();
    }
}
