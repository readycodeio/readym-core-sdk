using System;

namespace ReadyM.Api.Mapping;

internal interface INativeMappingPolicyDirectory : IMappingPolicyDirectory
{
    bool CanGameEventNotifyEcs(int eventId);
    bool CanGameEventNotifyEcs(int eventId, IntPtr context);

    bool CanEcsInvokeGameEvent(int eventId);
    bool CanEcsInvokeGameEvent(int eventId, IntPtr context);

    bool CanGameEventRunLocally(int eventId);
    bool CanGameEventRunLocally(int eventId, IntPtr context);
}