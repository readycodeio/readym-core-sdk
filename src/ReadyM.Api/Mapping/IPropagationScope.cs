using System;

namespace ReadyM.Api.Mapping;

internal interface IPropagationScope
{
    PropagationDirection Direction { get; }
    Type EventType { get; }
}

internal static class PropagationScopeExtensions
{
    extension(IPropagationScope scope)
    {
        public bool Equals(IPropagationScope other)
        {
            return other.Direction == scope.Direction && other.EventType == scope.EventType;
        }
    }
}