using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.ConflictResolution;
using ReadyM.Relay.Server.Sdk.Ecs;

namespace ReadyM.Relay.Server.Sdk.ConflictResolution;

public class ChangeTrackingStore(EcsApi ecs) : IChangeTrackingStore
{
    public ref T GetChangeComponent<T>(int id)
        where T : struct, IComponent
    {
        if (!ecs.HasComponent<T>(id))
        {
            // TEMPORARY DIAGNOSTIC, REMOVE BEFORE MERGE. A mod's ILogger goes to the plugin host's own
            // console sink, which the test output never sees, so this goes to a file instead.
            Trace($"MISSING {typeof(T)} for entity {id}");
            throw new InvalidOperationException($"Missing change {typeof(T)} for entity {id}");
        }

        // TEMPORARY DIAGNOSTIC, REMOVE BEFORE MERGE.
        Trace($"FOUND {typeof(T)} for entity {id}");
        return ref ecs.GetComponentRef<T>(id);
    }

    /// <summary>TEMPORARY DIAGNOSTIC, REMOVE BEFORE MERGE. Set READYM_CHANGE_TRACE to a path to enable.</summary>
    internal static void Trace(string message)
    {
        var path = Environment.GetEnvironmentVariable("READYM_CHANGE_TRACE");
        if (string.IsNullOrEmpty(path))
            return;

        try
        {
            System.IO.File.AppendAllText(path, message + Environment.NewLine);
        }
        catch
        {
            // Diagnostics must never change what the thing being diagnosed does.
        }
    }

    internal void ForceAOT<T>()
        where T : struct, IComponent
        => GetChangeComponent<T>(0);
}
