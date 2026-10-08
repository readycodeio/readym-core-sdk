using System;

namespace ReadyM.SDK.Commands;

public interface ICommandPermissions
{
    bool IsAllowed(Guid? caller, string permission);
}
