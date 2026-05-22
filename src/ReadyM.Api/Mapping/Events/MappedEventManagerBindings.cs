using System;
using System.Runtime.InteropServices;

namespace ReadyM.Api.Mapping.Events;

public static class NativeMappedEventManagerBindings
{
    public delegate void RegisterNativeGameEventHandlerDelegate(IntPtr managerPtr, int eventId, NativeEventCallback callback);

    public static void RegisterNativeGameEventHandler(IntPtr managerPtr, int eventId, NativeEventCallback callback)
    {
        var manager = (NativeMappedEventManager)GCHandle.FromIntPtr(managerPtr).Target!;
        manager.RegisterNativeGameEventHandler(eventId, callback);
    }

    public delegate void RegisterNativeEcsEventHandlerDelegate(IntPtr managerPtr, int eventId, NativeEventCallback callback);

    public static void RegisterNativeEcsEventHandler(IntPtr managerPtr, int eventId, NativeEventCallback callback)
    {
        var manager = (NativeMappedEventManager)GCHandle.FromIntPtr(managerPtr).Target!;
        manager.RegisterNativeEcsEventHandler(eventId, callback);
    }

    public delegate byte NotifyEcsIfApplicableDelegate(IntPtr managerPtr, int eventId, IntPtr data, IntPtr context);

    public static byte NotifyEcsIfApplicable(IntPtr managerPtr, int eventId, IntPtr data, IntPtr context)
    {
        var manager = (NativeMappedEventManager)GCHandle.FromIntPtr(managerPtr).Target!;
        return manager.NotifyEcsIfApplicable(eventId, data, context) ? (byte)1 : (byte)0;
    }
}