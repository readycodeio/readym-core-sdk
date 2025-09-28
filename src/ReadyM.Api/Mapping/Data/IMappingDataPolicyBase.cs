using System;

namespace ReadyM.Api.Mapping.Data;

public interface IMappingDataPolicyBase
{
    Type ContextType { get; }
}