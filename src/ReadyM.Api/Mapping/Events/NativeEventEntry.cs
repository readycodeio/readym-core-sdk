using System;
using System.Runtime.CompilerServices;
using ReadyM.Api.Interop;

namespace ReadyM.Api.Mapping.Events;

/// <summary>Reads one native event type from a pointer and calls the manager with it; looked up by event id.</summary>
internal abstract class NativeEventEntry
{
    public abstract bool NotifyEcsIfApplicable(IMappedEventManager manager, IntPtr data);
    public abstract bool InvokeInGameIfApplicable(IMappedEventManager manager, IntPtr data);
    public abstract GameEventResult CanGameEventRunLocally(IMappedEventManager manager, IntPtr data);
    public abstract void RegisterNativeGameEventHandler(IMappedEventManager manager, ClosureTrampoline1 callback);
}

internal sealed unsafe class NativeEventEntry<TEvent> : NativeEventEntry
    where TEvent : struct, IGameEvent
{
    public override bool NotifyEcsIfApplicable(IMappedEventManager manager, IntPtr data)
        => manager.NotifyEcsIfApplicable(Unsafe.Read<TEvent>((void*)data));

    public override bool InvokeInGameIfApplicable(IMappedEventManager manager, IntPtr data)
        => manager.InvokeInGameIfApplicable(Unsafe.Read<TEvent>((void*)data));

    public override GameEventResult CanGameEventRunLocally(IMappedEventManager manager, IntPtr data)
        => manager.CanGameEventRunLocally(Unsafe.Read<TEvent>((void*)data));

    public override void RegisterNativeGameEventHandler(IMappedEventManager manager, ClosureTrampoline1 callback)
        => manager.RegisterGameEventHandler<TEvent, ClosureTrampoline1>(InvokeNative, callback);

    private static void InvokeNative(TEvent ev, ClosureTrampoline1 callback)
        => callback.Invoke((IntPtr)Unsafe.AsPointer(ref ev));
}
