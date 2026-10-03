using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.Helpers;
using ReadyM.Api.Interop;
using ReadyM.Api.Interop.Registry;

namespace ReadyM.Api.Mapping.Events;

internal class NativeMappedEventManager : MappedEventManager, ITypeRegistryCallbackBase<INativeTypeRegistry, ValueType, IGameEvent>
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ManagedBinding
    {
        public IntPtr ManagedTarget;
    }

    // NOTE: Built once from the registry; a native call finds its typed entry by the event's native id.
    private readonly Dictionary<int, NativeEventEntry> _entries = new();
    private readonly INativeTypeRegistry _nativeRegistry;
    private readonly ILogger _logger;

    public NativeMappedEventManager(
        DataSideChannel sideChannel,
        GameEventContextRegistry contexts,
        INativeTypeRegistry nativeRegistry,
        ILogger logger
    ) : base(sideChannel, contexts, logger)
    {
        _nativeRegistry = nativeRegistry;
        _logger = logger;
        nativeRegistry.Accept(this);
    }

    public ManagedBinding GetManagedBinding() => new()
    {
        ManagedTarget = GCHandle.ToIntPtr(GCHandle.Alloc(this))
    };

    public void AcceptComponent<T>(INativeTypeRegistry registry, T defaultValue = default)
        where T : struct
    {
        // Components have no event entry.
    }

    public void AcceptModComponent(INativeTypeRegistry registry, ModComponentInfo info, string typeFullName)
    {
        // Components have no event entry.
    }

    public void AcceptEvent<T>(INativeTypeRegistry registry)
        where T : struct, IGameEvent
    {
        // NOTE: The native registry has set the event's interop id by now; reading it boxes once per type, at start-up.
        if ((object)default(T) is not IInteropType interop)
            throw new InvalidOperationException($"Native game event {typeof(T).FullName} is not an interop type");

        _entries.Add(interop.GetClassId(), new NativeEventEntry<T>());
    }

    public void RegisterNativeGameEventHandler(int eventId, ClosureTrampoline1 callback)
    {
        if (TryGetEntry(eventId, out var entry))
            entry.RegisterNativeGameEventHandler(this, callback);
    }

    // NOTE: These are called from native code, so they must not throw: an unknown id is logged and never runs.
    public bool NotifyEcsIfApplicable(int eventId, IntPtr data)
        => TryGetEntry(eventId, out var entry) && entry.NotifyEcsIfApplicable(this, data);

    public bool InvokeInGameIfApplicable(int eventId, IntPtr data)
        => TryGetEntry(eventId, out var entry) && entry.InvokeInGameIfApplicable(this, data);

    public byte CanGameEventRunLocally(int eventId, IntPtr data)
        => (byte)(TryGetEntry(eventId, out var entry) ? entry.CanGameEventRunLocally(this, data) : GameEventResult.Rejected);

    private bool TryGetEntry(int eventId, out NativeEventEntry entry)
    {
        if (_entries.TryGetValue(eventId, out entry!))
            return true;

        _logger.LogError("No native game event is registered with id {EventId} (type {EventType})", eventId, _nativeRegistry.GetTypeById(eventId)?.FullName ?? "none");
        return false;
    }
}
