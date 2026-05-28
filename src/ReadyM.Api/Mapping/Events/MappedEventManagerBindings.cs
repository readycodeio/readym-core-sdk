using System;
using System.Runtime.InteropServices;
using ReadyM.Api.Interop;

namespace ReadyM.Api.Mapping.Events;

internal static class NativeMappedEventManagerBindings
{
    public delegate void RegisterNativeGameEventHandlerDelegate(IntPtr managerPtr, int eventId, ClosureTrampoline1 callback);

    public static void RegisterNativeGameEventHandler(IntPtr managerPtr, int eventId, ClosureTrampoline1 callback)
    {
        var manager = (NativeMappedEventManager)GCHandle.FromIntPtr(managerPtr).Target!;
        manager.RegisterNativeGameEventHandler(eventId, callback);
    }

    public delegate void RegisterNativeEcsEventHandlerDelegate(IntPtr managerPtr, int eventId, ClosureTrampoline1 callback);

    public static void RegisterNativeEcsEventHandler(IntPtr managerPtr, int eventId, ClosureTrampoline1 callback)
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
    
    public delegate byte NotifyEcsIfApplicableNoCtxDelegate(IntPtr managerPtr, int eventId, IntPtr data);

    public static byte NotifyEcsIfApplicableNoCtx(IntPtr managerPtr, int eventId, IntPtr data)
    {
        var manager = (NativeMappedEventManager)GCHandle.FromIntPtr(managerPtr).Target!;
        return manager.NotifyEcsIfApplicable(eventId, data) ? (byte)1 : (byte)0;
    }

    public delegate byte InvokeInGameIfApplicableDelegate(IntPtr managerPtr, int eventId, IntPtr data, IntPtr context);

    public static byte InvokeInGameIfApplicable(IntPtr managerPtr, int eventId, IntPtr data, IntPtr context)
    {
        var manager = (NativeMappedEventManager)GCHandle.FromIntPtr(managerPtr).Target!;
        return manager.InvokeInGameIfApplicable(eventId, data, context) ? (byte)1 : (byte)0;
    }
    
    public delegate byte InvokeInGameIfApplicableNoCtxDelegate(IntPtr managerPtr, int eventId, IntPtr data);
    
    public static byte InvokeInGameIfApplicableNoCtx(IntPtr managerPtr, int eventId, IntPtr data)
    {
        var manager = (NativeMappedEventManager)GCHandle.FromIntPtr(managerPtr).Target!;
        return manager.InvokeInGameIfApplicable(eventId, data) ? (byte)1 : (byte)0;
    }
    
    public delegate void InvokeInGameAndNotifyEcsDelegate(IntPtr managerPtr, int eventId, IntPtr data, IntPtr context);
    
    public static void InvokeInGameAndNotifyEcs(IntPtr managerPtr, int eventId, IntPtr data, IntPtr context)
    {
        var manager = (NativeMappedEventManager)GCHandle.FromIntPtr(managerPtr).Target!;
        manager.InvokeInGameAndNotifyEcs(eventId, data, context);
    }
    
    public delegate void InvokeInGameAndNotifyEcsNoCtxDelegate(IntPtr managerPtr, int eventId, IntPtr data);
    
    public static void InvokeInGameAndNotifyEcsNoCtx(IntPtr managerPtr, int eventId, IntPtr data)
    {
        var manager = (NativeMappedEventManager)GCHandle.FromIntPtr(managerPtr).Target!;
        manager.InvokeInGameAndNotifyEcs(eventId, data);
    }
}