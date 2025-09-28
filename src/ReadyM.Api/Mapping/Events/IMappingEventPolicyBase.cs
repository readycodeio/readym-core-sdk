using System;

namespace ReadyM.Api.Mapping.Events;

public interface IMappingEventPolicyBase
{
    Type ContextType { get; }
}