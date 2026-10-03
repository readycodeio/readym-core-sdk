using System;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace ReadyM.Api.Mapping;

/// <exclude />
[UsedImplicitly(ImplicitUseTargetFlags.Members)]
public static class NativeMappingPolicyDirectoryBindings
{
    public delegate byte ShouldGameCopyToEcsForEntityDelegate(IntPtr objectPtr, int componentId, int entityId);

    public static byte ShouldGameCopyToEcsForEntity(IntPtr objectPtr, int componentId, int entityId)
    {
        var manager = (NativeMappingPolicyDirectory)GCHandle.FromIntPtr(objectPtr).Target!;
        return manager.ShouldGameCopyToEcsForEntity(componentId, entityId) ? (byte)1 : (byte)0;
    }

    public delegate byte ShouldEcsCopyToGameForEntityDelegate(IntPtr objectPtr, int componentId, int entityId);

    public static byte ShouldEcsCopyToGameForEntity(IntPtr objectPtr, int componentId, int entityId)
    {
        var manager = (NativeMappingPolicyDirectory)GCHandle.FromIntPtr(objectPtr).Target!;
        return manager.ShouldEcsCopyToGameForEntity(componentId, entityId) ? (byte)1 : (byte)0;
    }

    public delegate byte CanSetFromApiForEntityDelegate(IntPtr objectPtr, int componentId, int entityId);

    public static byte CanSetFromApiForEntity(IntPtr objectPtr, int componentId, int entityId)
    {
        var manager = (NativeMappingPolicyDirectory)GCHandle.FromIntPtr(objectPtr).Target!;
        return manager.CanSetFromApiForEntity(componentId, entityId) ? (byte)1 : (byte)0;
    }

    public delegate byte CanGameSetLocallyForEntityDelegate(IntPtr objectPtr, int componentId, int entityId);

    public static byte CanGameSetLocallyForEntity(IntPtr objectPtr, int componentId, int entityId)
    {
        var manager = (NativeMappingPolicyDirectory)GCHandle.FromIntPtr(objectPtr).Target!;
        return manager.CanGameSetLocallyForEntity(componentId, entityId) ? (byte)1 : (byte)0;
    }

    public delegate byte ShouldGameCopyToEcsDelegate(IntPtr objectPtr, int componentId, IntPtr context);

    public static byte ShouldGameCopyToEcs(IntPtr objectPtr, int componentId, IntPtr context)
    {
        var manager = (NativeMappingPolicyDirectory)GCHandle.FromIntPtr(objectPtr).Target!;
        return manager.ShouldGameCopyToEcs(componentId, context) ? (byte)1 : (byte)0;
    }

    public delegate byte ShouldEcsCopyToGameDelegate(IntPtr objectPtr, int componentId, IntPtr context);

    public static byte ShouldEcsCopyToGame(IntPtr objectPtr, int componentId, IntPtr context)
    {
        var manager = (NativeMappingPolicyDirectory)GCHandle.FromIntPtr(objectPtr).Target!;
        return manager.ShouldEcsCopyToGame(componentId, context) ? (byte)1 : (byte)0;
    }

    public delegate byte CanSetFromApiDelegate(IntPtr objectPtr, int componentId, IntPtr context);

    public static byte CanSetFromApi(IntPtr objectPtr, int componentId, IntPtr context)
    {
        var manager = (NativeMappingPolicyDirectory)GCHandle.FromIntPtr(objectPtr).Target!;
        return manager.CanSetFromApi(componentId, context) ? (byte)1 : (byte)0;
    }

    public delegate byte CanGameSetLocallyDelegate(IntPtr objectPtr, int componentId, IntPtr context);

    public static byte CanGameSetLocally(IntPtr objectPtr, int componentId, IntPtr context)
    {
        var manager = (NativeMappingPolicyDirectory)GCHandle.FromIntPtr(objectPtr).Target!;
        return manager.CanGameSetLocally(componentId, context) ? (byte)1 : (byte)0;
    }
}