using System;
using ReadyM.Api.Helpers;
using ReadyM.Api.Mapping.Policies.Event;

namespace ReadyM.Api.Mapping;

internal class NativeMappingPolicyDirectory(DataSideChannel sideChannel) : MappingPolicyDirectory(sideChannel), INativeMappingPolicyDirectory
{
    public IMappingEventPolicy<TContext> ForEventOpaque<TContext>(Type eventType)
    {
        lock (eventLock)
        {
            var key = (eventType, typeof(TContext));

            if (!eventPolicies.TryGetValue(key, out var untypedPolicy))
            {
                foreach (var factory in eventPolicyFactories)
                {
                    if (!factory.Supports(eventType, typeof(TContext)))
                        continue;

                    untypedPolicy = factory.CreatePolicy<TContext>(eventType);
                    break;
                }

                if (untypedPolicy == null)
                    throw new ArgumentException($"No event policy registered for event type {eventType}");

                eventPolicies.Add(key, untypedPolicy);
            }

            return (IMappingEventPolicy<TContext>)untypedPolicy;
        }
    }
}