using System;

namespace ReadyM.Api.Multiplayer.Mapping.Policies.Event;

public interface IMappingEventPolicyBase
{
    Type ContextType { get; }
}