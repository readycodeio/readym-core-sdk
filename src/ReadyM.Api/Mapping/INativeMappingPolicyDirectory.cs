using System;
using ReadyM.Api.Mapping.Policies.Event;

namespace ReadyM.Api.Mapping;

internal interface INativeMappingPolicyDirectory : IMappingPolicyDirectory
{
    IMappingEventPolicy<TContext> ForEventOpaque<TContext>(Type eventType);
}