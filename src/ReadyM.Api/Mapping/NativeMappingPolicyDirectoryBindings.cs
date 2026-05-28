using System;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace ReadyM.Api.Mapping;

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
public static class NativeMappingPolicyDirectoryBindings
{
    public delegate byte CanGameEventNotifyEcsDelegate(IntPtr objectPtr, int eventId, IntPtr context);

    public static byte CanGameEventNotifyEcs(IntPtr objectPtr, int eventId, IntPtr context)
    {
        var manager = (NativeMappingPolicyDirectory)GCHandle.FromIntPtr(objectPtr).Target!;
        return manager.CanGameEventNotifyEcs(eventId, context) ? (byte)1 : (byte)0;
    }

    public delegate byte CanGameEventNotifyEcsNoCtxDelegate(IntPtr objectPtr, int eventId);

    public static byte CanGameEventNotifyEcsNoCtx(IntPtr objectPtr, int eventId)
    {
        var manager = (NativeMappingPolicyDirectory)GCHandle.FromIntPtr(objectPtr).Target!;
        return manager.CanGameEventNotifyEcs(eventId) ? (byte)1 : (byte)0;
    }

    public delegate byte CanEcsInvokeGameEventDelegate(IntPtr objectPtr, int eventId, IntPtr context);

    public static byte CanEcsInvokeGameEvent(IntPtr objectPtr, int eventId, IntPtr context)
    {
        var manager = (NativeMappingPolicyDirectory)GCHandle.FromIntPtr(objectPtr).Target!;
        return manager.CanEcsInvokeGameEvent(eventId, context) ? (byte)1 : (byte)0;
    }

    public delegate byte CanEcsInvokeGameEventNoCtxDelegate(IntPtr objectPtr, int eventId);

    public static byte CanEcsInvokeGameEventNoCtx(IntPtr objectPtr, int eventId)
    {
        var manager = (NativeMappingPolicyDirectory)GCHandle.FromIntPtr(objectPtr).Target!;
        return manager.CanEcsInvokeGameEvent(eventId) ? (byte)1 : (byte)0;
    }

    public delegate byte CanGameEventRunLocallyDelegate(IntPtr objectPtr, int eventId, IntPtr context);

    public static byte CanGameEventRunLocally(IntPtr objectPtr, int eventId, IntPtr context)
    {
        var manager = (NativeMappingPolicyDirectory)GCHandle.FromIntPtr(objectPtr).Target!;
        return manager.CanGameEventRunLocally(eventId, context) ? (byte)1 : (byte)0;
    }

    public delegate byte CanGameEventRunLocallyNoCtxDelegate(IntPtr objectPtr, int eventId);

    public static byte CanGameEventRunLocallyNoCtx(IntPtr objectPtr, int eventId)
    {
        var manager = (NativeMappingPolicyDirectory)GCHandle.FromIntPtr(objectPtr).Target!;
        return manager.CanGameEventRunLocally(eventId) ? (byte)1 : (byte)0;
    }
}