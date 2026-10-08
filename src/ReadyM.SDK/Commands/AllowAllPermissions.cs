using System;

namespace ReadyM.SDK.Commands;

public sealed class AllowAllPermissions : ICommandPermissions
{
    public bool IsAllowed(Guid? caller, string permission) => true;
}
